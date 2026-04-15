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
- `Postprocess Drill GCode`: genera G-code de drill leyendo un postprocesador real de Aspire.
- `Create Operation JSON`: genera el JSON de una operacion.
- `Create Job JSON`: empaqueta material, defaults y operaciones en el job final.
- `Write Text File`: escribe un JSON o texto a disco para pruebas de flujo.

Los iconos del plugin se toman de [grasshopper/icons](../icons).

## Legibilidad en Grasshopper

Los nicknames de entradas y salidas se sincronizan con nombres legibles para que en el canvas se vean palabras completas, no solo iniciales, tambien al recargar instancias ya existentes.

## Convencion de seleccion de herramienta

Siempre que un componente permita elegir numero de herramienta, la UI debe ofrecer una `Value List` con estas opciones:

- `Catalog`
- `T1`
- `T2`
- `T3`

La salida final debe propagar ese `tool_number` al selector JSON y el resto de componentes deben respetarlo durante preview y postprocesado.

En el caso actual, `Pick Tool` crea y conecta automaticamente esa `Value List` al añadirse al canvas.

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

## Prueba de drill a G-code

La prueba corta de postprocesado directo esta descrita en [docs/drill-gcode-smoke-test.md](../../docs/drill-gcode-smoke-test.md).

## Siguiente paso recomendado

Añadir un componente que exporte directamente DXF + JSON desde Grasshopper, en vez de limitarse a montar strings JSON sueltos.