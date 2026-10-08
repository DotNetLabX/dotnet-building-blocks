using System.Diagnostics;
using System.Text;

namespace Blocks.ForeignSnapshot;

public sealed class GitException(string message) : Exception(message);

public static class Git
{
    public static string Text(string repo, params string[] args) => Encoding.UTF8.GetString(Bytes(repo, args));

    public static byte[] Bytes(string repo, params string[] args)
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

        using var process = Process.Start(start) ?? throw new GitException("git could not be started");
        var stderr = process.StandardError.ReadToEndAsync();
        using var stdout = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(stdout);
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new GitException($"git {string.Join(' ', args)} failed in {repo}: {stderr.Result.Trim()}");

        return stdout.ToArray();
    }
}
