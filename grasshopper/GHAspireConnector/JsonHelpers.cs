using System.Text.Json;
using System.Text.Json.Nodes;

namespace GHAspireConnector;

internal static class JsonHelpers
{
    internal static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true
    };

    public static JsonObject? ParseOptionalObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var node = JsonNode.Parse(json);
        return node as JsonObject;
    }

    public static JsonObject ParseRequiredObject(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject;
        if (node is null)
        {
            throw new InvalidOperationException("Se esperaba un objeto JSON.");
        }

        return node;
    }

    public static string ToPrettyJson(JsonNode node)
    {
        return node.ToJsonString(PrettyOptions);
    }
}