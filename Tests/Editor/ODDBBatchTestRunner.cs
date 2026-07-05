using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TeamODD.ODDB.Tests.Editor
{
    public static class ODDBBatchTestRunner
    {
        private const string ResultsPathArg = "-oddbTestResults";
        private const string TestAssemblyName = "ODDB.Tests.Editor";
        private static BatchCallbacks _callbacks;
        private static double _deadline;

        public static void RunEditModeTests()
        {
            _deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update -= WaitForCompilationAndRun;
            EditorApplication.update += WaitForCompilationAndRun;
        }

        private static void WaitForCompilationAndRun()
        {
            if (EditorApplication.isCompiling)
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                {
                    Debug.LogError("[ODDBBatchTestRunner] Timed out waiting for script compilation.");
                    EditorApplication.Exit(2);
                }
                return;
            }

            EditorApplication.update -= WaitForCompilationAndRun;
            ExecuteTests();
        }

        private static void ExecuteTests()
        {
            try
            {
                var resultsPath = GetResultsPath();
                _callbacks = new BatchCallbacks(resultsPath);

                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.RegisterCallbacks(_callbacks);

                var settings = new ExecutionSettings(new Filter
                {
                    testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
                    assemblyNames = new[] { TestAssemblyName },
                })
                {
                    runSynchronously = true,
                };

                Debug.Log($"[ODDBBatchTestRunner] Running EditMode tests in {TestAssemblyName}.");
                api.Execute(settings);

                if (!_callbacks.HasFinished)
                {
                    Debug.LogError("[ODDBBatchTestRunner] Test run returned without RunFinished callback.");
                    EditorApplication.Exit(2);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(2);
            }
        }

        private static string GetResultsPath()
        {
            var explicitPath = GetArgValue(ResultsPathArg);
            var path = string.IsNullOrWhiteSpace(explicitPath)
                ? Path.Combine(Environment.CurrentDirectory, "Logs", "oddb-editmode-tests.xml")
                : explicitPath;

            path = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            return path;
        }

        private static string GetArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == name && i + 1 < args.Length)
                    return args[i + 1];

                var prefix = name + "=";
                if (args[i].StartsWith(prefix, StringComparison.Ordinal))
                    return args[i].Substring(prefix.Length);
            }

            return null;
        }

        private sealed class BatchCallbacks : ICallbacks
        {
            private readonly string _resultsPath;

            public BatchCallbacks(string resultsPath)
            {
                _resultsPath = resultsPath;
            }

            public bool HasFinished { get; private set; }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"[ODDBBatchTestRunner] Test cases discovered: {testsToRun.TestCaseCount}.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                HasFinished = true;
                TestRunnerApi.SaveResultToFile(result, _resultsPath);

                var total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
                var failed = result.FailCount + result.InconclusiveCount;
                Debug.Log(
                    $"[ODDBBatchTestRunner] Results saved to {_resultsPath}. " +
                    $"Passed={result.PassCount}, Failed={result.FailCount}, " +
                    $"Skipped={result.SkipCount}, Inconclusive={result.InconclusiveCount}.");

                EditorApplication.Exit(total == 0 || failed > 0 ? 1 : 0);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite || result.FailCount == 0)
                    return;

                Debug.LogError($"[ODDBBatchTestRunner] Failed: {result.FullName}\n{result.Message}\n{result.StackTrace}");
            }
        }
    }
}
