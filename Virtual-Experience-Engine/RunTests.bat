@echo off
setlocal enabledelayedexpansion

REM Set paths
set "PROJECT_PATH=C:\Unity Projects\VE2-Unity-Private\Virtual-Experience-Engine"
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.0.44f1\Editor\Unity.exe"
set "RESULTS_PATH=%PROJECT_PATH%\TestLogs\TestResults.xml"
set "LOG_PATH=%PROJECT_PATH%\TestLogs\test_log.txt"
set "FLAG_PATH=%PROJECT_PATH%\RunEditModeTests.flag"
set "DEBUG_LOG=C:\temp\unity_test_debug.txt"

REM Ensure debug log dir exists
if not exist "C:\temp" mkdir "C:\temp"

echo === RunTests.cmd STARTED === >> "%DEBUG_LOG%"
echo Running Unity EditMode tests... >> "%DEBUG_LOG%"

REM Check if Unity is running
tasklist /fi "IMAGENAME eq Unity.exe" | findstr /B /I "Unity.exe" >NUL
if %ERRORLEVEL%==0 (
    echo Unity is already running. Skipping test run. >> "%DEBUG_LOG%"
    echo. > "%FLAG_PATH%"
    echo === RunTests.cmd ENDED === >> "%DEBUG_LOG%"
    exit /b 0
) else (
    echo Unity is not running. Proceeding with test run. >> "%DEBUG_LOG%"
)

if not defined FROM_START (
    set FROM_START=1
    start "" cmd /c "%~f0"
    exit /b
)

:: Ask user to confirm
echo Run Unity tests? [Y/N]
choice /c YN /n

:: Check what the user chose
if errorlevel 2 (
    exit /b 0
)

echo You chose Yes. Proceeding with test run...

REM Run Unity EditMode tests
"%UNITY_EXE%" -runTests -batchmode -projectPath "%PROJECT_PATH%" -testResults "%RESULTS_PATH%" -testPlatform EditMode -logFile "%LOG_PATH%"


:: Parse the TestResults.xml file for passed and failed tests
for /f "tokens=2 delims=<>" %%a IN ('findstr "passed=" "%RESULTS_PATH%"') DO (
    Set LINE=%%a 
    goto parse2
)

:parse2
for /f "tokens=27 delims== " %%a in ('echo !LINE! ^| findstr /i "passed="') do (
    set PASSED=%%a
)

rem Extract the value of 'failed'
for /f "tokens=29 delims== " %%a in ('echo !LINE! ^| findstr /i "failed="') do (
    set FAILED=%%a
)

set PASSED=%PASSED:"=%
set FAILED=%FAILED:"=%

echo Running Tests completed.
echo Tests Passed: !PASSED!
echo Tests Failed: !FAILED!

timeout /t 5 >nul
exit

