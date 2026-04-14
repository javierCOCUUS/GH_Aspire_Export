# Grasshopper Aspire Connector

Conector de trabajo entre Grasshopper y Vectric Aspire.

El objetivo del proyecto es definir un flujo donde Grasshopper genere la geometria y la intencion de mecanizado, exporte un DXF y un JSON, y Aspire consuma ambos archivos mediante Lua para reconstruir el trabajo y crear las operaciones CAM.

## Estado actual

- Grasshopper ya genera un archivo DXF con capas de trabajo.
- Grasshopper ya genera un archivo JSON con la definicion del job y de las operaciones.
- El script [GH_Aspire_importer.lua](GH_Aspire_importer.lua) importa el DXF asociado al JSON, ajusta el stock y crea toolpaths de tipo `profile`, `pocket` y `drill` en Aspire.
- Hay un ejemplo funcional basado en `pieza_001`.

## Flujo actual

1. Grasshopper exporta `pieza_001.dxf`.
2. Grasshopper exporta `pieza_001.json` con:
   - nombre del job
   - unidades
   - espesor de material
   - origen XY
   - operaciones y capas
3. Aspire ejecuta [GH_Aspire_importer.lua](GH_Aspire_importer.lua).
4. Lua localiza el JSON, deduce el DXF asociado, importa la geometria y genera las toolpaths.

## Archivos actuales

- [GH_Aspire_importer.lua](GH_Aspire_importer.lua): importador principal en Lua para Aspire.
- [pieza_001.dxf](pieza_001.dxf): ejemplo de geometria 2D exportada.
- [pieza_001.json](pieza_001.json): ejemplo de definicion del job.
- [pieza_001.3dm](pieza_001.3dm): geometria base asociada al ejemplo.
- [tools/fetched_from_vtdb.json](tools/fetched_from_vtdb.json): datos de herramientas extraidos de VTDB.

## Convencion de datos actual

El JSON esperado contiene esta estructura base:

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
      "side": "inside"
    }
  ]
}
```

## Limitaciones actuales

- El script Lua usa una ruta fija en `JSON_PATH`.
- La configuracion de herramientas esta embebida en el propio script Lua.
- El parser JSON en Lua es simple y esta pensado para el formato actual exportado por Grasshopper.

## Direccion prevista

- Separar ejemplos, scripts y documentacion.
- Parametrizar la ruta del JSON y la seleccion del archivo a importar.
- Conectar la base de datos de herramientas con la generacion de toolpaths.
- Mover la mayor parte de la logica CAM 2.5D a Grasshopper, dejando Aspire como ejecutor/importador cuando tenga sentido.

## Siguiente estructura recomendada

- `grasshopper/`: definiciones GH, exportadores y logica CAM.
- `aspire/`: scripts Lua y utilidades especificas de Aspire.
- `samples/`: juegos de prueba DXF/JSON/3DM.
- `tools/`: catalogos y conversiones de herramientas.
- `docs/`: notas de formato, decisiones y roadmap.

## Primeros siguientes pasos

1. Mover el script Lua a una carpeta `aspire/` sin romper el flujo actual.
2. Guardar uno o dos ejemplos completos en `samples/`.
3. Hacer que Lua reciba el JSON de forma menos rigida.
4. Definir el contrato exacto entre Grasshopper y Aspire.