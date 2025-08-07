@echo off
REM Podmanコンテナを使用してバックエンドのビルドとテストを実行するバッチファイル

setlocal enabledelayedexpansion

REM 設定
set CONFIGURATION=Debug
set DOTNET_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0
set VOLUME_MOUNT=%CD%/backend:/src
set WORK_DIR=/src

REM 引数解析
:parse_args
if "%1"=="" goto start
if /i "%1"=="--release" (
    set CONFIGURATION=Release
    shift
    goto parse_args
)
if /i "%1"=="--coverage" (
    set COVERAGE=true
    shift
    goto parse_args
)
if /i "%1"=="--clean" (
    set CLEAN=true
    shift
    goto parse_args
)
if /i "%1"=="--help" (
    goto show_help
)
shift
goto parse_args

:show_help
echo.
echo バックエンドのビルドとテストを実行します
echo.
echo 使用方法: test-backend.cmd [オプション]
echo.
echo オプション:
echo   --release    リリース構成でビルド
echo   --coverage   テストカバレッジレポートを生成
echo   --clean      ビルド前にクリーンを実行
echo   --help       このヘルプを表示
echo.
echo 例:
echo   test-backend.cmd
echo   test-backend.cmd --release --coverage
echo   test-backend.cmd --clean
echo.
goto end

:start
echo.
echo === バックエンドのビルドとテスト ===
echo 構成: %CONFIGURATION%
if defined COVERAGE echo テストカバレッジ: 有効
if defined CLEAN echo クリーン: 有効
echo.

REM Podmanの可用性チェック
podman --version >nul 2>&1
if errorlevel 1 (
    echo エラー: Podmanが見つかりません。Podman Desktopがインストールされていることを確認してください。
    goto error
)

REM バックエンドディレクトリの存在確認
if not exist "backend" (
    echo エラー: backendディレクトリが見つかりません。プロジェクトルートから実行してください。
    goto error
)

REM クリーン実行
if defined CLEAN (
    echo クリーン実行中...
    podman run --rm -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet clean -c %CONFIGURATION%
    if errorlevel 1 (
        echo クリーンに失敗しました。
        goto error
    )
    echo クリーン完了
)

REM パッケージ復元
echo NuGetパッケージを復元中...
podman run --rm -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet restore
if errorlevel 1 (
    echo パッケージ復元に失敗しました。
    goto error
)
echo パッケージ復元完了

REM ビルド実行
echo ビルド実行中...
podman run --rm -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet build -c %CONFIGURATION% --no-restore
if errorlevel 1 (
    echo ビルドに失敗しました。
    goto error
)
echo ビルド完了

REM テスト実行
echo テスト実行中...
if defined COVERAGE (
    podman run --rm -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet test -c %CONFIGURATION% --no-build --verbosity normal --collect:"XPlat Code Coverage"
) else (
    podman run --rm -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet test -c %CONFIGURATION% --no-build --verbosity normal
)

if errorlevel 1 (
    echo テストに失敗しました。
    goto error
)

echo すべてのテストが成功しました！
if defined COVERAGE (
    echo テストカバレッジレポートが生成されました。
    echo レポートの場所: backend\TestResults\
)

echo.
echo === 処理完了 ===
goto end

:error
echo.
echo === エラーで終了 ===
exit /b 1

:end
endlocal