using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HeartsHUD : MonoBehaviour
{
    [System.Serializable]
    public class HeartSlot
    {
        public Image heartImage;          // la imagen del corazón
        public RectTransform fxAnchor;    // dónde instanciar la explosión UI
    }

    [Header("Setup")]
    public List<HeartSlot> slots = new List<HeartSlot>(); // arrastra 3
    public GameObject heartExplosionUIPrefab;             // tu prefab UI de explosión

    [Header("Opciones")]
    public float hideDelay = 0.15f;       // pequeña espera antes de ocultar el corazón
    public bool useUnscaled = true;       // para que la explosión corra aunque pauses

    int _max = 3;
    int _current = 3;
    bool _initialized = false;

    public void Init(int maxVidas, int vidasActuales)
    {
        _max = Mathf.Clamp(maxVidas, 0, slots.Count);
        _current = Mathf.Clamp(vidasActuales, 0, _max);

        // Activa sólo los corazones que existan
        for (int i = 0; i < slots.Count; i++)
        {
            bool visible = i < _max;
            if (slots[i].heartImage) slots[i].heartImage.gameObject.SetActive(visible);
        }

        // Muestra como “encendidos” los _current primeros
        for (int i = 0; i < _max; i++)
        {
            if (slots[i].heartImage)
                slots[i].heartImage.enabled = (i < _current);
        }

        _initialized = true;
    }

    /// <summary>
    /// Llamar cuando pierdas una vida. Recibe vidasActuales ya decrementadas.
    /// </summary>
    public void OnLifeLost(int vidasActuales)
    {
        if (!_initialized) return;
        _current = Mathf.Clamp(vidasActuales, 0, _max);

        // Índice del corazón que “explota” es justamente _current (el que se acaba de perder)
        // porque si pasaste de 3→2, el que se apaga es el índice 2 (0-based).
        int lostIndex = _current; 
        if (lostIndex >= 0 && lostIndex < _max)
            StartCoroutine(ExplodeAndHide(lostIndex));
    }

    IEnumerator ExplodeAndHide(int index)
    {
        var slot = slots[index];
        // Instancia FX en UI
        if (heartExplosionUIPrefab != null && slot.fxAnchor != null)
        {
            var fx = Instantiate(heartExplosionUIPrefab, slot.fxAnchor);
            // Si quieres que ignore timeScale, configura el Animator/Particle UI a tiempo no escalado
            if (useUnscaled)
            {
                var anim = fx.GetComponent<Animator>();
                if (anim) anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                // Si usas TMPro/animaciones por script, también usa Time.unscaledDeltaTime
            }
            Destroy(fx, 2f); // limpia FX
        }

        // Espera breve y “apaga” el corazón
        if (useUnscaled) yield return new WaitForSecondsRealtime(hideDelay);
        else yield return new WaitForSeconds(hideDelay);

        if (slot.heartImage) slot.heartImage.enabled = false;
    }

    /// <summary>
    /// Resetea la UI (p.ej. al cambiar de nivel/reintentar).
    /// </summary>
    public void ResetHearts(int maxVidas, int vidasActuales)
    {
        Init(maxVidas, vidasActuales);
    }
}
