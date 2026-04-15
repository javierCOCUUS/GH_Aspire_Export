using System.Collections.Generic;
using Rhino.Geometry;

namespace GHAspireConnector.Models;

public sealed class ContourToolpathPass
{
    public double Z { get; set; }

    public List<Curve> Toolpaths { get; set; } = new();
}

public sealed class ContourPathResult
{
    public ToolCatalogEntry ToolEntry { get; set; } = new();

    public double StartDepth { get; set; }

    public double CutDepth { get; set; }

    public List<ContourToolpathPass> Passes { get; set; } = new();
}

public sealed class ContourPreviewCurves
{
    public List<Curve> RapidPaths { get; set; } = new();

    public List<Curve> ApproachPaths { get; set; } = new();

    public List<Curve> PlungePaths { get; set; } = new();

    public List<Curve> CutPaths { get; set; } = new();

    public List<Curve> RetractPaths { get; set; } = new();
}