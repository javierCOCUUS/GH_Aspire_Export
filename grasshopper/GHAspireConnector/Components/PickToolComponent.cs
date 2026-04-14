using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;
using GHAspireConnector.Models;

namespace GHAspireConnector.Components;

public sealed class PickToolComponent : ReadableParamsComponentBase
{
    public PickToolComponent()
        : base("Pick Tool", "PickTool", "Selecciona una herramienta del catalogo filtrado por operacion y devuelve su selector JSON.", "GH Aspire", "Tools")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Catalog Path", "Catalog Path", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Operation Type", "Operation Type", "Tipo de operacion para filtrar, por ejemplo profile, pocket o drill.", GH_ParamAccess.item, string.Empty);
        pManager[1].Optional = true;
        pManager.AddIntegerParameter("Index", "Index", "Indice dentro de la lista filtrada. Se usa si Tool Id esta vacio.", GH_ParamAccess.item, 0);
        pManager.AddTextParameter("Tool Id", "Tool Id", "Id opcional de la herramienta a seleccionar directamente.", GH_ParamAccess.item, string.Empty);
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Display Name", "Display Name", "Nombre visible de la herramienta seleccionada.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Id", "Tool Id", "Id de la herramienta seleccionada.", GH_ParamAccess.item);
        pManager.AddTextParameter("Selector", "Selector", "Selector JSON listo para exportar al job.", GH_ParamAccess.item);
        pManager.AddTextParameter("Entry", "Entry", "Entrada completa del catalogo para la herramienta seleccionada.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Filtered Count", "Filtered Count", "Numero de herramientas disponibles tras el filtro.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string? path = null;
        string operationType = string.Empty;
        int index = 0;
        string toolId = string.Empty;

        if (!da.GetData(0, ref path) || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        da.GetData(1, ref operationType);
        da.GetData(2, ref index);
        da.GetData(3, ref toolId);

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
        da.SetData(4, tools.Count);

        if (tools.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No hay herramientas disponibles para el filtro actual.");
            return;
        }

        ToolCatalogEntry? selected = null;
        if (!string.IsNullOrWhiteSpace(toolId))
        {
            selected = tools.FirstOrDefault(tool => tool.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"No se encontro la herramienta con id {toolId}. Se usara el indice.");
            }
        }

        if (selected is null)
        {
            if (index < 0 || index >= tools.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Indice fuera de rango. Se usara 0 sobre {tools.Count} herramientas.");
                index = 0;
            }

            selected = tools[index];
        }

        da.SetData(0, selected.DisplayName);
        da.SetData(1, selected.Id);
        da.SetData(2, JsonHelpers.ToPrettyJson(selected.Selector.ToJsonObject()));
        da.SetData(3, JsonSerializer.Serialize(selected, JsonHelpers.PrettyOptions));
    }

    protected override System.Drawing.Bitmap? Icon => IconLoader.Load("opciones.png");

    public override Guid ComponentGuid => new("e4b595ef-1850-48be-b380-75f86d24f765");
}