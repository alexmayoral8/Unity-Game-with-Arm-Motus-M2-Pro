using UnityEngine;
using UnityEngine.UI;

public class GifLikeAnimator : MonoBehaviour
{
    public Image targetImage;       // La Image de UI donde se verá la animación
    public Sprite[] frames;         // Las imágenes que formarán el "gif"
    public float framesPerSecond = 10f;  // Velocidad de la animación

    private int currentFrame = 0;
    private float timer = 0f;

    void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || targetImage == null)
            return;

        timer += Time.deltaTime;

        float frameTime = 1f / framesPerSecond;

        if (timer >= frameTime)
        {
            timer -= frameTime;

            currentFrame++;
            if (currentFrame >= frames.Length)
            {
                currentFrame = 0; // vuelve al inicio para que sea bucle
            }

            targetImage.sprite = frames[currentFrame];
        }
    }
}
