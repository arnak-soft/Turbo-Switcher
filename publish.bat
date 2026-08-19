@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"

set "APP=src\TypoSwitch\TypoSwitch.csproj"
set "OUT=%~dp0publish"

for /f "usebackq delims=" %%i in (`dotnet msbuild "%APP%" -nologo -getProperty:Version`) do set "VER=%%i"
if not defined VER (
  echo Не удалось прочитать Version из проекта.
  exit /b 1
)
set "VER=%VER: =%"

set "FULL_NAME=TurboSwitcher %VER%.exe"
set "NET8_NAME=TurboSwitcher %VER%-net8.exe"

echo Версия: %VER%
echo.

echo === Self-contained (runtime внутри) ===
dotnet publish "%APP%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -o "%OUT%\self-contained"
if errorlevel 1 exit /b 1

echo.
echo === Framework-dependent (нужен .NET 8 Desktop Runtime) ===
dotnet publish "%APP%" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -p:EnableCompressionInSingleFile=false -o "%OUT%\framework"
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
