using System;
using System.Collections.Generic;
using GHAspireConnector.Models;
using Rhino.Geometry;

namespace GHAspireConnector;

internal static class DrillPathBuilder
{
    public static DrillPathResult Build(
        IList<Point3d> drillPoints,
        ToolCatalogEntry toolEntry,
        double startDepth,
        double cutDepth,
        double safeZ,
        double approachZ)
    {
        if (drillPoints.Count == 0)
        {
            throw new InvalidOperationException("No hay puntos de taladrado.");
        }

        var topZ = -startDepth;
        var targetZ = -(startDepth + cutDepth);

        var result = new DrillPathResult
        {
            ToolEntry = toolEntry,
            StartDepth = startDepth,
            CutDepth = cutDepth,
            SafeZ = safeZ,
            ApproachZ = approachZ,
            Holes = new List<DrillHolePath>()
        };

        foreach (var drillPoint in drillPoints)
        {
            result.Holes.Add(new DrillHolePath
            {
                SafePoint = new Point3d(drillPoint.X, drillPoint.Y, safeZ),
                ApproachPoint = new Point3d(drillPoint.X, drillPoint.Y, approachZ),
                TopPoint = new Point3d(drillPoint.X, drillPoint.Y, topZ),
                BottomPoint = new Point3d(drillPoint.X, drillPoint.Y, targetZ)
            });
        }

        return result;
    }
}