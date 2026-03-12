using System.IO;
using UnityEngine;

public static class SessionManager
{
    public static int numeroRonda = -1;

    public static void Inicializar(string carpeta, string piloto, string concepto)
    {
        if (numeroRonda != -1) return; // ya inicializado

        string fecha = System.DateTime.Now.ToString("yyyyMMdd");
        string patron = $"{fecha}_{piloto}_{concepto}_*_n1.csv";

        int rondasExistentes = Directory.GetFiles(carpeta, patron).Length;
        numeroRonda = rondasExistentes + 1;

        Debug.Log($"Ronda actual: {numeroRonda}");
    }

    public static string ObtenerRondaFormateada()
    {
        return numeroRonda.ToString("D2");
    }
}