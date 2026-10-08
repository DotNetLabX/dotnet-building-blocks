using System.Text.Json;

namespace Blocks.Sync;

internal sealed record Manifest(string Source, string Commit, string BlocksFolder, string PackagesFile, IReadOnlyList<string> Blocks)
{
    public const string FileName = "blocks.json";

    public static Manifest Load(string appRoot)
    {
        var path = Path.Combine(appRoot, FileName);
        if (!File.Exists(path))
            throw new EnvironmentException($"no {FileName} in {appRoot}");

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new EnvironmentException($"{FileName} cannot be read: {ex.Message}");
        }

        var manifest = new Manifest(
            Text(root, "source"),
            Text(root, "commit"),
            Containment.CheckRelative(Text(root, "blocksFolder"), "blocksFolder"),
            Containment.CheckRelative(Text(root, "packagesFile"), "packagesFile"),
            BlockList(root));

        Containment.Inside(appRoot, manifest.BlocksFolder, "blocksFolder");
        Containment.Inside(appRoot, manifest.PackagesFile, "packagesFile");
        foreach (var block in manifest.Blocks)
            Containment.CheckBlockName(block, FileName);
        return manifest;
    }

    private static string Text(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new EnvironmentException($"{FileName} needs a text value '{name}'");
        }

        return value.GetString()!;
    }

    private static List<string> BlockList(JsonElement root)
    {
        if (!root.TryGetProperty("blocks", out var blocks)
            || blocks.ValueKind != JsonValueKind.Array
            || blocks.EnumerateArray().Any(b => b.ValueKind != JsonValueKind.String))
        {
            throw new EnvironmentException($"{FileName} needs a list of block names 'blocks'");
        }

        return blocks.EnumerateArray().Select(b => b.GetString()!).Distinct(StringComparer.Ordinal).ToList();
    }
}
