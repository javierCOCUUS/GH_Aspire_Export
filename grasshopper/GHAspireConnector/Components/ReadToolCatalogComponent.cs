using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;
using GHAspireConnector.Models;

namespace GHAspireConnector.Components;

public sealed class ReadToolCatalogComponent : GH_Component
{
    public ReadToolCatalogComponent()
        : base("Read Tool Catalog", "ToolCatalog", "Lee el catalogo simplificado de herramientas para Grasshopper.", "GH Aspire", "Tools")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Catalog Path", "P", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Operation Type", "O", "Filtro opcional por tipo de operacion, por ejemplo profile, pocket o drill.", GH_ParamAccess.item, string.Empty);
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Display Names", "N", "Nombres visibles de las herramientas filtradas.", GH_ParamAccess.list);
        pManager.AddTextParameter("Ids", "I", "Ids de las herramientas filtradas.", GH_ParamAccess.list);
        pManager.AddTextParameter("Selectors", "S", "Objetos selector JSON listos para exportar al job.", GH_ParamAccess.list);
        pManager.AddTextParameter("Entries", "E", "Entradas completas del catalogo filtrado como JSON.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string? path = null;
        string operationType = string.Empty;

        if (!da.GetData(0, ref path) || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        da.GetData(1, ref operationType);

        if (!File.Exists(path))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"No existe el catalogo: {path}");
            return;
        }

        ToolCatalog? catalog;
        try
        {
            var raw = File.ReadAllText(path);
            catalog = JsonSerializer.Deserialize<ToolCatalog>(raw);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"No se pudo leer el catalogo: {ex.Message}");
            return;
        }

        if (catalog is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "El catalogo no contiene herramientas validas.");
            return;
        }

        var filtered = catalog.Tools.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(operationType))
        {
            filtered = filtered.Where(tool => tool.OperationTypes.Any(value => value.Equals(operationType, StringComparison.OrdinalIgnoreCase)));
        }

        var tools = filtered.ToList();
        da.SetDataList(0, tools.Select(tool => tool.DisplayName));
        da.SetDataList(1, tools.Select(tool => tool.Id));
        da.SetDataList(2, tools.Select(tool => JsonHelpers.ToPrettyJson(tool.Selector.ToJsonObject())));
        da.SetDataList(3, tools.Select(tool => JsonSerializer.Serialize(tool, JsonHelpers.PrettyOptions)));
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override Guid ComponentGuid => new("fe634d16-e426-4bc2-a12a-b4a372983f8a");
}