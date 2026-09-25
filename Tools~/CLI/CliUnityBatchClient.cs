using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Cli;

internal static class CliUnityBatchClient
{
    public static object Execute(string project, string operation, string uri, JObject args, string unityOverride)
    {
        var unity = ResolveUnity(project, unityOverride);
        var folder = Path.Combine(Path.GetFullPath(project), "Library", "ODDB", "cli", "batch");
        Directory.CreateDirectory(folder);
        var id = Guid.NewGuid().ToString("N");
        var requestPath = Path.Combine(folder, id + ".request.json");
        var responsePath = Path.Combine(folder, id + ".response.json");
        var logPath = Path.Combine(folder, id + ".unity.log");
        File.WriteAllText(requestPath, JsonConvert.SerializeObject(new { operation, uri, args }));
        try
        {
            var start = new ProcessStartInfo(unity)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in new[]
            {
                "-batchmode", "-nographics", "-quit", "-projectPath", Path.GetFullPath(project),
                "-executeMethod", "TeamODD.ODDB.Editors.CLI.ODDBCliBatchRunner.Run",
                "-oddbCliRequest", requestPath, responsePath, "-logFile", logPath
            }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new IOException("Unity process did not start");
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(300_000))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"Unity CLI command timed out; log: {logPath}");
            }
            Task.WaitAll(output, errors);
            if (!File.Exists(responsePath))
                throw new IOException($"Unity did not produce a CLI response (exit {process.ExitCode}); log: {logPath}");
            var data = JObject.Parse(File.ReadAllText(responsePath));
            if (!data.Value<bool>("success"))
                throw new CliRemoteException(data["error"]?["code"]?.ToString() ?? "INTERNAL",
                    data["error"]?["message"]?.ToString() ?? "Unknown Unity error");
            return data["result"];
        }
        finally
        {
            try { File.Delete(requestPath); } catch { }
            try { File.Delete(responsePath); } catch { }
        }
    }

    private static string ResolveUnity(string project, string explicitPath)
    {
        var supplied = explicitPath ?? Environment.GetEnvironmentVariable("ODDB_UNITY_PATH");
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            if (!File.Exists(supplied)) throw new FileNotFoundException("Unity executable was not found", supplied);
            return supplied;
        }
        var versionFile = Path.Combine(project, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(versionFile)) throw new FileNotFoundException("Unity ProjectVersion.txt was not found", versionFile);
        var match = Regex.Match(File.ReadAllText(versionFile), @"(?m)^m_EditorVersion:\s*(\S+)");
        if (!match.Success) throw new InvalidDataException("Unity editor version is missing from ProjectVersion.txt");
        var version = match.Groups[1].Value;
        string candidate;
        if (OperatingSystem.IsWindows())
            candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor", version, "Editor", "Unity.exe");
        else if (OperatingSystem.IsMacOS())
            candidate = Path.Combine("/Applications/Unity/Hub/Editor", version, "Unity.app/Contents/MacOS/Unity");
        else
            candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Unity", "Hub", "Editor", version, "Editor", "Unity");
        if (!File.Exists(candidate)) throw new FileNotFoundException("Unity executable was not found; pass --unity or set ODDB_UNITY_PATH", candidate);
        return candidate;
    }
}
