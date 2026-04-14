using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Grasshopper.Kernel;

namespace GHAspireConnector.Components;

public sealed class CreateJobJsonComponent : GH_Component
{
    public CreateJobJsonComponent()
        : base("Create Job JSON", "JobJson", "Construye el JSON completo del job para Aspire a partir de defaults y operaciones.", "GH Aspire", "JSON")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Job Name", "J", "Nombre del job.", GH_ParamAccess.item);
        pManager.AddTextParameter("Units", "U", "Unidades del job, normalmente mm o in.", GH_ParamAccess.item, "mm");
        pManager.AddNumberParameter("Thickness", "T", "Espesor del material.", GH_ParamAccess.item);
        pManager.AddTextParameter("Origin", "O", "Origen XY, por ejemplo lower_left.", GH_ParamAccess.item, "lower_left");
        pManager.AddTextParameter("Z Zero", "Z", "Referencia Z del material.", GH_ParamAccess.item, "material_top");
        pManager.AddTextParameter("Profile Tool", "P", "Selector JSON por defecto para operaciones profile.", GH_ParamAccess.item, string.Empty);
        pManager[5].Optional = true;
        pManager.AddTextParameter("Pocket Tool", "PK", "Selector JSON por defecto para operaciones pocket.", GH_ParamAccess.item, string.Empty);
        pManager[6].Optional = true;
        pManager.AddTextParameter("Drill Tool", "D", "Selector JSON por defecto para operaciones drill.", GH_ParamAccess.item, string.Empty);
        pManager[7].Optional = true;
        pManager.AddTextParameter("Operations", "OPS", "Lista de operaciones JSON.", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Job JSON", "JSON", "Job completo serializado como JSON.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string? jobName = null;
        string units = "mm";
        double thickness = 0.0;
        string origin = "lower_left";
        string zZero = "material_top";
        string profileSelector = string.Empty;
        string pocketSelector = string.Empty;
        string drillSelector = string.Empty;
        var operations = new List<string>();

        if (!da.GetData(0, ref jobName) || string.IsNullOrWhiteSpace(jobName)) return;
        if (!da.GetData(1, ref units)) return;
        if (!da.GetData(2, ref thickness)) return;
        da.GetData(3, ref origin);
        da.GetData(4, ref zZero);
        da.GetData(5, ref profileSelector);
        da.GetData(6, ref pocketSelector);
        da.GetData(7, ref drillSelector);
        if (!da.GetDataList(8, operations)) return;

        var job = new JsonObject
        {
            ["job_name"] = jobName,
            ["units"] = units,
            ["origin"] = origin,
            ["material"] = new JsonObject
            {
                ["thickness"] = thickness,
                ["z_zero"] = zZero
            }
        };

        try
        {
            var defaults = new JsonObject();
            var profileNode = JsonHelpers.ParseOptionalObject(profileSelector);
            var pocketNode = JsonHelpers.ParseOptionalObject(pocketSelector);
            var drillNode = JsonHelpers.ParseOptionalObject(drillSelector);

            if (profileNode is not null) defaults["profile"] = profileNode;
            if (pocketNode is not null) defaults["pocket"] = pocketNode;
            if (drillNode is not null) defaults["drill"] = drillNode;

            if (defaults.Count > 0)
            {
                job["tool_defaults"] = defaults;
            }

            var operationArray = new JsonArray();
            foreach (var operationJson in operations)
            {
                if (string.IsNullOrWhiteSpace(operationJson))
                {
                    continue;
                }

                operationArray.Add(JsonHelpers.ParseRequiredObject(operationJson));
            }

            job["operations"] = operationArray;
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"No se pudo construir el job JSON: {ex.Message}");
            return;
        }

        da.SetData(0, JsonHelpers.ToPrettyJson(job));
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override Guid ComponentGuid => new("3f09fa50-456e-4e4b-82f9-2ae65d41fd0f");
}