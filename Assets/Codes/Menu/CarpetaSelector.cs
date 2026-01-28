using UnityEngine;
using SimpleFileBrowser; // 👈 Importante

public class CarpetaSelector : MonoBehaviour
{
    public void SeleccionarCarpeta()
    {
        // Mostrar el diálogo para elegir una carpeta
        FileBrowser.ShowLoadDialog(
            (paths) => {
                // Guardamos la carpeta elegida en el CSVExporter
                CSVExporter.saveFolder = paths[0];
                Debug.Log("📂 Carpeta seleccionada: " + CSVExporter.saveFolder);
            },
            () => { Debug.Log("❌ Selección cancelada"); },
            FileBrowser.PickMode.Folders
        );
    }
}
