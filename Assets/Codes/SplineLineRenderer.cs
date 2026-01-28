using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[ExecuteAlways]  // 👈 Ejecuta también en modo edición
[RequireComponent(typeof(LineRenderer), typeof(SplineContainer))]
public class SplineLineRenderer : MonoBehaviour
{
    [Range(10, 200)]
    public int resolution = 50;

    private LineRenderer line;
    private SplineContainer spline;

    void OnEnable()
    {
        line = GetComponent<LineRenderer>();
        spline = GetComponent<SplineContainer>();
        UpdateLine();
    }

    void Update()
    {
        UpdateLine();
    }

    private void UpdateLine()
    {
        if (line == null || spline == null) return;

        line.positionCount = resolution;

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            float3 pos = spline.EvaluatePosition(t);
            line.SetPosition(i, pos);
        }
    }
}
