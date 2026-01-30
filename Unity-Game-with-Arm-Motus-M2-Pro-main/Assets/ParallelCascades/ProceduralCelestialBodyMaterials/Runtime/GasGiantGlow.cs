using System;
using UnityEngine;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Runtime
{
    [ExecuteInEditMode]
    public class GasGiantGlow : PostFXGlowEffect
    {        
        protected static readonly int GlowColor = Shader.PropertyToID("_Glow_Color");

        [SerializeField][ColorUsage(false,true)]
        protected Color _color = Color.white;

        // Overwrites base class default value
        private void Reset()
        {
            _effectScale = 1.5f;
        }

        protected override void UpdateMaterial()
        {
            if (_material == null)
            {
                return;
            }
            
            var meshAdjustedScale = _objectScale * _meshRadius;
            var meshAdjustedEffectScale = _effectScale * _meshRadius;
            
            _material.SetFloat(EffectRadius, meshAdjustedScale * (1+meshAdjustedEffectScale));
            _material.SetFloat(ObjectRadius, meshAdjustedScale);            
            _material.SetFloat(DensityFalloff, _densityFalloff);
            _material.SetColor(GlowColor, _color);
        }

        public void SetColor(Color glowColor)
        {
#if UNITY_EDITOR
            // This allows us to record the change in the editor, when using Editor UI
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.RecordObject(this, "Set Glow Color");
            }
#endif
    
            _color = glowColor;
    
            if (_material != null)
            {
                _material.SetColor(GlowColor, _color);
            }
    
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}