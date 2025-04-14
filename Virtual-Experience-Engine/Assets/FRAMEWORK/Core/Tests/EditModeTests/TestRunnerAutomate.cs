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
    public void RunStarted(ITestAdaptor testsToRun) 
    {
        TestRunnerWindow.ShowWindow();
    }

    public void RunFinished(ITestResultAdaptor result) 
    { 

    }

    public void TestStarted(ITestAdaptor test) 
    { 

    }

    public void TestFinished(ITestResultAdaptor result)
    {

    }
}
#endif
