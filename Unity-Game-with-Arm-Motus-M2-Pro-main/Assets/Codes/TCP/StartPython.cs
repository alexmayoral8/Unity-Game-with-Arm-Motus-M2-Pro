using UnityEngine;
using System.IO;
using Diagnostics = System.Diagnostics;

public class StartPython : MonoBehaviour
{
    private Diagnostics.Process pythonProcess;
    private static StartPython instance;
    public string pythonPath = @"C:\Users\Master\anaconda3\envs\inno_usb2can\python.exe";
    public string scriptPath = @"D:\PEFUnity\ArmMotus\codigo_python_hilos_generico.py";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartPythonScript();
    }

    void StartPythonScript()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            UnityEngine.Debug.Log("[PY] Ya estaba corriendo.");
            return;
        }

        var start = new Diagnostics.ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" --no-plot",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath)
        };

        pythonProcess = new Diagnostics.Process();
        pythonProcess.StartInfo = start;

        pythonProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.Log("[PY OUT] " + e.Data); };
        pythonProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError("[PY ERR] " + e.Data); };

        pythonProcess.Start();
        pythonProcess.BeginOutputReadLine();
        pythonProcess.BeginErrorReadLine();

        UnityEngine.Debug.Log("[PY] Proceso iniciado.");
    }

    void OnApplicationQuit()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
            pythonProcess.Kill();
    }
}