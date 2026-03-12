using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class EmgTcpClient : MonoBehaviour
{
    [Header("MATLAB EMG Server")]
    private string host = "192.168.10.101";
    public int port = 5000;

    private TcpClient client;
    private NetworkStream stream;
    private bool isRecording = false;
    public static EmgTcpClient Instance {get; private set;}
    public bool autoReconnect = true;

    void Awake()
    {
        if(Instance!=null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance=this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        try
        {
            client = new TcpClient();
            client.NoDelay = true; // baja latencia para comandos cortos
            client.Connect(host, port);
            stream = client.GetStream();
            Debug.Log($"[EMG] Connected to {host}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError("[EMG] Connect error: " + e.Message);
        }
    }

    public void StartRecording()
    {
        if (isRecording) return;
        if (!EnsureConnected()) return;

        SendLine("1");
        isRecording = true;
        Debug.Log("[EMG] START sent");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        if (!EnsureConnected())
        {
            isRecording = false;
            return;
        }

        SendLine("0");
        isRecording = false;
        Debug.Log("[EMG] STOP sent");
    }

    private bool EnsureConnected()
    {
        if (client != null && client.Connected && stream != null) return true;
        if (!autoReconnect) return false;

        Connect();
        return (client != null && client.Connected && stream != null);
    }

    private void SendLine(string msg)
    {
        try
        {
            byte[] data = Encoding.ASCII.GetBytes(msg + "\n"); // LF
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("[EMG] Send error: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        try { StopRecording(); } catch { }
        try { stream?.Close(); client?.Close(); } catch { }
    }
}

