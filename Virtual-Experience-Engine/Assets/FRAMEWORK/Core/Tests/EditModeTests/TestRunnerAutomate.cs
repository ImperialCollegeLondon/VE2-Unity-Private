#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class TestRunnerAutomate
{
    static TestRunnerAutomate()
    {
        EditorApplication.update += CheckForTestSignal;
        EditorApplication.quitting += () => EditorApplication.update -= CheckForTestSignal;
    }

    static void CheckForTestSignal()
    {
        string signalFile = "RunEditModeTests.flag";
        if (File.Exists(signalFile))
        {
            File.Delete(signalFile);
            RunTests();
        }
    }

    static void RunTests()
    {
        TestRunnerApi testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        testRunnerApi.RegisterCallbacks(new TestRunnerApiCallbacks());
        Filter filter = new()
        {
            testMode = TestMode.EditMode
        };
        testRunnerApi.Execute(new ExecutionSettings(filter));
    }
}

class TestRunnerApiCallbacks : ICallbacks
{
    public static Dictionary<ITestAdaptor, ITestResultAdaptor> testResults = new Dictionary<ITestAdaptor, ITestResultAdaptor>();
    public void RunStarted(ITestAdaptor testsToRun) 
    { 
        TestResultsWindow.ShowWindow();
        TestResultsWindow.UpdateWindow("Running tests...");
        TestRunnerWindow.ShowWindow();
    }

    public void RunFinished(ITestResultAdaptor result) 
    { 
        string results = $"Test run finished.\n" +
                         $"Passed: {result.PassCount}, Failed: {result.FailCount}, Inconclusive: {result.InconclusiveCount}\n";
        TestResultsWindow.UpdateWindow(results);

        if (result.FailCount > 0)
        {
            foreach (ITestResultAdaptor testResult in testResults.Values.Where(r => r.TestStatus == TestStatus.Failed))
            {
                results += $"Test failed: {testResult.Test.FullName}: {testResult.Message}\n";
            }

            TestResultsWindow.UpdateWindow(results);
        }
        else
        {
            results += "All tests passed!";
            TestResultsWindow.UpdateWindow(results);
            //TestResultsWindow.CloseWindow();
        }
    }

    public void TestStarted(ITestAdaptor test) 
    { 

    }

    public void TestFinished(ITestResultAdaptor result)
    {
        testResults.Add(result.Test, result);
    }
}
#endif
