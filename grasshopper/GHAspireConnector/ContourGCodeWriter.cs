using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GHAspireConnector.Models;
using Rhino.Geometry;

namespace GHAspireConnector;

internal static class ContourGCodeWriter
{
    public static List<string> Build(
        ContourPathResult pathResult,
        PostProcessorDefinition post,
        Point3d home,
        double safeZ,
        double approachZ)
    {
        if (pathResult.Passes.Count == 0)
        {
            throw new InvalidOperationException("No hay pasadas de contorno para postprocesar.");
        }

        var toolEntry = pathResult.ToolEntry;
        var lines = new List<string>();
        var tokenValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["[T]"] = toolEntry.ToolNumber.ToString(CultureInfo.InvariantCulture),
            ["[S]"] = FormatToken("S", toolEntry.RpmRecommend, 0),
            ["[F]"] = FormatToken("F", toolEntry.FeedRecommendMmPerMin, 1),
            ["[XH]"] = FormatToken("X", home.X),
            ["[YH]"] = FormatToken("Y", home.Y),
            ["[ZH]"] = FormatToken("Z", home.Z)
        };

        AppendBlock(lines, post.GetBlock("HEADER"), tokenValues, toolEntry.ToolNumber);

        var previousSafePoint = home;
        var tolerance = Rhino.RhinoMath.ZeroTolerance;

        foreach (var pass in pathResult.Passes)
        {
            foreach (var toolpath in pass.Toolpaths)
            {
                var points = SampleCurvePoints(toolpath, toolEntry.DiameterMm).ToList();
                if (points.Count < 2)
                {
                    continue;
                }

                var startPoint = points[0];
                var safeStart = new Point3d(startPoint.X, startPoint.Y, safeZ);
                var approachPoint = new Point3d(startPoint.X, startPoint.Y, approachZ);

                if (previousSafePoint.DistanceTo(safeStart) > tolerance)
                {
                    tokenValues["[X]"] = FormatToken("X", safeStart.X);
                    tokenValues["[Y]"] = FormatToken("Y", safeStart.Y);
                    tokenValues["[Z]"] = string.Empty;
                    AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);
                }

                if (System.Math.Abs(previousSafePoint.Z - safeZ) > tolerance)
                {
                    tokenValues["[X]"] = string.Empty;
                    tokenValues["[Y]"] = string.Empty;
                    tokenValues["[Z]"] = FormatToken("Z", safeZ);
                    AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);
                }

                if (System.Math.Abs(safeZ - approachZ) > tolerance)
                {
                    tokenValues["[X]"] = string.Empty;
                    tokenValues["[Y]"] = string.Empty;
                    tokenValues["[Z]"] = FormatToken("Z", approachZ);
                    AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);
                }

                tokenValues["[X]"] = string.Empty;
                tokenValues["[Y]"] = string.Empty;
                tokenValues["[Z]"] = FormatToken("Z", startPoint.Z);
                tokenValues["[F]"] = FormatToken("F", toolEntry.PlungeRecommendMmPerMin, 1);
                AppendBlock(lines, post.GetBlock("FIRST_FEED_MOVE"), tokenValues, toolEntry.ToolNumber);

                tokenValues["[F]"] = FormatToken("F", toolEntry.FeedRecommendMmPerMin, 1);
                var firstCutMove = true;
                for (var index = 1; index < points.Count; index++)
                {
                    var point = points[index];
                    tokenValues["[X]"] = FormatToken("X", point.X);
                    tokenValues["[Y]"] = FormatToken("Y", point.Y);
                    tokenValues["[Z]"] = FormatToken("Z", point.Z);

                    AppendBlock(
                        lines,
                        post.GetBlock(firstCutMove ? "FIRST_FEED_MOVE" : "FEED_MOVE"),
                        tokenValues,
                        toolEntry.ToolNumber);

                    firstCutMove = false;
                }

                var safeEnd = new Point3d(points[^1].X, points[^1].Y, safeZ);
                tokenValues["[X]"] = string.Empty;
                tokenValues["[Y]"] = string.Empty;
                tokenValues["[Z]"] = FormatToken("Z", safeEnd.Z);
                AppendBlock(lines, post.GetBlock("RAPID_MOVE"), tokenValues, toolEntry.ToolNumber);
                previousSafePoint = safeEnd;
            }
        }

        AppendBlock(lines, post.GetBlock("FOOTER"), tokenValues, toolEntry.ToolNumber);
        return lines;
    }

    private static IEnumerable<Point3d> SampleCurvePoints(Curve curve, double toolDiameterMm)
    {
        var tolerance = Rhino.RhinoMath.ZeroTolerance;
        var length = curve.GetLength();
        var targetSegmentLength = System.Math.Max(toolDiameterMm / 4.0, 0.5);
        var segmentCount = System.Math.Max(12, (int)System.Math.Ceiling(length / targetSegmentLength));
        var sampledPoints = new List<Point3d>(segmentCount + 1);

        for (var index = 0; index <= segmentCount; index++)
        {
            var parameter = curve.Domain.ParameterAt((double)index / segmentCount);
            var point = curve.PointAt(parameter);
            if (sampledPoints.Count == 0 || sampledPoints[^1].DistanceTo(point) > tolerance)
            {
                sampledPoints.Add(point);
            }
        }

        if (curve.IsClosed && sampledPoints.Count > 1 && sampledPoints[^1].DistanceTo(sampledPoints[0]) > tolerance)
        {
            sampledPoints.Add(sampledPoints[0]);
        }

        return sampledPoints;
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
}