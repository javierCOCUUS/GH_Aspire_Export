# JSON Contract

Contrato actual propuesto entre Grasshopper y Aspire.

## Job

- `job_name`: nombre del trabajo.
- `units`: `mm` o `in`.
- `origin`: `lower_left`, `BOTTOM_LEFT`, `TOP_LEFT`, `TOP_RIGHT`, `BOTTOM_RIGHT` o `CENTER`.
- `material.thickness`: espesor del material.
- `material.z_zero`: referencia Z.

## Operations

Cada elemento de `operations[]` puede incluir:

- `name`: nombre de la operacion.
- `layer`: capa del DXF a usar.
- `type`: `profile`, `pocket` o `drill`.
- `start_depth`: profundidad inicial.
- `cut_depth`: profundidad final de corte.
- `side`: `inside` u `outside` para `profile`.
- `tool`: selector opcional de herramienta.

## Tool selector

`tool` puede incluir cualquiera de estos selectores:

- `id`: identificador exacto del catalogo.
- `tool_type`: nombre normalizado como `end_mill` o `through_drill`.
- `diameter_mm`: diametro esperado.
- `tool_number`: numero de herramienta.
- `aspire_group`: grupo esperado dentro de Aspire.

## Resolucion actual

El importador busca coincidencias exactas en el catalogo JSON y usa fallback interno si no encuentra una herramienta valida.

## Extension futura sugerida

Si mas adelante Grasshopper resuelve CAM 2.5D con mayor detalle, este contrato puede ampliarse con:

- estrategia de corte
- tabs
- ramps
- offsets
- orden de mecanizado
- postprocesador