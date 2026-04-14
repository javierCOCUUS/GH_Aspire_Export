# Roadmap

## Fase 1

- Consolidar el formato DXF + JSON exportado desde Grasshopper.
- Versionar ejemplos minimos reproducibles.
- Hacer reproducible la importacion en Aspire.

## Fase 2

- Extraer configuracion de herramientas fuera del script Lua.
- Leer parametros de herramienta desde una fuente comun.
- Validar capas y operaciones antes de abrir Aspire.

## Fase 3

- Resolver operaciones CAM 2.5D directamente en Grasshopper cuando sea mas robusto.
- Mantener Aspire como entorno de verificacion, postprocesado o ejecucion final.

## Preguntas abiertas

- Como se selecciona el archivo de entrada en Aspire: ruta fija, dialogo o carpeta observada.
- Que parte del tool library vive en Grasshopper y cual en Aspire.
- Que nivel de detalle CAM debe viajar en el JSON final.