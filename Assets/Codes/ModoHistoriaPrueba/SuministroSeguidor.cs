using UnityEngine;

public class SuministroSeguidor : MonoBehaviour
{
    [Header("Configuración de seguimiento")]
    public Transform nave;
    public Vector3 offset = new Vector3(-0.5f, 0, 0); // posición relativa a la nave
    public float followSpeed = 5f;
    public bool seguir = false; // 🔹 controlado por CargarSuministros

    void Update()
    {
        if (!seguir || nave == null) return;

        // Calcula la posición objetivo en el espacio local de la nave
        Vector3 targetPos = nave.position + nave.rotation * offset;

        // Movimiento suave hacia la posición objetivo
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        // Hace que el suministro rote igual que la nave
        transform.rotation = Quaternion.Lerp(transform.rotation, nave.rotation, followSpeed * Time.deltaTime);
    }

    public void ActivarSeguimiento(Transform nuevaNave)
    {
        nave = nuevaNave;
        seguir = true;
    }

    public void DesactivarSeguimiento()
    {
        seguir = false;
    }
}
