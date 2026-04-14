# Grasshopper Aspire Connector

Conector de trabajo entre Grasshopper y Vectric Aspire.

El objetivo del proyecto es definir un flujo donde Grasshopper genere la geometria y la intencion de mecanizado, exporte un DXF y un JSON, y Aspire consuma ambos archivos mediante Lua para reconstruir el trabajo, ajustar el stock y crear las operaciones CAM.

## Estructura actual

- [aspire/GH_Aspire_importer.lua](aspire/GH_Aspire_importer.lua): importador principal para Aspire.
- [grasshopper/README.md](grasshopper/README.md): carpeta reservada para definiciones GH y exportadores.
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

## Contrato de datos actual

El JSON del job contiene:

- `job_name`, `units`, `origin`
- `material.thickness` y `material.z_zero`
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

1. `operations[].tool.id`
2. coincidencia por `tool_type`, `diameter_mm`, `tool_number` o `aspire_group`
3. fallback interno si el catalogo no encuentra una coincidencia valida

Los avances y avances de corte del catalogo VTDB se aplican en Aspire usando los valores recomendados del JSON de herramientas.

## Estado actual

- Ya hay seleccion interactiva de JSON y DXF en Aspire.
- Ya se usa el catalogo de herramientas JSON para crear las herramientas.
- Se mantienen fallbacks internos para no bloquear el flujo si una herramienta no se encuentra.

## Siguientes pasos razonables

1. Formalizar mejor el contrato JSON en un documento dedicado.
2. Llevar los exportadores de Grasshopper a [grasshopper/README.md](grasshopper/README.md) y sus archivos asociados.
3. Definir qué operaciones CAM 2.5D deben resolverse directamente en Grasshopper.