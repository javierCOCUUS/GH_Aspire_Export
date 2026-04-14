using System;
using System.IO;
using Grasshopper.Kernel;

namespace GHAspireConnector.Components;

public sealed class WriteTextFileComponent : ReadableParamsComponentBase
{
    public WriteTextFileComponent()
        : base("Write Text File", "WriteFile", "Escribe texto o JSON a disco para probar el flujo Grasshopper -> archivo.", "GH Aspire", "IO")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Content", "Content", "Contenido a escribir en el archivo.", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "Path", "Ruta completa del archivo de salida.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Write", "Write", "Si es true, escribe el archivo en disco.", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Path", "Path", "Ruta del archivo de salida.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Exists", "Exists", "Indica si el archivo existe tras la operacion.", GH_ParamAccess.item);
        pManager.AddTextParameter("Status", "Status", "Estado de la escritura.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string? content = null;
        string? path = null;
        bool write = false;

        if (!da.GetData(0, ref content) || content is null)
        {
            return;
        }

        if (!da.GetData(1, ref path) || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!da.GetData(2, ref write))
        {
            return;
        }

        var status = "Esperando Write = true";

        try
        {
            if (write)
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, content);
                status = "Archivo escrito";
            }
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            status = $"Error: {ex.Message}";
        }

        da.SetData(0, path);
        da.SetData(1, File.Exists(path));
        da.SetData(2, status);
    }

    protected override System.Drawing.Bitmap? Icon => IconLoader.Load("write.png");

    public override Guid ComponentGuid => new("34e71f58-01b9-46d5-9878-bceb34abf9c2");
}