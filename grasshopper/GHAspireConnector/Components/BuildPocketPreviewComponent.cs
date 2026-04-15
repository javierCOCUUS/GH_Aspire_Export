using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using GHAspireConnector.Models;
using Rhino.Geometry;

namespace GHAspireConnector.Components;

public sealed class BuildPocketPreviewComponent : ReadableParamsComponentBase
{
    public BuildPocketPreviewComponent()
        : base("Build Pocket Preview", "PocketPreview", "Genera la previsualizacion determinista de una operacion pocket usando offsets interiores, stepdown y stepover del catalogo.", "GH Aspire", "CAM")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Pocket Curves", "Pocket Curves", "Curvas cerradas limite de la zona a vaciar.", GH_ParamAccess.list);
        pManager.AddTextParameter("Catalog Path", "Catalog Path", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Selector", "Tool Selector", "Selector JSON de la herramienta pocket.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Start Depth", "Start Depth", "Profundidad inicial desde la cara superior del material.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Cut Depth", "Cut Depth", "Profundidad de corte relativa desde Start Depth.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Safe Z", "Safe Z", "Altura segura para rapids y retract final.", GH_ParamAccess.item, 5.0);
        pManager.AddNumberParameter("Approach Z", "Approach Z", "Plano de acercamiento antes del plunge.", GH_ParamAccess.item, 0.0);
        pManager[6].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Tool Name", "Tool Name", "Nombre visible de la herramienta resuelta.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Tool Diameter", "Tool Diameter", "Diametro de la herramienta resuelta en mm.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Spindle RPM", "Spindle RPM", "RPM recomendadas en el catalogo.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Feed Rate", "Feed Rate", "Feed recomendado en mm/min.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Plunge Rate", "Plunge Rate", "Plunge recomendado en mm/min.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Pass Count", "Pass Count", "Numero de pasadas de profundidad calculadas.", GH_ParamAccess.item);
        pManager.AddCurveParameter("Rapid Paths", "Rapid Paths", "Movimientos rapidos entre inicios de corte a Safe Z.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Approach Paths", "Approach Paths", "Movimientos verticales de acercamiento desde Safe Z hasta Approach Z.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Plunge Paths", "Plunge Paths", "Movimientos verticales de plunge hasta la pasada de corte.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Cut Paths", "Cut Paths", "Trayectorias de corte interiores de pocket.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Retract Paths", "Retract Paths", "Movimientos verticales de retract desde el final de cada pasada hasta Safe Z.", GH_ParamAccess.list);
        pManager.AddColourParameter("Rapid Color", "Rapid Color", "Color sugerido para rapids.", GH_ParamAccess.item);
        pManager.AddColourParameter("Approach Color", "Approach Color", "Color sugerido para approaches.", GH_ParamAccess.item);
        pManager.AddColourParameter("Plunge Color", "Plunge Color", "Color sugerido para plunges.", GH_ParamAccess.item);
        pManager.AddColourParameter("Cut Color", "Cut Color", "Color sugerido para cortes.", GH_ParamAccess.item);
        pManager.AddColourParameter("Retract Color", "Retract Color", "Color sugerido para retracts.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var pocketCurves = new List<Curve>();
        string? catalogPath = null;
        string selectorJson = string.Empty;
        double startDepth = 0.0;
        double cutDepth = 0.0;
        double safeZ = 5.0;
        double approachZ = 0.0;

        if (!da.GetDataList(0, pocketCurves) || pocketCurves.Count == 0) return;
        if (!da.GetData(1, ref catalogPath) || string.IsNullOrWhiteSpace(catalogPath)) return;
        if (!da.GetData(2, ref selectorJson) || string.IsNullOrWhiteSpace(selectorJson)) return;
        if (!da.GetData(3, ref startDepth)) return;
        if (!da.GetData(4, ref cutDepth)) return;
        da.GetData(5, ref safeZ);
        da.GetData(6, ref approachZ);

        ToolCatalogEntry? toolEntry;
        try
        {
            var catalog = ToolCatalogResolver.LoadCatalog(catalogPath);
            toolEntry = ToolCatalogResolver.Resolve(catalog, selectorJson, "pocket");
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        if (toolEntry is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No se pudo resolver la herramienta pocket desde el catalogo.");
            return;
        }

        ContourPathResult pathResult;
        try
        {
            pathResult = ContourPathBuilder.BuildPocket(pocketCurves, toolEntry, startDepth, cutDepth);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        var preview = ContourPreviewBuilder.Build(pathResult, safeZ, approachZ);

        da.SetData(0, toolEntry.DisplayName);
        da.SetData(1, toolEntry.DiameterMm);
        da.SetData(2, toolEntry.RpmRecommend);
        da.SetData(3, toolEntry.FeedRecommendMmPerMin);
        da.SetData(4, toolEntry.PlungeRecommendMmPerMin);
        da.SetData(5, pathResult.Passes.Count);
        da.SetDataList(6, preview.RapidPaths);
        da.SetDataList(7, preview.ApproachPaths);
        da.SetDataList(8, preview.PlungePaths);
        da.SetDataList(9, preview.CutPaths);
        da.SetDataList(10, preview.RetractPaths);
        da.SetData(11, Color.FromArgb(255, 242, 153, 74));
        da.SetData(12, Color.FromArgb(255, 255, 211, 105));
        da.SetData(13, Color.FromArgb(255, 225, 92, 92));
        da.SetData(14, Color.FromArgb(255, 64, 184, 166));
        da.SetData(15, Color.FromArgb(255, 76, 140, 245));
    }

    protected override Bitmap? Icon => IconLoader.Load("opciones.png");

    public override Guid ComponentGuid => new("fd20462a-8622-4d80-ad9d-5a94f4e6fe95");
}