using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class MenuController : MonoBehaviour
{
    //public TMP_Dropdown levelDropdown;
    public TMP_Dropdown pilotoDropdown; // Nuevo: para seleccionar ID del piloto
    public TMP_Dropdown controlDropdown;
    public Toggle emgToggle;
    public Image previewImage;                 // Image en el Canvas donde se verá la miniatura
    public Sprite[] levelPreviews;             // Sprites de cada nivel (mismo orden que el dropdown)

    void Start()
    {
        Debug.Log($"[Menu] Start() pilotoDropdown null? {pilotoDropdown == null}");
        Debug.Log($"[Menu] opciones piloto: {(pilotoDropdown?.options != null ? pilotoDropdown.options.Count : -1)}  value: {(pilotoDropdown != null ? pilotoDropdown.value : -1)}");

        // Piloto
        if (pilotoDropdown != null && pilotoDropdown.options != null && pilotoDropdown.options.Count > 0)
        {
            GameSettings.pilotoID = pilotoDropdown.options[pilotoDropdown.value].text;
            Debug.Log($"[Menu] Piloto inicial seteado: '{GameSettings.pilotoID}'");
        }
        else
        {
            Debug.LogError("[Menu] pilotoDropdown sin opciones o no asignado. No se pudo setear pilotoID.");
        }

        pilotoDropdown.onValueChanged.AddListener(delegate { PilotoChanged(pilotoDropdown); });

        // (tu código de nivel y preview igual)
        // Nivel
        //GameSettings.nivelSeleccionado = levelDropdown.options[levelDropdown.value].text;
        //levelDropdown.onValueChanged.AddListener(delegate { NivelChanged(levelDropdown); });
        // Mostrar preview inicial del nivel seleccionado al abrir el menú
        //ActualizarPreview(levelDropdown.value);
        // Piloto

        // ===== Cargar configuraciones guardadas =====

        bool useArmMotus =
            PlayerPrefs.GetInt("UseArmMotus", 0) == 1;

        bool useEMG =
            PlayerPrefs.GetInt("UseEMG", 0) == 1;

        // Actualizar UI
        if (controlDropdown != null)
        {
            controlDropdown.value = useArmMotus ? 1 : 0;

            controlDropdown.onValueChanged.AddListener(delegate
            {
                ControlChanged(controlDropdown);
            });
        }

        if (emgToggle != null)
        {
            emgToggle.isOn = useEMG;

            emgToggle.onValueChanged.AddListener(delegate
            {
                EMGChanged(emgToggle);
            });
        }
    }

    void NivelChanged(TMP_Dropdown change)
    {
        GameSettings.nivelSeleccionado = change.options[change.value].text;
        Debug.Log("Nivel seleccionado: " + GameSettings.nivelSeleccionado);
    
        // Actualizar preview al cambiar de nivel
        ActualizarPreview(change.value);
    }
    void ActualizarPreview(int index)
    {
        // Imagen
        if (previewImage != null &&
            levelPreviews != null &&
            index >= 0 && index < levelPreviews.Length &&
            levelPreviews[index] != null)
        {
            previewImage.sprite = levelPreviews[index];
            previewImage.enabled = true;
        }
        else if (previewImage != null)
        {
            // Si no hay sprite para ese índice, ocultamos la imagen
            previewImage.enabled = false;
        }

    }
    void PilotoChanged(TMP_Dropdown change)
    {
        GameSettings.pilotoID = change.options[change.value].text;
        Debug.Log("Piloto seleccionado: " + GameSettings.pilotoID);
    }

    /*public void StartGame()
    {
        Debug.Log($"[Menu] StartGame() pilotoID actual: '{GameSettings.pilotoID}'");
        int index = levelDropdown.value + 4; // Ajusta según tu build
        SceneManager.LoadScene(index);
    }*/

    public void IrAMisionPersonalizable(int indice)
    {
        GuardarConfiguracionActual();
        SceneManager.LoadScene(indice);
    }
    public void IrAConfiguracion()
    {
        GuardarConfiguracionActual();
        SceneManager.LoadScene("ConfiguracionDeMision");
    }
        public void IrAInstrucciones()
    {
        GuardarConfiguracionActual();
        SceneManager.LoadScene("InstruccionesDeMision");
    }
    public void StartModoHistoria()
    {
        GuardarConfiguracionActual();
        SceneManager.LoadScene("Nivel0Historia");
    }
    public void IrAMenuInicial()
    {
       GuardarConfiguracionActual();
        SceneManager.LoadScene("MenuInicial");
    }
    public void IrAModoLibre()
    {
        GuardarConfiguracionActual();

        SceneManager.LoadScene("ModoLibre");
    }
    public void IrAModoHistoria()
    {
        GuardarConfiguracionActual();

        SceneManager.LoadScene("ModoHistoria");
    }
    public void IrACalibracion()
    {
        GuardarConfiguracionActual();
        SceneManager.LoadScene("calibracion");
    }
    void ControlChanged(TMP_Dropdown change)
    {
        bool useArmMotus = change.value == 1;

        GameSettings.useArmMotus = useArmMotus;

        PlayerPrefs.SetInt("UseArmMotus", useArmMotus ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Modo control guardado: " + (useArmMotus ? "ArmMotus" : "Mouse"));
    }



    void EMGChanged(Toggle change)
    {
        bool useEMG = change.isOn;

        GameSettings.useEMG = useEMG;

        PlayerPrefs.SetInt("UseEMG", useEMG ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("EMG guardado: " + (useEMG ? "Activado" : "Desactivado"));
    }
    private void GuardarConfiguracionActual()
    {
        // CONTROL
        if (controlDropdown != null)
        {
            bool useArmMotus = controlDropdown.value == 1;

            GameSettings.useArmMotus = useArmMotus;
            PlayerPrefs.SetInt("UseArmMotus", useArmMotus ? 1 : 0);

            Debug.Log($"[Menu] Control guardado desde dropdown: ArmMotus={useArmMotus}");
        }
        else
        {
            bool useArmMotus = PlayerPrefs.GetInt("UseArmMotus", 0) == 1;
            GameSettings.useArmMotus = useArmMotus;

            Debug.Log($"[Menu] No hay controlDropdown en esta escena. Se conserva ArmMotus={useArmMotus}");
        }

        // EMG
        if (emgToggle != null)
        {
            bool useEMG = emgToggle.isOn;

            GameSettings.useEMG = useEMG;
            PlayerPrefs.SetInt("UseEMG", useEMG ? 1 : 0);

            Debug.Log($"[Menu] EMG guardado desde toggle: EMG={useEMG}");
        }
        else
        {
            bool useEMG = PlayerPrefs.GetInt("UseEMG", 0) == 1;
            GameSettings.useEMG = useEMG;

            Debug.Log($"[Menu] No hay emgToggle en esta escena. Se conserva EMG={useEMG}");
        }

        PlayerPrefs.Save();
    }

}
