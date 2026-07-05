using System;
using System.Collections.Generic;
using NUnit.Framework;
using TeamODD.ODDB.Editors.MCP;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class McpServerStartupPolicyTests
    {
        [Test]
        public void TryStartConfiguredPort_DoesNotProbeFallbackPortsWhenBindFails()
        {
            var attemptedPorts = new List<int>();

            var started = McpServerStartupPolicy.TryStartConfiguredPort(
                "127.0.0.1",
                9123,
                null,
                (host, port, dispatcher) =>
                {
                    attemptedPorts.Add(port);
                    throw new InvalidOperationException("port busy");
                },
                out _,
                out var error);

            Assert.That(started, Is.False);
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            Assert.That(attemptedPorts, Is.EqualTo(new[] { 9123 }));
        }

        [Test]
        public void TryStartConfiguredPort_ReturnsStartedServerOnConfiguredPort()
        {
            var attemptedPorts = new List<int>();
            var expectedServer = new ODDBMcpServer();

            var started = McpServerStartupPolicy.TryStartConfiguredPort(
                "127.0.0.1",
                9123,
                null,
                (host, port, dispatcher) =>
                {
                    attemptedPorts.Add(port);
                    return expectedServer;
                },
                out var server,
                out var error);

            Assert.That(started, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(server, Is.SameAs(expectedServer));
            Assert.That(attemptedPorts, Is.EqualTo(new[] { 9123 }));
        }
    }
}
