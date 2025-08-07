#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Podmanコンテナを使用してバックエンドのビルドとテストを実行します。

.DESCRIPTION
    このスクリプトは.NET 8.0 SDKコンテナを使用して、バックエンドプロジェクトの
    ビルドと単体テストを実行します。

.PARAMETER Configuration
    ビルド構成を指定します。デフォルトは "Debug" です。

.PARAMETER Coverage
    テストカバレッジレポートを生成する場合に指定します。

.PARAMETER Clean
    ビルド前にクリーンを実行する場合に指定します。

.EXAMPLE
    ./test-backend.ps1
    デバッグ構成でビルドとテストを実行します。

.EXAMPLE
    ./test-backend.ps1 -Configuration Release -Coverage
    リリース構成でビルドとテストを実行し、カバレッジレポートを生成します。

.EXAMPLE
    ./test-backend.ps1 -Clean
    クリーン後にビルドとテストを実行します。
#>

param(
    [Parameter(HelpMessage = "ビルド構成 (Debug/Release)")]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter(HelpMessage = "テストカバレッジレポートを生成")]
    [switch]$Coverage,
    
    [Parameter(HelpMessage = "ビルド前にクリーンを実行")]
    [switch]$Clean
)

# エラー時に停止
$ErrorActionPreference = "Stop"

# 色付きメッセージ出力関数
function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Podmanが利用可能かチェック
function Test-PodmanAvailable {
    try {
        $null = podman --version
        return $true
    }
    catch {
        Write-ColorMessage "エラー: Podmanが見つかりません。Podman Desktopがインストールされていることを確認してください。" "Red"
        return $false
    }
}

# メイン処理
function Main {
    Write-ColorMessage "=== バックエンドのビルドとテスト ===" "Cyan"
    Write-ColorMessage "構成: $Configuration" "Yellow"
    
    if ($Coverage) {
        Write-ColorMessage "テストカバレッジ: 有効" "Yellow"
    }
    
    if ($Clean) {
        Write-ColorMessage "クリーン: 有効" "Yellow"
    }
    
    Write-ColorMessage ""

    # Podmanの可用性チェック
    if (-not (Test-PodmanAvailable)) {
        exit 1
    }

    # バックエンドディレクトリの存在確認
    if (-not (Test-Path "backend")) {
        Write-ColorMessage "エラー: backendディレクトリが見つかりません。プロジェクトルートから実行してください。" "Red"
        exit 1
    }

    # .NET SDKコンテナイメージ
    $dotnetImage = "mcr.microsoft.com/dotnet/sdk:8.0"
    $volumeMount = "${PWD}/backend:/src"
    $workDir = "/src"

    try {
        # クリーン実行
        if ($Clean) {
            Write-ColorMessage "クリーン実行中..." "Yellow"
            podman run --rm -v $volumeMount -w $workDir $dotnetImage dotnet clean -c $Configuration
            
            if ($LASTEXITCODE -ne 0) {
                Write-ColorMessage "クリーンに失敗しました。" "Red"
                exit 1
            }
            Write-ColorMessage "クリーン完了" "Green"
        }

        # パッケージ復元
        Write-ColorMessage "NuGetパッケージを復元中..." "Yellow"
        podman run --rm -v $volumeMount -w $workDir $dotnetImage dotnet restore
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColorMessage "パッケージ復元に失敗しました。" "Red"
            exit 1
        }
        Write-ColorMessage "パッケージ復元完了" "Green"

        # ビルド実行
        Write-ColorMessage "ビルド実行中..." "Yellow"
        podman run --rm -v $volumeMount -w $workDir $dotnetImage dotnet build -c $Configuration --no-restore
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColorMessage "ビルドに失敗しました。" "Red"
            exit 1
        }
        Write-ColorMessage "ビルド完了" "Green"

        # テスト実行
        Write-ColorMessage "テスト実行中..." "Yellow"
        
        $testArgs = @("test", "-c", $Configuration, "--no-build", "--verbosity", "normal")
        
        if ($Coverage) {
            $testArgs += @("--collect:XPlat Code Coverage")
        }
        
        podman run --rm -v $volumeMount -w $workDir $dotnetImage dotnet @testArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorMessage "すべてのテストが成功しました！" "Green"
            
            if ($Coverage) {
                Write-ColorMessage "テストカバレッジレポートが生成されました。" "Green"
                Write-ColorMessage "レポートの場所: backend/TestResults/" "Cyan"
            }
        } else {
            Write-ColorMessage "テストに失敗しました。" "Red"
            exit 1
        }

    }
    catch {
        Write-ColorMessage "予期しないエラーが発生しました: $($_.Exception.Message)" "Red"
        exit 1
    }

    Write-ColorMessage ""
    Write-ColorMessage "=== 処理完了 ===" "Cyan"
}

# スクリプト実行
Main