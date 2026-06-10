using System.Collections;
using System.IO;
using System.Text;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class AnalyticsClient : MonoBehaviour
{
    private const string AnalyzeSessionUrl = "http://127.0.0.1:8000/analyze-session";
    private static AnalyticsClient instance;
    public static event Action<string> MetricsJsonReceived;
    public static event Action<string> MetricsUnavailable;

    public static void SendCsv(string csvPath)
    {
        if (string.IsNullOrEmpty(csvPath))
        {
            Debug.LogError("[Analytics] Ruta CSV vacia. No se envio a la API.");
            MetricsUnavailable?.Invoke("Ruta CSV vacia.");
            return;
        }

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[Analytics] CSV no encontrado: {csvPath}");
            MetricsUnavailable?.Invoke("CSV no encontrado.");
            return;
        }

        EnsureInstance();
        instance.StartCoroutine(instance.SendCsvCoroutine(csvPath));
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        GameObject go = new GameObject("AnalyticsClient");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AnalyticsClient>();
    }

    private IEnumerator SendCsvCoroutine(string csvPath)
    {
        byte[] csvBytes;

        try
        {
            csvBytes = File.ReadAllBytes(csvPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Analytics] No se pudo leer el CSV '{csvPath}': {e.Message}");
            MetricsUnavailable?.Invoke(e.Message);
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", csvBytes, Path.GetFileName(csvPath), "text/csv");

        using (UnityWebRequest request = UnityWebRequest.Post(AnalyzeSessionUrl, form))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Analytics] Error enviando CSV a la API local: {request.error}");
                MetricsUnavailable?.Invoke(request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
            if (SaveSummaryJson(csvPath, json))
            {
                AppendSessionHistory(csvPath, json);
            }
            MetricsJsonReceived?.Invoke(json);
            Debug.Log($"[Analytics] Respuesta API: {json}");
        }
    }

    private static bool SaveSummaryJson(string csvPath, string json)
    {
        string directory = Path.GetDirectoryName(csvPath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(csvPath);
        string summaryPath = Path.Combine(directory, $"{fileNameWithoutExtension}_summary.json");

        try
        {
            File.WriteAllText(summaryPath, json, Encoding.UTF8);
            Debug.Log($"[Analytics] Resumen JSON guardado: {summaryPath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Analytics] No se pudo guardar el resumen JSON '{summaryPath}': {e.Message}");
            return false;
        }
    }

    private static void AppendSessionHistory(string csvPath, string json)
    {
        AnalyticsResponse response = JsonUtility.FromJson<AnalyticsResponse>(json);
        if (response == null || response.metrics == null)
        {
            Debug.LogError("[Analytics] No se pudo agregar historial: JSON de metricas invalido.");
            return;
        }

        string directory = Path.GetDirectoryName(csvPath);
        string historyPath = Path.Combine(directory, "session_history.csv");
        bool writeHeader = !File.Exists(historyPath) || new FileInfo(historyPath).Length == 0;

        try
        {
            StringBuilder sb = new StringBuilder();
            if (writeHeader)
            {
                sb.AppendLine("timestamp,participant_id,level,status,mean_error,max_error,std_error,sparc,total_time,mean_velocity,collisions,final_supply");
            }

            AnalyticsMetrics metrics = response.metrics;
            sb.Append(EscapeCsv(DateTime.Now.ToString("o", CultureInfo.InvariantCulture))).Append(',');
            sb.Append(EscapeCsv(metrics.participant_id)).Append(',');
            sb.Append(EscapeCsv(metrics.level)).Append(',');
            sb.Append(EscapeCsv(metrics.status)).Append(',');
            sb.Append(FormatCsvFloat(metrics.mean_error)).Append(',');
            sb.Append(FormatCsvFloat(metrics.max_error)).Append(',');
            sb.Append(FormatCsvFloat(metrics.std_error)).Append(',');
            sb.Append(FormatCsvFloat(metrics.sparc)).Append(',');
            sb.Append(FormatCsvFloat(metrics.total_time)).Append(',');
            sb.Append(FormatCsvFloat(metrics.mean_velocity)).Append(',');
            sb.Append(metrics.collisions.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.AppendLine(FormatCsvFloat(metrics.final_supply));

            File.AppendAllText(historyPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[Analytics] Historial actualizado: {historyPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Analytics] No se pudo actualizar session_history.csv: {e.Message}");
        }
    }

    private static string FormatCsvFloat(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return "";
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
        if (!mustQuote) return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    [Serializable]
    private class AnalyticsResponse
    {
        public bool success;
        public AnalyticsMetrics metrics;
    }

    [Serializable]
    private class AnalyticsMetrics
    {
        public string participant_id;
        public string level;
        public string status;
        public float mean_error;
        public float max_error;
        public float std_error;
        public float sparc;
        public float total_time;
        public float mean_velocity;
        public int collisions;
        public float final_supply;
    }
}
