@echo off
setlocal EnableDelayedExpansion
:: 窗口配置
title Unity APK Deployment Tool v2.6 (Loop Mode)
color 0B

:: 锚定上下文
pushd "%~dp0"

:: --- [模块] 设备状态自检 (仅启动时检查一次) ---
:CheckDevice
cls
echo =================================================================
echo                    设备连接状态检查 (Device Check)
echo =================================================================
echo.

tasklist | find "adb.exe" >nul
if %errorlevel% neq 0 (
    echo [Info] Starting ADB Server...
    adb start-server >nul
)

adb devices | findstr /C:"device" | findstr /V /C:"List of" >nul

if %errorlevel% EQU 0 (
    goto :MainMenu
) else (
    color 0C
    echo [Status] ?? 未检测到有效设备!
    echo.
    echo -------------------------------------------------------------
    echo  [排查清单]:
    echo  1. USB 线是否插好?
    echo  2. 手机是否弹出 "允许 USB 调试"? (必须点允许)
    echo -------------------------------------------------------------
    echo.
    echo [ADB 原始返回]:
    adb devices
    echo.
    echo =================================================================
    echo  [R] 重试 (Retry)    [X] 退出 (Exit)
    echo =================================================================
    set /p retry=">> 请输入指令: "
    if /i "!retry!"=="r" (
        color 0B
        goto :CheckDevice
    )
    exit
)

:: --- [模块] 主菜单 (循环回归点) ---
:MainMenu
color 0B
cls
echo =================================================================
echo                    Unity APK 自动化部署工具 (ADB)
echo             [WorkDir]: %~dp0
echo =================================================================
echo.
echo  [操作指南 / Instructions]:
echo.
echo    [1] 自动模式 (Auto-Latest)
echo        - 机制: 自动安装修改时间最新的 APK 包。
echo        - 场景: Daily Build 快速验证。
echo.
echo    [2] 手动模式 (Manual-Select)
echo        - 机制: 显示列表，手动输入文件名。
echo        - 场景: 回滚版本 (Regression) 测试。
echo.
echo =================================================================
echo  * 设备状态: (已连接) Online
echo =================================================================
echo.

set /p choice=">> 请输入指令 (1 或 2) [输入 x 退出]: "

if /i "%choice%"=="x" goto :End
if "%choice%"=="1" goto :FindLatest
if "%choice%"=="2" goto :ManualInput
echo [Error] 指令无效。 & pause >nul & goto :MainMenu

:: --- [模块] 自动查找 ---
:FindLatest
echo.
echo [Sys] Scanning directory for latest APK...
set "TARGET_APK="
for /f "delims=" %%i in ('dir *.apk /b /a-d /o-d') do (
    set "TARGET_APK=%%i"
    goto :ExecuteInstall
)

if "%TARGET_APK%"=="" (
    color 0C
    echo [Error] 当前目录下未发现 .apk 文件!
    pause
    goto :MainMenu
)

:: --- [模块] 手动输入 ---
:ManualInput
echo.
echo [File List]:
echo -----------------------------------
dir *.apk /b /o-d
echo -----------------------------------
echo.
set /p TARGET_APK=">> 请输入完整文件名 (支持右键粘贴): "

if not exist "%TARGET_APK%" (
    color 0C
    echo.
    echo [Error] 文件 '%TARGET_APK%' 不存在。
    pause
    goto :MainMenu
)

:: --- [模块] 执行安装 ---
:ExecuteInstall
cls
color 07
echo =================================================================
echo  [Deploying Package]
echo  Target : %TARGET_APK%
echo  Command: adb install -r -d -t -g
echo =================================================================
echo.
echo  Processing... (正在推流安装，请勿断开 USB)

adb install -r -d -t -g "%TARGET_APK%"

if %ERRORLEVEL% EQU 0 goto :InstallSuccess
goto :InstallFailed

:InstallSuccess
    color 0A
    echo.
    echo =================================================================
    echo  [SUCCESS] 安装完成 (Install Complete)
    echo =================================================================
    goto :PostInstallMenu

:InstallFailed
    color 0C
    echo.
    echo =================================================================
    echo  [FAILED] 安装失败 (Install Failed)
    echo  ErrorCode: %ERRORLEVEL%
    echo =================================================================
    goto :PostInstallMenu

:: --- [新增模块] 任务后菜单 (实现不关闭窗口) ---
:PostInstallMenu
echo.
echo -----------------------------------------------------------------
echo  [Enter] 返回主菜单 (Continue)     [X] 退出脚本 (Exit)
echo -----------------------------------------------------------------
set /p post_choice=">> 请按回车继续，或输入 x 退出: "

if /i "%post_choice%"=="x" goto :End
goto :MainMenu

:End
popd
exit