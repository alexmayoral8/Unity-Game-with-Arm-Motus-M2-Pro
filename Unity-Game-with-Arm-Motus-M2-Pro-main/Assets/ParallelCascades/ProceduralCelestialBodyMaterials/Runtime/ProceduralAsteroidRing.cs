using System;
using ParallelCascades.Common.Runtime;
using ParallelCascades.ProceduralShaders.Editor;
using ParallelCascades.ProceduralShaders.Runtime.PropertyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Runtime
{
    public class ProceduralAsteroidRing : MonoBehaviour
    {
        private bool _asteroidRing;
        
        [SerializeField] private Material _material;
        [SerializeField] private Texture2D _colorGradientTexture;
        
        [Header("Shader Properties")] 
        [BindGradientToTexture("_colorGradientTexture")][SerializeField]
        private Gradient _colorGradient = new(){colorKeys =  new GradientColorKey[2]
        {
            new() {color = Color.white, time = 0},
            new() {color = Color.black, time = 1}
        }};
        
        [SerializeField] private float _innerRadius = 0.1f;
        [SerializeField] private float _outerRadius = 0.3f;
        [SerializeField] private float _fadeStrength = 2.23f;
        [SerializeField] private float _flowSpeed = 0.01f;
        
        [SerializeField] private RandomizationProperties m_RandomizationProperties;
        
        private static readonly int ColorGradientTexture = Shader.PropertyToID("_Color_Gradient_Texture");
        private static readonly int InnerRadius = Shader.PropertyToID("_Inner_Radius");
        private static readonly int OuterRadius = Shader.PropertyToID("_Outer_Radius");
        private static readonly int FadeStrength = Shader.PropertyToID("_Fade_Strength");
        private static readonly int FlowSpeed = Shader.PropertyToID("_Flow_Speed");

        [Serializable]
        private class RandomizationProperties
        {
            [InspectorButton("GenerateRandomAsteroidRing", "Randomize Asteroid Ring")]
            [InspectorButton("RandomizeColor", "Randomize Color")]
            [InspectorButton("GenerateColor", "Generate Color")]
            public int ColorSeed;
            public AnimationCurve TwoColorGradientChanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            [InspectorButton("RandomizeShape", "Randomize Shape")]
            [InspectorButton("UpdateShapePropertiesFromCurrentSeed", "Generate Shape")]
            public int ShapeSeed;
            [Tooltip("X - Inner Radius Min, Y - Inner Max/Outer Min, Z - Outer Max")]
            public Vector3 RadiusRange = new Vector3(0.125f, 0.3f, 0.5f);
            public Vector2 FadeStrengthRange = new Vector2(1f, 3f);
            public Vector2 FlowSpeedRange = new Vector2(0.005f, 0.02f);
        }
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
            _material.SetFloat(InnerRadius, _innerRadius);
            _material.SetFloat(OuterRadius, _outerRadius);
            _material.SetFloat(FadeStrength, _fadeStrength);
            _material.SetFloat(FlowSpeed, _flowSpeed);
        }
        
        private void RandomizeColorSeed()
        {
            m_RandomizationProperties.ColorSeed = System.DateTime.Now.Millisecond;
        }


        private void GenerateColorWithCurrentSeed()
        {
            Random.InitState(m_RandomizationProperties.ColorSeed);
            
            Color mainColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            
            // Gradient generation
            float twoColorGradientChance = m_RandomizationProperties.TwoColorGradientChanceCurve.Evaluate(Random.value);
            float randomValue = Random.value;
            if (randomValue < twoColorGradientChance)
            {
                Color colorB = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                _colorGradient = ColorUtilities.CreateRandomGradientFromTwoColors(mainColor, colorB, 8, 8, .5f, 2f); // always generate ring gradients with 8 steps - makes for better visuals
            }
            else
            {
                _colorGradient = ColorUtilities.CreateRandomGradientFromColor(mainColor, 8, 8, .5f, 2f); // always generate ring gradients with 8 steps - makes for better visuals
            }
        }

        private void RandomizeColor()
        {
            RandomizeColorSeed();
            GenerateColorWithCurrentSeed();
            
            RuntimeTextureUtilities.SetTextureFromGradient(_colorGradient, _colorGradientTexture);
            UpdateMaterial();
        }

        private void GenerateColor()
        {
            GenerateColorWithCurrentSeed();
            
            RuntimeTextureUtilities.SetTextureFromGradient(_colorGradient, _colorGradientTexture);
            UpdateMaterial();
        }
        
        private void  RandomizeShapeSeed()
        {
            m_RandomizationProperties.ShapeSeed = System.DateTime.Now.Millisecond;
        }
        
        private void UpdateShapePropertiesFromCurrentSeed()
        {
            Random.InitState(m_RandomizationProperties.ShapeSeed);
            
            _innerRadius = Random.Range(m_RandomizationProperties.RadiusRange.x, m_RandomizationProperties.RadiusRange.y);
            _outerRadius = Random.Range(m_RandomizationProperties.RadiusRange.y, m_RandomizationProperties.RadiusRange.z);
            _fadeStrength = Random.Range(m_RandomizationProperties.FadeStrengthRange.x, m_RandomizationProperties.FadeStrengthRange.y);
            _flowSpeed = Random.Range(m_RandomizationProperties.FlowSpeedRange.x, m_RandomizationProperties.FlowSpeedRange.y);
        }
        
        public void RandomizeShape()
        {
            RandomizeShapeSeed();
            UpdateShapePropertiesFromCurrentSeed();
            
            UpdateMaterial();
        }
        
        public void GenerateRandomAsteroidRing()
        {
            RandomizeColorSeed();
            GenerateColorWithCurrentSeed();
            
            RandomizeShapeSeed();
            UpdateShapePropertiesFromCurrentSeed();
            
            RuntimeTextureUtilities.SetTextureFromGradient(_colorGradient,_colorGradientTexture);
            
            UpdateMaterial();
        }


    }
}