@echo off
setlocal
set "compiler=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%compiler%" (
    echo The .NET Framework x64 C# compiler was not found. 1>&2
    exit /b 1
)
if not exist "%~dp0build" mkdir "%~dp0build"
if errorlevel 1 exit /b 1
"%compiler%" /nologo /optimize+ /target:exe /platform:x64 /out:"%~dp0build\Patch-OriginOleOverline.exe" "%~dp0src\Patch-OriginOleOverline.cs"
exit /b %errorlevel%
