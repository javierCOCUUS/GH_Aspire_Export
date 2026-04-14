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
            var byId = tools.FirstOrDefault(tool => tool.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (byId is not null)
            {
                return byId;
            }
        }

        var toolType = selector["tool_type"]?.GetValue<string>();
        var aspireGroup = selector["aspire_group"]?.GetValue<string>();
        var diameter = selector["diameter_mm"]?.GetValue<double>();
        var toolNumber = selector["tool_number"]?.GetValue<int>();

        return tools.FirstOrDefault(tool =>
            (string.IsNullOrWhiteSpace(toolType) || tool.ToolType.Equals(toolType, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(aspireGroup) || tool.AspireGroup.Equals(aspireGroup, StringComparison.OrdinalIgnoreCase)) &&
            (!diameter.HasValue || Math.Abs(tool.DiameterMm - diameter.Value) < 0.001) &&
            (!toolNumber.HasValue || tool.ToolNumber == toolNumber.Value));
    }
}