@echo off
setlocal
set "CBM_EXECUTABLE=%LOCALAPPDATA%\Programs\codebase-memory-mcp\codebase-memory-mcp.exe"
if not exist "%CBM_EXECUTABLE%" (
    echo Codebase Memory managed runtime not found: "%CBM_EXECUTABLE%" 1>&2
    exit /b 1
)
"%CBM_EXECUTABLE%" %*
exit /b %ERRORLEVEL%
