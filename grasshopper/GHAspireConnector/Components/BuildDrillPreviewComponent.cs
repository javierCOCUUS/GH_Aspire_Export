using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GHAspireConnector.Components;

public sealed class BuildDrillPreviewComponent : ReadableParamsComponentBase
{
    public BuildDrillPreviewComponent()
        : base("Build Drill Preview", "DrillPreview", "Genera la previsualizacion de una operacion drill leyendo la herramienta desde el catalogo y separando rapid, approach, plunge y retract.", "GH Aspire", "CAM")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Drill Points", "Drill Points", "Puntos de taladrado en coordenadas de pieza.", GH_ParamAccess.list);
        pManager.AddTextParameter("Catalog Path", "Catalog Path", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Selector", "Tool Selector", "Selector JSON de la herramienta drill.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Start Depth", "Start Depth", "Profundidad inicial desde la cara superior del material.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Cut Depth", "Cut Depth", "Profundidad de corte relativa desde Start Depth.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Safe Z", "Safe Z", "Altura segura para rapids y retract final.", GH_ParamAccess.item, 5.0);
        pManager.AddNumberParameter("Approach Z", "Approach Z", "Plano de acercamiento antes del plunge, normalmente 0 o una altura de clearance.", GH_ParamAccess.item, 0.0);
        pManager[6].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Tool Name", "Tool Name", "Nombre visible de la herramienta resuelta.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Tool Diameter", "Tool Diameter", "Diametro de la herramienta resuelta en mm.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Spindle RPM", "Spindle RPM", "RPM recomendadas en el catalogo.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Feed Rate", "Feed Rate", "Feed recomendado en mm/min.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Plunge Rate", "Plunge Rate", "Plunge recomendado en mm/min.", GH_ParamAccess.item);
        pManager.AddCurveParameter("Rapid Paths", "Rapid Paths", "Movimientos rapidos entre puntos de taladrado a Safe Z.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Approach Paths", "Approach Paths", "Movimientos verticales de acercamiento desde Safe Z hasta Approach Z.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Plunge Paths", "Plunge Paths", "Movimientos verticales de plunge desde Approach Z hasta la profundidad final.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Retract Paths", "Retract Paths", "Movimientos verticales de retract desde la profundidad final hasta Safe Z.", GH_ParamAccess.list);
        pManager.AddColourParameter("Rapid Color", "Rapid Color", "Color sugerido para rapids.", GH_ParamAccess.item);
        pManager.AddColourParameter("Approach Color", "Approach Color", "Color sugerido para approaches.", GH_ParamAccess.item);
        pManager.AddColourParameter("Plunge Color", "Plunge Color", "Color sugerido para plunges.", GH_ParamAccess.item);
        pManager.AddColourParameter("Retract Color", "Retract Color", "Color sugerido para retracts.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var drillPoints = new List<Point3d>();
        string? catalogPath = null;
        string selectorJson = string.Empty;
        double startDepth = 0.0;
        double cutDepth = 0.0;
        double safeZ = 5.0;
        double approachZ = 0.0;

        if (!da.GetDataList(0, drillPoints) || drillPoints.Count == 0)
        {
            return;
        }

        if (!da.GetData(1, ref catalogPath) || string.IsNullOrWhiteSpace(catalogPath))
        {
            return;
        }

        if (!da.GetData(2, ref selectorJson) || string.IsNullOrWhiteSpace(selectorJson))
        {
            return;
        }

        if (!da.GetData(3, ref startDepth)) return;
        if (!da.GetData(4, ref cutDepth)) return;
        da.GetData(5, ref safeZ);
        da.GetData(6, ref approachZ);

        Models.ToolCatalogEntry? toolEntry;
        try
        {
            var catalog = ToolCatalogResolver.LoadCatalog(catalogPath);
            toolEntry = ToolCatalogResolver.Resolve(catalog, selectorJson, "drill");
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        if (toolEntry is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No se pudo resolver la herramienta drill desde el catalogo.");
            return;
        }

        var topZ = -startDepth;
        var targetZ = -(startDepth + cutDepth);
        var rapidCurves = new List<Curve>();
        var approachCurves = new List<Curve>();
        var plungeCurves = new List<Curve>();
        var retractCurves = new List<Curve>();

        Point3d? previousSafePoint = null;
        foreach (var drillPoint in drillPoints)
        {
            var safePoint = new Point3d(drillPoint.X, drillPoint.Y, safeZ);
            var approachPoint = new Point3d(drillPoint.X, drillPoint.Y, approachZ);
            var topPoint = new Point3d(drillPoint.X, drillPoint.Y, topZ);
            var bottomPoint = new Point3d(drillPoint.X, drillPoint.Y, targetZ);

            if (previousSafePoint.HasValue)
            {
                rapidCurves.Add(new LineCurve(previousSafePoint.Value, safePoint));
            }

            if (Math.Abs(safeZ - approachZ) > Rhino.RhinoMath.ZeroTolerance)
            {
                approachCurves.Add(new LineCurve(safePoint, approachPoint));
            }

            var plungeStart = Math.Abs(approachZ - topZ) > Rhino.RhinoMath.ZeroTolerance ? approachPoint : topPoint;
            plungeCurves.Add(new LineCurve(plungeStart, bottomPoint));
            retractCurves.Add(new LineCurve(bottomPoint, safePoint));

            previousSafePoint = safePoint;
        }

        da.SetData(0, toolEntry.DisplayName);
        da.SetData(1, toolEntry.DiameterMm);
        da.SetData(2, toolEntry.RpmRecommend);
        da.SetData(3, toolEntry.FeedRecommendMmPerMin);
        da.SetData(4, toolEntry.PlungeRecommendMmPerMin);
        da.SetDataList(5, rapidCurves);
        da.SetDataList(6, approachCurves);
        da.SetDataList(7, plungeCurves);
        da.SetDataList(8, retractCurves);
        da.SetData(9, Color.FromArgb(255, 242, 153, 74));
        da.SetData(10, Color.FromArgb(255, 255, 211, 105));
        da.SetData(11, Color.FromArgb(255, 225, 92, 92));
        da.SetData(12, Color.FromArgb(255, 76, 140, 245));
    }

    protected override Bitmap? Icon => IconLoader.Load("opciones.png");

    public override Guid ComponentGuid => new("f5060af5-4243-4c0d-9ecb-58dce93c1ca2");
}