using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace TeamODD.ODDB.Editors.MCP
{
    /// <summary>
    /// Minimal HTTP-only JSON-RPC server. Uses a raw TcpListener instead of
    /// <see cref="HttpListener"/> because Mono's HttpListener has known
    /// reliability issues inside Unity Editor (long blocking accepts, lost
    /// connections after the first request).
    /// </summary>
    public class ODDBMcpServer
    {
        private TcpListener _listener;
        private Thread _thread;
        private CancellationTokenSource _cts;
        private McpDispatcher _dispatcher;
        private int _port;
        private string _host;

        public int Port => _port;
        public bool IsRunning => _listener != null && _thread != null && _thread.IsAlive;

        public void Start(string host, int port, McpDispatcher dispatcher)
        {
            if (IsRunning) Stop();
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _port = port;
            _host = host;
            var addr = IPAddress.Parse(host);
            _listener = new TcpListener(addr, port);
            _listener.Start();
            _cts = new CancellationTokenSource();
            _thread = new Thread(() => Loop(_cts.Token)) { IsBackground = true, Name = "ODDB-MCP" };
            _thread.Start();
            McpLog.Info($"server started on http://{host}:{port}");
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            try { _thread?.Join(2000); } catch { }
            try { _cts?.Dispose(); } catch { }
            _listener = null;
            _thread = null;
            _cts = null;
            McpLog.Info("server stopped");
        }

        private void Loop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try { client = _listener.AcceptTcpClient(); }
                catch { break; }
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { HandleClient(client); }
                    catch (Exception ex) { McpLog.Error($"unhandled: {ex.Message}"); }
                });
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = 5000;
                stream.WriteTimeout = 5000;

                // Read request: header up to \r\n\r\n, then body of Content-Length.
                var headerBytes = new MemoryStream();
                var buf = new byte[1];
                int seq = 0;
                while (seq < 4)
                {
                    int n = stream.Read(buf, 0, 1);
                    if (n <= 0) return;
                    headerBytes.WriteByte(buf[0]);
                    bool match =
                        (seq == 0 || seq == 2) ? buf[0] == (byte)'\r'
                      : (seq == 1 || seq == 3) ? buf[0] == (byte)'\n' : false;
                    seq = match ? seq + 1 : (buf[0] == (byte)'\r' ? 1 : 0);
                }

                var headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
                int contentLength = ParseContentLength(headerText);
                var bodyBytes = new byte[contentLength];
                int read = 0;
                while (read < contentLength)
                {
                    int r = stream.Read(bodyBytes, read, contentLength - read);
                    if (r <= 0) break;
                    read += r;
                }
                var body = Encoding.UTF8.GetString(bodyBytes, 0, read);

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

                var resp = new StringBuilder();
                resp.Append("HTTP/1.1 200 OK\r\n");
                resp.Append("Content-Type: application/json; charset=utf-8\r\n");
                resp.Append($"Content-Length: {bytes.Length}\r\n");
                resp.Append("Connection: close\r\n");
                resp.Append("\r\n");
                var head = Encoding.ASCII.GetBytes(resp.ToString());
                stream.Write(head, 0, head.Length);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();

                McpLog.Info($"{request?.Method ?? "<parse-error>"} → {(response.Error == null ? "ok" : response.Error.Code.ToString())}");
            }
        }

        private static int ParseContentLength(string headerText)
        {
            foreach (var line in headerText.Split(new[] { "\r\n" }, StringSplitOptions.None))
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    var v = line.Substring("Content-Length:".Length).Trim();
                    if (int.TryParse(v, out var cl)) return cl;
                }
            }
            return 0;
        }

    }
}
