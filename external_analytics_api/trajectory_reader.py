import pandas as pd


def read_unity_trajectory_csv(csv_path: str):
    """
    Reads a Unity trajectory CSV file.

    Expected format:
    - Row 1, column 2: Nivel
    - Row 2, column 2: PilotID
    - Row 3, column 2: Status
    - Data table starts after 4 header lines
    - Required column: Tipo
    - Expected values in Tipo: ideal, real
    - Expected numeric columns: T, PX, PY, PZ, Choque, Suministro
    """

    with open(csv_path, "r", encoding="utf-8-sig") as file:
        lines = file.readlines()

    if len(lines) < 5:
        raise ValueError(f"CSV file is too short or invalid: {csv_path}")

    def get_metadata_value(line_index: int):
        try:
            parts = lines[line_index].strip().split(",")
            return parts[1].strip() if len(parts) > 1 else None
        except IndexError:
            return None

    metadata = {
        "Nivel": get_metadata_value(0),
        "PilotID": get_metadata_value(1),
        "Status": get_metadata_value(2),
    }

    df = pd.read_csv(csv_path, skiprows=4)
    df.columns = [str(col).strip() for col in df.columns]

    if "Tipo" not in df.columns:
        raise ValueError(f'Column "Tipo" does not exist in {csv_path}')

    df["Tipo"] = df["Tipo"].astype(str).str.strip().str.lower()

    numeric_columns = ["T", "PX", "PY", "PZ", "Choque", "Suministro"]
    for col in numeric_columns:
        if col in df.columns:
            df[col] = pd.to_numeric(df[col], errors="coerce")

    required_columns = ["T", "PX", "PY"]
    missing_columns = [col for col in required_columns if col not in df.columns]
    if missing_columns:
        raise ValueError(
            f"Missing required columns in {csv_path}: {missing_columns}"
        )

    ideal = df[df["Tipo"] == "ideal"].copy()
    real = df[df["Tipo"] == "real"].copy()

    if len(real) == 0:
        raise ValueError(f"No real trajectory rows found in {csv_path}")

    if len(ideal) == 0:
        raise ValueError(f"No ideal trajectory rows found in {csv_path}")

    real = real.dropna(subset=["T", "PX", "PY"]).copy()
    ideal = ideal.dropna(subset=["PX", "PY"]).copy()

    if len(real) == 0:
        raise ValueError(f"No valid real trajectory samples found in {csv_path}")

    if len(ideal) < 2:
        raise ValueError(
            f"Ideal trajectory must have at least 2 valid points in {csv_path}"
        )

    real["T"] = real["T"] - real["T"].iloc[0]

    return {
        "metadata": metadata,
        "table": df,
        "ideal": ideal,
        "real": real,
    }
