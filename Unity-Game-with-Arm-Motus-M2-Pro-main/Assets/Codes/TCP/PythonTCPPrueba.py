import socket
import math

HOST = "127.0.0.1"
PORT = 12345

def make_square_points(side=2.0, steps_per_edge=20, origin=(0.0, 0.0)):
    """
    Crea una ruta cuadrada: (0,0)->(side,0)->(side,side)->(0,side)->(0,0)
    con interpolación lineal por tramos.
    """
    ox, oy = origin
    corners = [
        (ox, oy),
        (ox + side, oy),
        (ox + side, oy + side),
        (ox, oy + side),
        (ox, oy),
    ]

    pts = []
    for i in range(4):
        x0, y0 = corners[i]
        x1, y1 = corners[i + 1]
        for s in range(steps_per_edge):
            t = s / float(steps_per_edge)
            pts.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t))
    pts.append(corners[-1])
    return pts

def make_force_samples(points, fx_scale=5.0, fy_scale=5.0):
    """
    Genera fuerzas simuladas (fx, fy) para cada punto.
    Aquí las hacemos variar suave con seno/coseno para que cambien con el índice.
    """
    data = []
    n = len(points)
    for i, (x, y) in enumerate(points):
        # fuerzas “fake” pero suaves y repetibles
        angle = 2.0 * math.pi * (i / max(1, n - 1))
        fx = fx_scale * math.cos(angle)
        fy = fy_scale * math.sin(angle)
        data.append((x, y, fx, fy))
    return data  # esto es tu "matriz" Nx4 (lista de tuplas)

def main():
    points = make_square_points(side=2.0, steps_per_edge=25, origin=(0.0, 0.0))

    # Matriz Nx4: [x, y, fx, fy]
    matrix = make_force_samples(points, fx_scale=5.0, fy_scale=5.0)
    idx = 0

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server.bind((HOST, PORT))
        server.listen(1)
        print(f"[PY] Listening on {HOST}:{PORT} ...")

        conn, addr = server.accept()
        print(f"[PY] Connection from {addr}")

        with conn:
            f = conn.makefile("r")
            while True:
                line = f.readline()
                if not line:
                    print("[PY] Client disconnected.")
                    break

                cmd = line.strip()

                if cmd == "GET":
                    x, y, fx, fy = matrix[idx]
                    idx = (idx + 1) % len(matrix)  # loop

                    # Enviamos 4 columnas en una sola línea
                    msg = f"{x:.4f},{y:.4f},{fx:.4f},{fy:.4f}\n"
                    conn.sendall(msg.encode("ascii"))

                elif cmd == "CLOSE":
                    print("[PY] CLOSE received. Shutting down.")
                    break

                else:
                    conn.sendall(b"ERR\n")

if __name__ == "__main__":
    main()
