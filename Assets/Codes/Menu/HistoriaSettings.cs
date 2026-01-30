using UnityEngine;

public static class HistoriaSettings
{
    // Config de un nivel dentro del modo historia
    public struct NivelHistoria
    {
        public string sceneName;        // Nombre de la escena del nivel
        public int maxVidas;           // Vidas permitidas en ese nivel
        public int suministrosObjetivo;// Cuántos suministros debe entregar
        public bool invertido;         // Si el nivel está invertido
    }

    public static bool historiaPersonalizadaActiva = false;

    public static int indiceNivelActual = 0;

    // Asumimos 5 niveles en la historia
    public static NivelHistoria[] niveles = new NivelHistoria[5];

    public static NivelHistoria ObtenerNivelActual()
    {
        return niveles[indiceNivelActual];
    }
}
