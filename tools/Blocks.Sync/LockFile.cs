using System.Text.Json;
using System.Text.RegularExpressions;

namespace Blocks.Sync;

internal sealed record LockEntry(string Commit, SortedDictionary<string, string> Files);

internal sealed partial class LockFile
{
    public const string FileName = "blocks.lock.json";

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex FingerprintPattern();

    public SortedDictionary<string, LockEntry> Blocks { get; } = new(StringComparer.Ordinal);

    public static LockFile Load(string appRoot, string blocksFolder)
    {
        var lockFile = new LockFile();
        var path = Path.Combine(appRoot, FileName);
        if (!File.Exists(path))
            return lockFile;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Object)
                throw Unreadable("it has no 'blocks' object");

            foreach (var block in blocks.EnumerateObject())
                lockFile.Blocks[block.Name] = ReadEntry(block, appRoot, blocksFolder);
        }
        catch (JsonException ex)
        {
            throw Unreadable(ex.Message);
        }

        return lockFile;
    }

    private static LockEntry ReadEntry(JsonProperty block, string appRoot, string blocksFolder)
    {
        Containment.CheckBlockName(block.Name, FileName);
        var value = block.Value;
        if (value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty("commit", out var commit) || commit.ValueKind != JsonValueKind.String
            || !value.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Object)
        {
            throw Unreadable($"{block.Name} needs a 'commit' text and a 'files' object");
        }

        var folder = Path.Combine(appRoot, blocksFolder, block.Name);
        if (!Directory.Exists(folder))
            throw new RefusedException($"refused: {FileName} has an entry for {block.Name} but the app has no folder {folder}");

        var entries = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files.EnumerateObject())
        {
            Containment.Inside(folder, file.Name, $"{FileName} path of {block.Name}");
            if (Containment.CheckRelative(file.Name, $"{FileName} path of {block.Name}") != file.Name || !BlockVersion.IsInFileSet(file.Name)
                || file.Value.ValueKind != JsonValueKind.String || !FingerprintPattern().IsMatch(file.Value.GetString()!))
            {
                throw Unreadable($"{block.Name}: '{file.Name}' is not an entry of the block's files");
            }

            entries[file.Name] = file.Value.GetString()!;
        }

        return new LockEntry(commit.GetString()!, entries);
    }

    private static RefusedException Unreadable(string reason) => new($"refused: {FileName} cannot be read: {reason}");

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            writer.WriteStartObject();
            writer.WriteStartObject("blocks");
            foreach (var (name, entry) in Blocks)
            {
                writer.WriteStartObject(name);
                writer.WriteString("commit", entry.Commit);
                writer.WriteStartObject("files");
                foreach (var (path, fingerprint) in entry.Files)
                    writer.WriteString(path, fingerprint);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        stream.WriteByte((byte)'\n');
        return stream.ToArray();
    }
}
