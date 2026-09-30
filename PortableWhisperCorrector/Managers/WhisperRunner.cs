using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace PortableWhisperCorrector.Managers
{
    public class WhisperRunOptions
    {
        public string ExePath { get; set; }
        public string ModelPath { get; set; }
        public string AudioPath { get; set; }
        public string OutputDir { get; set; }
        public int Threads { get; set; } = 0;
        public bool DisableGpu { get; set; } = false;
        public int GpuDevice { get; set; } = 0;
        public bool FlashAttn { get; set; } = false;
        public string ContextPrompt { get; set; } = "";
        public bool EnableVad { get; set; } = true;
        public string Language { get; set; } = "ja";
    }

    public class WhisperRunner
    {
        private static readonly Regex ProgressRegex = new Regex(@"whisper_print_progress:\s*progress\s*=\s*(\d+)%", RegexOptions.Compiled);

        public async Task<string> RunInferenceAsync(WhisperRunOptions options, Action<string> onLog, Action<int> onProgress)
        {
            return await Task.Run(async () =>
            {
                // まず指定された設定で実行
                var (success, result, exitCode) = await ExecuteProcessAsync(options, onLog, onProgress);

                // もしGPUモードで異常終了し、CPUモードではなかった場合、自動フォールバックを試行
                if (!success && !options.DisableGpu)
                {
                    onLog?.Invoke($"[自動フォールバック] GPU処理が異常終了しました (エラーコード: {exitCode})。安全なCPUモード(-ng)で自動再試行します...");
                    var fallbackOptions = new WhisperRunOptions
                    {
                        ExePath = options.ExePath,
                        ModelPath = options.ModelPath,
                        AudioPath = options.AudioPath,
                        OutputDir = options.OutputDir,
                        Threads = options.Threads,
                        DisableGpu = true, // GPUを無効化してCPU実行
                        GpuDevice = 0,
                        FlashAttn = false,
                        ContextPrompt = options.ContextPrompt,
                        EnableVad = options.EnableVad,
                        Language = options.Language
                    };

                    var fallbackResult = await ExecuteProcessAsync(fallbackOptions, onLog, onProgress);
                    if (fallbackResult.success)
                    {
                        onLog?.Invoke("[自動フォールバック] CPUモードでの文字起こしが正常に完了しました！");
                        return CleanRepetitions(fallbackResult.result);
                    }
                    else
                    {
                        throw new Exception($"CPUフォールバック処理も失敗しました。エラーコード: {fallbackResult.exitCode}");
                    }
                }

                if (!success)
                {
                    throw new Exception($"Whisperの実行に失敗しました。エラーコード: {exitCode}");
                }

                return CleanRepetitions(result);
            });
        }

        private async Task<(bool success, string result, int exitCode)> ExecuteProcessAsync(WhisperRunOptions options, Action<string> onLog, Action<int> onProgress)
        {
            return await Task.Run(() =>
            {
                string baseName = Path.Combine(options.OutputDir, "whisper_raw");
                string outTxt = baseName + ".txt";

                var psi = new ProcessStartInfo
                {
                    FileName = options.ExePath,
                    WorkingDirectory = Path.GetDirectoryName(options.ExePath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var args = new StringBuilder();
                args.Append($"-m \"{options.ModelPath}\" ");
                args.Append($"-f \"{options.AudioPath}\" ");
                args.Append($"-otxt -osrt -of \"{baseName}\" ");
                args.Append($"-l {options.Language} ");
                args.Append("--print-progress ");

                if (options.Threads > 0)
                {
                    args.Append($"-t {options.Threads} ");
                }

                if (options.DisableGpu)
                {
                    args.Append("-ng ");
                }
                else
                {
                    args.Append($"-dev {options.GpuDevice} ");
                    if (options.FlashAttn)
                    {
                        args.Append("-fa ");
                    }
                    else
                    {
                        args.Append("-nfa ");
                    }
                }

                // 無音カット＆非音声抑制（リピート幻覚対策）
                if (options.EnableVad)
                {
                    // 外部VADモデルが存在する場合のみ --vad と -vm を指定
                    string vadModelPath = Path.Combine(Path.GetDirectoryName(options.ModelPath), "ggml-silero-vad.bin");
                    if (File.Exists(vadModelPath))
                    {
                        args.Append($"--vad -vm \"{vadModelPath}\" ");
                    }
                    args.Append("-sns -nth 0.6 ");
                }

                // 文脈プロンプト（固有名詞・会議名等）
                if (!string.IsNullOrWhiteSpace(options.ContextPrompt))
                {
                    // 改行やダブルクォーテーションをエスケープして1行で渡す
                    string sanitizedPrompt = options.ContextPrompt.Replace("\"", "'").Replace("\r", " ").Replace("\n", " ").Trim();
                    args.Append($"--prompt \"{sanitizedPrompt}\" ");
                }

                psi.Arguments = args.ToString();

                using var process = new Process { StartInfo = psi };

                process.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    var match = ProgressRegex.Match(e.Data);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int prog))
                    {
                        onProgress?.Invoke(prog);
                    }
                    else
                    {
                        onLog?.Invoke(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    var match = ProgressRegex.Match(e.Data);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int prog))
                    {
                        onProgress?.Invoke(prog);
                    }
                    else
                    {
                        onLog?.Invoke(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                int exitCode = process.ExitCode;
                bool success = exitCode == 0;

                string text = "";
                if (File.Exists(outTxt))
                {
                    text = File.ReadAllText(outTxt, Encoding.UTF8);
                    // タイムスタンプ行の除去
                    text = Regex.Replace(text, @"\[\d{2}:\d{2}:\d{2}\.\d{3} --> \d{2}:\d{2}:\d{2}\.\d{3}\]\s*", "");
                    text = string.Join("\n", text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                }

                return (success, text.Trim(), exitCode);
            });
        }

        /// <summary>
        /// Whisper特有の同一フレーズ無限ループ（「ありがとうございました」等の幻覚）を抑制するフィルター
        /// </summary>
        private static string CleanRepetitions(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // 1. 同一フレーズ（2文字〜30文字）が3回以上連続して繰り返されている場合に1回に置換
            // 例: (ありがとうございました){3,} -> ありがとうございました
            string pattern = @"(.{2,30}?)\1{2,}";
            string cleaned = Regex.Replace(input, pattern, "$1");

            // 2. 代表的な幻覚フレーズの重複をさらに安全にクリーンアップ
            string[] knownHallucinations = new[] { "ありがとうございました", "ご視聴ありがとうございました", "チャンネル登録よろしくお願いします", "はい", "そうですね" };
            foreach (var h in knownHallucinations)
            {
                string repPattern = $@"({Regex.Escape(h)}[\s、。！？]*){{2,}}";
                cleaned = Regex.Replace(cleaned, repPattern, h + "。");
            }

            return cleaned;
        }
    }
}
