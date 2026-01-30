using UnityEngine;
using System.Collections;

public class NaveAveriaVFX : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer sr;          // si no lo asignas, se busca en hijos
    public ParticleSystem humo;        // opcional

    [Header("Parpadeo daño")]
    public float blinkFreq = 16f;      // Hz (color)
    public float hardBlinkHz = 10f;    // Hz (on/off)
    public Color hitTint = new Color(1f, 0.6f, 0.6f, 1f);
    public bool useHardBlinkFallback = true; // toggle visible si el tint no se nota

    Color originalColor;

    void Awake()
    {
        // Sólo inicializamos el humo aquí
        if (humo != null)
        {
            var em = humo.emission;
            em.rateOverTime = 0f;  // inicia sin humo
        }
    }

    void Start()
    {
        EnsureSpriteRef();
    }

    void EnsureSpriteRef()
    {
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                originalColor = sr.color;
            }
            else
            {
                Debug.LogWarning("[NaveAveriaVFX] No encontré SpriteRenderer (ni en hijos).");
            }
        }
    }

    public void FlashHitImmediate(float duration)
    {
        EnsureSpriteRef();
        if (sr == null) return;

        StopAllCoroutines();
        StartCoroutine(FlashHit(duration));
    }

    public IEnumerator FlashHit(float duration)
    {
        EnsureSpriteRef();
        if (sr == null) yield break;

        float t = 0f;

        // 1) Intento con tint (suave)
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.PingPong(t * blinkFreq, 1f);
            sr.color = Color.Lerp(originalColor, hitTint, k);
            yield return null;
        }

        // restaurar color
        sr.color = originalColor;

        // 2) Hard blink (on/off)
        if (useHardBlinkFallback)
        {
            float t2 = 0f;
            float interval = 1f / Mathf.Max(1f, hardBlinkHz);
            bool state = true;
            while (t2 < duration * 0.5f)
            {
                t2 += interval;
                state = !state;
                sr.enabled = state;
                yield return new WaitForSeconds(interval);
            }
            sr.enabled = true;
        }
    }

    // Ajusta la cantidad de humo (0..1)
    public void SetHumoNivel(float level01)
    {
        if (humo == null) return;
        var em = humo.emission;
        em.rateOverTime = Mathf.Lerp(0f, 35f, Mathf.Clamp01(level01));
        if (!humo.isPlaying && level01 > 0f) humo.Play();
        if (level01 <= 0f && humo.isPlaying) humo.Stop();
    }

    public void ResetVisual()
    {
        EnsureSpriteRef();
        if (sr != null)
        {
            sr.color = originalColor;
            sr.enabled = true;
        }
        SetHumoNivel(0f);
    }
}
