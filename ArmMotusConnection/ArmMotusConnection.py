# -*- coding: utf-8 -*-
"""
Created on Tue Jan 27 13:01:04 2026

@author: alexmayoral8
"""

import time
import struct
import argparse
import threading
import socket
import csv
import sys, os
print("PYTHON:", sys.executable, flush=True)
print("CONDA_PREFIX:", os.environ.get("CONDA_PREFIX"), flush=True)
print("CONDA_DEFAULT_ENV:", os.environ.get("CONDA_DEFAULT_ENV"), flush=True)

from gs_usb.gs_usb import GsUsb
from gs_usb.gs_usb_frame import GsUsbFrame
from gs_usb.constants import CAN_EFF_MASK

GS_USB_NONE_ECHO_ID = 0xFFFFFFFF

GS_CAN_MODE_LISTEN_ONLY = (1 << 0)  # solo escucha

shared_px = None
shared_py = None
shared_fx = None
shared_fy = None
data_lock = threading.Lock()
#Funciones decodificacion datos
#------------------------------------------------------------------------------
def reorder_bytes_excel_style(b):
    """
    Rearreglo bytes:
    [b0, b1, b2, b3] -> [b3, b2, b1, b0]
    """
    return [b[3], b[2], b[1], b[0]]

def excel_int32_from_hex_bytes(b):
    """
    - rearreglo de bytes
    - hex -> decimal
    - corrección si num>2147483648
    """
    rb = reorder_bytes_excel_style(b)
    hex_str = ''.join(f'{byte:02X}' for byte in rb)
    val = int(hex_str, 16)

    if val > 2147483648:
        val = val - 4294967295

    return val

def decode_armmotus_excel(payload: bytes, arb_id: int):
    """
    Decodificación para ArmMotus M2 Pro
    """
    if len(payload) < 8:
        return None

    b = list(payload)

    # a = últimos 4 bytes
    a_raw = excel_int32_from_hex_bytes(b[4:8])

    # b = primeros 4 bytes
    b_raw = excel_int32_from_hex_bytes(b[0:4])

    # Devolver siempre
    return [a_raw, b_raw]

def decode_payload(payload: bytes, mode: str):
    """
    IGNORAR FUNCION ANTIGUA
    Devuelve una lista de valores decodificados.
    mode:
      - u16le4 : 4 x uint16 little-endian
      - i16le4 : 4 x int16 little-endian
      - u32le2 : 2 x uint32 little-endian
      - i32le2 : 2 x int32 little-endian
      - f32le2 : 2 x float32 little-endian
      - u8     : 8 x uint8 (0..255)
    """
    if len(payload) != 8:
        # CAN clásico suele ser 0..8. Para simplificar, aquí pedimos 8.
        payload = payload.ljust(8, b"\x00")

    if mode == "u16le4":
        return list(struct.unpack("<4H", payload))
    if mode == "i16le4":
        return list(struct.unpack("<4h", payload))
    if mode == "u32le2":
        return list(struct.unpack("<2I", payload))
    if mode == "i32le2":
        return list(struct.unpack("<2i", payload))
    if mode == "f32le2":
        return list(struct.unpack("<2f", payload))
    if mode == "u8":
        return list(payload)

    raise ValueError(f"Modo desconocido: {mode}")

#------------------------------------------------------------------------------


def main():
    global shared_px, shared_py, shared_fx, shared_fy
    ap = argparse.ArgumentParser()
    ap.add_argument("--no-plot", action="store_true", help="No abrir matplotlib (modo Unity)")
    ap.add_argument("--bitrate", type=int, default=1_000_000)
    #181 posicion X, 182 posicion en Y
    #ap.add_argument("--id", type=lambda s: int(s, 0), default=0x182, help="ID a seguir (ej: 0x401)")#183 es fuerza en X, 184 fuerza en Y
    ap.add_argument("--mode", choices=["u16le4", "i16le4", "u32le2", "i32le2", "f32le2", "u8"], default="u32le2")
    ap.add_argument("--seconds", type=float, default=5000.0, help="Tiempo de captura")
    ap.add_argument("--timeout-ms", type=int, default=1000)
    args = ap.parse_args()

    devs = GsUsb.scan()
    if not devs:
        print("No encuentro el USB2CAN (gs_usb).")
        return 1

    dev = devs[0]
    try:
        dev.stop()
    except Exception:
        pass

    if not dev.set_bitrate(args.bitrate):
        print("No puedo fijar bitrate.")
        return 1

    dev.start(GS_CAN_MODE_LISTEN_ONLY)
    print(f"Escuchando {args.seconds}s a {args.bitrate} bps (IDs 0x181 y 0x182)")


    iframe = GsUsbFrame()
    t0 = time.time()
    hex_payloads = []  
    pos_x = []
    pos_y = []
    last_fx = None
    last_px = None
    last_fy = None
    last_py = None
    last_hex_181 = None
    last_hex_182 = None
    value1_201= None
    value2_201= None
    value1_202= None
    value2_202= None
    value1_206= None
    value2_206= None
    value1_183= None
    value2_183= None
    value1_184= None
    value2_184= None
    value1_281= None
    value2_281= None
    value1_282= None
    value2_282= None
    value1_301= None
    value2_301= None
    value1_302= None
    value2_302= None
    value1_401= None
    value2_401= None
    value1_402= None
    value2_402= None
    value1_501= None
    value2_501= None
    value1_502= None
    value2_502= None

   
    data_matrix = []  
    sample_idx = 1
    scale=100000
    
    primer_80 = False

    try:
        while time.time() - t0 < args.seconds:
            if dev.read(iframe, args.timeout_ms):
                if iframe.echo_id != GS_USB_NONE_ECHO_ID:
                    continue

                arb_id = iframe.can_id & CAN_EFF_MASK
                

                if not primer_80 and arb_id != 0x80:
                    continue
                elif not primer_80 and arb_id == 0x80:
                   primer_80 = True
    
                if arb_id not in (0x181, 0x182, 0x183, 0x184, 0x185, 0x201, 0x202, 0x206, 0x281, 0x282, 0x301, 0x302, 0x401, 0x402, 0x501, 0x502):
                    continue


                payload = bytes(iframe.data[:iframe.can_dlc])  # bytes reales recibidos
                # Para decodificar como 8 bytes, rellenamos a 8 si DLC<8
                payload8 = payload.ljust(8, b"\x00")
               
                hex_str = " ".join(f"{b:02X}" for b in payload8)
                #hex_payloads.append(hex_str)
                if arb_id == 0x181:
                    last_hex_181 = hex_str
                elif arb_id == 0x182:
                    last_hex_182 = hex_str

                #values = decode_payload(payload8, args.mode)
                values = decode_armmotus_excel(payload8, arb_id)
                if values is None:
                    continue
               
                if arb_id == 0x181:
                    last_fx = values[0]
                    last_px = values[1]
                elif arb_id == 0x182:
                    last_fy = values[0]
                    last_py = values[1]
                elif arb_id == 0x183:
                    value1_183 = values[0]
                    value2_183 = values[1]
                elif arb_id == 0x184:
                    value1_184 = values[0]
                    value2_184 = values[1]
                elif arb_id == 0x185:
                    value1_185 = values[0]
                    value2_185 = values[1]
                elif arb_id == 0x201:
                    value1_201 = values[0]
                    value2_201 = values[1]
                elif arb_id == 0x202:
                    value1_202 = values[0]
                    value2_202 = values[1]
                elif arb_id == 0x206:
                    value1_206 = values[0]
                    value2_206 = values[1]
                elif arb_id == 0x281:
                    value1_281 = values[0]
                    value2_281 = values[1]
                elif arb_id == 0x282:
                    value1_282 = values[0]
                    value2_282 = values[1]
                elif arb_id == 0x301:
                    value1_301 = values[0]
                    value2_301 = values[1]
                elif arb_id == 0x302:
                    value1_302 = values[0]
                    value2_302 = values[1]
                elif arb_id == 0x401:
                    value1_401 = values[0]
                    value2_401 = values[1]
                elif arb_id == 0x402:
                    value1_402 = values[0]
                    value2_402 = values[1]
                elif arb_id == 0x501:
                    value1_501 = values[0]
                    value2_501 = values[1]
                elif arb_id == 0x502:
                    value1_502 = values[0]
                    value2_502 = values[1]
               
                if (last_fx is not None and last_px is not None and
                    last_fy is not None and last_py is not None):
               
                    # guardar para la gráfica
                    pos_x.append(-last_px/scale)
                    pos_y.append(last_py/scale)
                   
                    with data_lock:
                        shared_px = -last_px/scale
                        shared_py = last_py/scale
                        shared_fx = last_fx
                        shared_fy = last_fy

                    # guardar fila de la matriz
                    data_matrix.append([
                        sample_idx,
                        last_fx,
                        -last_px/scale,   # X invertida como ya definiste
                        last_fy,
                        last_py/scale,
                        #last_hex_181,
                        #last_hex_182,
                        #value1_183, value2_183,
                        #value1_184, value2_184,
                        #value1_185, value2_185,
                        #value1_201, value2_201,
                        #value1_202, value2_202,
                        #value1_206, value2_206,
                        #value1_281, value2_281,
                        #value1_282, value2_282,
                        #value1_301, value2_301,
                        #value1_302, value2_302,
                        #value1_401, value2_401,
                        #value1_402, value2_402,
                        #value1_501, value2_501,
                        #value2_502, value2_502
                    ])
                    # imprime una línea para ver que va llegando
                    print(f"{sample_idx}: FX={last_fx} PX={-last_px/scale} FY={last_fy} PY={last_py/scale}")
                    sample_idx += 1

                #print(f"t={now:7.3f}s  id=0x{arb_id:X}  dlc={iframe.can_dlc}  raw={hex_str}  decoded={values}")

    except KeyboardInterrupt:
        pass
    finally:
        try:
            dev.stop()
        except Exception:
            pass

   
    with open("armmotus_matrix.csv", "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["n", "px", "fx", "py", "fy"]) #"hex_0x181", "hex_0x182","183_value1", "183_value2", "184_value1", "184_value2", "185_value1", "185_value2", "201_value1", "201_value2", "202_value1", "202_value2", "206_value1", "206_value2", "281_value1", "281_value2", "282_value1", "282_value2", "301_value1", "301_value2", "302_value1", "302_value2", "401_value1", "401_value2", "402_value1", "402_value2", "501_value1", "501_value2", "502_value1", "502_value2"])
        for row in data_matrix:
            writer.writerow(row)
    if not pos_x or not pos_y:
        print("No hay suficientes datos X-Y para plotear")
        return 1
    print("Puntos capturados:", len(pos_x))
    if not args.no_plot:
        import matplotlib.pyplot as plt
        plt.figure()
        plt.plot(pos_x, pos_y, '-o', markersize=2)
        plt.xlabel("Posición X (0x181)")
        plt.ylabel("Posición Y (0x182)")
        plt.title("Trayectoria X vs Y – ArmMotus M2 Pro")
        plt.grid(True)
        plt.axis("equal")
        plt.show()

    
    return 0

def tcp_server():
    HOST = "127.0.0.1"
    PORT = 12345

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server.bind((HOST, PORT))
        server.listen(1)
        print(f"[PY] TCP server listening on {HOST}:{PORT}")

        while True:
            conn, addr = server.accept()
            print(f"[PY] Unity connected from {addr}")

            try:
                with conn:
                    f = conn.makefile("r")
                    while True:
                        line = f.readline()
                        if not line:
                            print("[PY] Unity disconnected")
                            break

                        cmd = line.strip()

                        if cmd == "GET":
                            with data_lock:
                                x = shared_px
                                y = shared_py
                                fx = shared_fx
                                fy = shared_fy

                            # Si todavía no hay datos de CAN, responde algo por defecto
                            if x is None or y is None or fx is None or fy is None:
                                conn.sendall(b"NaN,NaN,NaN,NaN\n")
                                continue
                            
                            msg = f"{x:+.4f},{y:+.4f},{fx:+.4f},{fy:+.4f}\n"
                            conn.sendall(msg.encode("ascii"))

                        elif cmd == "CLOSE":
                            print("[PY] CLOSE received")
                            break

                        else:
                            # comandos desconocidos
                            pass

            except Exception as e:
                print("[PY] Connection error:", e)

            print("[PY] Waiting for next Unity connection...")


if __name__ == "__main__":
    tcp_thread = threading.Thread(target=tcp_server, daemon=True)
    tcp_thread.start()

    try:
        main()  # tu código CAN
    except KeyboardInterrupt:
        pass