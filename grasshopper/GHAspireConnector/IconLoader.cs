using System.Drawing;
using System.Reflection;

namespace GHAspireConnector;

internal static class IconLoader
{
    public static Bitmap? Load(string fileName)
    {
        var resourceName = $"GHAspireConnector.Icons.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        return new Bitmap(stream);
    }
}