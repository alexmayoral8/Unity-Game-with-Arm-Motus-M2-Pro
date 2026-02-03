using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SensibilidadRobot : MonoBehaviour
{
    [Header("UI")]
    public Slider robotSensSlider;
    public TextMeshProUGUI robotSensLabel;

    [Header("Robot Sensitivity")]
    public float minSens = 0.5f;
    public float maxSens = 2.0f;
    public float defaultSens = 1.0f;

    const string PREF_KEY_ROBOT_SENS = "Robot_Sens";

    void Awake()
    {
        // Si quieres que persista entre escenas:
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        float s = LoadRobotSens();
        SetupUI(s);
        ApplyRobotSensToCurrentNave(s);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cada vez que cargas un nivel nuevo, reaplica al NaveHistoriaPrueba de esa escena
        ApplyRobotSensToCurrentNave(LoadRobotSens());
    }

    float LoadRobotSens()
    {
        float s = PlayerPrefs.GetFloat(PREF_KEY_ROBOT_SENS, defaultSens);
        return Mathf.Clamp(s, minSens, maxSens);
    }

    void SaveRobotSens(float s)
    {
        PlayerPrefs.SetFloat(PREF_KEY_ROBOT_SENS, s);
        PlayerPrefs.Save();
    }

    void SetupUI(float s)
    {
        if (robotSensSlider != null)
        {
            robotSensSlider.minValue = minSens;
            robotSensSlider.maxValue = maxSens;
            robotSensSlider.value = s;

            // evita duplicar listeners si re-entras a escena
            robotSensSlider.onValueChanged.RemoveListener(OnRobotSensChanged);
            robotSensSlider.onValueChanged.AddListener(OnRobotSensChanged);
        }

        UpdateLabel(s);
    }

    void UpdateLabel(float s)
    {
        if (robotSensLabel != null)
            robotSensLabel.text = $"Sensibilidad robot: {s:0.00}x";
    }

    // Se llama al mover el slider
    public void OnRobotSensChanged(float value)
    {
        float s = Mathf.Clamp(value, minSens, maxSens);
        SaveRobotSens(s);
        UpdateLabel(s);
        ApplyRobotSensToCurrentNave(s);
    }

    void ApplyRobotSensToCurrentNave(float s)
    {
        // Aplica a la nave activa (si existe en esta escena)
        var nave = FindAnyObjectByType<NaveHistoriaPrueba>();
        if (nave != null)
        {
            nave.robotSensitivity = s;

            // opcional: fuerza que re-evalúe target si quieres evitar saltos
            // (si agregaste un método público para esto)
            // nave.ReinitTarget();
        }
    }
}