using System;
using System.Text.Json.Nodes;
using Grasshopper.Kernel;

namespace GHAspireConnector.Components;

public sealed class CreateOperationComponent : ReadableParamsComponentBase
{
    public CreateOperationComponent()
        : base("Create Operation JSON", "OpJson", "Construye el JSON de una operacion para Aspire.", "GH Aspire", "JSON")
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        EnsureConnectedTextValueList(
            document,
            2,
            "Type",
            "Tipo de operacion.",
            new[] { "profile", "pocket", "drill" });
        EnsureConnectedTextValueList(
            document,
            5,
            "Side",
            "Lado de compensacion para profile.",
            new[] { "outside", "inside" });
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "Name", "Nombre de la operacion.", GH_ParamAccess.item);
        pManager.AddTextParameter("Layer", "Layer", "Nombre de la capa en el DXF.", GH_ParamAccess.item);
        pManager.AddTextParameter("Type", "Type", "Tipo de operacion: profile, pocket o drill.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Start Depth", "Start Depth", "Profundidad inicial.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Cut Depth", "Cut Depth", "Profundidad de corte.", GH_ParamAccess.item);
        pManager.AddTextParameter("Side", "Side", "Side para perfiles: inside u outside.", GH_ParamAccess.item, string.Empty);
        pManager[5].Optional = true;
        pManager.AddTextParameter("Tool Selector", "Tool Selector", "Selector JSON opcional para override de herramienta.", GH_ParamAccess.item, string.Empty);
        pManager[6].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Operation JSON", "Operation JSON", "Operacion serializada como JSON.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string? name = null;
        string? layer = null;
        string? type = null;
        double startDepth = 0.0;
        double cutDepth = 0.0;
        string side = string.Empty;
        string selectorJson = string.Empty;

        if (!da.GetData(0, ref name) || string.IsNullOrWhiteSpace(name)) return;
        if (!da.GetData(1, ref layer) || string.IsNullOrWhiteSpace(layer)) return;
        if (!da.GetData(2, ref type) || string.IsNullOrWhiteSpace(type)) return;
        if (!da.GetData(3, ref startDepth)) return;
        if (!da.GetData(4, ref cutDepth)) return;
        da.GetData(5, ref side);
        da.GetData(6, ref selectorJson);

        var operation = new JsonObject
        {
            ["name"] = name,
            ["layer"] = layer,
            ["type"] = type,
            ["start_depth"] = startDepth,
            ["cut_depth"] = cutDepth
        };

        if (!string.IsNullOrWhiteSpace(side))
        {
            operation["side"] = side;
        }

        try
        {
            var selector = JsonHelpers.ParseOptionalObject(selectorJson);
            if (selector is not null)
            {
                operation["tool"] = selector;
            }
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Selector JSON invalido: {ex.Message}");
            return;
        }

        da.SetData(0, JsonHelpers.ToPrettyJson(operation));
    }

    protected override System.Drawing.Bitmap? Icon => IconLoader.Load("json.png");

    public override Guid ComponentGuid => new("547257ac-3229-4eb7-b3c0-f347175c0735");
}