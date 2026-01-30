using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer), typeof(SplineContainer))]
public class SplineLineRendererPretty : MonoBehaviour
{
    [Header("Sampling")]
    [Tooltip("Si está activo, coloca vértices espaciados por distancia para que las curvas no se 'engrosen'")]
    public bool uniformByDistance = true;
    [Range(0.01f, 1f)] public float segmentLength = 0.06f; // distancia entre vértices (~6 cm en mundo)
    [Range(8, 500)] public int resolutionByT = 50;         // usado si uniformByDistance = false
    [Range(1, 1024)] public int oversampleForLength = 200; // LUT para longitudes

    [Header("Appearance")]
    public float width = 0.2f;
    [Range(0, 16)] public int cornerVertices = 6;
    [Range(0, 16)] public int endCapVertices = 6;
    [Tooltip("Multiplica las UV para que no se estire la textura en curvas")]
    public float textureTile = 4f;

    [Header("Transparency & Gradient")]
    public Gradient colorGradient = DefaultGradient();
    public Material ribbonMaterial; // Usa Particles/Standard Unlit (Blending: Alpha o Premultiply)

    LineRenderer line;
    SplineContainer spline;

    void OnEnable()
    {
        line = GetComponent<LineRenderer>();
        spline = GetComponent<SplineContainer>();
        ApplyStaticAppearance();
        UpdateLine();
    }

    void Update()
    {
        // Se actualiza en edición y en juego
        ApplyStaticAppearance(); // por si cambias sliders en el inspector
        UpdateLine();
    }

    void ApplyStaticAppearance()
    {
        if (!line) return;
        if (ribbonMaterial) line.sharedMaterial = ribbonMaterial;

        line.widthCurve = AnimationCurve.Constant(0, 1, width); // ancho constante (puedes poner curva)
        line.numCornerVertices = cornerVertices;
        line.numCapVertices = endCapVertices;
        //line.textureMode = LineTextureMode.Tile; // evita stretch
        line.colorGradient = colorGradient;

        // Tiling por material (Built-in): usa MainTex Scale en el material.
        // Si quieres forzar desde código:
        if (line.sharedMaterial && line.sharedMaterial.HasProperty("_MainTex"))
        {
            var mat = line.sharedMaterial;
            var st = mat.mainTextureScale;
            st.x = textureTile;
            mat.mainTextureScale = st;
        }
    }

    void UpdateLine()
    {
        if (line == null || spline == null || spline.Spline == null) return;

        if (uniformByDistance)
        {
            // 1) Construye LUT de longitudes aproximada
            int N = Mathf.Max(oversampleForLength, 32);
            Vector3 prev = spline.EvaluatePosition(0f);
            float totalLen = 0f;
            var dist = new float[N];
            var tvals = new float[N];
            tvals[0] = 0f; dist[0] = 0f;

            for (int i = 1; i < N; i++)
            {
                float t = i / (float)(N - 1);
                Vector3 p = spline.EvaluatePosition(t);
                totalLen += Vector3.Distance(prev, p);
                dist[i] = totalLen;
                tvals[i] = t;
                prev = p;
            }
            if (totalLen <= Mathf.Epsilon) { line.positionCount = 0; return; }

            // 2) Coloca vértices cada 'segmentLength' a lo largo de la distancia
            int count = Mathf.Max(2, Mathf.CeilToInt(totalLen / Mathf.Max(0.001f, segmentLength)) + 1);
            line.positionCount = count;
            float step = totalLen / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float targetD = i * step;
                // Busca t por interpolación lineal en la LUT
                int idx = System.Array.FindIndex(dist, d => d >= targetD);
                if (idx <= 0) { line.SetPosition(i, spline.EvaluatePosition(0f)); continue; }
                float d0 = dist[idx - 1], d1 = dist[idx];
                float t0 = tvals[idx - 1], t1 = tvals[idx];
                float f = Mathf.InverseLerp(d0, d1, targetD);
                float tInterp = Mathf.Lerp(t0, t1, f);
                line.SetPosition(i, spline.EvaluatePosition(tInterp));
            }
        }
        else
        {
            int n = Mathf.Max(2, resolutionByT);
            line.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                line.SetPosition(i, spline.EvaluatePosition(t));
            }
        }
    }

    // Gradiente por defecto translúcido (alpha 0.6 → 0.6)
    static Gradient DefaultGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[] {
                new GradientAlphaKey(0.6f, 0f),
                new GradientAlphaKey(0.6f, 1f)
            }
        );
        return g;
    }
}
