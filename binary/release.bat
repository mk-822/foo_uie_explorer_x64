@echo off
:: 文字化け防止のため一時的にUTF-8を利用
chcp 65001 > nul
setlocal

echo リリース用のZIPファイルを作成しています...

:: バッチファイルの場所 (binフォルダ) を基準にパスを構築
set "BASE_DIR=%~dp0.."
set "DLL_CORE=%BASE_DIR%\vc16\bin\NativeOutput\Release\foo_uie_explorer_core.dll"
set "DLL_MAIN=%BASE_DIR%\vc16\Release\foo_uie_explorer_x64.dll"
set "ZIP_OUT=%~dp0foo_uie_explorer_x64.zip"

:: 必要なDLLが存在するかチェック
if not exist "%DLL_CORE%" (
    echo エラー: foo_uie_explorer_core.dll が見つかりません。
    echo パス: %DLL_CORE%
    goto :error
)
if not exist "%DLL_MAIN%" (
    echo エラー: foo_uie_explorer_x64.dll が見つかりません。
    echo パス: %DLL_MAIN%
    goto :error
)

:: PowerShellのCompress-Archiveを利用して2つのDLLをZIP化 (Forceで上書き)
powershell -NoProfile -Command "Compress-Archive -Path '%DLL_CORE%', '%DLL_MAIN%' -DestinationPath '%ZIP_OUT%' -Force"

if exist "%ZIP_OUT%" (
    echo.
    echo 成功: %ZIP_OUT% を作成しました。
    echo リリース準備が完了しました！
) else (
    echo.
    echo エラー: ZIPファイルの作成に失敗しました。
)

pause
exit /b 0

:error
echo.
echo 処理を中断しました。ビルドが完了しているか確認してください。
pause
exit /b 1