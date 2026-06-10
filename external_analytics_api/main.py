import math
import os
import tempfile

from fastapi import FastAPI, File, HTTPException, UploadFile

from trajectory_metrics import analyze_session


app = FastAPI(
    title="Unity Trajectory Analytics API",
    version="0.1.0",
)


def sanitize_for_json(value):
    if isinstance(value, float) and not math.isfinite(value):
        return None

    if isinstance(value, dict):
        return {key: sanitize_for_json(item) for key, item in value.items()}

    if isinstance(value, list):
        return [sanitize_for_json(item) for item in value]

    return value


@app.get("/health")
def health():
    return {"success": True, "status": "ok"}


@app.post("/analyze-session")
async def analyze_session_endpoint(file: UploadFile = File(...)):
    suffix = os.path.splitext(file.filename or "")[1] or ".csv"
    temp_path = None

    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as temp_file:
            temp_path = temp_file.name
            temp_file.write(await file.read())

        metrics = analyze_session(temp_path)

        return {
            "success": True,
            "metrics": sanitize_for_json(metrics),
        }
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc
    finally:
        if temp_path and os.path.exists(temp_path):
            os.remove(temp_path)
