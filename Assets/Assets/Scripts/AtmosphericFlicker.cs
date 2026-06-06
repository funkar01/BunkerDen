using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BunkerTools
{
    [RequireComponent(typeof(Light))]
    public class AtmosphericFlicker : MonoBehaviour
    {
        public enum FlickerMode
        {
            Random,
            PerlinNoise,
            Sparks,
            Strobe,
            Breathing
        }

        [Header("Flicker Setup")]
        [Tooltip("The style of flickering to apply.")]
        public FlickerMode mode = FlickerMode.Random;
        
        [Tooltip("Minimum light intensity during flicker.")]
        public float minIntensity = 0.1f;
        
        [Tooltip("Maximum light intensity during flicker.")]
        public float maxIntensity = 1.5f;
        
        [Tooltip("Speed/frequency of the flickering effect.")]
        public float speed = 15.0f;

        [Header("Emissive Synchronization")]
        [Tooltip("The mesh renderer of the light bulb/fixture to sync emissive glow with.")]
        public Renderer targetRenderer;
        
        [Tooltip("The index of the material on the renderer that has the emissive map.")]
        public int materialIndex = 0;
        
        [Tooltip("Color of the emission when the light is fully on.")]
        [ColorUsage(true, true)]
        public Color emissiveColor = Color.white;

        [Header("HDRP Extras")]
        [Tooltip("Sync the light's volumetric multiplier in real-time?")]
        public bool syncVolumetric = true;

        private Light lightComponent;
        private HDAdditionalLightData hdLightData;
        private MaterialPropertyBlock propertyBlock;
        private float baseIntensity;
        private float noiseTime;
        private int emissiveColorId;

        private void Start()
        {
            lightComponent = GetComponent<Light>();
            hdLightData = GetComponent<HDAdditionalLightData>();
            propertyBlock = new MaterialPropertyBlock();
            
            // Cached property ID for performance
            emissiveColorId = Shader.PropertyToID("_EmissiveColor");
            
            baseIntensity = lightComponent.intensity;
            noiseTime = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            float targetValue = CalculateFlicker();

            // Set Light Intensity
            float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, targetValue);
            lightComponent.intensity = currentIntensity;

            // Sync volumetric shafts in HDRP
            if (syncVolumetric && hdLightData != null)
            {
                // Volumetric intensity scales with direct light intensity
                hdLightData.volumetricDimmer = targetValue;
            }

            // Sync Emissive Material using MaterialPropertyBlock (no memory leaks/draw call breaking)
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                
                // Scale emissive color intensity based on the current flicker value
                Color currentEmissive = emissiveColor * targetValue;
                propertyBlock.SetColor(emissiveColorId, currentEmissive);
                
                targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
            }
        }

        private float CalculateFlicker()
        {
            noiseTime += Time.deltaTime * speed;
            float output = 1f;

            switch (mode)
            {
                case FlickerMode.Random:
                    output = Random.value;
                    break;

                case FlickerMode.PerlinNoise:
                    output = Mathf.PerlinNoise(noiseTime, 0f);
                    break;

                case FlickerMode.Sparks:
                    // High probability of being fully ON, with sharp, brief drops
                    float noiseVal = Mathf.PerlinNoise(noiseTime, 0f);
                    if (noiseVal < 0.35f)
                    {
                        // Spark drop
                        output = Random.Range(0.05f, 0.4f);
                    }
                    else
                    {
                        output = Random.Range(0.85f, 1f);
                    }
                    break;

                case FlickerMode.Strobe:
                    output = (Time.time * speed) % 2.0f < 1.0f ? 1.0f : 0.0f;
                    break;

                case FlickerMode.Breathing:
                    // Smooth sin wave breathing
                    output = (Mathf.Sin(Time.time * speed) + 1.0f) * 0.5f;
                    break;
            }

            return output;
        }
    }
}
