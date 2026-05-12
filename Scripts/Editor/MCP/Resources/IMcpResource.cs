namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public interface IMcpResource
    {
        // Static resources return a fixed URI; dynamic ones return a URI template like "oddb://views/{id}".
        string UriOrTemplate { get; }
        string Description { get; }
        string MimeType { get; }     // typically "application/json"

        // For static resources, uri == UriOrTemplate.
        // For templated resources, uri is the concrete URI requested by the client.
        bool TryMatch(string uri);
        object Read(string uri);     // returns payload; throws McpException for errors
    }
}
