@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
cd /d "%~dp0"

set "APP=src\TypoSwitch\TypoSwitch.csproj"
set "OUT=%~dp0publish"

for /f "usebackq delims=" %%i in (`git describe --tags --match "v*" --abbrev^=0 2^>nul`) do set "TAG=%%i"
if not defined TAG (
  echo Нет git-тега вида v*.*.* — используется версия 0.0.0-dev
  set "VER=0.0.0-dev"
) else (
  set "VER=!TAG:v=!"
)
set "VER_PROP=-p:Version=%VER%"
set "VER_INFO=%VER%"
if /i "%VER:~-4%"=="-dev" set "VER_INFO=0.0.0.0"

set "FULL_NAME=TurboSwitcher %VER%.exe"
set "NET8_NAME=TurboSwitcher %VER%-net8.exe"

echo Версия: %VER%
echo.

echo === Self-contained (runtime внутри) ===
dotnet publish "%APP%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true %VER_PROP% -o "%OUT%\self-contained"
if errorlevel 1 exit /b 1

echo.
echo === Framework-dependent (нужен .NET 8 Desktop Runtime) ===
dotnet publish "%APP%" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -p:EnableCompressionInSingleFile=false %VER_PROP% -o "%OUT%\framework"
if errorlevel 1 exit /b 1

del /q "%OUT%\TurboSwitcher *.exe" 2>nul
copy /Y "%OUT%\self-contained\TurboSwitch.exe" "%OUT%\%FULL_NAME%" >nul
copy /Y "%OUT%\framework\TurboSwitch.exe" "%OUT%\%NET8_NAME%" >nul

echo.
echo Готово:
echo   publish\%FULL_NAME%  — со встроенным .NET, копируется как есть
echo   publish\%NET8_NAME%  — маленький, нужен .NET 8 Desktop Runtime
echo   https://dotnet.microsoft.com/download/dotnet/8.0
echo.
dir "%OUT%\%FULL_NAME%" "%OUT%\%NET8_NAME%"

echo.
echo === Installer (Inno Setup 6) ===
set "ISCC="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"
if defined ISCC (
  "%ISCC%" /DMyAppVersion=%VER% /DMyAppVersionInfo=%VER_INFO% installer\TurboSwitcher.iss
  if errorlevel 1 exit /b 1
  echo   publish\TurboSwitcher Setup %VER%.exe
) else (
  echo Inno Setup 6 не найден — установщик не собран.
  echo Скачать: https://jrsoftware.org/isdl.php
)
