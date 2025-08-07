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
    [Parameter(HelpMessage = "Build configuration (Debug/Release)")]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter(HelpMessage = "Generate test coverage report")]
    [switch]$Coverage,
    
    [Parameter(HelpMessage = "Clean before build")]
    [switch]$Clean
)

# Stop on error
$ErrorActionPreference = "Stop"

# Color message output function
function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Check if Podman is available
function Test-PodmanAvailable {
    try {
        $null = podman --version
        return $true
    }
    catch {
        Write-ColorMessage "Error: Podman not found. Please ensure Podman Desktop is installed." "Red"
        return $false
    }
}

# Main process
function Main {
    Write-ColorMessage "=== Backend Build and Test ===" "Cyan"
    Write-ColorMessage "Configuration: $Configuration" "Yellow"
    
    if ($Coverage) {
        Write-ColorMessage "Test Coverage: Enabled" "Yellow"
    }
    
    if ($Clean) {
        Write-ColorMessage "Clean: Enabled" "Yellow"
    }
    
    Write-ColorMessage ""

    # Check Podman availability
    if (-not (Test-PodmanAvailable)) {
        exit 1
    }

    # Check backend directory exists
    if (-not (Test-Path "backend")) {
        Write-ColorMessage "Error: backend directory not found. Please run from project root." "Red"
        exit 1
    }

    # .NET SDK container image
    $dotnetImage = "mcr.microsoft.com/dotnet/sdk:8.0"
    $volumeMount = "${PWD}/backend:/src"
    $workDir = "/src"

    try {
        # Clean execution
        if ($Clean) {
            Write-ColorMessage "Cleaning..." "Yellow"
            podman run --rm --network=host -v $volumeMount -w $workDir $dotnetImage dotnet clean -c $Configuration
            
            if ($LASTEXITCODE -ne 0) {
                Write-ColorMessage "Clean failed." "Red"
                exit 1
            }
            Write-ColorMessage "Clean completed" "Green"
        }

        # Package restore and build execution
        Write-ColorMessage "Restoring packages and building..." "Yellow"
        podman run --rm --network=host -v $volumeMount -w $workDir $dotnetImage dotnet build -c $Configuration
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColorMessage "Build failed." "Red"
            exit 1
        }
        Write-ColorMessage "Build completed" "Green"

        # Test execution
        Write-ColorMessage "Running tests..." "Yellow"
        
        $testArgs = @("test", "-c", $Configuration, "--no-build", "--verbosity", "normal")
        
        if ($Coverage) {
            $testArgs += @("--collect:XPlat Code Coverage")
        }
        
        podman run --rm --network=host -v $volumeMount -w $workDir $dotnetImage dotnet @testArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorMessage "All tests passed!" "Green"
            
            if ($Coverage) {
                Write-ColorMessage "Test coverage report generated." "Green"
                Write-ColorMessage "Report location: backend/TestResults/" "Cyan"
            }
        } else {
            Write-ColorMessage "Tests failed." "Red"
            exit 1
        }

    }
    catch {
        Write-ColorMessage "Unexpected error occurred: $($_.Exception.Message)" "Red"
        exit 1
    }

    Write-ColorMessage ""
    Write-ColorMessage "=== Process Completed ===" "Cyan"
}

# Execute script
Main