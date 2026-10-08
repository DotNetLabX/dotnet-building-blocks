using System.Diagnostics;

namespace Blocks.Portability.Tests;

internal sealed record ShellResult(int ExitCode, string Output);

internal static class Shell
{
    public static ShellResult Run(string program, string workingDirectory, params string[] args)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
            start.ArgumentList.Add(arg);

        var inherited = start.Environment.Keys
            .Where(k => k.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase) || k.StartsWith("VSTEST", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var key in inherited)
            start.Environment.Remove(key);

        using var process = Process.Start(start)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new ShellResult(process.ExitCode, stdout + stderr.Result);
    }

    public static string Git(string repo, params string[] args)
    {
        string[] settings = ["--no-optional-locks", "-c", "core.autocrlf=false", "-c", "user.name=Portability Check",
            "-c", "user.email=portability@example.test", "-c", "commit.gpgsign=false", "-C", repo];
        var result = Run("git", repo, [.. settings, .. args]);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)}: {result.Output}");
        return result.Output;
    }

    public static void DeleteTree(string root)
    {
        if (!Directory.Exists(root))
            return;

        var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint };
        foreach (var file in Directory.EnumerateFiles(root, "*", options))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(root, recursive: true);
    }
}
