using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PulseFactor.EditorTools
{
    // Salva o resultado de toda execucao do Test Runner em Logs/TestResults-<modo>.txt,
    // para o resultado poder ser lido fora do editor.
    [InitializeOnLoad]
    public static class TestResultSaver
    {
        static TestResultSaver()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
        }

        class Callbacks : ICallbacks
        {
            string mode = "Unknown";

            public void RunStarted(ITestAdaptor testsToRun)
            {
                mode = testsToRun.TestMode.ToString();
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"run: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} mode={mode}");
                sb.AppendLine($"total={result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount} passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}");
                AppendFailures(result, sb);
                Directory.CreateDirectory("Logs");
                File.WriteAllText($"Logs/TestResults-{mode}.txt", sb.ToString());
            }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            static void AppendFailures(ITestResultAdaptor r, StringBuilder sb)
            {
                if (!r.HasChildren && r.TestStatus == TestStatus.Failed)
                    sb.AppendLine($"FAIL {r.FullName}: {r.Message?.Trim()}");
                if (!r.HasChildren) return;
                foreach (var c in r.Children) AppendFailures(c, sb);
            }
        }
    }
}
