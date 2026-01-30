using UnityEngine;

public class TrayectoriaDetector : MonoBehaviour
{
    public LineRenderer lineaTrayectoria;
    public float maxDistanciaPermitida = 1f; // umbral
    public Transform nave;
    private bool juegoTerminado = false;
    
    void Update()
    {
        bool fueraDelCamino = true;

        for (int i = 0; i < lineaTrayectoria.positionCount - 1; i++)
        {
            Vector3 puntoA = lineaTrayectoria.GetPosition(i);
            Vector3 puntoB = lineaTrayectoria.GetPosition(i + 1);

            float distancia = DistanciaPuntoSegmento(nave.position, puntoA, puntoB);

            if (distancia < maxDistanciaPermitida)
            {
                fueraDelCamino = false;
                break;
            }
        }

        if (fueraDelCamino)
        {
            //Debug.Log("🚨 ¡La nave está fuera de la trayectoria!");
            // Aquí puedes cambiar color, restar puntos, etc.
        }
        // Detectar llegada al último punto
        Vector3 ultimoPunto = lineaTrayectoria.GetPosition(lineaTrayectoria.positionCount - 1);
        if (Vector3.Distance(nave.position, ultimoPunto) < 0.3f && !juegoTerminado)
        {
            juegoTerminado = true;
            Debug.Log("🎉 ¡Llegaste al final!");
            // Aquí puedes mostrar UI o congelar la nave
        }

    }

    // Calcula la distancia mínima entre un punto y un segmento
    float DistanciaPuntoSegmento(Vector3 punto, Vector3 a, Vector3 b)
    {
        Vector3 ap = punto - a;
        Vector3 ab = b - a;
        float abSquared = ab.sqrMagnitude;
        float dot = Vector3.Dot(ap, ab) / abSquared;
        dot = Mathf.Clamp01(dot);
        Vector3 puntoMasCercano = a + dot * ab;
        return Vector3.Distance(punto, puntoMasCercano);
    }
}
