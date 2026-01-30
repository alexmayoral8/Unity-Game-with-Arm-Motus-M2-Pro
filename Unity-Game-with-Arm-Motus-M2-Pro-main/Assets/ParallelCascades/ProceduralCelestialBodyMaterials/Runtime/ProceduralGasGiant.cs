using System;
using ParallelCascades.Common.Runtime;
using ParallelCascades.ProceduralShaders.Editor;
using ParallelCascades.ProceduralShaders.Runtime.PropertyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Runtime
{
    public class ProceduralGasGiant : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private Texture2D _colorGradientTexture;
        
        // Necessary for procedural generation - randomization/update of the gas giant glow
        [SerializeField] private GasGiantGlow _glow;
        
        [SerializeField][BindGradientToTexture("_colorGradientTexture")] 
        private Gradient _colorGradient = new(){colorKeys =  new GradientColorKey[2]
        {
            new() {color = Color.white, time = 0},
            new() {color = Color.black, time = 1}
        }};
        
        #region Material Properties
        [Header("Material Properties")]
        [SerializeField][ColorUsage(false,true)] private Color _fresnelColor = Color.white;
        [SerializeField][Range(0.001f,1)] private float _scale = 1f;
        [SerializeField][Range(1,20)] private float _fresnelPower = 10f;
        [SerializeField][Range(0.0f,0.1f)] private float _flowSpeed = 0.001f;
        [SerializeField][Range(0.0f,0.2f)] private float _zScaling = 0.01f;
        [SerializeField][Range(0.0f,0.2f)] private float _xScaling = 0.01f;
        [SerializeField][Range(0.0f,5f)] private float _yGradientScale = 0.1f;
        [SerializeField][Range(0.0f,0.1f)] private float _yGradientStrength = 0.1f;
        [SerializeField] private Vector3 _offset;
        #endregion

        [SerializeField] private RandomizationProperties m_RandomizationProperties;
        
        [Serializable]
        private class RandomizationProperties
        {
            [InspectorButton("GenerateRandomGasGiant", "Randomize Gas Giant")]
            [InspectorButton("RandomizeColor", "Randomize Color")]
            [InspectorButton("GenerateColor", "Generate Color")]
            [SerializeField] public int ColorSeed;
            [SerializeField] public Vector2 RandomColorRange = new Vector2(0.5f, 1f);
            [SerializeField] public AnimationCurve TwoColorGradientChanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            [InspectorButton("RandomizeNoisePattern", "Randomize Noise")]
            [InspectorButton("GenerateNoisePattern", "Generate Noise")]
            [SerializeField] public int NoisePatternSeed;
            [SerializeField] public Vector2 PatternScaleRange = new Vector2(0.05f, 1f);
            [SerializeField] public Vector2 PlanetFlowSpeedRange = new Vector2(0.001f, 0.005f);
            
            [SerializeField] public Vector2 ZScalingRange = new Vector2(0.005f, 0.2f);
            
            [SerializeField] public Vector2 XScalingRange = new Vector2(0.005f, 0.2f);
            
            [SerializeField] public Vector2 YGradientScaleRange = new Vector2(0f, 2f);
            [SerializeField] public Vector2 YGradientStrengthRange = new Vector2(0.01f, 0.1f);
        }

        private static readonly int ColorGradientTexture = Shader.PropertyToID("_Color_Gradient_Texture");
        private static readonly int FresnelColor = Shader.PropertyToID("_Fresnel_Color");
        private static readonly int Scale = Shader.PropertyToID("_Scale");
        private static readonly int FresnelPower = Shader.PropertyToID("_Fresnel_Power");
        private static readonly int FlowSpeed = Shader.PropertyToID("_Flow_Speed");
        private static readonly int ZScaling = Shader.PropertyToID("_Z_Scaling");
        private static readonly int XScaling = Shader.PropertyToID("_X_Scaling");
        private static readonly int YGradientScale = Shader.PropertyToID("_Y_Gradient_Scale");
        private static readonly int YGradientStrength = Shader.PropertyToID("_Y_Gradient_Strength");
        private static readonly int Offset = Shader.PropertyToID("_Offset");
        

        private void OnValidate()
        {
            // Necessary to restore the texture when exiting play mode
            if (_colorGradientTexture)
            {
                RuntimeTextureUtilities.SetTextureFromGradient(_colorGradient,_colorGradientTexture);
            }

            UpdateMaterial();
        }
        
        public void UpdateMaterial()
        {
            if(_material == null || _colorGradientTexture == null)
            {
                return;
            }
            
            _material.SetTexture(ColorGradientTexture, _colorGradientTexture);
            _material.SetColor(FresnelColor, _fresnelColor);
            _material.SetFloat(Scale, _scale);
            _material.SetFloat(FresnelPower, _fresnelPower);
            _material.SetFloat(FlowSpeed, _flowSpeed);
            _material.SetFloat(ZScaling, _zScaling);
            _material.SetFloat(XScaling, _xScaling);
            _material.SetFloat(YGradientScale, _yGradientScale);
            _material.SetFloat(YGradientStrength, _yGradientStrength);
            _material.SetVector(Offset, _offset);
        }

        public void RandomizeNoisePatternSeed()
        {
            m_RandomizationProperties.NoisePatternSeed = System.DateTime.Now.Millisecond;
        }

        /// <summary>
        /// If you want to randomize the noise pattern of the gas giant, you need to call <c>RandomizeNoisePatternSeed()</c> first.
        /// To actually pass the updated properties to the material, you need to call <c>UpdateMaterial()</c> after this method.
        /// </summary>
        public void UpdateNoisePropertiesFromCurrentSeed()
        {
            Random.InitState(m_RandomizationProperties.NoisePatternSeed);
            
            _scale = Random.Range(m_RandomizationProperties.PatternScaleRange.x, m_RandomizationProperties.PatternScaleRange.y);
            _flowSpeed = Random.Range(m_RandomizationProperties.PlanetFlowSpeedRange.x, m_RandomizationProperties.PlanetFlowSpeedRange.y);
            _zScaling = Random.Range(m_RandomizationProperties.ZScalingRange.x, m_RandomizationProperties.ZScalingRange.y);
            _xScaling = Random.Range(m_RandomizationProperties.XScalingRange.x, m_RandomizationProperties.XScalingRange.y);
            _yGradientScale = Random.Range(m_RandomizationProperties.YGradientScaleRange.x, m_RandomizationProperties.YGradientScaleRange.y);
            _yGradientStrength = Random.Range(m_RandomizationProperties.YGradientStrengthRange.x, m_RandomizationProperties.YGradientStrengthRange.y);
        }

        private void RandomizeColorSeed()
        {
            m_RandomizationProperties.ColorSeed = System.DateTime.Now.Millisecond;
        }

        public void GenerateColorWithCurrentSeed()
        {
            Random.InitState(m_RandomizationProperties.ColorSeed);

            Color mainColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            
            // Gradient generation
            float twoColorGradientChance = m_RandomizationProperties.TwoColorGradientChanceCurve.Evaluate(Random.value);
            float randomValue = Random.value;
            if (randomValue < twoColorGradientChance)
            {
                Color colorB = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                _colorGradient = ColorUtilities.CreateRandomGradientFromTwoColors(mainColor, colorB, 8, 8, .5f, 2f);
            }
            else
            {
                _colorGradient = ColorUtilities.CreateRandomGradientFromColor(mainColor, 2, 8, .5f, 2f); 
            }
            
            // Fresnel is more saturated and brighter (HDR) version of the main color
            Color.RGBToHSV(mainColor, out float h, out float s, out float v);
            _fresnelColor = Color.HSVToRGB(h, Mathf.Clamp01(s+0.2f), v*1.4f, true);
            
            if(_glow != null)
            {
                _glow.SetColor(_fresnelColor);
            }
        }

        /// <summary>
        /// Use this method to generate a random gas giant at runtime.
        /// Randomizes noise and color properties of the gas giant.
        /// Updates gradient texture and passes properties to the material, updating the visuals at runtime.
        /// </summary>
        public void GenerateRandomGasGiant()
        {
            RandomizeColorSeed();
            RandomizeNoisePatternSeed();
            GenerateColorWithCurrentSeed();
            UpdateNoisePropertiesFromCurrentSeed();
            
            RuntimeTextureUtilities.SetTextureFromGradient(_colorGradient,_colorGradientTexture);
            
            UpdateMaterial();
        }

        public void RandomizeColor()
        {
            RandomizeColorSeed();
            GenerateColorWithCurrentSeed();
            UpdateMaterial();
        }
        
        public void GenerateColor()
        {
            GenerateColorWithCurrentSeed();
            UpdateMaterial();
        }

        public void RandomizeNoisePattern()
        {
            RandomizeNoisePatternSeed();
            UpdateNoisePropertiesFromCurrentSeed();
            UpdateMaterial();
        }

        public void GenerateNoisePattern()
        {
            UpdateNoisePropertiesFromCurrentSeed();
            UpdateMaterial();
        }

    }
}