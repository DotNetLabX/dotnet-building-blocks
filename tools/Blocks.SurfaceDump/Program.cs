using System.Text;
using Blocks.SurfaceDump;

var assemblies = new List<string>();
var probes = new List<string>();
string? output = null;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--probe" when i + 1 < args.Length:
            probes.Add(args[++i]);
            break;
        case "--out" when i + 1 < args.Length:
            output = args[++i];
            break;
        default:
            assemblies.Add(args[i]);
            break;
    }
}

if (assemblies.Count == 0)
{
    Console.Error.WriteLine("usage: Blocks.SurfaceDump <assembly.dll>... [--probe <folder>]... [--out <file>]");
    return 2;
}

var text = string.Concat(SurfaceDumper.Dump(assemblies, probes).Select(line => line + "\n"));
if (output is null)
    Console.Out.Write(text);
else
    File.WriteAllText(output, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

return 0;
