using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class MetaFinalLibre : MonoBehaviour
{
    private bool juegoTerminado = false;
    public GameObject panelFinal;    // 🔸 Asigna en el Inspector
    public NaveLibre naveScript;     // 🔸 Asigna el script de la nave
    public LineRenderer trayectoriaIdeal;
    public TMP_Text textoResultados; // 🔸 Texto donde se muestran métricas

    void Start()
    {
        if (panelFinal != null)
            panelFinal.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !juegoTerminado)
        {
            juegoTerminado = true;
            if (panelFinal != null) panelFinal.SetActive(true);
            if (naveScript != null) naveScript.puedeMoverse = false;

            // 🔹 Calcular métricas
            float error = CalcularErrorPromedio(naveScript.trayectoriaReal, trayectoriaIdeal);
            float estabilidad = CalcularEstabilidad(naveScript.trayectoriaReal, naveScript.tiemposTrayectoria);

            // 🔹 Mostrar en pantalla
            if (textoResultados != null)
            {
                textoResultados.text =
                    "Error promedio: " + error.ToString("F2") +
                    "   Estabilidad: " + estabilidad.ToString("F4");
            }

            Debug.Log("✅ Error promedio: " + error);
            Debug.Log("✅ Estabilidad (jerk² normalizado): " + estabilidad);
            
            // 🔹 Guardar CSV (nombre con fecha/hora para no sobreescribir)
            //string nombreArchivo = "Resultados_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";

            //CSVExporter.GuardarDatosCSV(nombreArchivo, trayectoriaIdeal, naveScript.trayectoriaReal,naveScript.tiemposTrayectoria, error, estabilidad);
        }
    }

    public void ReiniciarJuego()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VolverMenu()
    {
        SceneManager.LoadScene("MenuPrincipal"); 
    }

    // ==============================
    //   MÉTRICA 1: Error promedio
    // ==============================
    float CalcularErrorPromedio(List<Vector3> trayectoriaReal, LineRenderer trayectoriaIdeal)
    {
        if (trayectoriaReal == null || trayectoriaIdeal == null) return 0f;

        int total = trayectoriaReal.Count;
        int puntosTrayectoria = trayectoriaIdeal.positionCount;
        if (total == 0 || puntosTrayectoria < 2) return 0f;

        // 1) Cachear posiciones del LineRenderer en WORLD si tus puntos reales están en world
        Vector3[] ptsWorld = new Vector3[puntosTrayectoria];
        bool world = trayectoriaIdeal.useWorldSpace;
        Transform tr = trayectoriaIdeal.transform;

        for (int k = 0; k < puntosTrayectoria; k++)
        {
            Vector3 p = trayectoriaIdeal.GetPosition(k);
            ptsWorld[k] = world ? p : tr.TransformPoint(p); // convertir a world si era local
        }

        float sumaDistancias = 0f;

        for (int i = 0; i < total; i++)
        {
            Vector3 punto = trayectoriaReal[i]; // se asume en world
            float distanciaMinima = float.MaxValue;

            for (int j = 0; j < puntosTrayectoria - 1; j++)
            {
                Vector3 a = ptsWorld[j];
                Vector3 b = ptsWorld[j + 1];

                float distancia = DistanciaPuntoSegmento(punto, a, b);
                if (distancia < distanciaMinima)
                    distanciaMinima = distancia;

                // (Opcional) Early-exit si ya es cero:
                // if (distanciaMinima <= 0f) break;
            }

            sumaDistancias += distanciaMinima;
        }

        return sumaDistancias / total;
    }

    float DistanciaPuntoSegmento(Vector3 punto, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float ab2 = ab.sqrMagnitude;
        if (ab2 <= 1e-8f) // segmento degenerado
            return Vector3.Distance(punto, a);

        float t = Vector3.Dot(punto - a, ab) / ab2; // proyección escalar
        t = Mathf.Clamp01(t);                       // limitar al segmento
        Vector3 q = a + t * ab;                     // punto más cercano en el segmento
        return Vector3.Distance(punto, q);
    }

    // ==============================
    //   MÉTRICA 2: Estabilidad
    // ==============================
    float CalcularEstabilidad(List<Vector3> trayectoria, List<float> tiempos)
    {
        if (trayectoria == null || tiempos == null || trayectoria.Count < 4 || tiempos.Count < 4)
            return 0f;

        float jerkIntegral = 0f;
        int muestrasJerk = 0;

        // Calcular distancia total
        float distanciaTotal = 0f;
        for (int i = 1; i < trayectoria.Count; i++)
            distanciaTotal += Vector3.Distance(trayectoria[i], trayectoria[i - 1]);

        if (distanciaTotal <= 0f) return 0f;

        // Calcular jerk²
        for (int i = 2; i < trayectoria.Count - 1; i++)
        {
            float dt1 = tiempos[i] - tiempos[i - 1];
            float dt2 = tiempos[i + 1] - tiempos[i];
            if (dt1 <= 0 || dt2 <= 0) continue;

            // Velocidades
            Vector3 v1 = (trayectoria[i] - trayectoria[i - 1]) / dt1;
            Vector3 v2 = (trayectoria[i + 1] - trayectoria[i]) / dt2;

            // Aceleraciones
            Vector3 a = (v2 - v1) / ((dt1 + dt2) / 2f);

            // Jerk aproximado
            Vector3 v0 = (trayectoria[i - 1] - trayectoria[i - 2]) / (tiempos[i - 1] - tiempos[i - 2]);
            Vector3 a0 = (v1 - v0) / dt1;
            Vector3 jerk = (a - a0) / ((dt1 + dt2) / 2f);

            jerkIntegral += jerk.sqrMagnitude * ((dt1 + dt2) / 2f);
            muestrasJerk++;
        }

        if (muestrasJerk == 0) return 0f;

        // RMS jerk
        float rmsJerk = Mathf.Sqrt(jerkIntegral / muestrasJerk);

        // Normalizar por distancia total
        return rmsJerk / distanciaTotal;
    }

}