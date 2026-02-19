# foo_uie_explorer_x64

An Explorer Tree Panel component for foobar2000 (v2.0+ x64) / Columns UI.
foobar2000 (v2.0以降 x64版) の Columns UI 向けエクスプローラー風ツリーパネルコンポーネントです。

## Features / 機能

* **Native OS UI**: Built with C# (.NET 10 NativeAOT) and WinForms for a native Windows Explorer look and feel. / C# と WinForms を利用し、Windows 標準のエクスプローラーに近い操作感を実現。
* **Smart Filtering**: Automatically filters and adds only supported audio and playlist files when adding entire folders. / フォルダ追加時、foobar2000がサポートしている音声ファイルやプレイリストのみを自動的に判別してプレイリストに追加。
* **Favorites System**: Right-click to add frequently used folders to the top "Favorites" section. / よく使うフォルダを右クリックから「お気に入り」として最上部に追加可能。
* **Clear & Add**: Context menu option to clear the current playlist and instantly load selected tracks. / コンテキストメニューから「現在のプレイリストをクリアして追加」が可能。

## Requirements / 動作環境

* **OS**: Windows 10 / 11 (64-bit)
* **foobar2000**: v2.0 or newer (x64 version)
* **Columns UI**: v2.0.0 or newer

## Installation / インストール方法

1. Download the latest `foo_uie_explorer_x64.zip` from the [binary](../../tree/main/binary) folder.
2. Open foobar2000 and go to **File > Preferences > Components**.
3. Drag and drop the downloaded `.zip` file into the Components list.
4. Click **Apply** and restart foobar2000.
5. Go to **Preferences > Display > Columns UI > Layout**, right-click on a splitter, select **Insert panel > Panels > Explorer Tree**, and apply.

**(日本語)**
1. `binary` フォルダから最新の `foo_uie_explorer_x64.zip` をダウンロードします。
2. foobar2000 を開き、**File > Preferences > Components** を開きます。
3. ダウンロードした `.zip` ファイルをコンポーネント一覧の領域にドラッグ＆ドロップします。
4. **Apply** をクリックし、foobar2000 を再起動します。
5. **Preferences > Display > Columns UI > Layout** に移動し、スプリッターを右クリックして **Insert panel > Panels > Explorer Tree** を追加してください。

## Building from Source / ビルド方法

This project uses a hybrid architecture consisting of a C++ host and a C# NativeAOT UI.
このプロジェクトは C++ のホストと C# の NativeAOT を利用したハイブリッド構成です。

### Prerequisites
* Visual Studio 2026 (or compatible) with "Desktop development with C++" and ".NET desktop development" workloads.
* .NET 10.0 SDK

### Steps
1. Open the solution file in Visual Studio.
2. Select the **Release | x64** configuration.
3. Build the solution. The `foo_uie_explorer_core.csproj` is configured to automatically run `dotnet publish` with NativeAOT via a post-build target.
4. Use the provided `release.bat` script in the `bin` folder to package the output DLLs into a ZIP archive.

## License

This project is licensed under the [MIT License](LICENSE).