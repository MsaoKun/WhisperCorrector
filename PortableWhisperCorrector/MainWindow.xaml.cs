using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using PortableWhisperCorrector.Managers;
using PortableWhisperCorrector.Models;

namespace PortableWhisperCorrector
{
    public partial class MainWindow : Window
    {
        private readonly ConfigManager _configManager;
        private readonly WhisperRunner _whisperRunner;
        private readonly GeminiClient _geminiClient;
        private string _currentOutputDir = "";
        private CancellationTokenSource _downloadCts;

        public MainWindow()
        {
            InitializeComponent();
            _configManager = new ConfigManager();
            _whisperRunner = new WhisperRunner();
            _geminiClient = new GeminiClient();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _configManager.LoadConfigAsync();

            // AI設定
            chkAiCorrection.IsChecked = _configManager.Config.AiCorrection.Enabled;
            panelAiSettings.IsEnabled = _configManager.Config.AiCorrection.Enabled;
            txtApiKey.Password = _configManager.Config.Gemini.ApiKey;

            string mode = _configManager.Config.AiCorrection.Mode;
            if (mode == "format") rbModeFormat.IsChecked = true;
            else if (mode == "diarize") rbModeDiarize.IsChecked = true;
            else rbModeProofread.IsChecked = true;

            // Geminiモデル一覧初期化
            cmbGeminiModel.Items.Add("gemini-2.5-flash");
            cmbGeminiModel.Items.Add("gemini-2.5-pro");
            cmbGeminiModel.SelectedIndex = 0;

            // 文脈・固有名詞
            txtContextPrompt.Text = _configManager.Config.Whisper.ContextPrompt;

            // 詳細設定
            chkVad.IsChecked = _configManager.Config.Whisper.EnableVad;
            chkFlashAttn.IsChecked = _configManager.Config.Whisper.FlashAttn;
            txtGpuDevice.Text = _configManager.Config.Whisper.GpuDevice.ToString();
            txtThreads.Text = _configManager.Config.Whisper.Threads.ToString();

            // モデル一覧の初期化
            InitModelComboBox();

            // 初期出力フォルダの準備
            UpdateOutputDirPlaceholder();

            AppendLog("WhisperCorrector を起動しました。");
        }

        private void InitModelComboBox()
        {
            cmbModel.Items.Clear();
            string selectedId = _configManager.Config.Whisper.SelectedModelId;
            int selectIndex = 0;

            for (int i = 0; i < ModelCatalog.AvailableModels.Count; i++)
            {
                var m = ModelCatalog.AvailableModels[i];
                cmbModel.Items.Add(m);
                if (m.Id == selectedId)
                {
                    selectIndex = i;
                }
            }

            if (cmbModel.Items.Count > 0)
            {
                cmbModel.SelectedIndex = selectIndex;
            }
        }

        private void CmbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbModel.SelectedItem is WhisperModelInfo model)
            {
                UpdateModelStatusUI(model);
            }
        }

        private void UpdateModelStatusUI(WhisperModelInfo model)
        {
            bool downloaded = ModelCatalog.IsModelDownloaded(AppContext.BaseDirectory, model);
            if (downloaded)
            {
                lblModelStatus.Text = $"✓ ダウンロード済み ({model.SizeDescription})";
                lblModelStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74)); // 緑
                btnDownloadModel.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblModelStatus.Text = $"未ダウンロード ({model.SizeDescription})";
                lblModelStatus.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6)); // オレンジ
                btnDownloadModel.Visibility = Visibility.Visible;
                btnDownloadModel.Content = "ダウンロード";
            }
        }

        private async void BtnDownloadModel_Click(object sender, RoutedEventArgs e)
        {
            if (cmbModel.SelectedItem is not WhisperModelInfo model) return;

            string baseDir = AppContext.BaseDirectory;
            string targetPath = ModelCatalog.ResolveModelPath(baseDir, model);
            string targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            try
            {
                btnDownloadModel.IsEnabled = false;
                btnDownloadModel.Content = "DL中...";
                pbDownload.Visibility = Visibility.Visible;
                pbDownload.Value = 0;
                lblModelStatus.Text = "接続中...";

                _downloadCts = new CancellationTokenSource();
                AppendLog($"[{model.FileName}] のダウンロードを開始します ({model.DownloadUrl})...");

                using var client = new HttpClient();
                using var response = await client.GetAsync(model.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, _downloadCts.Token);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? model.EstimatedSizeBytes;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                var buffer = new byte[81920];
                long totalRead = 0;
                int read;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, _downloadCts.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read, _downloadCts.Token);
                    totalRead += read;
                    if (totalBytes > 0)
                    {
                        double pct = (double)totalRead / totalBytes * 100.0;
                        pbDownload.Value = pct;
                        lblModelStatus.Text = $"{pct:F0}% ({totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB)";
                    }
                }

                AppendLog($"[{model.FileName}] のダウンロードが完了しました！");
                UpdateModelStatusUI(model);
            }
            catch (Exception ex)
            {
                AppendLog($"ダウンロード失敗: {ex.Message}");
                lblModelStatus.Text = "ダウンロード失敗";
                lblModelStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                if (File.Exists(targetPath))
                {
                    try { File.Delete(targetPath); } catch { }
                }
            }
            finally
            {
                btnDownloadModel.IsEnabled = true;
                btnDownloadModel.Content = "ダウンロード";
                pbDownload.Visibility = Visibility.Collapsed;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);

        private const uint WM_DROPFILES = 0x0233;
        private const uint WM_COPYDATA = 0x004A;
        private const uint WM_COPYGLOBALDATA = 0x0049;
        private const uint MSGFLT_ALLOW = 1;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                ChangeWindowMessageFilterEx(hwnd, WM_DROPFILES, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(hwnd, WM_COPYDATA, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(hwnd, WM_COPYGLOBALDATA, MSGFLT_ALLOW, IntPtr.Zero);
            }
            catch { }
        }

        #region ドラッグ＆ドロップ ＆ 音声ファイル選択

        private void Window_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                borderDropZone.Background = new SolidColorBrush(Color.FromRgb(240, 249, 255));
                borderDropZone.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            borderDropZone.Background = Brushes.White;
            borderDropZone.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    SetInputAudioFile(files[0]);
                }
            }
            e.Handled = true;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "文字起こしする音声ファイルを選択",
                Filter = "音声/動画ファイル|*.wav;*.mp3;*.m4a;*.mp4;*.flac;*.ogg;*.wma;*.aac|すべてのファイル|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                SetInputAudioFile(ofd.FileName);
            }
        }

        private void SetInputAudioFile(string filePath)
        {
            txtInputFile.Text = filePath;
            txtInputFile.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));

            // 出力先フォルダを音声ファイル名から即座に生成して明示
            string fileBaseName = Path.GetFileNameWithoutExtension(filePath);
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _currentOutputDir = Path.Combine(AppContext.BaseDirectory, "output", $"{timeStamp}_{fileBaseName}");
            txtOutputDir.Text = _currentOutputDir;

            lblStatus.Text = "準備完了。「開始」ボタンを押してください";
            lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
        }

        private void UpdateOutputDirPlaceholder()
        {
            if (string.IsNullOrEmpty(_currentOutputDir))
            {
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentOutputDir = Path.Combine(AppContext.BaseDirectory, "output", $"{timeStamp}_transcription");
                txtOutputDir.Text = _currentOutputDir;
            }
        }

        private void BtnOpenOutputDir_Click(object sender, RoutedEventArgs e)
        {
            string dirToOpen = _currentOutputDir;
            if (string.IsNullOrEmpty(dirToOpen) || !Directory.Exists(dirToOpen))
            {
                dirToOpen = Path.Combine(AppContext.BaseDirectory, "output");
            }
            if (!Directory.Exists(dirToOpen))
            {
                Directory.CreateDirectory(dirToOpen);
            }
            Process.Start("explorer.exe", dirToOpen);
        }

        #endregion

        #region AI校正設定

        private void ChkAiCorrection_Changed(object sender, RoutedEventArgs e)
        {
            if (panelAiSettings != null)
            {
                panelAiSettings.IsEnabled = chkAiCorrection.IsChecked == true;
            }
        }

        #endregion

        #region 実行処理

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string audioPath = txtInputFile.Text;
            if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
            {
                MessageBox.Show("有効な音声ファイルを選択またはドラッグ＆ドロップしてください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbModel.SelectedItem is not WhisperModelInfo model)
            {
                MessageBox.Show("モデルを選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string baseDir = AppContext.BaseDirectory;
            string modelPath = ModelCatalog.ResolveModelPath(baseDir, model);
            if (!File.Exists(modelPath))
            {
                var dr = MessageBox.Show($"選択されたモデル [{model.DisplayName}] はまだダウンロードされていません。\n今すぐダウンロードしますか？",
                    "モデルダウンロード", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (dr == MessageBoxResult.Yes)
                {
                    BtnDownloadModel_Click(null, null);
                }
                return;
            }

            string exePath = ResolveWhisperExecutable();
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show("Whisperの実行エンジン (whisper-cli.exe) が見つかりません。\nファイルを再確認してください。",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 設定の保存
            SaveSettings();

            // 出力ディレクトリの確定
            if (!Directory.Exists(_currentOutputDir))
            {
                Directory.CreateDirectory(_currentOutputDir);
            }

            // UIを処理中モードへ
            btnStart.IsEnabled = false;
            pbProgress.Value = 0;
            lblProgress.Text = "開始中...";
            lblStatus.Text = "文字起こしを実行中...";
            tabResults.SelectedIndex = 3; // ログタブをまず開く

            AppendLog("================ 処理開始 ================");
            AppendLog($"入力音声: {audioPath}");
            AppendLog($"使用モデル: {model.DisplayName}");
            AppendLog($"保存先フォルダ: {_currentOutputDir}");
            if (!string.IsNullOrWhiteSpace(txtContextPrompt.Text))
            {
                AppendLog($"文脈ヒント・固有名詞: {txtContextPrompt.Text}");
            }

            try
            {
                // Whisper 実行オプション
                bool disableGpu = cmbBackend.SelectedIndex == 2; // CPU
                int.TryParse(txtThreads.Text, out int threads);
                int.TryParse(txtGpuDevice.Text, out int gpuDev);

                var options = new WhisperRunOptions
                {
                    ExePath = exePath,
                    ModelPath = modelPath,
                    AudioPath = audioPath,
                    OutputDir = _currentOutputDir,
                    Threads = threads,
                    DisableGpu = disableGpu,
                    GpuDevice = gpuDev,
                    FlashAttn = chkFlashAttn.IsChecked == true,
                    ContextPrompt = txtContextPrompt.Text,
                    EnableVad = chkVad.IsChecked == true,
                    Language = "ja"
                };

                // 1. 文字起こし実行
                AppendLog("1. Whisper による高速音声認識を実行しています...");
                string rawText = await _whisperRunner.RunInferenceAsync(options,
                    msg => Dispatcher.Invoke(() => AppendLog(msg)),
                    pct => Dispatcher.Invoke(() =>
                    {
                        pbProgress.Value = pct;
                        lblProgress.Text = $"{pct}% 完了";
                    }));

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    throw new Exception("文字起こし結果が空でした。音声に発話が含まれているか確認してください。");
                }

                // 生出力の保存＆プレビューセット
                string rawOutPath = Path.Combine(_currentOutputDir, "raw_transcription.txt");
                File.WriteAllText(rawOutPath, rawText, Encoding.UTF8);
                txtPreviewRaw.Text = rawText;

                string srtPath = Path.Combine(_currentOutputDir, "whisper_raw.srt");
                if (File.Exists(srtPath))
                {
                    txtPreviewSrt.Text = File.ReadAllText(srtPath, Encoding.UTF8);
                }

                AppendLog($"文字起こし完了！(保存先: {rawOutPath})");
                pbProgress.Value = 100;
                lblProgress.Text = "文字起こし完了";

                // 2. AI補正 (Gemini) が有効な場合
                if (chkAiCorrection.IsChecked == true)
                {
                    string apiKey = txtApiKey.Password;
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        AppendLog("[警告] APIキーが未入力のため、AI校正はスキップされました。");
                        tabResults.SelectedIndex = 1; // 生テキストタブ
                    }
                    else
                    {
                        lblStatus.Text = "AI による文章校正・整形を実行中...";
                        AppendLog("2. Google Gemini AI による文章校正・整形を実行しています...");

                        string aiMode = rbModeDiarize.IsChecked == true ? "diarize" :
                                        rbModeFormat.IsChecked == true ? "format" : "proofread";
                        string geminiModel = cmbGeminiModel.Text.Split(' ')[0];
                        if (string.IsNullOrWhiteSpace(geminiModel)) geminiModel = "gemini-2.5-flash";

                        string corrected = await ProcessAiCorrectionAsync(rawText, apiKey, geminiModel, aiMode, txtContextPrompt.Text);

                        string correctedOutPath = Path.Combine(_currentOutputDir, "corrected.md");
                        File.WriteAllText(correctedOutPath, corrected, Encoding.UTF8);
                        txtPreviewCorrected.Text = corrected;

                        AppendLog($"AI校正完了！(保存先: {correctedOutPath})");
                        tabResults.SelectedIndex = 0; // 校正後テキストタブへ
                    }
                }
                else
                {
                    tabResults.SelectedIndex = 1; // 生テキストタブへ
                }

                lblStatus.Text = "すべての処理が正常に完了しました！";
                AppendLog("================ 処理完了 ================");
                MessageBox.Show("文字起こし・校正が完了しました！\n結果はプレビューで確認・コピーできます。",
                    "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"[エラー] {ex.Message}");
                lblStatus.Text = "処理中にエラーが発生しました";
                MessageBox.Show($"エラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnStart.IsEnabled = true;
            }
        }

        private string ResolveWhisperExecutable()
        {
            string baseDir = AppContext.BaseDirectory;
            // 優先探索パス
            string[] candidates = new[]
            {
                Path.Combine(baseDir, "system", "bin", "whisper-cli.exe"),
                Path.Combine(baseDir, "APP", "bin-vulkan", "whisper-cli-vulkan.exe"),
                Path.Combine(baseDir, "APP", "bin-cpu", "whisper-cli-cpu.exe"),
                Path.Combine(baseDir, "bin", "whisper-cli.exe"),
                Path.Combine(baseDir, "whisper-src", "build-vulkan-v194", "bin", "Release", "whisper-cli.exe")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path)) return path;
            }
            return "";
        }

        private async Task<string> ProcessAiCorrectionAsync(string text, string apiKey, string model, string mode, string contextPrompt)
        {
            int chunkSize = _configManager.Config.AiCorrection.ChunkChars;
            if (chunkSize <= 0) chunkSize = 3000;

            var chunks = Enumerable.Range(0, (text.Length / chunkSize) + 1)
                                   .Select(i => text.Substring(i * chunkSize, Math.Min(chunkSize, text.Length - i * chunkSize)))
                                   .Where(s => !string.IsNullOrEmpty(s))
                                   .ToList();

            var correctedChunks = new System.Collections.Generic.List<string>();

            for (int i = 0; i < chunks.Count; i++)
            {
                AppendLog($"AI校正チャンク {i + 1}/{chunks.Count} を処理中...");
                try
                {
                    var result = await _geminiClient.CorrectTextAsync(apiKey, model, chunks[i], mode, contextPrompt);
                    correctedChunks.Add(result.text);
                    if (!string.IsNullOrEmpty(result.tokenLog))
                    {
                        AppendLog($"チャンク {i + 1} トークン: {result.tokenLog}");
                    }
                    if (i < chunks.Count - 1)
                    {
                        await Task.Delay(1500); // レートリミット回避
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"チャンク {i + 1} エラー: {ex.Message} (原文を保持します)");
                    correctedChunks.Add(chunks[i]);
                }
            }

            return string.Join("\n\n", correctedChunks);
        }

        private async void SaveSettings()
        {
            if (cmbModel.SelectedItem is WhisperModelInfo model)
            {
                _configManager.Config.Whisper.SelectedModelId = model.Id;
            }
            _configManager.Config.Whisper.ContextPrompt = txtContextPrompt.Text;
            _configManager.Config.Whisper.EnableVad = chkVad.IsChecked == true;
            _configManager.Config.Whisper.FlashAttn = chkFlashAttn.IsChecked == true;
            if (int.TryParse(txtThreads.Text, out int th)) _configManager.Config.Whisper.Threads = th;
            if (int.TryParse(txtGpuDevice.Text, out int dev)) _configManager.Config.Whisper.GpuDevice = dev;

            _configManager.Config.AiCorrection.Enabled = chkAiCorrection.IsChecked == true;
            _configManager.Config.AiCorrection.Mode = rbModeDiarize.IsChecked == true ? "diarize" :
                                                    rbModeFormat.IsChecked == true ? "format" : "proofread";
            _configManager.Config.Gemini.ApiKey = txtApiKey.Password;

            await _configManager.SaveConfigAsync();
        }

        #endregion

        #region クリップボードコピー ＆ ログ

        private void BtnCopyCorrected_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPreviewCorrected.Text))
            {
                Clipboard.SetText(txtPreviewCorrected.Text);
                MessageBox.Show("校正後テキストをクリップボードにコピーしました！", "コピー完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCopyRaw_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPreviewRaw.Text))
            {
                Clipboard.SetText(txtPreviewRaw.Text);
                MessageBox.Show("文字起こしテキストをクリップボードにコピーしました！", "コピー完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCopySrt_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPreviewSrt.Text))
            {
                Clipboard.SetText(txtPreviewSrt.Text);
                MessageBox.Show("字幕テキスト (SRT) をクリップボードにコピーしました！", "コピー完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AppendLog(string message)
        {
            string masked = message;
            string key = txtApiKey.Password;
            if (!string.IsNullOrEmpty(key) && key.Length > 5)
            {
                masked = masked.Replace(key, "[API_KEY_HIDDEN]");
            }

            string line = $"[{DateTime.Now:HH:mm:ss}] {masked}\n";
            txtLog.AppendText(line);
            txtLog.ScrollToEnd();

            if (!string.IsNullOrEmpty(_currentOutputDir) && Directory.Exists(_currentOutputDir))
            {
                try
                {
                    File.AppendAllText(Path.Combine(_currentOutputDir, "process.log"), line);
                }
                catch { }
            }
        }

        #endregion
    }
}
