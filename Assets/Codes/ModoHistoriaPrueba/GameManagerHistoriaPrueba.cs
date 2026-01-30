using UnityEngine;
using UnityEngine.UI;   
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections.Generic;

public class GameManagerHistoriaPrueba : MonoBehaviour
{

    [Header("Nave")]
    public NaveHistoriaPrueba nave; // ahora solo UNA nave
    private NaveHistoriaPrueba naveSeleccionada;

    [Header("UI")]
    public GameObject panelGameOver; // Arrastra aquí tu panel de Game Over en el Inspector
    public TextMeshProUGUI mensajeUI;
    public GameObject panelFinal;
    public TMP_Text textoResultados;
    public LineRenderer trayectoriaIdeal;
    bool _gameOverRunning;
    public Slider barraEstabilidad;

    // ================= CSV tracking =================
    private List<int> choqueEstados = new List<int>();
    private List<int> suministrosSeries = new List<int>();
    private int lastSampleCount = 0;
    private int suministrosEntregados = 0; // acumulado
    private List<float> entregasT = new List<float>();
    private List<int>   entregasN = new List<int>();
    private List<float> entregasError = new List<float>();
    private List<float> entregasEstab = new List<float>();   // ⬅️ estabilidad por entrega

    void Start()
    {
        if (panelFinal != null) panelFinal.SetActive(false);
        nave.gameManager = this;
            // 🔹 Aplicar configuración personalizada de historia, si está activa
        if (HistoriaSettings.historiaPersonalizadaActiva)
        {
            var cfg = HistoriaSettings.ObtenerNivelActual();

            // Vidas
            nave.ConfigurarVidas(cfg.maxVidas);

            // Suministros
            nave.ConfigurarObjetivoSuministros(cfg.suministrosObjetivo);
        }
        if (LibreSettings.librePersonalizadaActiva)
        {
            var cfg = LibreSettings.ObtenerNivelActual();

            // Vidas
            nave.ConfigurarVidas(cfg.maxVidas);

            // Suministros
            nave.ConfigurarObjetivoSuministros(cfg.suministrosObjetivo);
        }
        nave.ActivarNave();
        ActualizarMensajeInicial();
        // reset CSV
        choqueEstados.Clear();
        suministrosSeries.Clear();
        lastSampleCount = 0;
        suministrosEntregados = 0;
        entregasT.Clear(); entregasN.Clear(); entregasError.Clear(); entregasEstab.Clear();
    }

    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        if (naveSeleccionada == null)
        {
            if (nave.inputMode == NaveHistoriaPrueba.InputMode.Mouse)
                nave.IntentarIniciar(mousePos);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (nave.naveActiva && Vector3.Distance(mousePos, nave.transform.position) < 0.5f)
                {
                    naveSeleccionada = nave;
                    nave.puedeMoverse = true;
                    BorrarMensaje();
                }
            }
        }
        // --- Sincronizar listas CSV con la trayectoria de la nave ---
        // Cada vez que la nave agrega una muestra, nosotros agregamos estado y suministros
        int newCount = nave != null ? nave.trayectoriaReal.Count : 0;
        if (newCount > lastSampleCount)
        {
            int toAdd = newCount - lastSampleCount;
            for (int i = 0; i < toAdd; i++)
            {
                // Estado actual de choque/invulnerable
                int estadoChoque = (nave != null) ? nave.ChoqueEstadoActual : 0;

                choqueEstados.Add(estadoChoque);
                suministrosSeries.Add(suministrosEntregados);
            }
            lastSampleCount = newCount;
        }
        // En tu update de la tarea / al final del movimiento:
        float estabilidad = CalcularEstabilidadMeanPeak(nave.trayectoriaReal,nave.tiemposTrayectoria,3);
        barraEstabilidad.value = estabilidad; // 0 = inestable, 1 = muy estable
    }
    public void ActualizarMensajeInicial()
    {
        if (mensajeUI != null)
            mensajeUI.text = "Coloca el mouse sobre la nave y presiona ESPACIO para iniciar el viaje";
    }
    public void BorrarMensaje()
    {
        if (mensajeUI != null)
            mensajeUI.text = "";
    }
    public void ActivarPanelFinal()
    {
        nave.SetSystemCursor(true, CursorLockMode.Confined);
        panelFinal.SetActive(true);
        nave.AltoNave();
        GuardarCSV("COMPLETADO");
    }
    // Llamado por CargarSuministros al completar una entrega
    public void OnSuministroEntregado()
    {
        suministrosEntregados++;
        float errorActual = CalcularErrorPromedio(nave.trayectoriaReal, trayectoriaIdeal);
        float estabActual = CalcularEstabilidad(nave.trayectoriaReal, nave.tiemposTrayectoria); // ⬅️
        float tEntrega = (nave.tiemposTrayectoria != null && nave.tiemposTrayectoria.Count > 0)
        ? nave.tiemposTrayectoria[nave.tiemposTrayectoria.Count - 1]
        : Time.timeSinceLevelLoad;

        // === SOLO acumulamos, NO escribimos aún ===
        entregasT.Add(tEntrega);
        entregasN.Add(suministrosEntregados);
        entregasError.Add(errorActual);
        entregasEstab.Add(estabActual); // ⬅️
    }

    // ============================== MÉTRICAS ==============================
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
   float CalcularEstabilidad(List<Vector3> x, List<float> t)
    {
        // Requisitos mínimos
        if (x == null || t == null) return 0f;
        int n = Mathf.Min(x.Count, t.Count);
        if (n < 5) return 0f; // necesitamos al menos 5 puntos para jerk central
        // Verificar tiempos crecientes (y descartar muestras malas)
        List<Vector3> X = new List<Vector3>(n);
        List<float>   T = new List<float>(n);
        for (int i = 0; i < n; i++)
        {
            if (i == 0 || t[i] > T[T.Count - 1])
            {
                X.Add(x[i]);
                T.Add(t[i]);
            }
            // si t[i] <= T[last] lo saltamos (duplicado o fuera de orden)
        }
        n = T.Count;
        if (n < 5) return 0f;
        // Distancia total para normalizar (como en tu implementación)
        float distanciaTotal = 0f;
        for (int i = 1; i < n; i++) distanciaTotal += Vector3.Distance(X[i], X[i - 1]);
        if (distanciaTotal <= 0f) return 0f;
        // 1) Velocidad en puntos medios (i-1/2 y i+1/2), luego aceleración central en i
        // v_{i+1/2} = (x_{i+1} - x_i) / (t_{i+1} - t_i)
        // a_i ≈ (v_{i+1/2} - v_{i-1/2}) / ((t_{i+1} - t_{i-1})/2)
        Vector3[] a = new Vector3[n]; // solo serán válidos i=1..n-2
        for (int i = 1; i <= n - 2; i++)
        {
            float dt_f = T[i + 1] - T[i];
            float dt_b = T[i] - T[i - 1];
            if (dt_f <= 0f || dt_b <= 0f) { a[i] = Vector3.zero; continue; }

            Vector3 v_f = (X[i + 1] - X[i]) / dt_f;   // v_{i+1/2}
            Vector3 v_b = (X[i] - X[i - 1]) / dt_b;   // v_{i-1/2}
            float dt_center = 0.5f * (T[i + 1] - T[i - 1]); // (dt_f + dt_b)/2

            a[i] = (v_f - v_b) / dt_center;
        }
        // 2) Jerk central en i usando aceleraciones: 
        // j_i ≈ (a_{i+1} - a_{i-1}) / (t_{i+1} - t_{i-1})
        // válidos i=2..n-3
        Vector3[] j = new Vector3[n];
        for (int i = 2; i <= n - 3; i++)
        {
            float dt = T[i + 1] - T[i - 1];
            if (dt <= 0f) { j[i] = Vector3.zero; continue; }
            j[i] = (a[i + 1] - a[i - 1]) / dt;
        }
        // 3) Integrar ||j||^2 con regla del trapecio sobre i=2..n-3
        // ∫ ||j||^2 dt ≈ Σ 0.5*(||j_i||^2 + ||j_{i+1}||^2) * (t_{i+1} - t_i)
        double integral = 0.0;
        for (int i = 2; i <= n - 4; i++) // emparejamos (i, i+1)
        {
            float dt = T[i + 1] - T[i];
            if (dt <= 0f) continue;
            double s1 = j[i].sqrMagnitude;
            double s2 = j[i + 1].sqrMagnitude;
            integral += 0.5 * (s1 + s2) * dt;
        }
        // 4) JRMS (root-mean-square del jerk) sobre el tiempo efectivo
        float t0 = T[2], t1 = T[n - 3];
        float dur = Mathf.Max(t1 - t0, 1e-6f);
        double jrms = System.Math.Sqrt(System.Math.Max(integral, 0.0) / dur); // unidades ~ m/s^3
        // 5) Normalización: tu versión dividía por distanciaTotal
        // Esto penaliza igual trayectos largos y cortos por metro recorrido.
        // (Ver notas abajo para alternativas.)
        return (float)(jrms) / distanciaTotal;
    }
    float CalcularEstabilidadMeanPeak(List<Vector3> x, List<float> t, float ventanaSegundos = -1f)
    {
        // Requisitos mínimos
        if (x == null || t == null) return 0f;
        int n = Mathf.Min(x.Count, t.Count);
        if (n < 2) return 0f; // con 1 solo punto no hay movimiento

        // 1) Verificar tiempos crecientes (y descartar muestras malas)
        List<Vector3> X = new List<Vector3>(n);
        List<float>   T = new List<float>(n);

        for (int i = 0; i < n; i++)
        {
            if (i == 0 || t[i] > T[T.Count - 1])
            {
                X.Add(x[i]);
                T.Add(t[i]);
            }
            // si t[i] <= T[last] lo saltamos (duplicado o fuera de orden)
        }

        n = T.Count;
        if (n < 2) return 0f;

        // 2) Determinar desde qué índice usamos datos (ventana de tiempo)
        int startIndex = 0;

        if (ventanaSegundos > 0f)
        {
            float tEnd = T[n - 1];
            float tMin = tEnd - ventanaSegundos;

            // Buscar el primer índice cuyo tiempo esté dentro de la ventana
            while (startIndex < n - 1 && T[startIndex] < tMin)
            {
                startIndex++;
            }

            // Si la ventana deja menos de 2 puntos, nos regresamos a usar todo
            if (n - startIndex < 2)
            {
                startIndex = 0;
            }
        }

        // 3) Calcular distancia total y velocidad pico en ese segmento
        float distanciaTotal = 0f;
        float peakSpeed = 0f;

        for (int i = startIndex + 1; i < n; i++)
        {
            float dt = T[i] - T[i - 1];
            if (dt <= 0f) continue;

            float ds = Vector3.Distance(X[i], X[i - 1]);
            distanciaTotal += ds;

            float v = ds / dt; // velocidad tangencial entre muestras
            if (v > peakSpeed) peakSpeed = v;
        }

        // 4) Duración del movimiento en la ventana
        float dur = T[n - 1] - T[startIndex];
        if (dur <= 0f || distanciaTotal <= 0f || peakSpeed <= 0f)
            return 0f;

        // 5) Velocidad media y ratio mean/peak
        float meanSpeed = distanciaTotal / dur;
        float ratio = meanSpeed / peakSpeed;

        // 6) Clamp a [0,1] para usarlo directo en la barra
        return Mathf.Clamp01(ratio);
    }


    //=====================================================================
    //Guardar resultados en carpetas
    //=====================================================================
    void GuardarCSV(string status)
    {
        float errorProm = CalcularErrorPromedio(nave.trayectoriaReal, trayectoriaIdeal);
        float estabilidad = CalcularEstabilidad(nave.trayectoriaReal, nave.tiemposTrayectoria);

        // Llama a tu CSVExporter NUEVO (el que definimos con status, choque y suministros)
        CSVExporter.GuardarDatosCSV(
            trayectoriaIdeal: trayectoriaIdeal,
            trayectoriaReal: nave.trayectoriaReal,
            tiemposTrayectoria: nave.tiemposTrayectoria,
            choqueEstadoPorMuestra: choqueEstados,
            suministrosPorMuestra: suministrosSeries,
            status: status,
            errorPromedio: errorProm,
            estabilidad: estabilidad,
            // === NUEVO ===
            entregasT: entregasT,
            entregasN: entregasN,
            entregasError: entregasError,
            entregasEstab: entregasEstab
            );
    }
    // ======================================================================
    //BOTONES
    // ======================================================================
    public void SiguienteNivel()
    {
        if (HistoriaSettings.historiaPersonalizadaActiva)
        {
            // Avanzamos al siguiente nivel de la historia
            HistoriaSettings.indiceNivelActual++;

            // ¿Queda algún nivel más configurado?
            if (HistoriaSettings.indiceNivelActual < HistoriaSettings.niveles.Length &&
                !string.IsNullOrEmpty(HistoriaSettings.niveles[HistoriaSettings.indiceNivelActual].sceneName))
            {
                string escena = HistoriaSettings.niveles[HistoriaSettings.indiceNivelActual].sceneName;
                SceneManager.LoadScene(escena);
            }
            else
            {
                // Termina la historia personalizada → volver al menú inicial
                HistoriaSettings.historiaPersonalizadaActiva = false;
                SceneManager.LoadScene("MenuInicial");
            }
        } 
        else if (LibreSettings.librePersonalizadaActiva)
        {
            // Avanzamos al siguiente nivel libre personalizado
            LibreSettings.indiceNivelActual++;

            // ¿Queda algún nivel más configurado?
            if (LibreSettings.indiceNivelActual < LibreSettings.niveles.Length &&
                !string.IsNullOrEmpty(LibreSettings.niveles[LibreSettings.indiceNivelActual].sceneName))
            {
                string escena = LibreSettings.niveles[LibreSettings.indiceNivelActual].sceneName;
                SceneManager.LoadScene(escena);
            }
            else
            {
                // Termina la historia personalizada → volver al menú inicial
                LibreSettings.librePersonalizadaActiva = false;
                SceneManager.LoadScene("MenuInicial");
            }
        }
        else
        {
            // Comportamiento original (por índice de build)
            int siguienteNivelIndex = SceneManager.GetActiveScene().buildIndex + 1;
            SceneManager.LoadScene(siguienteNivelIndex);
        }
    }
    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        int nivelActual = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(nivelActual);
    }
    public void VolverMenu()
    {
        SceneManager.LoadScene("MenuInicial");
    }
    public void GameOver()
    {
        if (_gameOverRunning) return;          // evita disparos dobles
        StartCoroutine(GameOverSequence(1f));  // espera 1s en tiempo real
    }

    private System.Collections.IEnumerator GameOverSequence(float waitSeconds)
    {
        _gameOverRunning = true;

        // Opcional: congela gameplay sin tocar el timeScale aún
        if (nave != null) nave.puedeMoverse = false;
        // Espera en TIEMPO REAL (ignora Time.timeScale)
        yield return new WaitForSecondsRealtime(waitSeconds);
        GuardarCSV("FALLADO");
        // Ahora sí muestra el panel y pausa
        if (panelGameOver != null) panelGameOver.SetActive(true);
        Time.timeScale = 0f;

        _gameOverRunning = false;
    }

}



