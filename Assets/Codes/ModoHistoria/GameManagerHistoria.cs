using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System;

public class GameManagerHistoria : MonoBehaviour
{
    private string[] nivelesHistoria = new string[]
    {
        "Nivel0Historia",
        "Nivel1Historia",
        "Nivel2Historia",
    };
    private int nivelActual = 0;

    [Header("Naves")]
    public NaveHistoria[] naves; // Las 5 naves
    private NaveHistoria naveSeleccionada;

    [Header("UI")]
    public TextMeshProUGUI mensajeUI;
    public GameObject panelFinal;
    public TMP_Text textoResultados;
    public LineRenderer trayectoriaIdeal;

    private int navesCompletadas = 0;
    private int navesPerdidas = 0;



    void Start()
    {
        if (panelFinal != null) panelFinal.SetActive(false);

        foreach (NaveHistoria nave in naves)
        {
            nave.ActivarNave();
            nave.gameManager = this;
        }

        ActualizarMensajeInicial();
    }

    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // Intentar iniciar nave si no hay seleccionada
        if (naveSeleccionada == null)
        {
            foreach (NaveHistoria nave in naves)
            {
                nave.IntentarIniciar(mousePos);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach (NaveHistoria nave in naves)
                {
                    if (!nave.naveActiva) continue;
                    if (Vector3.Distance(mousePos, nave.transform.position) < 0.5f)
                    {
                        naveSeleccionada = nave;
                        nave.puedeMoverse = true;
                        BorrarMensaje();
                        break;
                    }
                }
            }
        }
    }

    public void NaveCompletada(NaveHistoria nave)
    {
        nave.DesactivarNave();
        navesCompletadas++;

        // Calcular métricas
        float error = CalcularErrorPromedio(nave.trayectoriaReal, trayectoriaIdeal);
        float estabilidad = CalcularEstabilidad(nave.trayectoriaReal, nave.tiemposTrayectoria);

        // Guardar datos en CSV
        if (!string.IsNullOrEmpty(CSVExporter.saveFolder))
        {
            // Generamos un nombre de archivo único por nave y nivel
            //string horaActual = DateTime.Now.ToString("HH-mm-ss");
            //string nombreArchivo = $"Nivel{nivelActual}_Nave{System.Array.IndexOf(naves, nave) + 1}_{horaActual}.csv";
            //CSVExporter.GuardarDatosCSV(nombreArchivo, trayectoriaIdeal, nave.trayectoriaReal, nave.tiemposTrayectoria, error, estabilidad);
        }
        else
        {
            Debug.LogWarning("📂 No se ha seleccionado carpeta para guardar los CSV.");
        }

        // Mostrar panel final si todas las naves han terminado o se perdieron
        if (navesCompletadas + navesPerdidas >= naves.Length)
        {
            if (panelFinal != null) panelFinal.SetActive(true);
            if (textoResultados != null)
                textoResultados.text = $"Error promedio: {error:F2}   Estabilidad: {estabilidad:F4}";
        }
        else
        {
            naveSeleccionada = null;
            ActualizarMensajeInicial();
        }
    }


    public void NavePerdida(NaveHistoria nave)
    {
        // Si la nave que estaba controlando era esta, permitir seleccionar otra
        if (naveSeleccionada == nave)
            navesPerdidas++;
        naveSeleccionada = null;
        ActualizarMensajeInicial();

    }

    // ============================== MENSAJE UI ==============================
    public void ActualizarMensajeInicial()
    {
        if (mensajeUI != null)
            mensajeUI.text = "Coloca el mouse sobre la nave y presiona ESPACIO para controlar la nave";
    }

    public void BorrarMensaje()
    {
        if (mensajeUI != null)
            mensajeUI.text = "";
    }


    // ============================== MÉTRICAS ==============================
    float CalcularErrorPromedio(List<Vector3> trayectoriaReal, LineRenderer trayectoriaIdeal)
    {
        if (trayectoriaReal == null || trayectoriaIdeal == null) return 0f;
        int total = trayectoriaReal.Count;
        int puntos = trayectoriaIdeal.positionCount;
        if (total == 0 || puntos < 2) return 0f;

        float suma = 0f;
        for (int i = 0; i < total; i++)
        {
            Vector3 p = trayectoriaReal[i];
            float minDist = float.MaxValue;
            for (int j = 0; j < puntos - 1; j++)
            {
                Vector3 a = trayectoriaIdeal.GetPosition(j);
                Vector3 b = trayectoriaIdeal.GetPosition(j + 1);
                float dist = Vector3.Distance(p, a + Vector3.ClampMagnitude(b - a, 1f)); // aproximación simple
                if (dist < minDist) minDist = dist;
            }
            suma += minDist;
        }
        return suma / total;
    }

    float CalcularEstabilidad(List<Vector3> trayectoria, List<float> tiempos)
    {
        if (trayectoria == null || tiempos == null || trayectoria.Count < 4) return 0f;
        float jerkIntegral = 0f;
        int muestras = 0;
        float distanciaTotal = 0f;
        for (int i = 1; i < trayectoria.Count; i++) distanciaTotal += Vector3.Distance(trayectoria[i], trayectoria[i - 1]);
        if (distanciaTotal <= 0f) return 0f;

        for (int i = 2; i < trayectoria.Count - 1; i++)
        {
            float dt1 = tiempos[i] - tiempos[i - 1];
            float dt2 = tiempos[i + 1] - tiempos[i];
            if (dt1 <= 0 || dt2 <= 0) continue;
            Vector3 v1 = (trayectoria[i] - trayectoria[i - 1]) / dt1;
            Vector3 v2 = (trayectoria[i + 1] - trayectoria[i]) / dt2;
            Vector3 a = (v2 - v1) / ((dt1 + dt2) / 2f);
            Vector3 v0 = (trayectoria[i - 1] - trayectoria[i - 2]) / (tiempos[i - 1] - tiempos[i - 2]);
            Vector3 a0 = (v1 - v0) / dt1;
            Vector3 jerk = (a - a0) / ((dt1 + dt2) / 2f);
            jerkIntegral += jerk.sqrMagnitude * ((dt1 + dt2) / 2f);
            muestras++;
        }
        if (muestras == 0) return 0f;
        return Mathf.Sqrt(jerkIntegral / muestras) / distanciaTotal;
    }

    // ============================== Botones UI ==============================
    public void ReiniciarJuego()
    {
        SceneManager.LoadScene("Nivel0Historia");
    }
    public void VolverMenu()
    {
        SceneManager.LoadScene("MenuInicial");
    }

    public void SiguienteNivel()
    {
        nivelActual++;

        if (nivelActual < nivelesHistoria.Length + 1)
        {
            SceneManager.LoadScene(nivelesHistoria[nivelActual]);
        }
        else
        {
            Debug.Log("🎉 ¡Se completaron todos los niveles!");
            SceneManager.LoadScene("MenuInicial");
        }
    }
        public void Nivel2()
        {
            SceneManager.LoadScene("Nivel2Historia");
 
        }
}
