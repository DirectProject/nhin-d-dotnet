@echo off
title Setting up EventLog sources...
echo Setting up EventLog sources...

REM Use -ExecutionPolicy Bypass and -NoProfile to avoid user profile execution policy issues
for /F "eol=; tokens=1,2* delims=," %%i in (event-sources.txt) do powershell -ExecutionPolicy Bypass -NoProfile -File createeventlogsource.ps1 %%i "%%j"
if %ERRORLEVEL% NEQ 0 goto :error
echo Succeeded
goto :done

:error
echo Failed createeventlogsource.bat.  See logs... > CON
pause

:done
echo ******************
echo.
echo Script complete. Please review status messages.
echo.
echo ******************

