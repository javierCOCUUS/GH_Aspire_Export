using GHAspireConnector.Models;
using Rhino;
using Rhino.Geometry;

namespace GHAspireConnector;

internal static class ContourPreviewBuilder
{
    public static ContourPreviewCurves Build(ContourPathResult pathResult, double safeZ, double approachZ)
    {
        var result = new ContourPreviewCurves();
        var tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.01;
        Point3d? previousSafePoint = null;

        foreach (var pass in pathResult.Passes)
        {
            foreach (var toolpath in pass.Toolpaths)
            {
                var startPoint = toolpath.PointAtStart;
                var endPoint = toolpath.PointAtEnd;
                var safeStart = new Point3d(startPoint.X, startPoint.Y, safeZ);
                var safeEnd = new Point3d(endPoint.X, endPoint.Y, safeZ);
                var approachStart = new Point3d(startPoint.X, startPoint.Y, approachZ);

                if (previousSafePoint.HasValue && previousSafePoint.Value.DistanceTo(safeStart) > tolerance)
                {
                    result.RapidPaths.Add(new LineCurve(previousSafePoint.Value, safeStart));
                }

                if (Math.Abs(safeZ - approachZ) > tolerance)
                {
                    result.ApproachPaths.Add(new LineCurve(safeStart, approachStart));
                }

                var plungeStart = Math.Abs(safeZ - approachZ) > tolerance ? approachStart : safeStart;
                if (plungeStart.DistanceTo(startPoint) > tolerance)
                {
                    result.PlungePaths.Add(new LineCurve(plungeStart, startPoint));
                }

                result.CutPaths.Add(toolpath.DuplicateCurve());
                result.RetractPaths.Add(new LineCurve(endPoint, safeEnd));
                previousSafePoint = safeEnd;
            }
        }

        return result;
    }
}