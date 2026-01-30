using UnityEngine;

public class SuministroFlotante : MonoBehaviour
{
    [Header("Flotado")]
    public float driftSpeed = 0.6f;        // velocidad base de deriva
    public float spinSpeed = 20f;          // grados/seg de giro
    public float autoDampen = 0.98f;       // factor de amortiguamiento por frame
    public float impulsoInicial = 0.7f;    // “empujoncito” al soltarse

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Empuje inicial en una dirección semialeatoria
            Vector2 dir = Random.insideUnitCircle.normalized;
            rb.linearVelocity = dir * (driftSpeed + Random.Range(0f, 0.4f)) + (Vector2)transform.right * impulsoInicial;
            rb.angularVelocity = (Random.value > 0.5f ? 1 : -1) * spinSpeed;
        }
    }

    void Update()
    {
        // Amortigua poco a poco para que no se vaya demasiado lejos
        if (rb != null)
        {
            rb.linearVelocity *= autoDampen;
            rb.angularVelocity *= autoDampen;
        }
    }
}
