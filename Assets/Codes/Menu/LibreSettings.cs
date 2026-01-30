using UnityEngine;

public static class LibreSettings
{
    // Config de un nivel dentro del modo historia
    public struct NivelLibre
    {
        public string sceneName;        // Nombre de la escena del nivel
        public int maxVidas;           // Vidas permitidas en ese nivel
        public int suministrosObjetivo;// Cuántos suministros debe entregar
    }

    public static bool librePersonalizadaActiva = false;

    public static int indiceNivelActual = 0;

    public static NivelLibre[] niveles = new NivelLibre[1];

    public static NivelLibre ObtenerNivelActual()
    {
        return niveles[indiceNivelActual];
    }
}
