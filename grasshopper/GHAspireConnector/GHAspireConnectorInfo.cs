using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GHAspireConnector;

public sealed class GHAspireConnectorInfo : GH_AssemblyInfo
{
    public override string Name => "GH Aspire Connector";

    public override Bitmap? Icon => IconLoader.Load("json.png");

    public override string Description => "Componentes de Grasshopper para seleccionar herramientas y construir jobs JSON para Aspire.";

    public override Guid Id => new("618eaa2f-f255-420b-8250-34933d0fb3e7");

    public override string AuthorName => "GH-Aspire-Javi";

    public override string AuthorContact => "https://github.com/javierCOCUUS/GH_Aspire_Export";
}