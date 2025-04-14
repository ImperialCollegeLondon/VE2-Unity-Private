@echo off
setlocal

REM Set your project path
set "PROJECT_PATH=C:\Unity Projects\VE2-Unity-Private\Virtual-Experience-Engine"

REM Set your Unity executable path
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.0.44f1\Editor\Unity.exe"

REM Set output path for test results
set "RESULTS_PATH=%PROJECT_PATH%\TestResults.xml"

REM Run Unity in batch mode with tests
"%UNITY_EXE%" -runTests -batchmode -projectPath "%PROJECT_PATH%" -testResults "%RESULTS_PATH%" -testPlatform EditMode -logFile "%PROJECT_PATH%\test_log.txt" -testPlatform EditMode

echo Exit code: %errorlevel%
pause