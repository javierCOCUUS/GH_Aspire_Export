using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using GHAspireConnector.Models;
using Rhino.Geometry;

namespace GHAspireConnector.Components;

public sealed class BuildProfilePreviewComponent : ReadableParamsComponentBase
{
    public BuildProfilePreviewComponent()
        : base("Build Profile Preview", "ProfilePreview", "Genera la previsualizacion determinista de una operacion profile usando la herramienta resuelta desde el catalogo y separando rapid, approach, plunge, corte y retract.", "GH Aspire", "CAM")
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        EnsureConnectedTextValueList(
            document,
            5,
            "Side",
            "Compensacion lateral para profile.",
            new[] { "outside", "inside" });
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Profile Curves", "Profile Curves", "Curvas cerradas de perfil en coordenadas de pieza.", GH_ParamAccess.list);
        pManager.AddTextParameter("Catalog Path", "Catalog Path", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Selector", "Tool Selector", "Selector JSON de la herramienta profile.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Start Depth", "Start Depth", "Profundidad inicial desde la cara superior del material.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Cut Depth", "Cut Depth", "Profundidad de corte relativa desde Start Depth.", GH_ParamAccess.item);
        pManager.AddTextParameter("Side", "Side", "Lado de compensacion: inside u outside.", GH_ParamAccess.item, "outside");
        pManager.AddNumberParameter("Safe Z", "Safe Z", "Altura segura para rapids y retract final.", GH_ParamAccess.item, 5.0);
        pManager.AddNumberParameter("Approach Z", "Approach Z", "Plano de acercamiento antes del plunge.", GH_ParamAccess.item, 0.0);
        pManager[7].Optional = true;
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
        pManager.AddCurveParameter("Cut Paths", "Cut Paths", "Trayectorias de corte compensadas por radio de herramienta.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Retract Paths", "Retract Paths", "Movimientos verticales de retract desde el final de cada pasada hasta Safe Z.", GH_ParamAccess.list);
        pManager.AddColourParameter("Rapid Color", "Rapid Color", "Color sugerido para rapids.", GH_ParamAccess.item);
        pManager.AddColourParameter("Approach Color", "Approach Color", "Color sugerido para approaches.", GH_ParamAccess.item);
        pManager.AddColourParameter("Plunge Color", "Plunge Color", "Color sugerido para plunges.", GH_ParamAccess.item);
        pManager.AddColourParameter("Cut Color", "Cut Color", "Color sugerido para cortes.", GH_ParamAccess.item);
        pManager.AddColourParameter("Retract Color", "Retract Color", "Color sugerido para retracts.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var profileCurves = new List<Curve>();
        string? catalogPath = null;
        string selectorJson = string.Empty;
        double startDepth = 0.0;
        double cutDepth = 0.0;
        string side = "outside";
        double safeZ = 5.0;
        double approachZ = 0.0;

        if (!da.GetDataList(0, profileCurves) || profileCurves.Count == 0) return;
        if (!da.GetData(1, ref catalogPath) || string.IsNullOrWhiteSpace(catalogPath)) return;
        if (!da.GetData(2, ref selectorJson) || string.IsNullOrWhiteSpace(selectorJson)) return;
        if (!da.GetData(3, ref startDepth)) return;
        if (!da.GetData(4, ref cutDepth)) return;
        da.GetData(5, ref side);
        da.GetData(6, ref safeZ);
        da.GetData(7, ref approachZ);

        ToolCatalogEntry? toolEntry;
        try
        {
            var catalog = ToolCatalogResolver.LoadCatalog(catalogPath);
            toolEntry = ToolCatalogResolver.Resolve(catalog, selectorJson, "profile");
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        if (toolEntry is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No se pudo resolver la herramienta profile desde el catalogo.");
            return;
        }

        ContourPathResult pathResult;
        try
        {
            pathResult = ContourPathBuilder.BuildProfile(profileCurves, toolEntry, startDepth, cutDepth, side);
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
        da.SetData(14, Color.FromArgb(255, 76, 166, 76));
        da.SetData(15, Color.FromArgb(255, 76, 140, 245));
    }

    protected override Bitmap? Icon => IconLoader.Load("opciones.png");

    public override Guid ComponentGuid => new("1f0bc8f8-31a8-43c9-b5cb-ac7d7ad4e43e");
}