using System;
using Grasshopper.Kernel;
using GHAspireConnector.Models;
using Rhino.Geometry;

namespace GHAspireConnector.Components;

public sealed class PostprocessProfileGCodeComponent : ReadableParamsComponentBase
{
    private const string DefaultPostprocessorPath = @"C:\GH_Aspire_Export\postprocessors\aspire\mi_maquina.pp";

    public PostprocessProfileGCodeComponent()
        : base("Postprocess Profile GCode", "ProfileGCode", "Genera G-code de profile usando el postprocesador de Aspire y la herramienta resuelta desde el catalogo.", "GH Aspire", "CAM")
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        EnsureConnectedTextValueList(
            document,
            6,
            "Side",
            "Compensacion lateral para profile.",
            new[] { "outside", "inside" });
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Profile Curves", "Profile Curves", "Curvas cerradas de perfil en coordenadas de pieza.", GH_ParamAccess.list);
        pManager.AddTextParameter("Catalog Path", "Catalog Path", "Ruta al archivo grasshopper_tool_catalog.json.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Selector", "Tool Selector", "Selector JSON de la herramienta profile.", GH_ParamAccess.item);
        pManager.AddTextParameter("Postprocessor Path", "Postprocessor Path", "Ruta al archivo .pp de Aspire.", GH_ParamAccess.item, DefaultPostprocessorPath);
        pManager[3].Optional = true;
        pManager.AddNumberParameter("Start Depth", "Start Depth", "Profundidad inicial desde la cara superior del material.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Cut Depth", "Cut Depth", "Profundidad de corte relativa desde Start Depth.", GH_ParamAccess.item);
        pManager.AddTextParameter("Side", "Side", "Lado de compensacion: inside u outside.", GH_ParamAccess.item, "outside");
        pManager.AddNumberParameter("Safe Z", "Safe Z", "Altura segura para rapids y retract final.", GH_ParamAccess.item, 5.0);
        pManager.AddNumberParameter("Approach Z", "Approach Z", "Plano de acercamiento antes del plunge.", GH_ParamAccess.item, 0.0);
        pManager[8].Optional = true;
        pManager.AddPointParameter("Home Position", "Home Position", "Posicion home de la maquina para cabecera y cierre.", GH_ParamAccess.item, new Point3d(0, 0, 20));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Post Name", "Post Name", "Nombre del postprocesador cargado.", GH_ParamAccess.item);
        pManager.AddTextParameter("File Extension", "File Extension", "Extension de archivo definida por el postprocesador.", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool Name", "Tool Name", "Nombre visible de la herramienta resuelta.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Pass Count", "Pass Count", "Numero de pasadas de profundidad calculadas.", GH_ParamAccess.item);
        pManager.AddTextParameter("GCode Lines", "GCode Lines", "Lineas individuales del G-code generado.", GH_ParamAccess.list);
        pManager.AddTextParameter("GCode", "GCode", "G-code completo listo para escribir a disco.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var profileCurves = new List<Curve>();
        string? catalogPath = null;
        string selectorJson = string.Empty;
        string? postPath = null;
        double startDepth = 0.0;
        double cutDepth = 0.0;
        string side = "outside";
        double safeZ = 5.0;
        double approachZ = 0.0;
        var home = new Point3d(0, 0, 20);

        if (!da.GetDataList(0, profileCurves) || profileCurves.Count == 0) return;
        if (!da.GetData(1, ref catalogPath) || string.IsNullOrWhiteSpace(catalogPath)) return;
        if (!da.GetData(2, ref selectorJson) || string.IsNullOrWhiteSpace(selectorJson)) return;
        da.GetData(3, ref postPath);
        if (!da.GetData(4, ref startDepth)) return;
        if (!da.GetData(5, ref cutDepth)) return;
        da.GetData(6, ref side);
        da.GetData(7, ref safeZ);
        da.GetData(8, ref approachZ);
        da.GetData(9, ref home);

        if (string.IsNullOrWhiteSpace(postPath))
        {
            postPath = DefaultPostprocessorPath;
        }

        ToolCatalogEntry? toolEntry;
        ContourPathResult contourPath;
        PostProcessorDefinition post;

        try
        {
            var catalog = ToolCatalogResolver.LoadCatalog(catalogPath);
            toolEntry = ToolCatalogResolver.Resolve(catalog, selectorJson, "profile");
            if (toolEntry is null)
            {
                throw new InvalidOperationException("No se pudo resolver la herramienta profile desde el catalogo.");
            }

            contourPath = ContourPathBuilder.BuildProfile(profileCurves, toolEntry, startDepth, cutDepth, side);
            post = PostProcessorParser.Load(postPath);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        var lines = ContourGCodeWriter.Build(contourPath, post, home, safeZ, approachZ);

        da.SetData(0, post.PostName);
        da.SetData(1, post.FileExtension);
        da.SetData(2, toolEntry.DisplayName);
        da.SetData(3, contourPath.Passes.Count);
        da.SetDataList(4, lines);
        da.SetData(5, string.Join(Environment.NewLine, lines));
    }

    protected override System.Drawing.Bitmap? Icon => IconLoader.Load("write.png");

    public override Guid ComponentGuid => new("9ee4d655-3270-4f60-94f7-a8053d067748");
}