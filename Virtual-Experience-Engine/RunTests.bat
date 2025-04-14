@echo off
setlocal

REM Set the working directory to the project path
cd /d "C:\Unity Projects\VE2-Unity-Private\Virtual-Experience-Engine"

REM Set paths
set "PROJECT_PATH=C:\Unity Projects\VE2-Unity-Private\Virtual-Experience-Engine"
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.0.44f1\Editor\Unity.exe"
set "RESULTS_PATH=%PROJECT_PATH%\TestResults.xml"
set "LOG_PATH=%PROJECT_PATH%\test_log.txt"
set "FLAG_PATH=%PROJECT_PATH%\RunEditModeTests.flag"
set "DEBUG_LOG=C:\temp\unity_test_debug.txt"

REM Ensure debug log dir exists
if not exist "C:\temp" mkdir "C:\temp"

echo === RunTests.cmd STARTED === >> "%DEBUG_LOG%"
echo Running Unity EditMode tests... >> "%DEBUG_LOG%"

REM Create flag
echo. > "%FLAG_PATH%"

REM Check if Unity is running
tasklist /FI "IMAGENAME eq Unity.exe" 2>NUL | find /I "Unity.exe" >NUL
if %ERRORLEVEL%==0 (
    echo Unity is already running. Skipping test run. >> "%DEBUG_LOG%"
    echo === RunTests.cmd ENDED === >> "%DEBUG_LOG%"
    exit /b 0
) else (
    echo Unity is not running. Proceeding with test run. >> "%DEBUG_LOG%"
)

REM Run Unity EditMode tests
echo Starting Unity in batchmode... >> "%DEBUG_LOG%"
"%UNITY_EXE%" -runTests -batchmode -projectPath "%PROJECT_PATH%" -testResults "%RESULTS_PATH%" -testPlatform EditMode -logFile "%LOG_PATH%"
echo Unity exited with code %errorlevel% >> "%DEBUG_LOG%"

echo === RunTests.cmd ENDED === >> "%DEBUG_LOG%"

