using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TeamODD.ODDB.Cli;

internal static class CliEditorClient
{
    private static string Root(string project) => Path.Combine(Path.GetFullPath(project), "Library", "ODDB", "cli");

    public static bool IsActive(string project)
    {
        var heartbeat = Path.Combine(Root(project), "heartbeat.json");
        if (!File.Exists(heartbeat)) return false;
        try
        {
            var data = JObject.Parse(File.ReadAllText(heartbeat));
            if (data.Value<string>("mode") != "editor") return false;
            var updated = data.Value<DateTime>("updatedUtc");
            return DateTime.UtcNow - updated.ToUniversalTime() < TimeSpan.FromSeconds(5);
        }
        catch { return false; }
    }

    public static bool ProjectMayBeOpen(string project) =>
        File.Exists(Path.Combine(project, "Temp", "UnityLockfile"));

    public static object Execute(string project, string operation, string uri, JObject args)
    {
        var root = Root(project);
        var requests = Path.Combine(root, "requests");
        var responses = Path.Combine(root, "responses");
        Directory.CreateDirectory(requests);
        Directory.CreateDirectory(responses);
        var id = Guid.NewGuid().ToString("N");
        var request = Path.Combine(requests, id + ".json");
        var temporary = request + ".tmp";
        var response = Path.Combine(responses, id + ".json");
        var timeout = operation == "oddb_generate_code" ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(60);
        File.WriteAllText(temporary, JsonConvert.SerializeObject(new
        {
            operation, uri, args,
            expiresUtc = DateTime.UtcNow.Add(timeout)
        }));
        File.Move(temporary, request);
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(response))
            {
                try
                {
                    var data = JObject.Parse(File.ReadAllText(response));
                    if (data.Value<bool>("success")) return data["result"];
                    var error = data["error"];
                    throw new CliRemoteException(error?["code"]?.ToString() ?? "INTERNAL",
                        error?["message"]?.ToString() ?? "Unknown Editor error");
                }
                finally { File.Delete(response); }
            }
            Thread.Sleep(50);
        }
        try { File.Delete(request); } catch { }
        throw new TimeoutException($"Unity Editor did not answer the CLI request within {timeout.TotalSeconds:0} seconds");
    }
}

internal sealed class CliRemoteException : Exception
{
    public string Code { get; }
    public CliRemoteException(string code, string message) : base(message) => Code = code;
}
