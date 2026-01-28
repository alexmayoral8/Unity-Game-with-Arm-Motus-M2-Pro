using UnityEngine;
using UnityEngine.UI;

public class ForceBar : MonoBehaviour
{
    [Header("Refs")]
    public UnityClient tcpClient;   // arrastra el objeto que tiene UnityClient
    public Image fillImage;         // arrastra ForceBar_Fill

    [Header("Config")]
    public float maxForce = 5f;     // fuerza que corresponde a barra llena
    public bool useMagnitude = true;

    void Update()
    {
        if (tcpClient == null || fillImage == null) return;

        float fx = tcpClient.lastFx;
        float fy = tcpClient.lastFy;

        float value = useMagnitude ? Mathf.Sqrt(fx * fx + fy * fy) : fx; // por si luego quieres otra cosa

        float normalized = (maxForce <= 0f) ? 0f : Mathf.Clamp01(value / maxForce);

        fillImage.fillAmount = normalized;
    }
}
