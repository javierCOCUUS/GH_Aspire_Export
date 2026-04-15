using System;
using System.Collections.Generic;
using System.Globalization;
using Grasshopper.Kernel;
using GHAspireConnector.Models;
using Rhino.Geometry;

namespace GHAspireConnector.Components;

public sealed class PostprocessDrillGCodeComponent : ReadableParamsComponentBase
{
    private const string DefaultPostprocessorPath = @"C:\GH_Aspire_Export\postprocessors\aspire\mi_maquina.pp";

    public PostprocessDrillGCodeComponent()
        : base("Postprocess Drill GCode", "DrillGCode", "Genera G-code de taladrado usando el postprocesador de Aspire y la herramienta resuelta desde el catalogo.", "GH Aspire", "CAM")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Drill Points", "Drill Points", "Puntos de taladrado en coordenadas de pieza.", GH_ParamAccess.list);
        pManager.AddTextParameter("Catalog Path", "Catalog Path", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Selector", "Tool Selector", "Selector JSON de la herramienta drill.", GH_ParamAccess.item);
        pManager.AddTextParameter("Postprocessor Path", "Postprocessor Path", "Ruta al archivo .pp de Aspire.", GH_ParamAccess.item, DefaultPostprocessorPath);
        pManager[3].Optional = true;
        pManager.AddNumberParameter("Start Depth", "Start Depth", "Profundidad inicial desde la cara superior del material.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Cut Depth", "Cut Depth", "Profundidad de corte relativa desde Start Depth.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Safe Z", "Safe Z", "Altura segura para rapids y retract final.", GH_ParamAccess.item, 5.0);
        pManager.AddNumberParameter("Approach Z", "Approach Z", "Plano de acercamiento antes del plunge.", GH_ParamAccess.item, 0.0);
        pManager[7].Optional = true;
        pManager.AddPointParameter("Home Position", "Home Position", "Posicion home de la maquina para cabecera y cierre.", GH_ParamAccess.item, new Point3d(0, 0, 20));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Post Name", "Post Name", "Nombre del postprocesador cargado.", GH_ParamAccess.item);
        pManager.AddTextParameter("File Extension", "File Extension", "Extension de archivo definida por el postprocesador.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Name", "Tool Name", "Nombre visible de la herramienta resuelta.", GH_ParamAccess.item);
        pManager.AddTextParameter("GCode Lines", "GCode Lines", "Lineas individuales del G-code generado.", GH_ParamAccess.list);
        pManager.AddTextParameter("GCode", "GCode", "G-code completo listo para escribir a disco.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var drillPoints = new List<Point3d>();
        string? catalogPath = null;
        string selectorJson = string.Empty;
        string? postPath = null;
        double startDepth = 0.0;
        double cutDepth = 0.0;
        double safeZ = 5.0;
        double approachZ = 0.0;
        var home = new Point3d(0, 0, 20);

        if (!da.GetDataList(0, drillPoints) || drillPoints.Count == 0) return;
        if (!da.GetData(1, ref catalogPath) || string.IsNullOrWhiteSpace(catalogPath)) return;
        if (!da.GetData(2, ref selectorJson) || string.IsNullOrWhiteSpace(selectorJson)) return;
        da.GetData(3, ref postPath);
        if (!da.GetData(4, ref startDepth)) return;
        if (!da.GetData(5, ref cutDepth)) return;
        da.GetData(6, ref safeZ);
        da.GetData(7, ref approachZ);
        da.GetData(8, ref home);

        if (string.IsNullOrWhiteSpace(postPath))
        {
            postPath = DefaultPostprocessorPath;
        }

        Models.ToolCatalogEntry? toolEntry;
        DrillPathResult drillPath;
        PostProcessorDefinition post;

        try
        {
            var catalog = ToolCatalogResolver.LoadCatalog(catalogPath);
            toolEntry = ToolCatalogResolver.Resolve(catalog, selectorJson, "drill");
            if (toolEntry is null)
            {
                throw new InvalidOperationException("No se pudo resolver la herramienta drill desde el catalogo.");
            }

            drillPath = DrillPathBuilder.Build(drillPoints, toolEntry, startDepth, cutDepth, safeZ, approachZ);
            post = PostProcessorParser.Load(postPath);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        var lines = new List<string>();
        var tokenValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["[T]"] = toolEntry.ToolNumber.ToString(CultureInfo.InvariantCulture),
            ["[S]"] = FormatToken("S", toolEntry.RpmRecommend, 0),
            ["[F]"] = FormatToken("F", toolEntry.PlungeRecommendMmPerMin, 1),
            ["[XH]"] = FormatToken("X", home.X),
            ["[YH]"] = FormatToken("Y", home.Y),
            ["[ZH]"] = FormatToken("Z", home.Z)
        };

        AppendBlock(lines, post.GetBlock("HEADER"), tokenValues, toolEntry.ToolNumber);

        var currentZ = home.Z;

        foreach (var hole in drillPath.Holes)
        {
            tokenValues["[X]"] = FormatToken("X", hole.SafePoint.X);
            tokenValues["[Y]"] = FormatToken("Y", hole.SafePoint.Y);
            tokenValues["[Z]"] = string.Empty;
            AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);

            if (Math.Abs(currentZ - hole.SafePoint.Z) > Rhino.RhinoMath.ZeroTolerance)
            {
                tokenValues["[X]"] = string.Empty;
                tokenValues["[Y]"] = string.Empty;
                tokenValues["[Z]"] = FormatToken("Z", hole.SafePoint.Z);
                AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);
                currentZ = hole.SafePoint.Z;
            }

            if (Math.Abs(hole.SafePoint.Z - hole.ApproachPoint.Z) > Rhino.RhinoMath.ZeroTolerance)
            {
                tokenValues["[X]"] = string.Empty;
                tokenValues["[Y]"] = string.Empty;
                tokenValues["[Z]"] = FormatToken("Z", hole.ApproachPoint.Z);
                AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);
                currentZ = hole.ApproachPoint.Z;
            }

            tokenValues["[X]"] = string.Empty;
            tokenValues["[Y]"] = string.Empty;
            tokenValues["[Z]"] = FormatToken("Z", hole.BottomPoint.Z);
            AppendBlock(lines, post.GetBlock("FIRST_FEED_MOVE"), tokenValues, toolEntry.ToolNumber);
            currentZ = hole.BottomPoint.Z;

            tokenValues["[X]"] = string.Empty;
            tokenValues["[Y]"] = string.Empty;
            tokenValues["[Z]"] = FormatToken("Z", hole.SafePoint.Z);
            AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);
            currentZ = hole.SafePoint.Z;
        }

        AppendBlock(lines, post.GetBlock("FOOTER"), tokenValues, toolEntry.ToolNumber);

        da.SetData(0, post.PostName);
        da.SetData(1, post.FileExtension);
        da.SetData(2, toolEntry.DisplayName);
        da.SetDataList(3, lines);
        da.SetData(4, string.Join(Environment.NewLine, lines));
    }

    private static void AppendBlock(List<string> output, IReadOnlyList<string> block, Dictionary<string, string> tokens, int toolNumber)
    {
        foreach (var rawLine in block)
        {
            if (rawLine.Contains("WCS by M6Start", StringComparison.OrdinalIgnoreCase))
            {
                var workOffsetCode = GetWorkOffsetCode(toolNumber);
                if (!string.IsNullOrWhiteSpace(workOffsetCode))
                {
                    output.Add(workOffsetCode);
                }
                continue;
            }

            var line = rawLine;
            foreach (var token in tokens.OrderByDescending(item => item.Key.Length))
            {
                line = line.Replace(token.Key, token.Value, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                output.Add(line.Trim());
            }
        }
    }

    private static string FormatNumber(double value, int decimals)
    {
        var format = decimals <= 0 ? "0" : "0." + new string('0', decimals);
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    private static string FormatToken(string prefix, double value, int decimals = 3)
    {
        return prefix + FormatNumber(value, decimals);
    }

    private static string GetWorkOffsetCode(int toolNumber)
    {
        return toolNumber switch
        {
            1 => "G54",
            2 => "G55",
            3 => "G56",
            _ => "G54"
        };
    }

    protected override System.Drawing.Bitmap? Icon => IconLoader.Load("write.png");

    public override Guid ComponentGuid => new("e7f0e4c1-a7bd-4b8e-b28c-9c0cab0d6a0f");
}