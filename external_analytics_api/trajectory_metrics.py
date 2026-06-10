import numpy as np

from sparc import safe_sparc
from trajectory_reader import read_unity_trajectory_csv


def point_to_segment_distance(point, segment_start, segment_end):
    point = np.asarray(point, dtype=float)
    segment_start = np.asarray(segment_start, dtype=float)
    segment_end = np.asarray(segment_end, dtype=float)

    segment = segment_end - segment_start
    segment_length_sq = np.dot(segment, segment)

    if segment_length_sq == 0:
        return np.linalg.norm(point - segment_start)

    projection = np.dot(point - segment_start, segment) / segment_length_sq
    projection = np.clip(projection, 0, 1)

    closest_point = segment_start + projection * segment
    return np.linalg.norm(point - closest_point)


def compute_tracking_error(real_df, ideal_df):
    real_points = real_df[["PX", "PY"]].to_numpy(dtype=float)
    ideal_points = ideal_df[["PX", "PY"]].to_numpy(dtype=float)

    if len(real_points) == 0 or len(ideal_points) < 2:
        return {
            "mean_error": np.nan,
            "max_error": np.nan,
            "std_error": np.nan,
        }

    distances = []

    for point in real_points:
        min_distance = np.inf

        for i in range(len(ideal_points) - 1):
            distance = point_to_segment_distance(
                point,
                ideal_points[i],
                ideal_points[i + 1],
            )
            if distance < min_distance:
                min_distance = distance

        distances.append(min_distance)

    distances = np.asarray(distances)

    return {
        "mean_error": float(np.nanmean(distances)),
        "max_error": float(np.nanmax(distances)),
        "std_error": float(np.nanstd(distances)),
    }


def compute_speed_profile(real_df):
    t = real_df["T"].to_numpy(dtype=float)
    px = real_df["PX"].to_numpy(dtype=float)
    py = real_df["PY"].to_numpy(dtype=float)

    if len(real_df) < 2:
        return np.asarray([], dtype=float)

    dt = np.diff(t)
    dx = np.diff(px)
    dy = np.diff(py)

    valid = np.isfinite(dt) & np.isfinite(dx) & np.isfinite(dy) & (dt > 0)
    if not np.any(valid):
        return np.asarray([], dtype=float)

    return np.sqrt(dx[valid] ** 2 + dy[valid] ** 2) / dt[valid]


def estimate_sampling_frequency(real_df, default_ts=0.05):
    t = real_df["T"].to_numpy(dtype=float)
    dt = np.diff(t)
    dt = dt[np.isfinite(dt) & (dt > 0)]

    if len(dt) == 0:
        return 1 / default_ts

    return float(1 / np.nanmedian(dt))


def analyze_session(csv_path: str, ts: float = 0.05):
    info = read_unity_trajectory_csv(csv_path)

    metadata = info["metadata"]
    ideal = info["ideal"]
    real = info["real"]

    if len(real) == 0:
        raise ValueError("El archivo no contiene datos reales de trayectoria.")

    fs = estimate_sampling_frequency(real, default_ts=ts)

    error_metrics = compute_tracking_error(real, ideal)

    speed = compute_speed_profile(real)
    sparc_value = safe_sparc(speed, fs=fs, padlevel=4, fc=10.0, amp_th=0.05)

    t = real["T"].to_numpy(dtype=float)
    total_time = float(np.nanmax(t) - np.nanmin(t)) if len(t) > 0 else np.nan

    mean_velocity = float(np.nanmean(speed)) if len(speed) > 0 else np.nan

    collisions = 0
    if "Choque" in real.columns:
        choque = real["Choque"].fillna(0).astype(int).to_numpy()
        collisions = int(np.sum(np.diff(np.r_[0, choque == 1]) == 1))

    final_supply = np.nan
    if "Suministro" in real.columns:
        suministro = real["Suministro"].to_numpy(dtype=float)
        if np.any(np.isfinite(suministro)):
            final_supply = float(np.nanmax(suministro))

    return {
        "participant_id": metadata.get("PilotID"),
        "level": metadata.get("Nivel"),
        "status": metadata.get("Status"),
        "mean_error": error_metrics["mean_error"],
        "max_error": error_metrics["max_error"],
        "std_error": error_metrics["std_error"],
        "sparc": sparc_value,
        "total_time": total_time,
        "mean_velocity": mean_velocity,
        "collisions": collisions,
        "final_supply": final_supply,
        "samples_real": int(len(real)),
        "samples_ideal": int(len(ideal)),
    }
