using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.MCP;
using TeamODD.ODDB.Editors.MCP.Resources;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class McpMainThreadTests
    {
        [SetUp]
        public void SetUp() => McpMainThread.ResetForTesting();

        [TearDown]
        public void TearDown() => McpMainThread.ResetForTesting();

        [Test]
        public void Run_WhenQueuedWorkTimesOut_DoesNotExecuteItLater()
        {
            var executionCount = 0;
            var request = Task.Run(() => McpMainThread.Run(() => ++executionCount, 30));

            Assert.That(
                () => request.GetAwaiter().GetResult(),
                Throws.TypeOf<TimeoutException>());

            McpMainThread.PumpPendingForTesting();

            Assert.That(executionCount, Is.Zero);
        }

        [Test]
        public void ResourceRead_IsExecutedOnUnityMainThread()
        {
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var resource = new RecordingResource();
            var request = Task.Run(() => ODDBEditorRuntime.ReadResourceOnMainThread(resource, "oddb://test"));
            Assert.That(
                SpinWait.SpinUntil(() => McpMainThread.PendingCountForTesting > 0, 1_000),
                Is.True,
                "Resource read was not queued for the Unity main thread.");

            McpMainThread.PumpPendingForTesting();

            Assert.That(request.GetAwaiter().GetResult(), Is.EqualTo("ok"));
            Assert.That(resource.ThreadId, Is.EqualTo(mainThreadId));
        }

        private sealed class RecordingResource : IMcpResource
        {
            public int ThreadId { get; private set; }
            public string UriOrTemplate => "oddb://test";
            public string Description => "test";
            public string MimeType => "application/json";
            public bool TryMatch(string uri) => true;

            public object Read(string uri)
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                return "ok";
            }
        }
    }
}
