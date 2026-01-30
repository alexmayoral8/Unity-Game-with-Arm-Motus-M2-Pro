using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Sensibilidad : MonoBehaviour
{
    [Header("UI")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI valueLabel;

    // Clave única en PlayerPrefs
    public const string PREF_KEY = "CursorSensitivity";

    [Header("Rango y formato")]
    public float min = 0.001f;
    public float max = 2.0f;
    public int decimals = 2; // cuántos decimales mostrar

    void Awake()
    {
        // Configura rango
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = min;
            sensitivitySlider.maxValue = max;

            // Lee el valor guardado o usa 1.0f por defecto
            float saved = PlayerPrefs.GetFloat(PREF_KEY, 1.0f);
            saved = Mathf.Clamp(saved, min, max);
            sensitivitySlider.value = saved;

            // Suscribe el cambio
            sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
        }

        UpdateLabel();
    }

    public void OnSliderChanged(float v)
    {
        // Guarda inmediatamente
        PlayerPrefs.SetFloat(PREF_KEY, v);
        PlayerPrefs.Save();
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (valueLabel == null || sensitivitySlider == null) return;
        float v = sensitivitySlider.value;
        valueLabel.text = $"{v.ToString($"F{decimals}")}";
    }
}
