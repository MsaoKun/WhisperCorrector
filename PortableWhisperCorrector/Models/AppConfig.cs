using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace PortableWhisperCorrector.Models
{
    public class AppConfig
    {
        [JsonPropertyName("whisper")]
        public WhisperConfig Whisper { get; set; } = new();

        [JsonPropertyName("aiCorrection")]
        public AiCorrectionConfig AiCorrection { get; set; } = new();

        [JsonPropertyName("gemini")]
        public GeminiConfig Gemini { get; set; } = new();

        [JsonPropertyName("app")]
        public AppGeneralConfig App { get; set; } = new();
    }

    public class WhisperConfig
    {
        [JsonPropertyName("selectedModelId")]
        public string SelectedModelId { get; set; } = "large-v3-turbo";

        [JsonPropertyName("contextPrompt")]
        public string ContextPrompt { get; set; } = "";

        [JsonPropertyName("enableVad")]
        public bool EnableVad { get; set; } = true;

        [JsonPropertyName("threads")]
        public int Threads { get; set; } = 0; // 0 = 自動

        [JsonPropertyName("backend")]
        public string Backend { get; set; } = "auto"; // "auto", "vulkan", "cpu"

        [JsonPropertyName("gpuDevice")]
        public int GpuDevice { get; set; } = 0;

        [JsonPropertyName("flashAttn")]
        public bool FlashAttn { get; set; } = false; // 古いGPU互換性のため初期値false推奨

        [JsonPropertyName("language")]
        public string Language { get; set; } = "ja";
    }

    public class AiCorrectionConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "proofread"; // "proofread" (最小補正), "format" (文書整形), "diarize" (話者ラベリング)

        [JsonPropertyName("model")]
        public string Model { get; set; } = "gemini-2.5-flash";

        [JsonPropertyName("chunkChars")]
        public int ChunkChars { get; set; } = 3000;

        [JsonPropertyName("delaySeconds")]
        public int DelaySeconds { get; set; } = 2;
    }

    public class GeminiConfig
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = "";
    }

    public class AppGeneralConfig
    {
        [JsonPropertyName("customOutputDir")]
        public string CustomOutputDir { get; set; } = "";
    }
}
