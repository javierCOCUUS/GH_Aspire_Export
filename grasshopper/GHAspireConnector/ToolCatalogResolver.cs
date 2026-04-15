using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GHAspireConnector.Models;

namespace GHAspireConnector;

internal static class ToolCatalogResolver
{
    public static ToolCatalog LoadCatalog(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No existe el catalogo: {path}");
        }

        var raw = File.ReadAllText(path);
        var catalog = JsonSerializer.Deserialize<ToolCatalog>(raw);
        if (catalog is null)
        {
            throw new InvalidOperationException("El catalogo no contiene herramientas validas.");
        }

        return catalog;
    }

    public static ToolCatalogEntry? Resolve(ToolCatalog catalog, string selectorJson, string? operationType = null)
    {
        var selector = JsonHelpers.ParseOptionalObject(selectorJson);
        if (selector is null)
        {
            return null;
        }

        var filtered = catalog.Tools.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(operationType))
        {
            filtered = filtered.Where(tool => tool.OperationTypes.Any(value => value.Equals(operationType, StringComparison.OrdinalIgnoreCase)));
        }

        var tools = filtered.ToList();
        var id = selector["id"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(id))
        {
            var byId = tools.FirstOrDefault(tool =>
                tool.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                MatchesStaticSelectorFields(tool, selector));
            if (byId is not null)
            {
                return ApplySelectorOverrides(byId, selector);
            }
        }

        var toolType = selector["tool_type"]?.GetValue<string>();
        var aspireGroup = selector["aspire_group"]?.GetValue<string>();
        var diameter = selector["diameter_mm"]?.GetValue<double>();
        var toolNumber = selector["tool_number"]?.GetValue<int>();

        var resolved = tools.FirstOrDefault(tool =>
            (string.IsNullOrWhiteSpace(toolType) || tool.ToolType.Equals(toolType, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(aspireGroup) || tool.AspireGroup.Equals(aspireGroup, StringComparison.OrdinalIgnoreCase)) &&
            (!diameter.HasValue || Math.Abs(tool.DiameterMm - diameter.Value) < 0.001));

        return resolved is null ? null : ApplySelectorOverrides(resolved, selector);
    }

    private static bool MatchesStaticSelectorFields(ToolCatalogEntry tool, JsonObject selector)
    {
        var toolType = selector["tool_type"]?.GetValue<string>();
        var aspireGroup = selector["aspire_group"]?.GetValue<string>();
        var diameter = selector["diameter_mm"]?.GetValue<double>();

        return (string.IsNullOrWhiteSpace(toolType) || tool.ToolType.Equals(toolType, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(aspireGroup) || tool.AspireGroup.Equals(aspireGroup, StringComparison.OrdinalIgnoreCase)) &&
            (!diameter.HasValue || Math.Abs(tool.DiameterMm - diameter.Value) < 0.001);
    }

    private static ToolCatalogEntry ApplySelectorOverrides(ToolCatalogEntry tool, JsonObject selector)
    {
        var toolNumber = selector["tool_number"]?.GetValue<int>();
        if (!toolNumber.HasValue || toolNumber.Value <= 0 || toolNumber.Value == tool.ToolNumber)
        {
            return tool;
        }

        var selectorOverride = new ToolSelector
        {
            Id = tool.Selector.Id,
            ToolType = tool.Selector.ToolType,
            DiameterMm = tool.Selector.DiameterMm,
            ToolNumber = toolNumber.Value,
            AspireGroup = tool.Selector.AspireGroup
        };

        return new ToolCatalogEntry
        {
            Id = tool.Id,
            DisplayName = tool.DisplayName,
            ToolType = tool.ToolType,
            AspireGroup = tool.AspireGroup,
            DiameterMm = tool.DiameterMm,
            ToolNumber = toolNumber.Value,
            FluteCount = tool.FluteCount,
            StepdownMm = tool.StepdownMm,
            StepoverMm = tool.StepoverMm,
            RpmRecommend = tool.RpmRecommend,
            FeedRecommendMmPerMin = tool.FeedRecommendMmPerMin,
            PlungeRecommendMmPerMin = tool.PlungeRecommendMmPerMin,
            OperationTypes = tool.OperationTypes.ToList(),
            Selector = selectorOverride
        };
    }
}