using UnityEngine;
using System.Collections.Generic;
using TMPro;  // si usas TextMeshPro, si no, usa UnityEngine.UI para Text

[RequireComponent(typeof(LineRenderer))]
public class PathDrawer : MonoBehaviour
{
    [Header("Referencias")]
    public Camera cam;                       // arrastra tu cámara principal
    public TextMeshProUGUI mensajeTexto;     // arrastra el texto de instrucciones (opcional)

    [Header("Opciones de dibujo")]
    public float minDistEntrePuntos = 0.1f;  // distancia mínima entre puntos

    private LineRenderer line;
    private List<Vector3> puntos = new List<Vector3>();
    private bool puedeDibujar = false;   // se activa al presionar el botón DIBUJAR
    private bool estaDibujando = false;  // mientras se mantiene el click derecho
    private bool hayCamino = false;      // ya existe un camino guardado
    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;
    }

    void Update()
    {
        // Si aún no se ha presionado el botón DIBUJAR, no hacemos nada
        if (!puedeDibujar)
            return;

        // Si ya hay un camino dibujado, no permitimos otro hasta borrar
        if (hayCamino)
            return;

        // Click derecho presionado por primera vez → empezar camino
        if (Input.GetMouseButtonDown(0))  // 1 = click derecho
        {
            EmpezarNuevoCamino();
        }

        // Mientras se mantenga el click derecho → agregar puntos
        if (Input.GetMouseButton(0) && estaDibujando)
        {
            AgregarPuntoBajoMouse();
        }

        // Al soltar click derecho → terminar camino
        if (Input.GetMouseButtonUp(0) && estaDibujando)
        {
            TerminarCamino();
        }
    }

    // ==========================
    //   MÉTODOS PÚBLICOS
    // ==========================

    // Llamar desde el botón "DIBUJAR"
    public void OnBotonDibujar()
    {
        // Si ya hay un camino, no dejamos dibujar otro hasta borrar
        if (hayCamino)
        {
            mensajeTexto.text = "Ya hay un camino. Presione 'Borrar' para hacerlo de nuevo.";
            return;
        }

        puedeDibujar = true;
        mensajeTexto.text = "Presione click izquierdo para dibujar el camino";
    }

    // Llamar desde el botón "BORRAR"
    public void OnBotonBorrar()
    {
        puntos.Clear();
        line.positionCount = 0;
        hayCamino = false;
        estaDibujando = false;
        mensajeTexto.text = "";
    }

    public List<Vector3> GetPath()
    {
        return puntos;
    }

    // ==========================
    //   LÓGICA INTERNA
    // ==========================

    void EmpezarNuevoCamino()
    {
        estaDibujando = true;
        puntos.Clear();
        line.positionCount = 0;

        // Primer punto forzado
        AgregarPuntoBajoMouse(true);
    }

    void TerminarCamino()
    {
        estaDibujando = false;

        // Si se dibujó al menos un par de puntos, lo consideramos un camino
        if (puntos.Count > 1)
        {
            hayCamino = true;
        }

        // Opcional: si quieres que se tenga que volver a presionar "DIBUJAR" para intentar de nuevo
        puedeDibujar = false;
        mensajeTexto.text = "";
    }

    void AgregarPuntoBajoMouse(bool forzarPrimerPunto = false)
    {
        // Proyección del mouse a un plano 2D (por ejemplo, z=0)
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plano = new Plane(Vector3.forward, Vector3.zero); // plano XY en z=0

        float distancia;
        if (plano.Raycast(ray, out distancia))
        {
            Vector3 worldPos = ray.GetPoint(distancia);

            if (forzarPrimerPunto ||
                puntos.Count == 0 ||
                Vector3.Distance(puntos[puntos.Count - 1], worldPos) > minDistEntrePuntos)
            {
                puntos.Add(worldPos);
                line.positionCount = puntos.Count;
                line.SetPosition(puntos.Count - 1, worldPos);
            }
        }
    }
}
