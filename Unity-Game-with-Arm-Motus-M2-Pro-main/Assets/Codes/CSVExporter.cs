using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Importante para SceneUtility y SceneManager


public static class CSVExporter
{
    // Carpeta seleccionada por el usuario (se asigna desde FolderSelector)
    public static string saveFolder = "";

    /// Guarda un CSV con:
    ///  - Header: Nivel, PilotID, Status
    ///  - Tabla: Tipo,T,Suministro,PX,PY,Choque (Ideal: NaN en T/Suministro/Choque)
    ///  - Métricas finales: Error promedio, Estabilidad
    ///
    /// Requisitos de listas:
    ///  - trayectoriaReal.Count == tiemposTrayectoria.Count
    ///  - trayectoriaReal.Count == choqueEstadoPorMuestra.Count
    ///  - trayectoriaReal.Count == suministrosPorMuestra.Count
    /// 
    /// status: "COMPLETADO" o "FALLADO"
    /// choqueEstadoPorMuestra: 0 (normal), 1 (choque), 2 (invulnerable)
    /// suministrosPorMuestra: 0,1,2,... (número acumulado de entregas en ese instante)
    /// </summary>
    public static void GuardarDatosCSV(
        LineRenderer trayectoriaIdeal,
        List<Vector3> trayectoriaReal,
        List<float> tiemposTrayectoria,
        List<int> choqueEstadoPorMuestra,
        List<int> suministrosPorMuestra,
        string status,
        float errorPromedio,
        float estabilidad,
        List<float> entregasT,
        List<int> entregasN,
        List<float> entregasError,
        List<float> entregasEstab)
    {
        if (string.IsNullOrEmpty(saveFolder))
        {
            Debug.LogError("❌ No se ha seleccionado carpeta para guardar los CSV.");
            return;
        }

        // Validaciones básicas
        if (trayectoriaReal == null || tiemposTrayectoria == null ||
            choqueEstadoPorMuestra == null || suministrosPorMuestra == null)
        {
            Debug.LogError("❌ Listas nulas: verifica que pasas todas las colecciones requeridas.");
            return;
        }

        int n = trayectoriaReal.Count;
        if (tiemposTrayectoria.Count != n ||
            choqueEstadoPorMuestra.Count != n ||
            suministrosPorMuestra.Count != n)
        {
            Debug.LogError($"❌ Tamaños no coinciden. Real={n}, Tiempos={tiemposTrayectoria.Count}, Choque={choqueEstadoPorMuestra.Count}, Suministros={suministrosPorMuestra.Count}");
            return;
        }

        // Nombre de archivo: Nivel_PilotID_DDMMAA.csv
        // Obtener nombre del nivel actual
        int nivelActual = SceneManager.GetActiveScene().buildIndex;
        string scenePath = SceneUtility.GetScenePathByBuildIndex(nivelActual);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        string name = sceneName;
        string nivel = name;  // asumes que existe
        string piloto = GameSettings.pilotoID;          // asumes que existe
        string fecha = System.DateTime.Now.ToString("ddHHmmss"); // dd/hh/mm/ss
        string fileName = $"{nivel}_{piloto}_{fecha}.csv";
        string ruta = Path.Combine(saveFolder, fileName);

        var ci = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder(1024);

        // Encabezado de metadatos
        sb.AppendLine($"Nivel,{nivel}");
        sb.AppendLine($"PilotID,{piloto}");
        sb.AppendLine($"Status,{status}");
        sb.AppendLine();

        // Encabezado de la tabla
        sb.AppendLine("Tipo,T,Suministro,PX,PY,Choque");

        // --- Trayectoria Ideal ---
        if (trayectoriaIdeal != null)
        {
            int m = trayectoriaIdeal.positionCount;
            for (int i = 0; i < m; i++)
            {
                Vector3 p = trayectoriaIdeal.GetPosition(i);
                // Tipo=Ideal, T=NaN, Suministro=NaN, PX, PY, Choque=NaN
                sb.Append("Ideal,NaN,NaN,");
                sb.Append(p.x.ToString(ci)).Append(',');
                sb.Append(p.y.ToString(ci)).Append(',');
                sb.AppendLine("NaN");
            }
        }

        // --- Trayectoria Real ---
        for (int i = 0; i < n; i++)
        {
            Vector3 p = trayectoriaReal[i];
            float t = tiemposTrayectoria[i];
            int choque = choqueEstadoPorMuestra[i];      // 0 normal, 1 choque, 2 invuln
            int sumin = suministrosPorMuestra[i];        // acumulado de entregas

            sb.Append("Real,");
            sb.Append(t.ToString(ci)).Append(',');
            sb.Append(sumin.ToString(ci)).Append(',');
            sb.Append(p.x.ToString(ci)).Append(',');
            sb.Append(p.y.ToString(ci)).Append(',');
            sb.AppendLine(choque.ToString(ci));
        }

        // === ENTREGAS (al final del CSV) ===
        if (entregasT != null && entregasN != null && entregasError != null && entregasEstab != null &&
            entregasT.Count == entregasN.Count && entregasN.Count == entregasError.Count && entregasError.Count == entregasEstab.Count)
        {
            sb.AppendLine();
            sb.AppendLine("# Entregas");
            sb.AppendLine("Tipo,T,Suministro,Error,Estabilidad"); // ⬅️ nueva columna

            for (int i = 0; i < entregasT.Count; i++)
            {
                sb.Append("Entrega,")
                .Append(entregasT[i].ToString(ci)).Append(',')
                .Append(entregasN[i].ToString(ci)).Append(',')
                .Append(entregasError[i].ToString(ci)).Append(',')
                .AppendLine(entregasEstab[i].ToString(ci));
            }
        }

        // Métricas finales
        sb.AppendLine();

        // Promedio de errores por ENTREGA
        float errorPromedioEntregas = PromedioLimpio(entregasError);
        sb.Append("Error promedio,").AppendLine(errorPromedioEntregas.ToString(ci));

        // Promedio de estabilidad por ENTREGA  ⬅️ NUEVO
        float estabilidadPromedioEntregas = PromedioLimpio(entregasEstab);
        sb.Append("Estabilidad promedio,").AppendLine(estabilidadPromedioEntregas.ToString(ci));

        // Guardar archivo
        File.WriteAllText(ruta, sb.ToString(), Encoding.UTF8);
        //Debug.Log($"✅ Datos guardados en: {ruta}");
        Debug.Log($"[CSV] nivel='{nivel}' piloto='{piloto}' fecha='{fecha}' fileName='{fileName}'");

    }
    // Helper para promedio ignorando NaN/Infinity
    static float PromedioLimpio(List<float> xs)
    {
        if (xs == null || xs.Count == 0) return float.NaN;
        double suma = 0.0; int c = 0;
        for (int i = 0; i < xs.Count; i++)
        {
            float v = xs[i];
            if (!float.IsNaN(v) && !float.IsInfinity(v)) { suma += v; c++; }
        }
        return (c > 0) ? (float)(suma / c) : float.NaN;
    }
}
