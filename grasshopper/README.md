# Grasshopper

Esta carpeta queda reservada para:

- definiciones `.gh` y `.ghx`
- plugin en C# para Grasshopper
- exportadores DXF y JSON
- logica de preparacion CAM 2.5D
- pruebas de contrato de datos con Aspire

## Plugin base

El proyecto inicial del plugin esta en [grasshopper/GHAspireConnector/GHAspireConnector.csproj](grasshopper/GHAspireConnector/GHAspireConnector.csproj).
La solucion del plugin esta en [grasshopper/GHAspireConnector.slnx](grasshopper/GHAspireConnector.slnx).

Componentes iniciales previstos:

- lectura del catalogo simplificado de herramientas
- construccion de operaciones JSON
- construccion de jobs JSON

La idea es mover aqui la mayor parte de la logica que hoy seria incomoda de mantener con componentes Python dispersos.