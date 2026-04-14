# Grasshopper Smoke Test

Prueba minima para validar que el plugin `GH Aspire` funciona de extremo a extremo antes de seguir desarrollandolo.

## Objetivo

Comprobar cuatro cosas:

1. `ToolCatalog` lee el catalogo simplificado.
2. `OpJson` genera una operacion valida.
3. `JobJson` empaqueta un job valido.
4. `WriteFile` escribe el JSON final a disco.

## Ruta del catalogo

Usar esta ruta en Grasshopper:

```text
C:\GH_Aspire_Export\tools\grasshopper_tool_catalog.json
```

## Ruta de salida recomendada

Usar esta ruta en Grasshopper:

```text
C:\GH_Aspire_Export\samples\gh_smoke\gh_smoke.json
```

## Montaje minimo

### 1. ToolCatalog

- `P`: panel con `C:\GH_Aspire_Export\tools\grasshopper_tool_catalog.json`
- `O`: panel con `profile`

Salidas esperadas:

- `N`: lista de nombres
- `I`: lista de ids
- `S`: lista de selectores JSON

### 2. Elegir una herramienta

Usar un `List Item` de Grasshopper:

- conectar `S` de `ToolCatalog` a la lista del `List Item`
- poner el indice a `0`

La salida del `List Item` ira a `TS` en `OpJson`.

### 3. OpJson

Entradas:

- `N`: `CUT_TEST`
- `L`: `CUT_TEST`
- `T`: `profile`
- `SD`: `0.0`
- `CD`: `18.0`
- `S`: `outside`
- `TS`: salida del `List Item`

Salida esperada:

- `J`: un objeto JSON con `tool` incluido.

### 4. JobJson

Entradas:

- `J`: `gh_smoke`
- `U`: `mm`
- `T`: `18.0`
- `O`: `lower_left`
- `Z`: `material_top`
- `P`: opcional, vacio para esta prueba
- `PK`: opcional, vacio para esta prueba
- `D`: opcional, vacio para esta prueba
- `OPS`: salida `J` de `OpJson`

Salida esperada:

- `JSON`: job completo con `operations`.

### 5. WriteFile

Entradas:

- `C`: salida `JSON` de `JobJson`
- `P`: panel con `C:\GH_Aspire_Export\samples\gh_smoke\gh_smoke.json`
- `W`: `Boolean Toggle` en `true`

Salidas esperadas:

- `P`: ruta escrita
- `E`: `true`
- `S`: `Archivo escrito`

## Validacion final

Abrir el archivo generado:

- [samples/gh_smoke/gh_smoke.json](samples/gh_smoke/gh_smoke.json)

Si el archivo existe y contiene un `operations[]` con una operacion `profile`, la prueba de humo del plugin queda validada.

## Siguiente comprobacion opcional

Con ese JSON y un DXF compatible, abrir [aspire/GH_Aspire_importer.lua](aspire/GH_Aspire_importer.lua) en Aspire y comprobar si lo importa correctamente.