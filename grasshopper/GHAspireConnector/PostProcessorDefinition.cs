using System.Text.RegularExpressions;

namespace GHAspireConnector;

internal sealed class PostProcessorDefinition
{
    public string PostName { get; set; } = string.Empty;

    public string FileExtension { get; set; } = "tap";

    public string Units { get; set; } = "MM";

    public Dictionary<string, List<string>> Blocks { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> GetBlock(string name)
    {
        return Blocks.TryGetValue(name, out var block) ? block : Array.Empty<string>();
    }
}

internal static class PostProcessorParser
{
    private static readonly Regex QuotedValueRegex = new("\"(?<value>.*?)\"", RegexOptions.Compiled);

    public static PostProcessorDefinition Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No existe el postprocesador: {path}");
        }

        var definition = new PostProcessorDefinition();
        string? currentBlock = null;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('+'))
            {
                continue;
            }

            if (line.StartsWith("POST_NAME", StringComparison.OrdinalIgnoreCase))
            {
                definition.PostName = ExtractQuotedValue(line);
                continue;
            }

            if (line.StartsWith("FILE_EXTENSION", StringComparison.OrdinalIgnoreCase))
            {
                definition.FileExtension = ExtractQuotedValue(line);
                continue;
            }

            if (line.StartsWith("UNITS", StringComparison.OrdinalIgnoreCase))
            {
                definition.Units = ExtractQuotedValue(line);
                continue;
            }

            if (line.StartsWith("begin ", StringComparison.OrdinalIgnoreCase))
            {
                currentBlock = line[6..].Trim();
                if (!definition.Blocks.ContainsKey(currentBlock))
                {
                    definition.Blocks[currentBlock] = new List<string>();
                }
                continue;
            }

            if (currentBlock is not null && line.StartsWith('"') && line.EndsWith('"'))
            {
                definition.Blocks[currentBlock].Add(ExtractQuotedValue(line));
            }
        }

        return definition;
    }

    private static string ExtractQuotedValue(string line)
    {
        var match = QuotedValueRegex.Match(line);
        return match.Success ? match.Groups["value"].Value : string.Empty;
    }
}