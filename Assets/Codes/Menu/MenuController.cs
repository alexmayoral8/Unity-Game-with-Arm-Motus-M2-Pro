using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class MenuController : MonoBehaviour
{
    public TMP_Dropdown levelDropdown;
    public TMP_Dropdown pilotoDropdown; // Nuevo: para seleccionar ID del piloto
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
        GameSettings.nivelSeleccionado = levelDropdown.options[levelDropdown.value].text;
        levelDropdown.onValueChanged.AddListener(delegate { NivelChanged(levelDropdown); });
        // Mostrar preview inicial del nivel seleccionado al abrir el menú
        ActualizarPreview(levelDropdown.value);
        // Piloto
        GameSettings.pilotoID = pilotoDropdown.options[pilotoDropdown.value].text;
        pilotoDropdown.onValueChanged.AddListener(delegate { PilotoChanged(pilotoDropdown); });
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

    public void StartGame()
    {
        Debug.Log($"[Menu] StartGame() pilotoID actual: '{GameSettings.pilotoID}'");
        int index = levelDropdown.value + 4; // Ajusta según tu build
        SceneManager.LoadScene(index);
    }

    public void IrAMisionPersonalizable(int indice)
    {
        SceneManager.LoadScene(indice);
    }
    public void IrAConfiguracion()
    {
        SceneManager.LoadScene("ConfiguracionDeMision");
    }
    public void StartModoHistoria()
    {
        SceneManager.LoadScene("Nivel0Historia");
    }
    public void IrAMenuInicial()
    {
        SceneManager.LoadScene("MenuInicial");
    }
    public void IrAModoLibre()
    {
        SceneManager.LoadScene("ModoLibre");
    }
    public void IrAModoHistoria()
    {
        SceneManager.LoadScene("ModoHistoria");
    }
}
