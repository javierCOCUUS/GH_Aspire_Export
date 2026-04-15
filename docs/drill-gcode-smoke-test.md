# Drill G-code Smoke Test

Prueba corta para validar el flujo `Grasshopper -> selector de herramienta -> postprocesador Aspire -> G-code`.

## Objetivo

Comprobar estas tres cosas:

- el selector de herramienta llega correctamente al componente de postprocesado
- el numero de herramienta elegido genera `T1`, `T2` o `T3` segun corresponda
- el WCS resultante sale como `G54`, `G55` o `G56`

## Componentes a usar

1. `Pick Tool`
2. `Postprocess Drill GCode`
3. `Write Text File`

## Preparacion

1. Usa como catalogo [tools/grasshopper_tool_catalog.json](../tools/grasshopper_tool_catalog.json).
2. Usa como postprocesador [postprocessors/aspire/mi_maquina.pp](../postprocessors/aspire/mi_maquina.pp).
3. Define una lista sencilla de puntos de taladrado.
4. En `Pick Tool`, filtra por operacion `drill`.
5. En `Pick Tool`, usa la `Value List` de `Tool Number` con una de estas opciones:
   `Catalog`, `T1`, `T2`, `T3`.

## Prueba minima

1. Selecciona una broca con `Pick Tool`.
2. Pon `Tool Number = T1`.
3. Conecta la salida `Selector` a `Postprocess Drill GCode`.
4. Genera el texto final y revisa que aparezcan estas lineas al principio:

```text
T1M6
G54
```

5. Repite la prueba con `Tool Number = T2` y luego con `Tool Number = T3`.

## Resultado esperado

- `T1` debe producir `T1M6` y `G54`
- `T2` debe producir `T2M6` y `G55`
- `T3` debe producir `T3M6` y `G56`

## Notas

- Si dejas `Tool Number = Catalog`, el plugin conserva el numero de herramienta definido por el catalogo.
- Si cambias un componente antiguo de `Pick Tool`, conviene insertarlo de nuevo en el canvas para que aparezca la `Value List` automaticamente.
- El ejemplo de referencia de Aspire sigue estando en [postprocessors/examples/drill_sample.tap](../postprocessors/examples/drill_sample.tap), pero la comprobacion principal aqui es la pareja `TnM6` + `G5n` correcta.