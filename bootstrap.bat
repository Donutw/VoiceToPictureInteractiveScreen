@echo off
setlocal

REM 目标版本
set VERSION=0.16.1

REM 下载 URL
set URL=https://github.com/homuler/MediaPipeUnityPlugin/releases/download/v%VERSION%/MediaPipeUnity.%VERSION%.unitypackage

REM 本地保存路径
set OUTDIR=Packages\LocalPackages
set OUTFILE=com.github.homuler.mediapipe-%VERSION%.tgz

REM 确保目录存在
if not exist "%OUTDIR%" (
    mkdir "%OUTDIR%"
)

REM 下载
echo Downloading MediaPipe Unity Plugin v%VERSION%...
powershell -Command "Invoke-WebRequest -Uri '%URL%' -OutFile '%OUTDIR%\%OUTFILE%'"

if errorlevel 1 (
    echo Download error，please check network。
    exit /b 1
)

echo Download success，file stored at %OUTDIR%\%OUTFILE%.

endlocal