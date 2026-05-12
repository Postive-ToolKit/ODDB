using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.MCP
{
    public class McpRequest
    {
        [JsonProperty("jsonrpc")] public string JsonRpc = "2.0";
        [JsonProperty("id")] public JToken Id;        // string or number; nullable for notifications
        [JsonProperty("method")] public string Method;
        [JsonProperty("params")] public JToken Params;
    }

    public class McpResponse
    {
        [JsonProperty("jsonrpc")] public string JsonRpc = "2.0";
        [JsonProperty("id")] public JToken Id;
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)] public object Result;
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)] public McpError Error;

        public static McpResponse Success(JToken id, object result) =>
            new McpResponse { Id = id, Result = result };

        public static McpResponse Failure(JToken id, McpError error) =>
            new McpResponse { Id = id, Error = error };
    }

    public class McpError
    {
        [JsonProperty("code")] public int Code;
        [JsonProperty("message")] public string Message;
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)] public object Data;

        public static McpError Of(McpErrorKind kind, string message, object details = null)
        {
            return new McpError
            {
                Code = -32000,
                Message = message,
                Data = new { kind = kind.ToWireString(), details },
            };
        }

        public static McpError MethodNotFound(string method) => new McpError
        {
            Code = -32601,
            Message = $"Method not found: {method}",
        };

        public static McpError ParseError() => new McpError { Code = -32700, Message = "Parse error" };
        public static McpError InvalidRequest(string detail) => new McpError { Code = -32600, Message = $"Invalid request: {detail}" };
    }

    public class McpException : Exception
    {
        public McpErrorKind Kind { get; }
        public object Details { get; }

        public McpException(McpErrorKind kind, string message, object details = null) : base(message)
        {
            Kind = kind;
            Details = details;
        }
    }
}
