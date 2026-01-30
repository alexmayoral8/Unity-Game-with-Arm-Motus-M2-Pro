using System.Collections.Generic;
using UnityEngine;

public class NaveSkinLoader : MonoBehaviour
{
    [Header("Skins disponibles (prefabs)")]
    public List<GameObject> skinPrefabs = new List<GameObject>();

    [Header("Dónde se va a instanciar la skin")]
    public Transform skinParent;  // opcional, si lo dejas vacío usa this.transform

    void Start()
    {
        if (skinPrefabs.Count == 0)
        {
            Debug.LogWarning("NaveSkinLoader: No hay skins asignadas.");
            return;
        }

        // Leemos el índice guardado en el menú
        int index = PlayerPrefs.GetInt(CharacterSelector.PREF_KEY_CHARACTER, 0);
        index = Mathf.Clamp(index, 0, skinPrefabs.Count - 1);

        Transform parent = skinParent != null ? skinParent : this.transform;

        // Instanciar la skin seleccionada como hija de la nave lógica
        GameObject skinInstance = Instantiate(skinPrefabs[index], parent);
        skinInstance.transform.localPosition = Vector3.zero;
        skinInstance.transform.localRotation = Quaternion.identity;
        skinInstance.transform.localScale = Vector3.one;

        // Por si acaso la skin aún tiene Rigidbody2D o NaveHistoriaPrueba, los removemos
        var rb = skinInstance.GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);

        var naveScript = skinInstance.GetComponent<NaveHistoriaPrueba>();
        if (naveScript != null) Destroy(naveScript);
    }
}
