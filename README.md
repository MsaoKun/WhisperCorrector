# WhisperCorrector

[![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)]()
[![whisper.cpp](https://img.shields.io/badge/whisper.cpp-v1.9.4-green.svg)](https://github.com/ggml-org/whisper.cpp)

**WhisperCorrector** は、インストール不要で使える Windows 向けの高精度・超高速文字起こし＆ AI 文章校正デスクトップアプリケーションです。

最先端の音声認識エンジン **whisper.cpp (v1.9.4)** を内蔵し、**Vulkan** による GPU 高速処理と、**Google Gemini API** による自然な日本語校正・文書整形・話者ラベリングをワンストップで提供します。

---

## 主な特徴

- 🚀 **誰でも迷わない直感的な 3 ステップ UI**
  - 音声ファイルをドラッグ＆ドロップして「開始」を押すだけのシンプル操作。
  - 初見ユーザーを迷わせる難解な設定（GPU ID、スレッド数等）は「詳細設定」に格納。
- ⚡ **どの PC でも快適に動作（Vulkan ＆ 自動フォールバック）**
  - NVIDIA / AMD / Intel のあらゆる GPU で超高速に動作。
  - GPU がない環境や古いグラフィックボードでも、自動判別して CPU で安全に完走。
- 📦 **全モデルを体系的サポート ＆ ワンクリック自動ダウンロード**
  - Tiny から Large-v3-turbo、メモリを節約できる量子化版（Q5_0 / Q8_0）、英語専用モデルまで網羅。
  - 未ダウンロードのモデルは進捗率（%）付きでアプリからワンクリック取得可能。
- 🏷️ **文脈ヒント・固有名詞プロンプト対応**
  - 会議タイトルや頻出する固有名詞（例: `権現ノ木川, 治水計画, 山田課長`）を入力可能。
  - Whisper と Gemini の双方に文脈を注入し、未知の固有名詞も正確に認識・補正。
- 🛡️ **「ありがとうございました」幻覚ループの防止**
  - Whisper 特有の無音区間リピートバグを抑止する **VAD（無音検出カット）** を標準搭載。
  - 連続した同一フレーズの重複を自動クリーンアップする独自フィルターを内蔵。
- 🤖 **Gemini AI による 3 つの校正モード**
  - **最小補正**: 誤字脱字、明らかな認識ミス、句読点のみを修正。
  - **文書整形**: 要約せず、Word 文書や議事録にしやすい見出し・箇条書きに整形。
  - **話者ラベリング**: 会話の文脈から発言者を特定し、【話者A】【話者B】に自動仕分け。
- 📁 **出力先がすぐわかる親切設計**
  - 保存先フルパスを画面に常時表示。
  - 「📁 出力フォルダを開く」ボタンで、いつでも結果ファイル（テキスト、Markdown、SRT字幕）にアクセス可能。

---

## 画面イメージ

```
+---------------------------------------------------------------+
|  🎙️ WhisperCorrector                                [ - ][ x ]|
+---------------------------------------------------------------+
|  1. 音声ファイル                                              |
|  [ ここに音声ファイルをドラッグ＆ドロップ / 参照... ]         |
|                                                               |
|  2. 文字起こしモデル ＆ 文脈設定                              |
|  モデル: [ Large-v3-turbo (高速・高精度 / 約1.5GB)       ▼ ]   |
|  文脈ヒント: [ 権現ノ木川, 治水計画, 山田課長              ]   |
|                                                               |
|  [v] AI文章校正・整形を行う (Google Gemini API)                |
|      APIキー: [ ******************************** ]            |
|      モード:  (o) 最小補正   ( ) 文書整形   ( ) 話者ラベリング |
|                                                               |
|  > ⚙️ 詳細設定 (ハードウェア / スレッド / 幻覚対策)            |
|                                                               |
|  📁 保存先: D:\whisper-app\output\20260929_会議音声\          |
|  [ ▶ 文字起こしを開始する ]               [ 📁 フォルダを開く ] |
|  進捗: [=======================>             ] 65%            |
|                                                               |
|  [ プレビュー: ✨ 校正後 | 📝 生テキスト | ⏱️ SRT字幕 | 📜 ログ ]|
|  +---------------------------------------------------------+  |
|  | 【議事録ドラフト】                                      |  |
|  | 本日の治水対策会議では、権現ノ木川の改修計画について... |  |
|  +---------------------------------------------------------+  |
|  [ 📋 クリップボードにコピー ]                                |
+---------------------------------------------------------------+
```

---

## クイックスタート

1. [Releases](https://github.com/MsaoKun/WhisperCorrector/releases) から最新の配布パッケージ（ZIP）をダウンロードして解凍します。
2. `WhisperCorrector.exe` をダブルクリックして起動します。
3. 音声ファイル（`.mp3`, `.wav`, `.m4a`, `.mp4` など）をドラッグ＆ドロップして **「文字起こしを開始する」** を押すだけです。

---

## フォルダ構成（配布版）

配布フォルダは一般ユーザーが見て迷わないよう、極めてシンプルに整理されています：

```
DIST_RELEASE/
  ├── WhisperCorrector.exe        # 起動用メインアプリ
  ├── README.txt                  # 初心者向けスタートアップガイド
  ├── output/                     # 文字起こし結果の保存先
  └── system/                     # 【内部】Whisperエンジン、モデル、設定
      ├── bin/                    # whisper-cli.exe, ggml-vulkan.dll 等
      ├── models/                 # ダウンロードされたモデル (*.bin)
      └── config.json             # ユーザー設定
```

---

## 開発環境とビルド

- **OS**: Windows 10 / 11 (x64)
- **GUI アプリ**: .NET Framework 4.8 / C# 10.0 / WPF
- **音声認識エンジン**: whisper.cpp v1.9.4 (Vulkan & CPU)
- **ビルドツール**:
  - .NET 8.0+ SDK (MSBuild)
  - CMake 3.20+
  - Visual Studio 2022+ (C++ 開発ワークロード)
  - Vulkan SDK 1.3+

### C# アプリのビルド
```powershell
dotnet build PortableWhisperCorrector/PortableWhisperCorrector.csproj -c Release
```

### whisper.cpp の Vulkan ビルド
```powershell
cmake -B whisper-src/build-vulkan -S whisper-src -DGGML_VULKAN=ON -DBUILD_SHARED_LIBS=ON -DCMAKE_BUILD_TYPE=Release
cmake --build whisper-src/build-vulkan --config Release --target whisper-cli
```

---

## ⚠️ ウイルス対策ソフトの誤検知について

本アプリは **VirusTotal（71 エンジン同時スキャン）で 3/71 エンジンが「疑わしい」と判定する**場合があります。

| 検出エンジン | 検出名 | 性質 |
|-------------|--------|------|
| Microsoft Defender | `Trojan:Win32/Wacatac.C!ml` | ML（機械学習）による誤検知 |
| MaxSecure | `Trojan.Malware.300983.susgen` | ML による誤検知 |
| Trapmine | `Malicious.moderate.ml.score` | ML による誤検知 |

検出名の末尾にある `!ml`・`susgen`・`ml.score` はいずれも **機械学習によるヒューリスティック判定** を意味します。コードのシグネチャではなく「構造のパターン」で疑われているだけであり、実際には無害です。

本アプリは DLL を exe 内に埋め込むライブラリ（Costura.Fody）を使用しており、その構造がマルウェアと類似しているためです。**ソースコードは全公開**しており、自身でビルドして内容を確認いただくことも可能です。

### 🛡️ Windows SmartScreen が表示された場合

初回起動時、Windows が「WindowsによってPCが保護されました」という青い警告画面を表示することがあります。

1. **「詳細情報」** をクリック
2. **「実行する」** をクリック

これはコード署名証明書のない個人開発アプリに共通の動作であり、本アプリ固有の問題ではありません。

### 🔍 自分でスキャンしたい場合

- VirusTotal: https://www.virustotal.com/gui/file/dd3c93b59d82971023b06b3541a915c067d0ac97af2766d936c7a1fad312487e

---

## ライセンス

本ソフトウェアは [MIT License](LICENSE) の下で公開されています。
内蔵されている音声認識エンジン whisper.cpp は Georgi Gerganov 氏およびコントリビューターによって開発された MIT ライセンスの成果物です。
