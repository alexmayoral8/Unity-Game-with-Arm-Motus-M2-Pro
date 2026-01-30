using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class NaveHistoria : MonoBehaviour
{
    [Header("Movimiento")]
    public float angleOffset = -45f;
    public float rotationSpeed = 500f;
    public bool puedeMoverse = false;

    [Header("Trayectoria")]
    public List<Vector3> trayectoriaReal = new List<Vector3>();
    public List<float> tiemposTrayectoria = new List<float>();
    public float intervaloDeMuestreo = 0.05f;
    private float tiempoMuestreo = 0f;
    private Vector3 ultimaPosicion;
    private Vector3 ultimaDireccionValida = Vector3.right;

    [Header("GameManager")]
    public GameManagerHistoria gameManager;
    [Header("Efectos")]
    public GameObject explosionPrefab;

    [HideInInspector] public bool juegoIniciado = false;
    [HideInInspector] public bool naveActiva = false;

    void Start()
    {
        ultimaPosicion = transform.position;
        // Solo para informar GameManager que esta nave está lista
        if (gameManager != null)
            gameManager.ActualizarMensajeInicial();
    }

    void Update()
    {
        if (!naveActiva || !juegoIniciado || !puedeMoverse) return; 

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;
        transform.position = mousePosition;

        Vector3 direccion = (mousePosition - ultimaPosicion);
        if (direccion.magnitude > 0.001f)
        {
            ultimaDireccionValida = direccion.normalized;
            ultimaPosicion = mousePosition;
        }

        float angle = Mathf.Atan2(ultimaDireccionValida.y, ultimaDireccionValida.x) * Mathf.Rad2Deg;
        Quaternion rotacionObjetivo = Quaternion.Euler(0, 0, angle + angleOffset);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionObjetivo, rotationSpeed * Time.deltaTime);

        tiempoMuestreo += Time.deltaTime;
        if (tiempoMuestreo >= intervaloDeMuestreo)
        {
            trayectoriaReal.Add(mousePosition);
            tiemposTrayectoria.Add(Time.time);
            tiempoMuestreo = 0f;
        }
    }

    public void IntentarIniciar(Vector3 mousePos)
    {
        if (!naveActiva || juegoIniciado) return;

        float distancia = Vector3.Distance(mousePos, transform.position);
        if (distancia < 0.5f && Input.GetKeyDown(KeyCode.Space))
        {
            juegoIniciado = true;
            puedeMoverse = true;
            if (gameManager != null)
                gameManager.BorrarMensaje();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Meta") && gameManager != null)
        {
            puedeMoverse = false;
            juegoIniciado = false;
            gameManager.NaveCompletada(this);
        }
         else if (other.CompareTag("Meteorito") && gameManager != null)
        {
            // Nave choca con meteorito
            puedeMoverse = false;
            juegoIniciado = false;
            naveActiva = false;

            // Instanciar efecto de explosión si existe
            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(explosion, 2f); // Ajusta duración de explosión
            }

            // Avisar al GameManager que esta nave se perdió
            gameManager.NavePerdida(this);

            // Desactivar la nave para que no se pueda seleccionar
            gameObject.SetActive(false);
        }
    }

    public void ActivarNave()
    {
        naveActiva = true;
        juegoIniciado = false;
        puedeMoverse = false;
    }

    public void DesactivarNave()
    {
        naveActiva = false;
        puedeMoverse = false;
        juegoIniciado = false;
    }

}
