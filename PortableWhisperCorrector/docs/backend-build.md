# Whisper Backend Build Guide

本ドキュメントでは、`whisper.cpp` の各バックエンドのビルド方針および配布時の注意点についてまとめます。

## 1. 公式リリースバイナリの状況
- 現在の `whisper.cpp` 公式のWindows向けReleaseには、CPU版(OpenBLAS/AVX等)のほか、CUDA版などが含まれることがあります。
- しかし、CUDA版は利用者にNVIDIAドライバやCUDAランタイムを要求するため、ポータブル配布には向きません。

## 2. Windows x64向け CPU版の同梱
- **必要な開発環境**: Visual Studio (MSVC) または MinGW
- **追加インストール要求**: 原則なし（`/MT` ビルド推奨）
- **実行ファイル名**: `whisper-cli-cpu.exe`
- **採用優先度**: 高 (MVP必須)
- **メリット**: どのWindows PCでも確実に動作する。
- **デメリット**: 処理速度がGPU版に比べて大幅に劣る。

## 3. Windows x64向け Vulkan版のビルド
- **必要な開発環境**: Vulkan SDK, CMake, MSVC
- **追加インストール要求**: 利用者側は最近のグラフィックドライバがあれば動作可能。
- **実行ファイル名**: `whisper-cli-vulkan.exe`
- **採用優先度**: 中〜高 (MVPで対応可能なら同梱)
- **メリット**: AMD/NVIDIA/Intel問わず幅広いGPUで高速化の恩恵を受けられる。
- **デメリット**: ドライバの相性が出やすい。

## 4. Windows x64向け DirectML版のビルド
- **ビルド可否**: `whisper.cpp` 本体でのDirectML標準サポートは限定的または未実装のケースが多い。
- **代替案**: DirectMLを使う場合は、ONNX Runtime + DirectML Execution Provider を用いた別実装（例: `onnx-directml-transcriber.exe`）を将来検討する。
- **追加インストール要求**: Windows 10/11に標準搭載されているため、追加インストール不要で動く可能性が高い。
- **実行ファイル名**: `whisper-cli-directml.exe`
- **採用優先度**: 現時点では調査段階 (MVPでは見送り)
- **メリット**: Windowsネイティブで安定したハードウェアアクセラレーション。

## 5. `/MT` Release ビルドの方法
利用者に `Visual C++ Redistributable` をインストールさせないため、MSVCでビルドする際はCランタイムを静的リンクします。
CMakeの場合、以下のオプションを付与します：
```bash
cmake -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded ..
```

## 6. 依存DLLの確認方法 (`dumpbin`)
ビルドした実行ファイルが依存しているDLLを確認するには、Visual Studio 開発者コマンドプロンプトで以下を実行します：
```bash
dumpbin /dependents whisper-cli.exe
```
`VCRUNTIME140.dll` や `MSVCP140.dll` が出力に含まれていなければ、`/MT` ビルドは成功しており、VC++再頒布可能パッケージは不要です。

## 7. 同梱すべきDLLの一覧
- `/MT` ビルドが成功している場合は、基本的に標準のWindowsシステムDLL (`KERNEL32.dll` など) のみ依存するため、追加のDLL同梱は不要です。
- Vulkan版の場合、`vulkan-1.dll` に依存しますが、これは通常グラフィックドライバと一緒にインストールされています。
