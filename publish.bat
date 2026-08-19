@echo off
setlocal
cd /d "%~dp0"

set "APP=src\TypoSwitch\TypoSwitch.csproj"
set "OUT=%~dp0publish"

echo === Self-contained (runtime внутри) ===
dotnet publish "%APP%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -o "%OUT%\self-contained"
if errorlevel 1 exit /b 1

echo.
echo === Framework-dependent (нужен .NET 8 Desktop Runtime) ===
dotnet publish "%APP%" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -p:EnableCompressionInSingleFile=false -o "%OUT%\framework"
if errorlevel 1 exit /b 1

copy /Y "%OUT%\self-contained\TurboSwitch.exe" "%OUT%\TurboSwitch.exe" >nul
copy /Y "%OUT%\framework\TurboSwitch.exe" "%OUT%\TurboSwitch-net8.exe" >nul

echo.
echo Готово:
echo   publish\TurboSwitch.exe       — со встроенным .NET, копируется как есть
echo   publish\TurboSwitch-net8.exe  — маленький, нужен .NET 8 Desktop Runtime
echo   https://dotnet.microsoft.com/download/dotnet/8.0
echo.
dir "%OUT%\TurboSwitch.exe" "%OUT%\TurboSwitch-net8.exe"
