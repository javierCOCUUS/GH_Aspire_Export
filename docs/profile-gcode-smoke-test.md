# Smoke test de Profile G-code

Prueba corta para validar el flujo directo de `profile` a G-code desde Grasshopper.

## Componentes

- `Pick Tool`
- `Postprocess Profile GCode`
- `Write Text File`

## Ajuste minimo

1. Cargar [tools/grasshopper_tool_catalog.json](../tools/grasshopper_tool_catalog.json).
2. En `Pick Tool`, poner `Operation Type = profile`.
3. Elegir una herramienta compatible con `profile`.
4. En `Postprocess Profile GCode`, conectar una o varias curvas cerradas planas.
5. Usar un `Cut Depth` pequeno para la primera prueba.
6. Escribir el resultado a un `.tap` con `Write Text File`.

## Comprobaciones

1. La salida debe empezar con cabecera de post y `TnM6`.
2. Debe aparecer `G54`, `G55` o `G56` segun `T1`, `T2` o `T3`.
3. Debe haber rapids a `Safe Z` antes de cada plunge.
4. Debe haber un plunge con feed de bajada y luego movimientos de corte con feed de trabajo.
5. El numero de niveles Z debe coincidir con `stepdown` y `Cut Depth`.
6. La herramienta resuelta debe coincidir con el selector elegido en `Pick Tool`.

## Limitacion actual

La salida de contornos se linealiza en segmentos para generar el G-code. Para esta fase la prioridad es validar alturas, herramienta, offsets y estructura del postprocesado.