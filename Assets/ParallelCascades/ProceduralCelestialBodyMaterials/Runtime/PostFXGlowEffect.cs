using System;
using ParallelCascades.ProceduralCelestialBodyMaterials.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Runtime
{
    public abstract class PostFXGlowEffect : MonoBehaviour
    {
        private static readonly int ObjectPosition = Shader.PropertyToID("_Object_Position");
        protected static readonly int EffectRadius = Shader.PropertyToID("_Effect_Radius");
        protected static readonly int ObjectRadius = Shader.PropertyToID("_Object_Radius");
        protected static readonly int DensityFalloff = Shader.PropertyToID("_Density_Falloff");
        
        [SerializeField] [Min(0.001f)] protected float _meshRadius = 0.5f;
        [SerializeField] [Range(0.1f,10)] protected float _effectScale = 3f;
        [SerializeField] protected float _densityFalloff = 20f;
        [SerializeField] protected Material _material;
        [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        
        private PostFXGlowPass m_PostFXGlowPass;
        protected float _objectScale;
        private bool _initialized;

        private void OnValidate()
        {
            UpdateMaterial();
        }

        private void OnDestroy()
        {
            CleanUpPostFXGlow();
        }

        private void OnDisable()
        {
            CleanUpPostFXGlow();
            _initialized = false;
        }

        private void OnEnable()
        {
            TryInitializePostFXGlow();
        }

        private void Update()
        {
            TryInitializePostFXGlow();
            
            SetBodyScale();
            UpdateMaterial();
        }

        private void SetBodyScale()
        {
            _objectScale = transform.localScale.x;
        }

        protected abstract void UpdateMaterial();

        public void TryInitializePostFXGlow()
        {
            if (!_initialized)
            {
                if (m_PostFXGlowPass == null)
                {
                    m_PostFXGlowPass = new PostFXGlowPass
                    {
                        renderPassEvent = _renderPassEvent
                    };
                }

                if (_material == null)
                {
                    return;
                }

                m_PostFXGlowPass.Setup(_material);
                m_PostFXGlowPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
                
                RenderPipelineManager.beginCameraRendering += PostFXGlowOnBeginCamera;
                _initialized = true;
            }
        }

        private void CleanUpPostFXGlow()
        {
            RenderPipelineManager.beginCameraRendering -= PostFXGlowOnBeginCamera;
            _initialized = false;
        }

        private void PostFXGlowOnBeginCamera(ScriptableRenderContext context, Camera cam)
        {
            
            if (cam.cameraType is not (CameraType.Game or CameraType.SceneView))
            {
                return;
            }
            
            UpdateEffectPosition(transform.position);
            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_PostFXGlowPass);
        }

        private void UpdateEffectPosition(Vector3 position)
        {
            if(_material == null)
            {
                return;
            }
            
            _material.SetVector(ObjectPosition, position);
        }
    }
}