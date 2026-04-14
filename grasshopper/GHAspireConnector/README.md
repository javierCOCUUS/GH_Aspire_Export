# GH Aspire Connector Plugin

Base de plugin para Grasshopper en C#.

## Objetivo

Mover a un plugin mantenible la logica que en Python seria dificil de escalar:

- lectura del catalogo de herramientas
- seleccion por tipo de operacion
- override por operacion
- construccion del JSON final para Aspire

## Componentes iniciales

- `Read Tool Catalog`: lee [tools/grasshopper_tool_catalog.json](../../tools/grasshopper_tool_catalog.json).
- `Create Operation JSON`: genera el JSON de una operacion.
- `Create Job JSON`: empaqueta material, defaults y operaciones en el job final.

## Build local

El proyecto referencia Rhino 8 y Grasshopper desde rutas tipicas de Windows:

- `C:\Program Files\Rhino 8\System\RhinoCommon.dll`
- `C:\Program Files\Rhino 8\Plug-ins\Grasshopper\Grasshopper.dll`

Compilacion:

```powershell
dotnet build grasshopper/GHAspireConnector/GHAspireConnector.csproj
```

Solucion:

```powershell
dotnet sln grasshopper/GHAspireConnector.slnx list
```

El ensamblado se genera con extension `.gha`.

## Siguiente paso recomendado

Añadir un componente que exporte directamente DXF + JSON desde Grasshopper, en vez de limitarse a montar strings JSON sueltos.