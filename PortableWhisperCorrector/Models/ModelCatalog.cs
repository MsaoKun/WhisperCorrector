using System.Collections.Generic;
using System.IO;

namespace PortableWhisperCorrector.Models
{
    public class WhisperModelInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string FileName { get; set; }
        public string Category { get; set; }
        public long EstimatedSizeBytes { get; set; }
        public string SizeDescription { get; set; }
        public string MemoryRequirement { get; set; }
        public string DownloadUrl { get; set; }

        public override string ToString() => DisplayName;
    }

    public static class ModelCatalog
    {
        public static readonly List<WhisperModelInfo> AvailableModels = new()
        {
            // 標準モデル (多言語・日本語対応)
            new WhisperModelInfo
            {
                Id = "large-v3-turbo",
                DisplayName = "Large-v3-turbo (高速・高精度 / 約1.5GB)",
                FileName = "ggml-large-v3-turbo.bin",
                Category = "多言語 (標準)",
                EstimatedSizeBytes = 1624555275,
                SizeDescription = "約 1.5 GB",
                MemoryRequirement = "VRAM 4GB以上 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo.bin"
            },
            new WhisperModelInfo
            {
                Id = "large-v3",
                DisplayName = "Large-v3 (最高精度 / 約2.9GB)",
                FileName = "ggml-large-v3.bin",
                Category = "多言語 (標準)",
                EstimatedSizeBytes = 3095033481,
                SizeDescription = "約 2.9 GB",
                MemoryRequirement = "VRAM 6GB以上 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin"
            },
            new WhisperModelInfo
            {
                Id = "medium",
                DisplayName = "Medium (標準 / 約1.5GB)",
                FileName = "ggml-medium.bin",
                Category = "多言語 (標準)",
                EstimatedSizeBytes = 1533763059,
                SizeDescription = "約 1.5 GB",
                MemoryRequirement = "VRAM 4GB以上 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin"
            },
            new WhisperModelInfo
            {
                Id = "small",
                DisplayName = "Small (軽量 / 約460MB)",
                FileName = "ggml-small.bin",
                Category = "多言語 (標準)",
                EstimatedSizeBytes = 487848419,
                SizeDescription = "約 466 MB",
                MemoryRequirement = "VRAM 2GB以上 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"
            },
            new WhisperModelInfo
            {
                Id = "base",
                DisplayName = "Base (超軽量 / 約140MB)",
                FileName = "ggml-base.bin",
                Category = "多言語 (標準)",
                EstimatedSizeBytes = 147964211,
                SizeDescription = "約 142 MB",
                MemoryRequirement = "VRAM 1GB以上 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin"
            },
            new WhisperModelInfo
            {
                Id = "tiny",
                DisplayName = "Tiny (最軽量 / 約75MB)",
                FileName = "ggml-tiny.bin",
                Category = "多言語 (標準)",
                EstimatedSizeBytes = 77691713,
                SizeDescription = "約 75 MB",
                MemoryRequirement = "VRAM 500MB以上 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin"
            },

            // 量子化・低メモリ版 (VRAMが少ない環境向け)
            new WhisperModelInfo
            {
                Id = "large-v3-turbo-q5_0",
                DisplayName = "Large-v3-turbo [Q5_0] (省メモリ / 約550MB)",
                FileName = "ggml-large-v3-turbo-q5_0.bin",
                Category = "量子化・低メモリ",
                EstimatedSizeBytes = 573570048,
                SizeDescription = "約 547 MB",
                MemoryRequirement = "VRAM 2GB環境で高精度動作",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo-q5_0.bin"
            },
            new WhisperModelInfo
            {
                Id = "large-v3-turbo-q8_0",
                DisplayName = "Large-v3-turbo [Q8_0] (高精度省メモリ / 約830MB)",
                FileName = "ggml-large-v3-turbo-q8_0.bin",
                Category = "量子化・低メモリ",
                EstimatedSizeBytes = 874512384,
                SizeDescription = "約 834 MB",
                MemoryRequirement = "VRAM 2GB〜3GB 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo-q8_0.bin"
            },
            new WhisperModelInfo
            {
                Id = "large-v3-q5_0",
                DisplayName = "Large-v3 [Q5_0] (省メモリ / 約1.1GB)",
                FileName = "ggml-large-v3-q5_0.bin",
                Category = "量子化・低メモリ",
                EstimatedSizeBytes = 1181116416,
                SizeDescription = "約 1.1 GB",
                MemoryRequirement = "VRAM 3GB以上 推奨",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-q5_0.bin"
            },
            new WhisperModelInfo
            {
                Id = "medium-q5_0",
                DisplayName = "Medium [Q5_0] (省メモリ / 約510MB)",
                FileName = "ggml-medium-q5_0.bin",
                Category = "量子化・低メモリ",
                EstimatedSizeBytes = 538968064,
                SizeDescription = "約 514 MB",
                MemoryRequirement = "VRAM 2GB環境向け",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium-q5_0.bin"
            },
            new WhisperModelInfo
            {
                Id = "small-q5_1",
                DisplayName = "Small [Q5_1] (省メモリ / 約180MB)",
                FileName = "ggml-small-q5_1.bin",
                Category = "量子化・低メモリ",
                EstimatedSizeBytes = 190840832,
                SizeDescription = "約 182 MB",
                MemoryRequirement = "VRAM 1GB環境向け",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin"
            },
            new WhisperModelInfo
            {
                Id = "base-q5_1",
                DisplayName = "Base [Q5_1] (省メモリ / 約57MB)",
                FileName = "ggml-base-q5_1.bin",
                Category = "量子化・低メモリ",
                EstimatedSizeBytes = 59768832,
                SizeDescription = "約 57 MB",
                MemoryRequirement = "VRAM 500MB環境向け",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base-q5_1.bin"
            },
            new WhisperModelInfo
            {
                Id = "tiny-q5_1",
                DisplayName = "Tiny [Q5_1] (省メモリ / 約31MB)",
                FileName = "ggml-tiny-q5_1.bin",
                Category = "量子化・低メモリ",
                EstimatedSizeBytes = 32505856,
                SizeDescription = "約 31 MB",
                MemoryRequirement = "超低負荷・省メモリ",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny-q5_1.bin"
            },

            // 英語専用モデル (.en)
            new WhisperModelInfo
            {
                Id = "medium.en",
                DisplayName = "Medium.en (英語専用 / 約1.5GB)",
                FileName = "ggml-medium.en.bin",
                Category = "英語専用",
                EstimatedSizeBytes = 1533763059,
                SizeDescription = "約 1.5 GB",
                MemoryRequirement = "英語音声専用",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.en.bin"
            },
            new WhisperModelInfo
            {
                Id = "small.en",
                DisplayName = "Small.en (英語専用 / 約460MB)",
                FileName = "ggml-small.en.bin",
                Category = "英語専用",
                EstimatedSizeBytes = 487848419,
                SizeDescription = "約 466 MB",
                MemoryRequirement = "英語音声専用",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.en.bin"
            },
            new WhisperModelInfo
            {
                Id = "base.en",
                DisplayName = "Base.en (英語専用 / 約140MB)",
                FileName = "ggml-base.en.bin",
                Category = "英語専用",
                EstimatedSizeBytes = 147964211,
                SizeDescription = "約 142 MB",
                MemoryRequirement = "英語音声専用",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin"
            },
            new WhisperModelInfo
            {
                Id = "tiny.en",
                DisplayName = "Tiny.en (英語専用 / 約75MB)",
                FileName = "ggml-tiny.en.bin",
                Category = "英語専用",
                EstimatedSizeBytes = 77691713,
                SizeDescription = "約 75 MB",
                MemoryRequirement = "英語音声専用",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin"
            }
        };

        public static string ResolveModelPath(string baseDir, WhisperModelInfo model)
        {
            // まず system/models/ または models/ または APP/models/ を確認
            string p1 = Path.Combine(baseDir, "system", "models", model.FileName);
            if (File.Exists(p1)) return p1;
            string p2 = Path.Combine(baseDir, "models", model.FileName);
            if (File.Exists(p2)) return p2;
            string p3 = Path.Combine(baseDir, "APP", "models", model.FileName);
            if (File.Exists(p3)) return p3;

            // デフォルト保存先は system/models/
            return p1;
        }

        public static bool IsModelDownloaded(string baseDir, WhisperModelInfo model)
        {
            return File.Exists(Path.Combine(baseDir, "system", "models", model.FileName))
                || File.Exists(Path.Combine(baseDir, "models", model.FileName))
                || File.Exists(Path.Combine(baseDir, "APP", "models", model.FileName));
        }
    }
}
