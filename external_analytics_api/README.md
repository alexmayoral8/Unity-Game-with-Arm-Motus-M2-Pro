# External Analytics API

API local en Python para analizar un CSV de trayectoria exportado por Unity y calcular métricas del nivel completo.

## Crear entorno virtual

Desde la carpeta `external_analytics_api`:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

En macOS o Linux:

```bash
python3 -m venv .venv
source .venv/bin/activate
```

## Instalar dependencias

```bash
pip install -r requirements.txt
```

## Correr la API

```bash
uvicorn main:app --reload --host 127.0.0.1 --port 8000
```

La API queda disponible en:

```text
http://127.0.0.1:8000
```

## Probar el endpoint

Endpoint:

```text
POST /analyze-session
```

Debe enviarse un `multipart/form-data` con el campo `file`.

Ejemplo con `curl`:

```bash
curl -X POST "http://127.0.0.1:8000/analyze-session" \
  -F "file=@ruta/al/archivo.csv"
```

En PowerShell:

```powershell
curl.exe -X POST "http://127.0.0.1:8000/analyze-session" `
  -F "file=@C:\ruta\al\archivo.csv"
```

Respuesta esperada:

```json
{
  "success": true,
  "metrics": {
    "participant_id": "...",
    "level": "...",
    "status": "...",
    "mean_error": 0.0,
    "max_error": 0.0,
    "std_error": 0.0,
    "sparc": 0.0,
    "total_time": 0.0,
    "mean_velocity": 0.0,
    "collisions": 0,
    "final_supply": 0,
    "samples_real": 0,
    "samples_ideal": 0
  }
}
```

## Formato esperado del CSV

- Fila 1, columna 2: `Nivel`
- Fila 2, columna 2: `PilotID`
- Fila 3, columna 2: `Status`
- La tabla de datos empieza después de 4 líneas de encabezado
- Columna requerida: `Tipo`, con valores `ideal` y `real`
- Columnas esperadas: `T`, `PX`, `PY`, `PZ`, `Choque`, `Suministro`

## Métricas calculadas

- `mean_error`: distancia mínima promedio de cada punto real a los segmentos de la trayectoria ideal usando `PX` y `PY`
- `max_error`
- `std_error`
- `sparc`: suavidad calculada desde el perfil de velocidad
- `total_time`
- `mean_velocity`
- `collisions`: número de transiciones hacia `Choque == 1`
- `final_supply`: máximo de `Suministro`
- `samples_real`
- `samples_ideal`
