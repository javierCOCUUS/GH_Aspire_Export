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
- `Pick Tool`: selecciona una herramienta filtrada y devuelve el selector JSON.
- `Build Drill Preview`: primera operacion CAM determinista con lectura de herramienta desde catalogo y salidas separadas para rapid, approach, plunge y retract.
- `Create Operation JSON`: genera el JSON de una operacion.
- `Create Job JSON`: empaqueta material, defaults y operaciones en el job final.
- `Write Text File`: escribe un JSON o texto a disco para pruebas de flujo.

Los iconos del plugin se toman de [grasshopper/icons](../icons).

## Legibilidad en Grasshopper

Los nicknames de entradas y salidas se sincronizan con nombres legibles para que en el canvas se vean palabras completas, no solo iniciales, tambien al recargar instancias ya existentes.

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

## Prueba intermedia recomendada

1. Usar `Read Tool Catalog` para leer el catalogo.
2. Elegir un selector de herramienta.
3. Generar una o varias operaciones con `Create Operation JSON`.
4. Empaquetarlas con `Create Job JSON`.
5. Escribir el resultado con `Write Text File` a un `.json`.

## Siguiente paso recomendado

Añadir un componente que exporte directamente DXF + JSON desde Grasshopper, en vez de limitarse a montar strings JSON sueltos.