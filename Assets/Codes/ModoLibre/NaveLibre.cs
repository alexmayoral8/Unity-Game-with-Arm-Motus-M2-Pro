using UnityEngine;
using UnityEngine.InputSystem; // nuevo input system
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class NaveLibre : MonoBehaviour
{
    [Header("UI Inicio")]
    public TextMeshProUGUI inicioTexto;          // Texto en pantalla para mensajes de inicio
    [TextArea] public string mensajeEsperando = "Coloca el cursor sobre la nave para iniciar";
    public string formatoCuentaAtras = "Mantén el cursor sobre la nave: {0:0.0} s";
    [Header("Movimiento")]
    public float angleOffset = -45f;
    public float rotationSpeed = 500f;
    public bool puedeMoverse = false;

    [Header("Mouse Sensitivity (tipo SO)")]
    public bool useVirtualCursor = true;         // Activa el “cursor virtual”
    [Range(0.1f, 2f)] public float cursorSensitivity = 1.0f; // 0.5 = la mitad de movimiento
    public bool clampToCamera = true;            // Limitar dentro de la vista
    public float clampPadding = 0.3f;            // margen para el clamping
    private Vector3 virtualCursor;               // en mundo
    private bool virtualCursorInit = false;

    [Header("Cursor")]
    public bool hideSystemCursorInPlay = true;     // ocultar cursor SO en juego
    public Vector3 cursorVisualOffset = Vector3.zero; // si quieres desplazar el sprite

    [Header("Trayectoria")]
    public List<Vector3> trayectoriaReal = new List<Vector3>();
    public List<float> tiemposTrayectoria = new List<float>();
    public float intervaloDeMuestreo = 0.05f;
    private float tiempoMuestreo = 0f;
    private Vector3 ultimaPosicion;
    private Vector3 ultimaDireccionValida = Vector3.right;

    [Header("GameManager")]
    public GameManagerLibre gameManager;

    [Header("Suministros")]
    public CargarSuministros cargadorSuministros; // <-- arrástralo en el inspector

    [Header("Vidas")]
    public int maxVidas = 3;
    public float invulnerableSeg = 1f;     // i-frames
    private int vidasRestantes;
    private bool invulnerable = false;
    public HeartsHUD heartsHUD; // arrástralo desde el Canvas
    [Header("VFX")]
    public NaveAveriaVFX vfx;              // arrástralo en el inspector
    public float duracionParpadeo = 0.35f;
    public GameObject explosionPrefab;
    public TextMeshProUGUI VidasHUD;
    public const string PREF_KEY_SENS = "CursorSensitivity";
    [Header("Inicio del juego")]
    public float tiempoNecesarioParaIniciar = 3f; // segundos que el cursor debe estar encima
    private float tiempoCursorSobreNave = 0f;     // contador interno
    [HideInInspector] public bool juegoIniciado = false;
    [HideInInspector] public bool naveActiva = false;
    [HideInInspector] public int ChoqueEstadoActual { get; private set; } = 0;

    // NUEVO: estados del viaje
    private bool yendoAlPlaneta = true;

    void Start()
    {
        // Lee sensibilidad guardada del menú
        if (PlayerPrefs.HasKey(PREF_KEY_SENS))
        {
            float s = PlayerPrefs.GetFloat(PREF_KEY_SENS, cursorSensitivity);
            cursorSensitivity = Mathf.Clamp(s, 0.1f, 2.0f);
        }
        SetSystemCursor(true, CursorLockMode.Confined); // mostrar cursor SO
        ultimaPosicion = transform.position;
        if (gameManager != null)
            gameManager.ActualizarMensajeInicial();
        vidasRestantes = Mathf.Max(1, maxVidas);
        if (vfx != null) vfx.ResetVisual();
        VidasHUD.text = "Vidas: " + vidasRestantes + "/" + maxVidas;
        // Cuando arranca el nivel
        if (heartsHUD != null)
            heartsHUD.Init(maxVidas, vidasRestantes);
        if (Camera.main != null)
        {
            Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mw.z = 0f;
            virtualCursor = mw;
            virtualCursorInit = true;
        }
        if (inicioTexto != null)
            inicioTexto.text = mensajeEsperando;
    }
    void Update()
    {
        if (!naveActiva || !juegoIniciado || !puedeMoverse) return;
        Vector3 targetPos;
        if (useVirtualCursor)
        {
            // Asegura inicialización si la cámara no estaba lista en Start
            if (!virtualCursorInit && Camera.main != null)
            {
                Vector3 mw0 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mw0.z = 0f;
                virtualCursor = mw0;
                virtualCursorInit = true;
            }

            // 1) Delta REAL en píxeles con el Nuevo Input System (1.0 = 1 px)
            Vector2 deltaPixels = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

            // 2) Convierte píxeles -> mundo y aplica sensibilidad (1.0 = 1:1)
            Vector3 deltaWorld = PixelDeltaToWorld(deltaPixels, transform.position.z);
            virtualCursor += deltaWorld * cursorSensitivity;

            if (clampToCamera)
                virtualCursor = ClampToCameraBounds(virtualCursor, clampPadding);

            targetPos = virtualCursor;
        }
        else
        {
            // Modo original (seguir cursor real)
            Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mw.z = 0f;
            if (clampToCamera)
                mw = ClampToCameraBounds(mw, clampPadding);
            targetPos = mw;

            // Mantén sincronizado el virtual por si luego activas el modo
            virtualCursor = mw;
        }
        // Rotación hacia el movimiento real
        Vector3 dir = (targetPos - transform.position);
        if (dir.sqrMagnitude > 0.000001f)
            ultimaDireccionValida = dir.normalized;

        float angle = Mathf.Atan2(ultimaDireccionValida.y, ultimaDireccionValida.x) * Mathf.Rad2Deg;
        Quaternion rotacionObjetivo = Quaternion.Euler(0, 0, angle + angleOffset);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionObjetivo, rotationSpeed * Time.deltaTime);
        // “Snap” como antes (si prefieres suavizado usa MoveTowards)
        transform.position = targetPos;

        tiempoMuestreo += Time.deltaTime;
        if (tiempoMuestreo >= intervaloDeMuestreo)
        {
            trayectoriaReal.Add(transform.position);
            tiemposTrayectoria.Add(Time.time);
            tiempoMuestreo = 0f;
        }

    }
    public void SetSystemCursor(bool visible, CursorLockMode lockMode)
    {
        Cursor.visible = visible;
        Cursor.lockState = lockMode; // Locked = centrado y sin salir de la ventana, Confined = dentro de la ventana, None = libre
    }
    Vector3 PixelDeltaToWorld(Vector2 pixelDelta, float zObj)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        if (cam.orthographic)
        {
            // Unidades de mundo por píxel en Y = (alto mundo) / (alto pantalla)
            // alto mundo = 2 * orthographicSize
            float worldPerPixelY = (2f * cam.orthographicSize) / Screen.height;
            float worldPerPixelX = worldPerPixelY * cam.aspect;
            return new Vector3(pixelDelta.x * worldPerPixelX, pixelDelta.y * worldPerPixelY, 0f);
        }
        else
        {
            // Aproximación para perspectiva: proyecta dos puntos separados por 1 px y resta
            float zDist = Mathf.Abs(cam.transform.position.z - zObj);
            Vector3 pA = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, zDist));
            Vector3 pB = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f + 1f, Screen.height * 0.5f + 1f, zDist));
            Vector3 perPixel = pB - pA; // delta por 1px en x,y
            return new Vector3(pixelDelta.x * perPixel.x, pixelDelta.y * perPixel.y, 0f);
        }
    }

    Vector3 ClampToCameraBounds(Vector3 pos, float pad)
    {
        var cam = Camera.main;
        if (cam == null) return pos;

        float zDist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 min = cam.ScreenToWorldPoint(new Vector3(0, 0, zDist));
        Vector3 max = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, zDist));
        pos.x = Mathf.Clamp(pos.x, min.x + pad, max.x - pad);
        pos.y = Mathf.Clamp(pos.y, min.y + pad, max.y - pad);
        pos.z = 0f;
        return pos;
    }
    private void IniciarJuego()
    {
        // Evita que se vuelva a iniciar si ya está corriendo
        if (juegoIniciado) return;

        SetSystemCursor(false, CursorLockMode.Locked); // ocultar cursor SO
        virtualCursor = transform.position;            // cursor virtual = donde está la nave
        virtualCursorInit = true;                      // márcalo como inicializado
        ultimaPosicion = transform.position;           // opcional, por consistencia

        juegoIniciado = true;
        puedeMoverse = true;
        // Ocultamos el texto de inicio al arrancar
        if (inicioTexto != null)
            inicioTexto.gameObject.SetActive(false);
            
        if (gameManager != null)
            gameManager.BorrarMensaje();
    }

    public void IntentarIniciar(Vector3 mousePos)
    {
        if (!naveActiva || juegoIniciado) return;

        float distancia = Vector3.Distance(mousePos, transform.position);

        if (distancia < 0.5f)
        {
            // Suma el tiempo con el cursor encima
            tiempoCursorSobreNave += Time.deltaTime;

            // Calcula cuánto falta
            float restante = Mathf.Clamp(tiempoNecesarioParaIniciar - tiempoCursorSobreNave, 0f, tiempoNecesarioParaIniciar);

            // Actualiza el texto de cuenta regresiva
            if (inicioTexto != null)
            {
                if (restante > 0f)
                    inicioTexto.text = string.Format(formatoCuentaAtras, restante);
                else
                    inicioTexto.text = "¡Despegando!";
            }

            // Si ya cumplió el tiempo, inicia el juego
            if (tiempoCursorSobreNave >= tiempoNecesarioParaIniciar)
            {
                IniciarJuego();
            }
        }
        else
        {
            // Si se sale de la nave, reinicia el contador y el mensaje
            tiempoCursorSobreNave = 0f;

            if (inicioTexto != null)
                inicioTexto.text = mensajeEsperando;
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Planeta") && gameManager != null && yendoAlPlaneta)
        {

            yendoAlPlaneta = false; // ahora va de regreso
        }
        else if (other.CompareTag("Estacion") && gameManager != null && !yendoAlPlaneta)
        {

            // 1) Entregar (apagará el seguimiento y hará el shrink a 0)
            if (cargadorSuministros != null)
            {
                cargadorSuministros.EntregarSuministro();
            }
            // 2) Reportar viaje completado
            yendoAlPlaneta = true;     
        }
        else if (other.CompareTag("Meteorito") && gameManager != null)
        {
            ProcesarChoque();
        }
    }

    void ProcesarChoque()
    {
        if (invulnerable) return;
        // 1) Flash inmediato (no lo 'awaits')
        if (vfx != null) vfx.FlashHitImmediate(duracionParpadeo);
        // pierde vida
        int prev = vidasRestantes;
        vidasRestantes = Mathf.Max(vidasRestantes - 1, 0);
        // 2) HUD: explota corazón
        if (heartsHUD != null && vidasRestantes < prev)
            heartsHUD.OnLifeLost(vidasRestantes);

        // Humo proporcional
        if (vfx != null)
        {
            float nivelHumo = 1f - (vidasRestantes / (float)maxVidas); // 0..1
            vfx.SetHumoNivel(nivelHumo);
        }
        // activar invulnerabilidad breve
        StartCoroutine(Invulnerabilidad(invulnerableSeg));
        //VidasHUD.text = "Vidas: " + vidasRestantes + "/"+ maxVidas;

        // ¿Se acabaron las vidas? => Game Over
        if (vidasRestantes <= 0)
        {
            SetSystemCursor(true, CursorLockMode.Confined);
            puedeMoverse = false;
            juegoIniciado = false;
            naveActiva = false;
            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(explosion, 2f);
            }
            // 3) SOLTAR suministros (dejarlos flotando)
            if (cargadorSuministros != null)
                cargadorSuministros.SoltarSuministroEnPosicionActual();

            gameObject.SetActive(false);
            if (gameManager != null)
            {
                gameManager.GameOver();        // muestra panel final / bloquea juego
            }
        }
    }

    IEnumerator Invulnerabilidad(float secs)
    {
        invulnerable = true;
        // Opcional: feedback visual extra (parpadeo alfa de sprite)
        ChoqueEstadoActual = 1;         // durante i-frames
        float t = 0f;
        while (t < secs)
        {
            t += Time.deltaTime;
            yield return null;
        }
        invulnerable = false;
        ChoqueEstadoActual = 0;         // durante i-frames

    }

    public void ActivarNave()
    {
        naveActiva = true;
        juegoIniciado = false;
        puedeMoverse = false;
        yendoAlPlaneta = true; // siempre comienza yendo al planeta
    }

    public void DesactivarNave()
    {
        naveActiva = false;
        puedeMoverse = false;
        juegoIniciado = false;
    }
    public void AltoNave()
    {
        puedeMoverse = false;
        juegoIniciado = false;
        naveActiva = false;
    }

   // ================== CONFIG DESDE MODO HISTORIA ==================

    // Llamado por el GameManager para ajustar vidas según la historia
    public void ConfigurarVidas(int maxVidasConfig)
    {
        maxVidas = Mathf.Max(1, maxVidasConfig);
        vidasRestantes = maxVidas;

        if (VidasHUD != null)
            VidasHUD.text = "Vidas: " + vidasRestantes + "/" + maxVidas;

        if (heartsHUD != null)
            heartsHUD.Init(maxVidas, vidasRestantes);

        // Reset visual (opcional)
        if (vfx != null)
        {
            vfx.ResetVisual();
            vfx.SetHumoNivel(0f);
        }
    }

    // Manda el objetivo al script que gestiona los suministros
    public void ConfigurarObjetivoSuministros(int objetivo)
    {
        if (cargadorSuministros != null)
        {
            // Tendrás que añadir este método en CargarSuministros
            cargadorSuministros.ConfigurarObjetivo(objetivo);
        }
    }



}
