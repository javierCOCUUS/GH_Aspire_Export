using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GHAspireConnector.Models;

public sealed class ToolCatalog
{
    [JsonPropertyName("catalog_version")]
    public int CatalogVersion { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public string GeneratedAt { get; set; } = string.Empty;

    [JsonPropertyName("tools")]
    public List<ToolCatalogEntry> Tools { get; set; } = new();
}

public sealed class ToolCatalogEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("tool_type")]
    public string ToolType { get; set; } = string.Empty;

    [JsonPropertyName("aspire_group")]
    public string AspireGroup { get; set; } = string.Empty;

    [JsonPropertyName("diameter_mm")]
    public double DiameterMm { get; set; }

    [JsonPropertyName("tool_number")]
    public int ToolNumber { get; set; }

    [JsonPropertyName("flute_count")]
    public int FluteCount { get; set; }

    [JsonPropertyName("stepdown_mm")]
    public double StepdownMm { get; set; }

    [JsonPropertyName("stepover_mm")]
    public double StepoverMm { get; set; }

    [JsonPropertyName("rpm_recommend")]
    public double RpmRecommend { get; set; }

    [JsonPropertyName("feed_recommend_mm_per_min")]
    public double FeedRecommendMmPerMin { get; set; }

    [JsonPropertyName("plunge_recommend_mm_per_min")]
    public double PlungeRecommendMmPerMin { get; set; }

    [JsonPropertyName("operation_types")]
    public List<string> OperationTypes { get; set; } = new();

    [JsonPropertyName("selector")]
    public ToolSelector Selector { get; set; } = new();
}

public sealed class ToolSelector
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("tool_type")]
    public string ToolType { get; set; } = string.Empty;

    [JsonPropertyName("diameter_mm")]
    public double DiameterMm { get; set; }

    [JsonPropertyName("tool_number")]
    public int ToolNumber { get; set; }

    [JsonPropertyName("aspire_group")]
    public string AspireGroup { get; set; } = string.Empty;

    public JsonObject ToJsonObject()
    {
        var node = new JsonObject();

        if (!string.IsNullOrWhiteSpace(Id))
        {
            node["id"] = Id;
        }

        if (!string.IsNullOrWhiteSpace(ToolType))
        {
            node["tool_type"] = ToolType;
        }

        if (DiameterMm > 0)
        {
            node["diameter_mm"] = DiameterMm;
        }

        if (ToolNumber > 0)
        {
            node["tool_number"] = ToolNumber;
        }

        if (!string.IsNullOrWhiteSpace(AspireGroup))
        {
            node["aspire_group"] = AspireGroup;
        }

        return node;
    }
}