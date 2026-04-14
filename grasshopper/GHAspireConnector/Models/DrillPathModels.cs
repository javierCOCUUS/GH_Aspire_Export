using Rhino.Geometry;

namespace GHAspireConnector.Models;

public sealed class DrillHolePath
{
    public Point3d SafePoint { get; set; }

    public Point3d ApproachPoint { get; set; }

    public Point3d TopPoint { get; set; }

    public Point3d BottomPoint { get; set; }
}

public sealed class DrillPathResult
{
    public ToolCatalogEntry ToolEntry { get; set; } = new();

    public double StartDepth { get; set; }

    public double CutDepth { get; set; }

    public double SafeZ { get; set; }

    public double ApproachZ { get; set; }

    public List<DrillHolePath> Holes { get; set; } = new();
}