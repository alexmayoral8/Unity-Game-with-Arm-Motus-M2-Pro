using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MouseParallaxUV : MonoBehaviour
{
    public Vector2 uvScale = new Vector2(0.02f, 0.01f); // sensibilidad del parallax (menor = más sutil)
    public float smooth = 10f;                           // suavizado
    private Renderer rend;
    private Vector2 baseOffset;
    private Vector2 currentOffset;

    void Start()
    {
        rend = GetComponent<Renderer>();
        // Clonar material para no afectar materiales compartidos
        rend.material = new Material(rend.material);
        baseOffset = rend.material.mainTextureOffset;
        currentOffset = baseOffset;
    }

    void Update()
    {
        // mouse normalizado a [-1, 1] respecto al centro de pantalla
        float nx = (Input.mousePosition.x / Screen.width  - 0.5f) * 2f;
        float ny = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        Vector2 target = baseOffset + new Vector2(nx * uvScale.x, ny * uvScale.y);
        currentOffset = Vector2.Lerp(currentOffset, target, smooth * Time.deltaTime);
        rend.material.mainTextureOffset = currentOffset;
    }
}
