using System.Diagnostics;
using System.Text;

namespace Blocks.Sync;

internal sealed class GitException(string message) : Exception(message);

internal static class Git
{
    public static string Text(string repo, params string[] args) => Encoding.UTF8.GetString(Bytes(repo, args));

    public static bool TryText(string repo, out string text, params string[] args)
    {
        var (exitCode, output, _) = Execute(repo, args);
        text = Encoding.UTF8.GetString(output);
        return exitCode == 0;
    }

    public static byte[] Bytes(string repo, params string[] args)
    {
        var (exitCode, output, error) = Execute(repo, args);
        if (exitCode != 0)
            throw new GitException($"git {string.Join(' ', args)} failed in {repo}: {error.Trim()}");
        return output;
    }

    private static (int ExitCode, byte[] Output, string Error) Execute(string repo, string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("--no-optional-locks");
        start.ArgumentList.Add("-C");
        start.ArgumentList.Add(repo);
        foreach (var arg in args)
            start.ArgumentList.Add(arg);

        Process process;
        try
        {
            process = Process.Start(start) ?? throw new GitException("git could not be started");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new GitException($"git could not be started: {ex.Message}");
        }

        using (process)
        {
            var stderr = process.StandardError.ReadToEndAsync();
            using var stdout = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(stdout);
            process.WaitForExit();
            return (process.ExitCode, stdout.ToArray(), stderr.Result);
        }
    }
}
