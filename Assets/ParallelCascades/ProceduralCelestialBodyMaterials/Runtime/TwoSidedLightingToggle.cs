using UnityEngine;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Runtime
{
    // This is a simple script used in the demo scene for toggling the two-sided lighting feature in the asteroid ring shader.
    public class TwoSidedLightingToggle : MonoBehaviour
    {
        [SerializeField] private Material m_Material;
        
        private static readonly int TwoSidedLighting = Shader.PropertyToID("_TwoSidedLighting");
        
        public void ToggleTwoSidedLighting(bool newValue)
        {
            if (m_Material != null)
            {
                m_Material.SetFloat(TwoSidedLighting, newValue ? 1f : 0f);
            }
        }
    }
}