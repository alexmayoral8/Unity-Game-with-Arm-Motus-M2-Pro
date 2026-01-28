using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Globalization;

public class UnityClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private byte[] dataBuffer = new byte[1024];
    public string host = "127.0.0.1";
    public int port = 12345;
    [Header("Where to send the ArmMotus coords")]
    public NaveHistoriaPrueba nave;   // arrastra el GO de la nave que tiene NaveHistoriaPrueba
    public float requestInterval = 0.05f;   // 20 Hz

    // NUEVO: últimas fuerzas recibidas (para debug o visualización)
    [Header("Last received values")]
    public float lastX;
    public float lastY;
    public float lastFx;
    public float lastFy;
    public bool logValues = false;
    public int logEveryN = 20;
    private int sampleCount = 0;

    private float requestTimer = 0f;
    private string rxAccum = "";            // para manejar mensajes por líneas

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
        if (client == null || !client.Connected) return;

        // 1) Pedir dato cada cierto tiempo
        requestTimer += Time.deltaTime;
        if (requestTimer >= requestInterval)
        {
            requestTimer = 0f;
            SendMessageToServer("GET");
        }

        // 2) Leer respuesta(s)
        ReceiveData();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(host, port);
            stream = client.GetStream();
            Debug.Log("[UNITY] Connected to server.");
        }
        catch (Exception e)
        {
            Debug.LogError("[UNITY] Connect error: " + e.Message);
        }
    }

    void ReceiveData()
    {
        try
        {
            while (client.Available > 0)
            {
                int bytesRead = stream.Read(dataBuffer, 0, dataBuffer.Length);
                string chunk = Encoding.ASCII.GetString(dataBuffer, 0, bytesRead);

                rxAccum += chunk;

                // Procesar por líneas (porque Python manda \n)
                int newlineIndex;
                while ((newlineIndex = rxAccum.IndexOf('\n')) >= 0)
                {
                    string line = rxAccum.Substring(0, newlineIndex).Trim();
                    rxAccum = rxAccum.Substring(newlineIndex + 1);

                    if (!string.IsNullOrEmpty(line))
                        ProcessServerLine(line);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[UNITY] Receive error: " + e.Message);
        }
    }

    void ProcessServerLine(string line)
    {
        string[] parts = line.Split(',');
        if (parts.Length != 4) return;
        float x = 0f, y = 0f, fx = 0f, fy = 0f;

        bool ok =
        float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
        float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out fx) &&
        float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out fy);


        if (!ok) return;

        lastX = x; lastY = y; lastFx = fx; lastFy = fy;

        // Pasa coordenadas a script controlador de nave (NaveHistoriaPrueba)
        if (nave != null) nave.OnRobotCoord(x, y);

        // Debug opcional
        if (logValues)
        {
            sampleCount++;
            if (sampleCount % Mathf.Max(1, logEveryN) == 0)
            {
                Debug.Log($"[UNITY] x={x:F3}, y={y:F3}, fx={fx:F3}, fy={fy:F3}");
            }
        }
    }


    void SendMessageToServer(string message)
    {
        if (client == null || !client.Connected) return;

        try
        {
            byte[] data = Encoding.ASCII.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("[UNITY] Send error: " + e.Message);
        }
    }

    void Disconnect()
    {
        try
        {
            if (stream != null) stream.Close();
            if (client != null) client.Close();
        }
        catch { }
        Debug.Log("[UNITY] Disconnected.");
    }

    void OnApplicationQuit()
    {
        SendMessageToServer("CLOSE");
        Disconnect();
    }
}
