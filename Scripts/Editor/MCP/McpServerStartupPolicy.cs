using System;

namespace TeamODD.ODDB.Editors.MCP
{
    internal delegate ODDBMcpServer McpServerStartAttempt(string host, int port, McpDispatcher dispatcher);

    internal static class McpServerStartupPolicy
    {
        public static bool TryStartConfiguredPort(
            string host,
            int configuredPort,
            McpDispatcher dispatcher,
            McpServerStartAttempt startAttempt,
            out ODDBMcpServer server,
            out Exception error)
        {
            if (startAttempt == null) throw new ArgumentNullException(nameof(startAttempt));

            server = null;
            error = null;

            try
            {
                server = startAttempt(host, configuredPort, dispatcher);
                if (server != null) return true;

                error = new InvalidOperationException("MCP server start returned null.");
                return false;
            }
            catch (Exception ex)
            {
                server = null;
                error = ex;
                return false;
            }
        }
    }
}
