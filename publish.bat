@echo off
setlocal
cd /d "%~dp0"
dotnet publish src\TypoSwitch\TypoSwitch.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
if errorlevel 1 exit /b 1
echo.
echo Готово: publish\TypoSwitch.exe
echo Файл можно копировать на другой ПК без установки .NET / .NET Framework.
dir publish\TypoSwitch.exe
