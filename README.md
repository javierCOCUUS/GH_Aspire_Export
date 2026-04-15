# Grasshopper Aspire Connector

Conector de trabajo entre Grasshopper y Vectric Aspire.

El objetivo del proyecto es definir un flujo donde Grasshopper genere la geometria y la intencion de mecanizado, exporte un DXF y un JSON, y Aspire consuma ambos archivos mediante Lua para reconstruir el trabajo, ajustar el stock y crear las operaciones CAM.

## Estructura actual

- [aspire/GH_Aspire_importer.lua](aspire/GH_Aspire_importer.lua): importador principal para Aspire.
- [grasshopper/README.md](grasshopper/README.md): entrada al trabajo de Grasshopper.
- [grasshopper/GHAspireConnector/GHAspireConnector.csproj](grasshopper/GHAspireConnector/GHAspireConnector.csproj): base del plugin en C# para Grasshopper.
- [samples/pieza_001/pieza_001.dxf](samples/pieza_001/pieza_001.dxf): ejemplo de geometria 2D exportada.
- [samples/pieza_001/pieza_001.json](samples/pieza_001/pieza_001.json): ejemplo de job con operaciones y selectores de herramienta.
- [samples/pieza_001/pieza_001.3dm](samples/pieza_001/pieza_001.3dm): geometria base asociada al ejemplo.
- [tools/fetched_from_vtdb.json](tools/fetched_from_vtdb.json): catalogo de herramientas extraido de VTDB.
- [docs/roadmap.md](docs/roadmap.md): roadmap del proyecto.

## Flujo actual

1. Grasshopper exporta un DXF con capas de mecanizado.
2. Grasshopper exporta un JSON con datos del job y operaciones.
3. En Aspire se ejecuta [aspire/GH_Aspire_importer.lua](aspire/GH_Aspire_importer.lua).
4. El script abre un dialogo para elegir JSON, DXF y catalogo de herramientas.
5. Lua importa el DXF, detecta la capa `STOCK`, ajusta el sheet y crea toolpaths.
6. Las herramientas se resuelven a partir del catalogo JSON y se aplican en Aspire.
7. Grasshopper puede elegir herramientas por tipo de operacion o por operacion individual exportando selectores en el job JSON.

## Contrato de datos actual

El JSON del job contiene:

- `job_name`, `units`, `origin`
- `material.thickness` y `material.z_zero`
- `tool_defaults` opcional para defaults por `profile`, `pocket` y `drill`
- `operations[]` con `name`, `layer`, `type`, `start_depth`, `cut_depth`
- `operations[].tool` opcional para seleccionar una herramienta concreta del catalogo

Ejemplo resumido:

```json
{
  "job_name": "pieza_001",
  "units": "mm",
  "material": {
    "thickness": 18.0,
    "z_zero": "material_top"
  },
  "origin": "lower_left",
  "operations": [
    {
      "name": "CUT_INSIDE",
      "layer": "CUT_INSIDE",
      "type": "profile",
      "start_depth": 0.0,
      "cut_depth": 18.2,
      "side": "inside",
      "tool": {
        "id": "g-g-0504af68-bb0f-4b1f-811f-46f94bbec767",
        "tool_type": "end_mill",
        "diameter_mm": 6.0
      }
    }
  ]
}
```

## Resolucion de herramientas

El importador intenta resolver la herramienta por este orden:

1. `tool_defaults.<type>` como base para cada tipo de operacion
2. `operations[].tool` como override puntual de una operacion concreta
3. coincidencia por `id`, `tool_type`, `diameter_mm`, `tool_number` o `aspire_group`
4. fallback interno si el catalogo no encuentra una coincidencia valida

Los avances y avances de corte del catalogo VTDB se aplican en Aspire usando los valores recomendados del JSON de herramientas.

Para simplificar la lectura desde Grasshopper, el repo incluye un catalogo simplificado generado para UI y selecciones: [tools/grasshopper_tool_catalog.json](tools/grasshopper_tool_catalog.json).

## Estado actual

- Ya hay seleccion interactiva de JSON y DXF en Aspire.
- Ya se usa el catalogo de herramientas JSON para crear las herramientas.
- Se mantienen fallbacks internos para no bloquear el flujo si una herramienta no se encuentra.
- Ya hay una base de plugin en C# para Grasshopper, preferible a seguir creciendo con componentes Python sueltos.
- Cuando un componente de Grasshopper necesita elegir numero de herramienta, la UI debe exponer una `Value List` tipo `Catalog`, `T1`, `T2`, `T3` en lugar de pedir enteros libres.

## Validacion de drill a G-code

El flujo recomendado para validar el postprocesado directo de `drill` esta descrito en [docs/drill-gcode-smoke-test.md](docs/drill-gcode-smoke-test.md).

## Siguientes pasos razonables

1. Formalizar mejor el contrato JSON en un documento dedicado.
2. Llevar los exportadores de Grasshopper al plugin en [grasshopper/GHAspireConnector/GHAspireConnector.csproj](grasshopper/GHAspireConnector/GHAspireConnector.csproj).
3. Definir qué operaciones CAM 2.5D deben resolverse directamente en Grasshopper.