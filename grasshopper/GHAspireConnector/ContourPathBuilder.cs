using System;
using System.Collections.Generic;
using System.Linq;
using GHAspireConnector.Models;
using Rhino;
using Rhino.Geometry;

namespace GHAspireConnector;

internal static class ContourPathBuilder
{
    public static ContourPathResult BuildProfile(
        IList<Curve> sourceCurves,
        ToolCatalogEntry toolEntry,
        double startDepth,
        double cutDepth,
        string side)
    {
        if (sourceCurves.Count == 0)
        {
            throw new InvalidOperationException("No hay curvas de perfil.");
        }

        if (!side.Equals("inside", StringComparison.OrdinalIgnoreCase) &&
            !side.Equals("outside", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Side debe ser inside u outside para profile.");
        }

        var tolerance = GetTolerance();
        var radius = GetToolRadius(toolEntry);
        var offsetOutward = side.Equals("outside", StringComparison.OrdinalIgnoreCase);
        var contours = new List<Curve>();

        foreach (var sourceCurve in sourceCurves)
        {
            var planarCurve = ToPlanarClosedCurve(sourceCurve, "profile");
            var offsetCurve = OffsetClosedCurve(planarCurve, radius, offsetOutward, tolerance);
            if (offsetCurve is null)
            {
                throw new InvalidOperationException("No se pudo calcular el offset de una curva de profile.");
            }

            contours.Add(offsetCurve);
        }

        return BuildDepthPasses(contours, toolEntry, startDepth, cutDepth);
    }

    public static ContourPathResult BuildPocket(
        IList<Curve> sourceCurves,
        ToolCatalogEntry toolEntry,
        double startDepth,
        double cutDepth)
    {
        if (sourceCurves.Count == 0)
        {
            throw new InvalidOperationException("No hay curvas de pocket.");
        }

        var tolerance = GetTolerance();
        var radius = GetToolRadius(toolEntry);
        var stepover = toolEntry.StepoverMm > tolerance ? toolEntry.StepoverMm : Math.Max(toolEntry.DiameterMm * 0.4, tolerance);
        var currentContours = new List<Curve>();

        foreach (var sourceCurve in sourceCurves)
        {
            var planarCurve = ToPlanarClosedCurve(sourceCurve, "pocket");
            var firstPass = OffsetClosedCurve(planarCurve, radius, false, tolerance);
            if (firstPass is null)
            {
                throw new InvalidOperationException("No se pudo calcular el primer offset interior de una curva de pocket.");
            }

            currentContours.Add(firstPass);
        }

        var pocketContours = new List<Curve>();
        var iterations = 0;
        while (currentContours.Count > 0)
        {
            pocketContours.AddRange(currentContours.Select(curve => curve.DuplicateCurve()));

            var nextContours = new List<Curve>();
            foreach (var contour in currentContours)
            {
                var nextContour = OffsetClosedCurve(contour, stepover, false, tolerance);
                if (nextContour is null)
                {
                    continue;
                }

                if (GetAbsoluteArea(nextContour) <= tolerance * tolerance)
                {
                    continue;
                }

                nextContours.Add(nextContour);
            }

            currentContours = nextContours;
            iterations++;
            if (iterations > 512)
            {
                throw new InvalidOperationException("El calculo de offsets para pocket supero el limite de iteraciones.");
            }
        }

        if (pocketContours.Count == 0)
        {
            throw new InvalidOperationException("No se pudieron generar trayectorias de pocket.");
        }

        return BuildDepthPasses(pocketContours, toolEntry, startDepth, cutDepth);
    }

    private static ContourPathResult BuildDepthPasses(
        IReadOnlyList<Curve> planarContours,
        ToolCatalogEntry toolEntry,
        double startDepth,
        double cutDepth)
    {
        if (cutDepth <= 0)
        {
            throw new InvalidOperationException("Cut Depth debe ser mayor que cero.");
        }

        var stepdown = toolEntry.StepdownMm > 0 ? toolEntry.StepdownMm : cutDepth;
        var tolerance = GetTolerance();
        var passes = new List<ContourToolpathPass>();
        var accumulatedDepth = 0.0;

        while (accumulatedDepth < cutDepth - tolerance)
        {
            accumulatedDepth = Math.Min(accumulatedDepth + stepdown, cutDepth);
            var z = -(startDepth + accumulatedDepth);
            var pass = new ContourToolpathPass { Z = z };

            foreach (var planarContour in planarContours)
            {
                var contourAtDepth = planarContour.DuplicateCurve();
                contourAtDepth.Transform(Transform.Translation(0.0, 0.0, z));
                pass.Toolpaths.Add(contourAtDepth);
            }

            passes.Add(pass);
        }

        return new ContourPathResult
        {
            ToolEntry = toolEntry,
            StartDepth = startDepth,
            CutDepth = cutDepth,
            Passes = passes
        };
    }

    private static Curve ToPlanarClosedCurve(Curve sourceCurve, string operationName)
    {
        var projectedCurve = sourceCurve.DuplicateCurve();
        projectedCurve.Transform(Transform.PlanarProjection(Plane.WorldXY));

        if (!projectedCurve.IsClosed)
        {
            throw new InvalidOperationException($"Las curvas de {operationName} deben ser cerradas.");
        }

        return projectedCurve;
    }

    private static Curve? OffsetClosedCurve(Curve sourceCurve, double distance, bool chooseLargerArea, double tolerance)
    {
        var candidates = new List<Curve>();
        AddOffsetCandidates(sourceCurve, distance, tolerance, candidates);
        AddOffsetCandidates(sourceCurve, -distance, tolerance, candidates);

        var valid = candidates
            .Where(curve => curve.IsClosed)
            .Select(curve => new { Curve = curve, Area = GetAbsoluteArea(curve) })
            .Where(item => item.Area > tolerance * tolerance)
            .ToList();

        if (valid.Count == 0)
        {
            return null;
        }

        return chooseLargerArea
            ? valid.OrderByDescending(item => item.Area).First().Curve
            : valid.OrderBy(item => item.Area).First().Curve;
    }

    private static void AddOffsetCandidates(Curve sourceCurve, double distance, double tolerance, List<Curve> candidates)
    {
        var offsetCurves = sourceCurve.Offset(Plane.WorldXY, distance, tolerance, CurveOffsetCornerStyle.Sharp);
        if (offsetCurves is null)
        {
            return;
        }

        foreach (var offsetCurve in offsetCurves)
        {
            var projectedCurve = offsetCurve.DuplicateCurve();
            projectedCurve.Transform(Transform.PlanarProjection(Plane.WorldXY));
            candidates.Add(projectedCurve);
        }
    }

    private static double GetAbsoluteArea(Curve curve)
    {
        var area = AreaMassProperties.Compute(curve);
        return area is null ? 0.0 : Math.Abs(area.Area);
    }

    private static double GetToolRadius(ToolCatalogEntry toolEntry)
    {
        if (toolEntry.DiameterMm <= 0)
        {
            throw new InvalidOperationException("La herramienta resuelta no tiene un diametro valido.");
        }

        return toolEntry.DiameterMm / 2.0;
    }

    private static double GetTolerance()
    {
        return RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.01;
    }
}