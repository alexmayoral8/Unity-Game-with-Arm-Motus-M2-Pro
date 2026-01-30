using System.Collections.Generic;
using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    public List<GameObject> characters = new List<GameObject>();
    private int currentCharacter = 1;

    public const string PREF_KEY_CHARACTER = "SelectedCharacterIndex";

    void Start()
    {
        if (characters.Count == 0) return;

        // Leer lo que ya estaba guardado (por defecto 0)
        int savedIndex = PlayerPrefs.GetInt(PREF_KEY_CHARACTER, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, characters.Count - 1);
        ActiveCharacter(savedIndex);
    }

    public void ActiveCharacter(int index)
    {
        foreach (GameObject character in characters)
            character.SetActive(false);

        if (index >= 0 && index < characters.Count)
        {
            characters[index].SetActive(true);
            currentCharacter = index;
        }

        PlayerPrefs.SetInt(PREF_KEY_CHARACTER, currentCharacter);
        PlayerPrefs.Save();
    }

    public void SwitchToPreviousCharacter()
    {
        int previousIndex = currentCharacter - 1;
        if (previousIndex < 0)
            previousIndex = characters.Count - 1;

        ActiveCharacter(previousIndex);
    }

    public void SwitchToNextCharacter()
    {
        int nextIndex = currentCharacter + 1;
        if (nextIndex >= characters.Count)
            nextIndex = 0;

        ActiveCharacter(nextIndex);
    }
}
