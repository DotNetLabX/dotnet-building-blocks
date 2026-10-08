namespace Blocks.Sync;

public static class SyncCli
{
    private const string Usage = "usage: {forward|back|status} --app <path> [--source <blocks-repo-path>] [--adopt] [--json]";

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (!TryParse(args, out var options))
        {
            stderr.WriteLine(Usage);
            return 2;
        }

        try
        {
            var source = options.Source ?? DefaultSource();
            var session = SyncSession.Open(options.App, source);
            return options.Command switch
            {
                "forward" => ForwardCommand.Run(session, options.Adopt, options.Json, stdout, stderr),
                "back" => BackCommand.Run(session, stdout, stderr),
                _ => StatusCommand.Run(session, stdout),
            };
        }
        catch (RefusedException ex)
        {
            stderr.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex) when (ex is EnvironmentException or GitException or IOException or UnauthorizedAccessException)
        {
            stderr.WriteLine(ex.Message);
            return 2;
        }
    }

    private sealed record Options(string Command, string App, string? Source, bool Adopt, bool Json);

    private static bool TryParse(string[] args, out Options options)
    {
        options = null!;
        if (args.Length == 0 || args[0] is not ("forward" or "back" or "status"))
            return false;

        string? app = null;
        string? source = null;
        bool adopt = false, json = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--app" when i + 1 < args.Length:
                    if (!TryFullPath(args[++i], out app))
                        return false;
                    break;
                case "--source" when i + 1 < args.Length:
                    if (!TryFullPath(args[++i], out source))
                        return false;
                    break;
                case "--adopt":
                    adopt = true;
                    break;
                case "--json":
                    json = true;
                    break;
                default:
                    return false;
            }
        }

        if (app is null || ((adopt || json) && args[0] != "forward"))
            return false;

        options = new Options(args[0], app, source, adopt, json);
        return true;
    }

    private static bool TryFullPath(string path, out string? fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            fullPath = Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string DefaultSource()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Blocks.slnx")))
                return folder.FullName;
        }

        throw new EnvironmentException("no blocks checkout found above " + AppContext.BaseDirectory + "; pass --source");
    }
}
