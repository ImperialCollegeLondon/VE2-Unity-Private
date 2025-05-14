#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace VE2.Core.Tests
{
    [InitializeOnLoad]
    internal static class TestRunnerAutomate
    {
        private static TestRunnerApi _testRunnerApi;

        static TestRunnerAutomate()
        {
            _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
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
            _testRunnerApi.RegisterCallbacks(new TestRunnerApiCallbacks());
            Filter filter = new()
            {
                testMode = TestMode.EditMode
            };
            _testRunnerApi.Execute(new ExecutionSettings(filter));
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
}
#endif
