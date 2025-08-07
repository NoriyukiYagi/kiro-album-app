@echo off
REM Batch file to build and test backend using Podman containers

setlocal enabledelayedexpansion

REM Configuration
set CONFIGURATION=Debug
set DOTNET_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0
set VOLUME_MOUNT=%CD%/backend:/src
set WORK_DIR=/src

REM Argument parsing
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
echo Build and test backend
echo.
echo Usage: test-backend.cmd [options]
echo.
echo Options:
echo   --release    Build in Release configuration
echo   --coverage   Generate test coverage report
echo   --clean      Clean before build
echo   --help       Show this help
echo.
echo Examples:
echo   test-backend.cmd
echo   test-backend.cmd --release --coverage
echo   test-backend.cmd --clean
echo.
goto end

:start
echo.
echo === Backend Build and Test ===
echo Configuration: %CONFIGURATION%
if defined COVERAGE echo Test Coverage: Enabled
if defined CLEAN echo Clean: Enabled
echo.

REM Check Podman availability
podman --version >nul 2>&1
if errorlevel 1 (
    echo Error: Podman not found. Please ensure Podman Desktop is installed.
    goto error
)

REM Check backend directory exists
if not exist "backend" (
    echo Error: backend directory not found. Please run from project root.
    goto error
)

REM Clean execution
if defined CLEAN (
    echo Cleaning...
    podman run --rm --network=host -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet clean -c %CONFIGURATION%
    if errorlevel 1 (
        echo Clean failed.
        goto error
    )
    echo Clean completed
)

REM Package restore and build execution
echo Restoring packages and building...
podman run --rm --network=host -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet build -c %CONFIGURATION%
if errorlevel 1 (
    echo Build failed.
    goto error
)
echo Build completed

REM Test execution
echo Running tests...
if defined COVERAGE (
    podman run --rm --network=host -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet test -c %CONFIGURATION% --no-build --verbosity normal --collect:"XPlat Code Coverage"
) else (
    podman run --rm --network=host -v "%VOLUME_MOUNT%" -w %WORK_DIR% %DOTNET_IMAGE% dotnet test -c %CONFIGURATION% --no-build --verbosity normal
)

if errorlevel 1 (
    echo Tests failed.
    goto error
)

echo All tests passed!
if defined COVERAGE (
    echo Test coverage report generated.
    echo Report location: backend\TestResults\
)

echo.
echo === Process Completed ===
goto end

:error
echo.
echo === Exited with Error ===
exit /b 1

:end
endlocal