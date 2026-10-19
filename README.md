# foo_uie_explorer_x64

An Explorer Tree Panel component for foobar2000 (v2.0+ x64) / Columns UI.
foobar2000 (v2.0以降 x64版) の Columns UI 向けエクスプローラー風ツリーパネルコンポーネントです。

![Screenshot](https://github.com/user-attachments/assets/4c90e953-00c8-4a70-b31e-2154e84b6d9e)

## Features / 機能

* **Native OS UI**: Built with C# (.NET 10 NativeAOT) and WinForms for a native Windows Explorer look and feel. / C# と WinForms を利用し、Windows 標準のエクスプローラーに近い操作感を実現。
* **Smart Filtering**: Automatically filters and adds only supported audio and playlist files when adding entire folders. / フォルダ追加時、foobar2000がサポートしている音声ファイルやプレイリストのみを自動的に判別してプレイリストに追加。
* **Favorites System**: Right-click to add frequently used folders to the top "Favorites" section. / よく使うフォルダを右クリックから「お気に入り」として最上部に追加可能。
* **Clear & Add**: Context menu option to clear the current playlist and instantly load selected tracks. / コンテキストメニューから「現在のプレイリストをクリアして追加」が可能。

* **Remember expanded folders**: Restores the tree expansion state after restart, including separate states for favorites and drives. / ツリーの展開状態を保存し、再起動時に復元します。お気に入りとドライブの状態は個別に保持します。
* **Configurable double-click**: Right-click the tree and choose **Config...** to select **Clear and Add All Tracks**, **Add All Tracks**, or either recursive variant. The default is **Add All Tracks** (current folder only). Use the plus/minus buttons to expand or collapse folders. / 右クリックの **Config...** から、ダブルクリック時の「クリアして全曲追加／全曲追加」と、それぞれの再帰版を選択できます。初期値は直下の全曲追加です。ツリーの開閉には＋／－を使います。

Expansion and double-click settings are saved in `explorer-settings.txt` beside `favorites.txt` in the component folder. / 展開状態とダブルクリック設定は、コンポーネントフォルダの `favorites.txt` と同じ場所にある `explorer-settings.txt` に保存されます。

## Requirements / 動作環境

* **OS**: Windows 10 / 11 (64-bit)
* **foobar2000**: v2.0 or newer (x64 version)
* **Columns UI**: v2.0.0 or newer

## Installation / インストール方法

1. Open the [latest release](https://github.com/mk-822/foo_uie_explorer_x64/releases/latest) and download `foo_uie_explorer_x64.zip` from **Assets**.
2. Open foobar2000 and go to **File > Preferences > Components**.
3. Click **Install...** and select the downloaded `foo_uie_explorer_x64.zip`, or drag and drop it into the Components list.
4. Click **Apply** and restart foobar2000.
5. Go to **Preferences > Display > Columns UI > Layout**, right-click on a splitter, select **Insert panel > Panels > Explorer Tree**, and apply.
6. To change the double-click action, right-click the Explorer Tree panel and choose **Config...**. Tree expansion and this setting are saved automatically.

**(日本語)**
1. [最新のリリース](https://github.com/mk-822/foo_uie_explorer_x64/releases/latest) を開き、**Assets** から `foo_uie_explorer_x64.zip` をダウンロードします。
2. foobar2000 を開き、**File > Preferences > Components** を開きます。
3. **Install...** からダウンロードした `foo_uie_explorer_x64.zip` を選択するか、コンポーネント一覧にドラッグ＆ドロップします。
4. **Apply** をクリックし、foobar2000 を再起動します。
5. **Preferences > Display > Columns UI > Layout** に移動し、スプリッターを右クリックして **Insert panel > Panels > Explorer Tree** を追加し、適用します。
6. ダブルクリック動作を変更する場合は、Explorer Treeパネルを右クリックし、**Config...** を開きます。ツリーの展開状態と設定は自動保存されます。

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
