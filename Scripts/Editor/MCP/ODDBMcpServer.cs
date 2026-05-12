using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace TeamODD.ODDB.Editors.MCP
{
    public class ODDBMcpServer
    {
        private HttpListener _listener;
        private Thread _thread;
        private CancellationTokenSource _cts;
        private McpDispatcher _dispatcher;
        private int _port;

        public int Port => _port;
        public bool IsRunning => _listener != null && _listener.IsListening;

        public void Start(string host, int port, McpDispatcher dispatcher)
        {
            if (IsRunning) Stop();
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{host}:{port}/");
            _listener.Start();
            _cts = new CancellationTokenSource();
            _thread = new Thread(() => Loop(_cts.Token)) { IsBackground = true, Name = "ODDB-MCP" };
            _thread.Start();
            McpLog.Info($"server started on http://{host}:{port}");
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Stop(); _listener?.Close(); } catch { }
            try { _thread?.Join(500); } catch { }
            _listener = null;
            _thread = null;
            _cts = null;
            McpLog.Info("server stopped");
        }

        private void Loop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = _listener.GetContext(); }
                catch { break; }
                try { Handle(ctx); }
                catch (Exception ex) { McpLog.Error($"unhandled: {ex}"); }
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                body = reader.ReadToEnd();

            McpRequest request = null;
            McpResponse response;
            try
            {
                request = JsonConvert.DeserializeObject<McpRequest>(body);
                response = _dispatcher.Dispatch(request);
            }
            catch (JsonException)
            {
                response = McpResponse.Failure(null, McpError.ParseError());
            }

            var payload = JsonConvert.SerializeObject(response);
            var bytes = Encoding.UTF8.GetBytes(payload);
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = bytes.LongLength;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();

            McpLog.Info($"{request?.Method ?? "<parse-error>"} → {(response.Error == null ? "ok" : response.Error.Code.ToString())}");
        }
    }
}
