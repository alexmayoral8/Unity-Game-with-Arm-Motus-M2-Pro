using System;
using ParallelCascades.Common.Runtime;
using ParallelCascades.ProceduralShaders.Editor;
using ParallelCascades.ProceduralShaders.Runtime.PropertyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Runtime
{
    public class ProceduralStar : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private Texture2D _colorGradientTexture;
        
        // Necessary for procedural generation - randomization/update of the star glow
        [SerializeField] private StarCorona _corona;
        
        [Header("Material Properties")]
        [BindGradientToTexture("_colorGradientTexture", true)][SerializeField]
        private Gradient _colorGradient = new(){colorKeys =  new GradientColorKey[2]
        {
            new() {color = Color.yellow, time = 0},
            new() {color = Color.red, time = 1}
        }};
        [SerializeField][ColorUsage(false,true)] private Color _colorR = Color.yellow;
        [SerializeField][ColorUsage(false,true)] private Color _colorQ = Color.red;
        [SerializeField][ColorUsage(false,true)] private Color _fresnelColor = Color.red;
        
        [SerializeField][Range(0.1f,10)] private float _scale = 1f;
        [SerializeField][Range(0.1f,10)] private float _warpAmount = 1f;
        [SerializeField][Range(1,20)] private float _fresnelPower = 1f;
        [SerializeField][Range(0.001f,1)] private float _flowSpeed = 0.1f;
        
        [SerializeField] private RandomizationProperties m_RandomizationProperties;
        
        [Serializable]
        private class RandomizationProperties
        {
            [InspectorButton("GenerateRandomStar", "Randomize Star")]
            [InspectorButton("RandomizeColor", "Randomize Color")]
            [InspectorButton("GenerateColor", "Generate Color")]
            [SerializeField] public int ColorSeed;
            [SerializeField] public Gradient RandomizationColorPalette = new Gradient
            {
                colorKeys = new GradientColorKey[4]
                {
                    new() {color = Color.red, time = 0},
                    new() {color = Color.yellow, time = .5f},
                    new() {color = Color.white, time = .75f},
                    new() {color = Color.cyan, time = 1}
                }
            };
            
            [InspectorButton("RandomizeNoise", "Randomize Noise")]
            [InspectorButton("GenerateNoise", "Generate Noise")]
            [SerializeField] public int NoiseSeed;
            [SerializeField] public Vector2 PatternScaleRange = new Vector2(1f, 5f);
            [SerializeField] public Vector2 WarpAmountRange = new Vector2(0.1f, 3f);
            [SerializeField] public Vector2 FresnelPowerRange = new Vector2(1f, 5f);
            [SerializeField] public Vector2 FlowSpeedRange = new Vector2(0.025f, 0.2f);
        }
        
        private static readonly int ColorGradientTexture = Shader.PropertyToID("_Color_Gradient_Texture");
        private static readonly int ColorR = Shader.PropertyToID("_Color_R");
        private static readonly int ColorQ = Shader.PropertyToID("_Color_Q");
        private static readonly int FresnelColor = Shader.PropertyToID("_Fresnel_Color");
        private static readonly int Scale = Shader.PropertyToID("_Scale");
        private static readonly int WarpAmount = Shader.PropertyToID("_Warp_Amount");
        private static readonly int FresnelPower = Shader.PropertyToID("_Fresnel_Power");
        private static readonly int FlowSpeed = Shader.PropertyToID("_Flow_Speed");

        private void OnValidate()
        {
            // Necessary to restore the texture when exiting play mode
            if (_colorGradientTexture)
            {
                RuntimeTextureUtilities.SetTextureFromGradient(_colorGradient,_colorGradientTexture);
            }
            
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if(_material == null || _colorGradientTexture == null)
            {
                return;
            }
            
            _material.SetTexture(ColorGradientTexture, _colorGradientTexture);
            _material.SetColor(ColorR, _colorR);
            _material.SetColor(ColorQ, _colorQ);
            _material.SetColor(FresnelColor, _fresnelColor);
            _material.SetFloat(Scale, _scale);
            _material.SetFloat(WarpAmount, _warpAmount);
            _material.SetFloat(FresnelPower, _fresnelPower);
            _material.SetFloat(FlowSpeed, _flowSpeed);
        }

        private void RandomizeColorSeed()
        {
            m_RandomizationProperties.ColorSeed = DateTime.Now.Millisecond;
        }

        private void GenerateColorWithCurrentSeed()
        {
            Random.InitState(m_RandomizationProperties.ColorSeed);

            _colorGradient = ColorUtilities.GenerateGradientSliceFromGradient(m_RandomizationProperties.RandomizationColorPalette, 0.15f);
            
            Color mainColor = _colorGradient.Evaluate(Random.Range(0f, 1f));
            _fresnelColor = mainColor;

            _colorR = ColorUtilities.RandomColorSaturationFromGradient(_colorGradient);
            
            _colorQ = ColorUtilities.RandomColorSaturationFromGradient(_colorGradient);

            if (_corona != null)
            {
                _corona.SetColor(mainColor);
            }

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

        private void RandomizeNoiseSeed()
        {
            m_RandomizationProperties.NoiseSeed = DateTime.Now.Millisecond;
        }

        private void UpdateNoisePropertiesWithCurrentSeed()
        {
            Random.InitState(m_RandomizationProperties.NoiseSeed);
            _scale = Random.Range(m_RandomizationProperties.PatternScaleRange.x, m_RandomizationProperties.PatternScaleRange.y);
            _warpAmount = Random.Range(m_RandomizationProperties.WarpAmountRange.x, m_RandomizationProperties.WarpAmountRange.y);
            _fresnelPower = Random.Range(m_RandomizationProperties.FresnelPowerRange.x, m_RandomizationProperties.FresnelPowerRange.y);
            _flowSpeed = Random.Range(m_RandomizationProperties.FlowSpeedRange.x, m_RandomizationProperties.FlowSpeedRange.y);
        }
        
        public void RandomizeNoise()
        {
            RandomizeNoiseSeed();
            UpdateNoisePropertiesWithCurrentSeed();
            UpdateMaterial();
        }
        
        public void GenerateNoise()
        {
            UpdateNoisePropertiesWithCurrentSeed();
            UpdateMaterial();
        }

        public void GenerateRandomStar()
        {            
            RandomizeColorSeed();
            GenerateColorWithCurrentSeed();

            RandomizeNoiseSeed();
            UpdateNoisePropertiesWithCurrentSeed();
            
            RuntimeTextureUtilities.SetTextureFromGradient(_colorGradient,_colorGradientTexture);
            
            UpdateMaterial();
        }
    }
}