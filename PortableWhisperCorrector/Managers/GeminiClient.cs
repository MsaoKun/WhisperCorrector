using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PortableWhisperCorrector.Managers
{
    public class GeminiClient
    {
        private readonly HttpClient _httpClient;

        public GeminiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(3);
        }

        public async Task<(string text, string tokenLog)> CorrectTextAsync(string apiKey, string model, string textChunk, string mode, string contextPrompt = "")
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            string contextSection = "";
            if (!string.IsNullOrWhiteSpace(contextPrompt))
            {
                contextSection = $@"
【最重要：会議の文脈および固有名詞】
以下の文脈情報・固有名詞・キーワードが指定されています。
テキスト中にこれらに関連する発言や誤変換と思われる箇所があれば、必ず以下の正しい固有名詞・表記へ補正・統一してください。
>>> {contextPrompt} <<<
";
            }

            string prompt = "";
            if (mode == "diarize")
            {
                prompt = $@"あなたの役割は、会議や対話の文字起こしテキストの話者分離および校正を行う専門アシスタントです。
以下のテキストはWhisperで自動文字起こしされた日本語です。
文脈、口調、発言の交代を注意深く分析し、話者ごとに【話者A】【話者B】（文脈から特定できる場合は【山田課長】など）のラベルを付与して読みやすく整理してください。
{contextSection}
厳守ルール：
- 話者の交代を検知し、発言ごとに「【話者A】発言内容」のように記述する
- 誤字脱字、明らかな音声認識ミス、助詞の抜けを修正する
- 音声認識特有の不自然なリピート（「ありがとうございました」等の連続ループ）は完全に削除する
- 内容を要約・省略・捏造しない
- 解説や挨拶などの余計なコメントは出力せず、整形後のテキスト本文のみを出力する

本文：
---
{textChunk}
---";
            }
            else if (mode == "format")
            {
                prompt = $@"あなたの文字起こしテキストの校正・整形者です。
以下のテキストはWhisperで自動文字起こしした日本語です。内容を要約せず、意味を変えず、読みやすいWord文書にしやすい形へ整えてください。
{contextSection}
行うこと：
- 誤字脱字、明らかな音声認識ミスを修正する
- 音声認識特有の不自然なリピート（「ありがとうございました」等の連続ループ）は完全に削除する
- 句読点と改行を整え、長すぎる文を読みやすく分ける
- 話題の切り替わりが明確な場合のみ、適宜Markdown見出し（## 見出し）を付ける
- 明確な列挙部分のみ箇条書きにする
- Word文書に変換しやすいMarkdown形式で出力する

禁止事項：
- 要約しない
- 内容を省略しない
- 内容を追加しない
- 発言の意味を変えない
- 原文にない結論、TODO、議事録項目を作らない
- 解説やコメントを出力しない
- 出力量は原文と同程度に抑える

本文：
---
{textChunk}
---";
            }
            else // "proofread" (最小補正)
            {
                prompt = $@"あなたの文字起こしテキストの校正者です。
以下のテキストはWhisperで自動文字起こしした日本語です。誤字脱字、明らかな音声認識ミス、句読点、改行のみを修正してください。
これはリライトではなく、校正作業です。元の語彙と内容を可能な限り維持してください。
{contextSection}
厳守事項：
- 要約しない
- 内容を省略しない
- 内容を追加しない
- 表現を自然に言い換えすぎない
- 音声認識特有の不自然なリピート（「ありがとうございました」等の連続ループ）は完全に削除する
- 不明瞭な箇所は「[不明瞭]」とする
- 解説やコメントは出力せず、修正後の本文のみを出力する
- 出力量は原文と同程度に抑える

本文：
---
{textChunk}
---";
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var resultObj = JsonSerializer.Deserialize<GeminiResponse>(responseString);

            string tokenLog = "";
            if (resultObj?.UsageMetadata != null)
            {
                tokenLog = $"[Token] Input: {resultObj.UsageMetadata.PromptTokenCount}, Output: {resultObj.UsageMetadata.CandidatesTokenCount}, Total: {resultObj.UsageMetadata.TotalTokenCount}";
            }

            if (resultObj?.Candidates != null && resultObj.Candidates.Count > 0)
            {
                return (resultObj.Candidates[0].Content?.Parts?[0].Text?.Trim() ?? textChunk, tokenLog);
            }

            return (textChunk, tokenLog);
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate> Candidates { get; set; }

            [JsonPropertyName("usageMetadata")]
            public UsageMetadata UsageMetadata { get; set; }
        }

        private class UsageMetadata
        {
            [JsonPropertyName("promptTokenCount")]
            public int PromptTokenCount { get; set; }

            [JsonPropertyName("candidatesTokenCount")]
            public int CandidatesTokenCount { get; set; }

            [JsonPropertyName("totalTokenCount")]
            public int TotalTokenCount { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part> Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }
    }
}
