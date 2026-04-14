# JSON Contract

Contrato actual propuesto entre Grasshopper y Aspire.

## Job

- `job_name`: nombre del trabajo.
- `units`: `mm` o `in`.
- `origin`: `lower_left`, `BOTTOM_LEFT`, `TOP_LEFT`, `TOP_RIGHT`, `BOTTOM_RIGHT` o `CENTER`.
- `material.thickness`: espesor del material.
- `material.z_zero`: referencia Z.
- `tool_defaults`: selector opcional de herramientas por tipo de operacion.

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

## Tool defaults

`tool_defaults` permite fijar un selector por tipo de operacion:

```json
{
	"tool_defaults": {
		"profile": {
			"id": "g-g-0504af68-bb0f-4b1f-811f-46f94bbec767"
		},
		"pocket": {
			"tool_type": "end_mill",
			"diameter_mm": 6.0
		},
		"drill": {
			"tool_type": "through_drill",
			"diameter_mm": 5.0
		}
	}
}
```

Luego cada operacion concreta puede sobreescribir ese selector usando `operations[].tool`.

## Resolucion actual

El importador resuelve la herramienta asi:

1. parte de `tool_defaults.<type>` si existe
2. aplica `operations[].tool` si existe
3. busca coincidencia en el catalogo JSON
4. usa fallback interno si no encuentra una herramienta valida

## Extension futura sugerida

Si mas adelante Grasshopper resuelve CAM 2.5D con mayor detalle, este contrato puede ampliarse con:

- estrategia de corte
- tabs
- ramps
- offsets
- orden de mecanizado
- postprocesador