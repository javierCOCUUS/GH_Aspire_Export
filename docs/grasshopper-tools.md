# Grasshopper Tool Selection

Propuesta de trabajo para que Grasshopper lea herramientas disponibles y permita elegirlas con suficiente flexibilidad sin cargar al usuario con el JSON bruto del VTDB.

## Modelo recomendado

Usar tres capas:

1. Catalogo simplificado para UI en Grasshopper.
2. Defaults por tipo de operacion.
3. Overrides por operacion concreta.

## Catalogo simplificado

Grasshopper no deberia leer directamente [tools/fetched_from_vtdb.json](tools/fetched_from_vtdb.json) para poblar menus, porque ese formato es demasiado crudo y mezcla campos internos con datos de seleccion.

Para la UI de GH conviene leer [tools/grasshopper_tool_catalog.json](tools/grasshopper_tool_catalog.json), que normaliza:

- `id`
- `display_name`
- `tool_type`
- `aspire_group`
- `diameter_mm`
- `tool_number`
- `operation_types`
- feeds y rpm recomendados
- `selector` listo para exportar al job JSON

## UX recomendada en Grasshopper

### Nivel 1: defaults globales

Tres inputs o dropdowns para elegir herramienta por defecto para:

- `profile`
- `pocket`
- `drill`

Eso alimenta `tool_defaults` en el JSON exportado.

### Nivel 2: overrides por operacion

Cada operacion generada por Grasshopper puede llevar una herramienta especifica distinta.

Campos minimos recomendados por operacion:

- `name`
- `layer`
- `type`
- `tool_override_enabled`
- `selected_tool_id`

Si `tool_override_enabled = false`, la operacion hereda el default de su tipo.
Si `tool_override_enabled = true`, la operacion exporta `operations[].tool`.

## JSON exportado por GH

### Caso simple

Defaults por tipo y sin overrides:

```json
{
  "tool_defaults": {
    "profile": { "id": "tool-profile-01" },
    "pocket": { "id": "tool-pocket-01" },
    "drill": { "id": "tool-drill-01" }
  },
  "operations": [
    { "name": "CUT_A", "type": "profile", "layer": "CUT_A" },
    { "name": "POCKET_A", "type": "pocket", "layer": "POCKET_A" }
  ]
}
```

### Caso mixto

Default para `profile`, pero una operacion concreta usa otra herramienta:

```json
{
  "tool_defaults": {
    "profile": { "id": "tool-profile-01" }
  },
  "operations": [
    {
      "name": "CUT_INSIDE",
      "type": "profile",
      "layer": "CUT_INSIDE"
    },
    {
      "name": "CUT_OUTSIDE",
      "type": "profile",
      "layer": "CUT_OUTSIDE",
      "tool": { "id": "tool-profile-02" }
    }
  ]
}
```

## Recomendacion practica para GH

La opcion mas robusta es:

- un componente que lea el catalogo simplificado
- una lista filtrable por `operation_types`
- un selector por defecto para cada tipo
- una tabla de overrides por operacion
- export final a JSON con `tool_defaults` y `operations[].tool`

Con esto no hay que duplicar toda la herramienta dentro de cada operacion. Solo se exportan selectores estables, y Aspire resuelve el resto desde el catalogo.