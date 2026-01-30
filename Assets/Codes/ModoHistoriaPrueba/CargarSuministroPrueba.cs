using UnityEngine;
using System.Collections;
using TMPro;

public class CargarSuministros : MonoBehaviour
{
    [HideInInspector] public bool juegoIniciado = false;
    [HideInInspector] public bool naveActiva = false;
    [HideInInspector] public bool puedeMoverse = false;

    [Header("Referencias")]
    public GameObject supplyPrefab;
    public Transform nave;
    public Transform planeta;
    public Transform estacion; // 🔹 Arrástrala desde el editor
    public TextMeshProUGUI EntregasHUD;
    public GameManagerHistoriaPrueba gameManager;

    [Header("Parámetros")]
    public float duracion = 2f;
    public Vector3 escalaMaxima = new Vector3(0.3f, 0.3f, 0.3f);
    private GameObject supplyInstance;
    public bool TieneSuministro => supplyInstance != null; // <-- helper
    private int SuministrosEntregados = 0;
    [Header("Objetivo de suministros")]
    public int suministrosObjetivo = 5;  

    void Start()
    {
        EntregasHUD.text = "Suministros entregados: 0/" + suministrosObjetivo;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Nave") && supplyInstance == null)
        {
            // Crear el suministro en el planeta
            supplyInstance = Instantiate(supplyPrefab, planeta.position, Quaternion.identity);
            supplyInstance.transform.localScale = Vector3.zero;

            // Animar hasta la nave
            StartCoroutine(AnimarCarga(supplyInstance.transform, nave, true));
        }

    }

    IEnumerator AnimarCarga(Transform supply, Transform destino, bool acoplar)
    {
        float t = 0f;
        Vector3 inicio = supply.position;
        Vector3 escalaInicial = supply.localScale;
        Vector3 escalaFinal = acoplar ? escalaMaxima : Vector3.zero;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            supply.position = Vector3.Lerp(inicio, destino.position, t);
            supply.localScale = Vector3.Lerp(escalaInicial, escalaFinal, t);
            yield return null;
        }

        if (acoplar)
        {
            // 🔹 Agregamos el script seguidor, pero sin activarlo aún
            var seguidor = supply.gameObject.AddComponent<SuministroSeguidor>();
            seguidor.offset = new Vector3(-0.5f, 0, 0);
            seguidor.seguir = false;

            // 🔹 Activar el seguimiento después de un pequeño retraso
            yield return new WaitForSeconds(0.1f);
            seguidor.ActivarSeguimiento(nave);
        }
        else
        {
            Destroy(supply.gameObject, 0.5f);
        }
    }


    IEnumerator AnimarEntrega(Transform supply, Transform estacion)
    {
        // 🔹 Desactivar seguimiento para que deje de moverse con la nave
        var seguidor = supply.GetComponent<SuministroSeguidor>();
        if (seguidor != null)
            seguidor.DesactivarSeguimiento();

        float t = 0f;
        Vector3 inicio = supply.position;
        Vector3 escalaInicial = supply.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            supply.position = Vector3.Lerp(inicio, estacion.position, t);
            supply.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t);
            yield return null;
        }

        Destroy(supply.gameObject);
    }

    public void EntregarSuministro()
    {
        if (supplyInstance != null)
        {
            gameManager.OnSuministroEntregado();
            StartCoroutine(AnimarEntrega(supplyInstance.transform, estacion));
            supplyInstance = null;
            SuministrosEntregados++;
            EntregasHUD.text = "Suministros entregados: " + SuministrosEntregados + "/" + suministrosObjetivo; // ⭐
            if (SuministrosEntregados >= suministrosObjetivo)
            {
                puedeMoverse = false;
                juegoIniciado = false;
                naveActiva = false;
                gameManager.ActivarPanelFinal();
            }


        }
    }
    public void SoltarSuministroEnPosicionActual()
    {
        if (supplyInstance == null) return;

        // 1) Apaga seguimiento
        var seguidor = supplyInstance.GetComponent<SuministroSeguidor>();
        if (seguidor != null) seguidor.DesactivarSeguimiento();

        // 2) Asegura un Rigidbody2D suave para drift (si no existe)
        var rb = supplyInstance.GetComponent<Rigidbody2D>();
        if (rb == null) rb = supplyInstance.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 1.5f;           // fricción ligera para que no acelere infinito
        rb.angularDamping = 1.5f;

        // 3) Añade el script de flotado (abajo)
        if (supplyInstance.GetComponent<SuministroFlotante>() == null)
            supplyInstance.AddComponent<SuministroFlotante>();

        // 4) (Opcional) cambia el tag para distinguirlo
        supplyInstance.tag = "SuministroFlotante";

        // 5) Muy importante: “libera” la referencia para permitir que el planeta
        // vuelva a generar nuevo suministro en el próximo viaje.
        supplyInstance = null;
    }
    // ⭐ Nuevo: permite que el modo historia cambie el objetivo para este nivel
    public void ConfigurarObjetivo(int objetivo)
    {
        suministrosObjetivo = Mathf.Max(1, objetivo);
        SuministrosEntregados = 0;

        if (EntregasHUD != null)
            EntregasHUD.text = "Suministros entregados: 0/" + suministrosObjetivo;
    }

}
