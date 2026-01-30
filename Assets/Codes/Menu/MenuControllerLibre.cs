using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuControllerLibre : MonoBehaviour
{
    [Header("Catálogo de niveles (modo libre)")]
    public string[] sceneNames;                 // p.ej. ["Nivel1Historia","Nivel2Historia",...]
    public Sprite[] levelPreviewsCatalog;       // mismas posiciones que sceneNames

    [Header("Slots")]
    public Image[] slotPreviewImages;           // 5 imágenes (una por recuadro)
    public TMP_Dropdown[] slotLevelDropdowns;   // 5 dropdowns (uno por recuadro)
    public TMP_InputField[] slotVidasInputs;    // 5 inputs de vidas
    public TMP_InputField[] slotSuministrosInputs; // 5 inputs de suministros

    void Start()
    {
        // Suscribimos cada dropdown para que cuando cambie, actualice su preview
        for (int i = 0; i < slotLevelDropdowns.Length; i++)
        {
            int slotIndex = i; // capturar índice local
            slotLevelDropdowns[i].onValueChanged.AddListener(
                (levelIndex) => OnSlotLevelChanged(slotIndex, levelIndex)
            );

            // Inicializar preview con el valor por defecto del dropdown
            OnSlotLevelChanged(slotIndex, slotLevelDropdowns[i].value);
        }

        // Opcional: inicializar textos con 3 vidas y 5 suministros
        for (int i = 0; i < slotVidasInputs.Length; i++)
        {
            if (slotVidasInputs[i] != null) slotVidasInputs[i].text = "3";
            if (slotSuministrosInputs[i] != null) slotSuministrosInputs[i].text = "5";
        }
    }

    void OnSlotLevelChanged(int slot, int levelIndex)
    {
        if (slot < 0 || slot >= slotPreviewImages.Length) return;
        if (levelIndex < 0 || levelIndex >= levelPreviewsCatalog.Length) return;

        if (slotPreviewImages[slot] != null)
        {
            slotPreviewImages[slot].sprite = levelPreviewsCatalog[levelIndex];
            slotPreviewImages[slot].enabled = true;
        }
    }

    // Llamado por el botón "Iniciar historia" en el menú
    public void EmpezarLibrePersonalizada()
    {
        LibreSettings.librePersonalizadaActiva = true;
        LibreSettings.indiceNivelActual = 0;

        // Llenamos la configuración global a partir de lo que eligió el terapeuta
        for (int i = 0; i < LibreSettings.niveles.Length; i++)
        {
            int levelIndex = slotLevelDropdowns[i].value;

            int vidas = 3;
            int suministros = 5;
            if (slotVidasInputs[i] != null)
                int.TryParse(slotVidasInputs[i].text, out vidas);
            if (slotSuministrosInputs[i] != null)
                int.TryParse(slotSuministrosInputs[i].text, out suministros);

            LibreSettings.niveles[i] = new LibreSettings.NivelLibre
            {
                sceneName = sceneNames[levelIndex],
                maxVidas = Mathf.Max(1, vidas),
                suministrosObjetivo = Mathf.Max(1, suministros)
            };
        }

        // Cargar el primer nivel según el primer slot
        string primeraEscena = LibreSettings.niveles[0].sceneName;
        SceneManager.LoadScene(primeraEscena);
    }
}
