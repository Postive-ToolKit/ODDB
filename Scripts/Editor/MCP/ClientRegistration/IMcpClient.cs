namespace TeamODD.ODDB.Editors.MCP.ClientRegistration
{
    /// <summary>
    /// Auto-configuration target for the ODDB MCP server. An IMcpClient knows
    /// where its host application stores MCP server entries and how to add or
    /// remove the ODDB entry without disturbing other servers.
    /// </summary>
    public interface IMcpClient
    {
        string DisplayName { get; }
        string ConfigPath { get; }   // resolved per-OS absolute path
        void Register(string key, string url);
        void Unregister(string key);
    }
}
