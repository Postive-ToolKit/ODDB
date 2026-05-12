using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.MCP
{
    public delegate McpResponse McpHandler(JToken id, JToken @params);

    public class McpDispatcher
    {
        private readonly Dictionary<string, McpHandler> _handlers = new Dictionary<string, McpHandler>();

        public void Register(string method, McpHandler handler)
        {
            if (string.IsNullOrEmpty(method)) throw new ArgumentNullException(nameof(method));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[method] = handler;
        }

        public McpResponse Dispatch(McpRequest request)
        {
            if (request == null)
                return McpResponse.Failure(null, McpError.InvalidRequest("null request"));

            if (string.IsNullOrEmpty(request.Method))
                return McpResponse.Failure(request.Id, McpError.InvalidRequest("missing method"));

            if (!_handlers.TryGetValue(request.Method, out var handler))
                return McpResponse.Failure(request.Id, McpError.MethodNotFound(request.Method));

            try
            {
                return handler(request.Id, request.Params);
            }
            catch (Exception ex)
            {
                return McpResponse.Failure(request.Id, McpError.Of(McpErrorKind.Internal, ex.Message));
            }
        }
    }
}
