using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;
using GHAspireConnector.Models;

namespace GHAspireConnector.Components;

public sealed class PickToolComponent : ReadableParamsComponentBase
{
    private const int OperationTypeInputIndex = 1;
    private const int ToolIdInputIndex = 3;
    private const int ToolNumberInputIndex = 4;

    public PickToolComponent()
        : base("Pick Tool", "PickTool", "Selecciona una herramienta del catalogo filtrado por operacion y devuelve su selector JSON.", "GH Aspire", "Tools")
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        EnsureOperationTypeValueList(document);
        EnsureToolIdValueList(document);
        EnsureToolNumberValueList(document);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Catalog Path", "Catalog Path", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Operation Type", "Operation Type", "Tipo de operacion para filtrar, por ejemplo profile, pocket o drill.", GH_ParamAccess.item, string.Empty);
        pManager[1].Optional = true;
        pManager.AddIntegerParameter("Index", "Index", "Indice dentro de la lista filtrada. Se usa si Tool Id esta vacio.", GH_ParamAccess.item, 0);
        pManager.AddTextParameter("Tool Id", "Tool Id", "Id opcional de la herramienta a seleccionar directamente.", GH_ParamAccess.item, string.Empty);
        pManager[3].Optional = true;
        pManager.AddIntegerParameter("Tool Number", "Tool Number", "Numero de herramienta a escribir en el selector JSON. Usa 0 para conservar el numero del catalogo.", GH_ParamAccess.item, 0);
        pManager[4].Optional = true;
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
        int toolNumber = 0;

        if (!da.GetData(0, ref path) || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        da.GetData(1, ref operationType);
        da.GetData(2, ref index);
        da.GetData(3, ref toolId);
        da.GetData(4, ref toolNumber);

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
        SyncToolIdValueList(tools);
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

        var selector = selected.Selector;
        if (toolNumber > 0)
        {
            selector = new ToolSelector
            {
                Id = selected.Selector.Id,
                ToolType = selected.Selector.ToolType,
                DiameterMm = selected.Selector.DiameterMm,
                ToolNumber = toolNumber,
                AspireGroup = selected.Selector.AspireGroup
            };
        }

        da.SetData(0, selected.DisplayName);
        da.SetData(1, selected.Id);
        da.SetData(2, JsonHelpers.ToPrettyJson(selector.ToJsonObject()));
        da.SetData(3, JsonSerializer.Serialize(selected, JsonHelpers.PrettyOptions));
    }

    protected override System.Drawing.Bitmap? Icon => IconLoader.Load("opciones.png");

    public override Guid ComponentGuid => new("e4b595ef-1850-48be-b380-75f86d24f765");

    private void EnsureToolNumberValueList(GH_Document document)
    {
        EnsureConnectedValueList(
            document,
            ToolNumberInputIndex,
            "Tool Number",
            "Selector rapido del numero de herramienta.",
            new (string Name, string Expression)[]
            {
                ("Catalog", "0"),
                ("T1", "1"),
                ("T2", "2"),
                ("T3", "3")
            });
    }

    private void EnsureOperationTypeValueList(GH_Document document)
    {
        EnsureConnectedValueList(
            document,
            OperationTypeInputIndex,
            "Operation Type",
            "Filtro rapido del tipo de operacion.",
            new (string Name, string Expression)[]
            {
                ("All", QuoteTextExpression(string.Empty)),
                ("profile", QuoteTextExpression("profile")),
                ("pocket", QuoteTextExpression("pocket")),
                ("drill", QuoteTextExpression("drill"))
            });
    }

    private void EnsureToolIdValueList(GH_Document document)
    {
        EnsureConnectedValueList(
            document,
            ToolIdInputIndex,
            "Tool Id",
            "Selector rapido de herramienta cargado desde el catalogo.",
            new (string Name, string Expression)[]
            {
                ("By Index", QuoteTextExpression(string.Empty))
            },
            220,
            10);
    }

    private void SyncToolIdValueList(IReadOnlyList<ToolCatalogEntry> tools)
    {
        var items = new List<(string Name, string Expression)>
        {
            ("By Index", QuoteTextExpression(string.Empty))
        };

        foreach (var tool in tools)
        {
            items.Add((tool.DisplayName, QuoteTextExpression(tool.Id)));
        }

        SyncConnectedValueListItems(
            ToolIdInputIndex,
            "Tool Id",
            "Selector rapido de herramienta cargado desde el catalogo.",
            items);
    }
}