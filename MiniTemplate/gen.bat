@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "WORKSPACE=%SCRIPT_DIR%.."
set "LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll"
set "CONF_ROOT=%SCRIPT_DIR%"

if not exist "%LUBAN_DLL%" (
    echo Luban tool was not found: %LUBAN_DLL%
    exit /b 1
)

dotnet "%LUBAN_DLL%" ^
    -t all ^
    -d json ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -x outputDataDir="%WORKSPACE%\Assets\TableDatas"

if errorlevel 1 exit /b %errorlevel%

dotnet "%LUBAN_DLL%" ^
    -t all ^
    -c cs-newtonsoft-json ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -x outputCodeDir="%CONF_ROOT%..\Assets\Scripts\Luban"

endlocal