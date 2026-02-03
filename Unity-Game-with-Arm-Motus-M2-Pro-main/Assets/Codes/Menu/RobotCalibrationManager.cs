using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RobotCalibrationManager_Click : MonoBehaviour
{
    public enum Corner { BottomLeft, BottomRight, TopRight, TopLeft, Done }

    [Header("Refs")]
    public UnityClient client;           // para leer lastX/lastY
    public NaveHistoriaPrueba nave;      // opcional: para aplicar calibración al final

    [Header("UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI countdownText;

    [Header("Settings")]
    public float holdSeconds = 2f;
    public int minSamplesForMedian = 20;
    public bool useUnscaledTime = true;

    // PlayerPrefs keys
    const string KEY_XMIN = "Robot_Xmin";
    const string KEY_XMAX = "Robot_Xmax";
    const string KEY_YMIN = "Robot_Ymin";
    const string KEY_YMAX = "Robot_Ymax";

    Corner current = Corner.BottomLeft;
    bool capturing = false;

    List<float> xs = new List<float>(256);
    List<float> ys = new List<float>(256);

    Vector2 rawBL, rawBR, rawTR, rawTL;

    void Start()
    {
        UpdateUI();
        if (countdownText != null) countdownText.text = "";
    }

    void UpdateUI()
    {
        if (statusText == null) return;

        if (current == Corner.Done)
        {
            statusText.text = "✅ Calibración completada.";
            return;
        }

        statusText.text =
            $"Paso: {CornerName(current)}\n" +
            "1) Lleva el robot a esa esquina física.\n" +
            "2) Cuando estés listo(a), presiona CAPTURAR.\n";
    }

    string CornerName(Corner c)
    {
        switch (c)
        {
            case Corner.BottomLeft:  return "Esquina inferior izquierda";
            case Corner.BottomRight: return "Esquina inferior derecha";
            case Corner.TopRight:    return "Esquina superior derecha";
            case Corner.TopLeft:     return "Esquina superior izquierda";
            default: return "Listo";
        }
    }

    Corner Next(Corner c)
    {
        switch (c)
        {
            case Corner.BottomLeft:  return Corner.BottomRight;
            case Corner.BottomRight: return Corner.TopRight;
            case Corner.TopRight:    return Corner.TopLeft;
            case Corner.TopLeft:     return Corner.Done;
            default: return Corner.Done;
        }
    }

    // Llama a esto desde el botón "Capturar"
    public void CaptureCurrentCorner()
    {
        if (capturing) return;
        if (current == Corner.Done) return;
        if (client == null)
        {
            if (statusText != null) statusText.text = "Falta asignar UnityClient.";
            return;
        }

        StartCoroutine(CaptureRoutine());
    }

    IEnumerator CaptureRoutine()
    {
        capturing = true;
        xs.Clear();
        ys.Clear();

        float t = 0f;
        while (t < holdSeconds)
        {
            // toma muestras RAW (no dependemos de mapeo)
            xs.Add(client.lastX);
            ys.Add(client.lastY);

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            if (countdownText != null)
                countdownText.text = $"Capturando… {Mathf.Max(0f, holdSeconds - t):0.0}s";

            yield return null;
        }

        if (countdownText != null) countdownText.text = "";

        if (xs.Count < minSamplesForMedian || ys.Count < minSamplesForMedian)
        {
            // puede pasar si el cliente no está actualizando valores
            if (statusText != null)
                statusText.text = "No se capturaron suficientes muestras. Verifica que el robot esté enviando datos.";
            capturing = false;
            yield break;
        }

        float mx = Median(xs);
        float my = Median(ys);
        SaveCornerMedian(current, new Vector2(mx, my));

        // avanzar
        current = Next(current);
        capturing = false;
        UpdateUI();

        if (current == Corner.Done)
            FinishCalibration();
    }

    void SaveCornerMedian(Corner c, Vector2 rawMed)
    {
        switch (c)
        {
            case Corner.BottomLeft:  rawBL = rawMed; break;
            case Corner.BottomRight: rawBR = rawMed; break;
            case Corner.TopRight:    rawTR = rawMed; break;
            case Corner.TopLeft:     rawTL = rawMed; break;
        }
    }

    void FinishCalibration()
    {
        float xmin = Mathf.Min(rawBL.x, rawTL.x, rawBR.x, rawTR.x);
        float xmax = Mathf.Max(rawBL.x, rawTL.x, rawBR.x, rawTR.x);
        float ymin = Mathf.Min(rawBL.y, rawTL.y, rawBR.y, rawTR.y);
        float ymax = Mathf.Max(rawBL.y, rawTL.y, rawBR.y, rawTR.y);

        // Detecta inversión
        bool invertX = (rawBR.x < rawBL.x); // derecha menor que izquierda
        bool invertY = (rawTL.y < rawBL.y); // arriba menor que abajo

        // Márgenes (opcional pero recomendado): 2% del rango
        //float xRange = Mathf.Max(0.0001f, xmax - xmin);
        //float yRange = Mathf.Max(0.0001f, ymax - ymin);
        //float marginX = 0.02f * xRange;
        //float marginY = 0.02f * yRange;

        //xmin += marginX; xmax -= marginX;
        //ymin += marginY; ymax -= marginY;

        PlayerPrefs.SetFloat(KEY_XMIN, xmin);
        PlayerPrefs.SetFloat(KEY_XMAX, xmax);
        PlayerPrefs.SetFloat(KEY_YMIN, ymin);
        PlayerPrefs.SetFloat(KEY_YMAX, ymax);
        PlayerPrefs.Save();

        if (statusText != null)
        {
            statusText.text =
                "✅ Calibración guardada.\n" +
                $"Xmin={xmin:F3} Xmax={xmax:F3}\n" +
                $"Ymin={ymin:F3} Ymax={ymax:F3}\n" +
                $"invertX={invertX} invertY={invertY}\n" +
                "Ya puedes cerrar esta pantalla y jugar.";
        }

        // aplica a la nave actual si existe (para que surta efecto inmediato)
        if (nave != null)
        {
            nave.Xmin = xmin; nave.Xmax = xmax;
            nave.Ymin = ymin; nave.Ymax = ymax;
            nave.invertX = invertX;
            nave.invertY = invertY;
        }
    }

    static float Median(List<float> v)
    {
        int n = v.Count;
        if (n == 0) return 0f;

        var tmp = new List<float>(v);
        tmp.Sort();

        if (n % 2 == 1) return tmp[n / 2];

        float a = tmp[(n / 2) - 1];
        float b = tmp[n / 2];
        return 0.5f * (a + b);
    }

    // Botón opcional "Reiniciar"
    public void Restart()
    {
        if (capturing) return;
        current = Corner.BottomLeft;
        UpdateUI();
        if (countdownText != null) countdownText.text = "";
    }
}

