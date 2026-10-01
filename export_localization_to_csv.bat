@echo off
setlocal

set "LANG_CODE="
set /p "LANG_CODE=Language code (leave blank for jpn): "
if not defined LANG_CODE set "LANG_CODE=jpn"

python3 "%~dp0convert_localization_to_csv.py" "%LANG_CODE%"
if errorlevel 1 (
    echo.
    echo [ERROR] Conversion failed. Check the language code and Python installation.
    pause
    exit /b 1
)

echo.
pause
