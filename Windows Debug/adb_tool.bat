@echo off
:: WARNING: DO NOT NAME THIS FILE "adb.bat"
setlocal EnableDelayedExpansion
title ADB Single-Device Tool Pro
cd /d "%~dp0"

:Menu
cls
echo ========================================
echo        ADB Single-Device Tool
echo ========================================

:: Step 1: Check ADB Environment 
where adb >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] ADB not found in PATH
    pause
    exit /b 1
)

:: Step 2: Capture Single Device & Model 
set "DEV_ID="
set "DEV_MODEL="
for /f "skip=1 tokens=1,2" %%A in ('adb devices 2^>nul') do (
    if "%%B"=="device" (
        set "DEV_ID=%%A"
        for /f "delims=" %%M in ('adb -s %%A shell getprop ro.product.model 2^>nul') do set "DEV_MODEL=%%M"
        goto :HeaderDisplay
    )
)

:HeaderDisplay
if "%DEV_ID%"=="" (
    echo [STATUS] NO DEVICE CONNECTED 
    echo ----------------------------------------
) else (
    echo [MODEL ] %DEV_MODEL%
    echo [SERIAL] %DEV_ID%
    echo ----------------------------------------
)

echo [1] Scan ^& Refresh Status
echo [2] Check Root Permission
echo [3] Check ro.debuggable
echo [4] Enable Debug (resetprop)
echo [5] Enter Root Shell (su)
echo [6] Capture Screenshot to Desktop
echo ----------------------------------------
echo [R] Refresh All  [K] Reset ADB  [Q/E] Exit
echo ========================================

:: Update choice to include '6'
choice /C 123456RKEQ /N /M "Action:"

if errorlevel 10 goto End
if errorlevel 9 goto End
if errorlevel 8 goto Restart 
if errorlevel 7 goto Menu
if errorlevel 6 goto Screenshot
if errorlevel 5 goto RootShell
if errorlevel 4 goto EnableDebug
if errorlevel 3 goto CheckDebug
if errorlevel 2 goto CheckRoot
if errorlevel 1 goto Menu

:Screenshot
if "%DEV_ID%"=="" ( echo [ERROR] No device! & pause & goto Menu )
:: Generate Timestamp 
set "TS=%DATE:~0,4%%DATE:~5,2%%DATE:~8,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
set "TS=%TS: =0%"
set "SAVE_PATH=%USERPROFILE%\Desktop\Temp\Screenshot"
set "FILE_NAME=Screen_%TS%.png"

echo [*] Creating directory: %SAVE_PATH%
if not exist "%SAVE_PATH%" mkdir "%SAVE_PATH%"

echo [*] Capturing screen...
adb -s %DEV_ID% shell screencap -p /sdcard/screen_temp.png
echo [*] Pulling to PC...
adb -s %DEV_ID% pull /sdcard/screen_temp.png "%SAVE_PATH%\%FILE_NAME%"
adb -s %DEV_ID% shell rm /sdcard/screen_temp.png

echo [SUCCESS] Saved to: %SAVE_PATH%\%FILE_NAME%
pause
goto Menu

:CheckRoot
if "%DEV_ID%"=="" ( echo [ERROR] No device! & pause & goto Menu )
set "IS_ROOT=NO"
for /f "tokens=*" %%R in ('adb -s %DEV_ID% shell id 2^>nul') do (
    echo %%R | findstr /C:"uid=0(root)" >nul 
    if !errorlevel! equ 0 set "IS_ROOT=YES"
)
echo [ROOT] !IS_ROOT!
pause
goto Menu

:CheckDebug
if "%DEV_ID%"=="" ( echo [ERROR] No device! & pause & goto Menu )
for /f "delims=" %%D in ('adb -s %DEV_ID% shell getprop ro.debuggable 2^>nul') do (
    echo [DEBUG] ro.debuggable=%%D
)
pause
goto Menu

:EnableDebug
if "%DEV_ID%"=="" ( echo [ERROR] No device! & pause & goto Menu )
echo [*] Restarting Framework... 
adb -s %DEV_ID% shell "resetprop ro.debuggable 1 && stop && start" 2>nul
pause
goto Menu

:RootShell
if "%DEV_ID%"=="" ( echo [ERROR] No device! & pause & goto Menu )
echo [*] Launching Root Shell...
adb -s %DEV_ID% shell su
goto Menu

:Restart 
echo [*] Killing ADB Server...
adb kill-server >nul 2>&1
adb start-server >nul 2>&1
timeout /t 2 >nul
goto Menu

:End 
echo Exiting...
endlocal
exit /b 0