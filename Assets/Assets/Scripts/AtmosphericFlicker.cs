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

        [Header("Audio Setup")]
        [Tooltip("Audio clip for the electricity flickering/buzzing sound.")]
        public AudioClip flickerSound;
        [Tooltip("Base volume of the flicker sound.")]
        [Range(0f, 1f)]
        public float baseVolume = 0.5f;
        [Tooltip("Maximum distance at which the sound can be heard (3D spatial audio).")]
        public float maxAudioDistance = 15.0f;
        [Tooltip("Minimum distance for spatial roll-off (3D spatial audio).")]
        public float minAudioDistance = 1.0f;
        [Tooltip("Sync the sound volume and pitch with light intensity fluctuations?")]
        public bool syncVolumeWithFlicker = true;

        private Light lightComponent;
        private HDAdditionalLightData hdLightData;
        private MaterialPropertyBlock propertyBlock;
        private float baseIntensity;
        private float noiseTime;
        private int emissiveColorId;

        private AudioSource audioSource;
        private Transform cameraTransform;

        private void Start()
        {
            lightComponent = GetComponent<Light>();
            hdLightData = GetComponent<HDAdditionalLightData>();
            propertyBlock = new MaterialPropertyBlock();
            
            // Cached property ID for performance
            emissiveColorId = Shader.PropertyToID("_EmissiveColor");
            
            baseIntensity = lightComponent.intensity;
            noiseTime = Random.Range(0f, 1000f);

            // Find main camera transform for distance culling
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            // Set up AudioSource if clip is assigned
            if (flickerSound != null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                
                audioSource.clip = flickerSound;
                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 100% 3D spatial audio
                audioSource.minDistance = minAudioDistance;
                audioSource.maxDistance = maxAudioDistance;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.volume = 0f; // Start silent, will be updated in Update
            }
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

            // Handle proximity-based culling for audio
            if (audioSource != null)
            {
                // Dynamically fetch camera if it was lost or not loaded on start
                if (cameraTransform == null && Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }

                if (cameraTransform != null)
                {
                    float distance = Vector3.Distance(transform.position, cameraTransform.position);
                    bool withinRange = distance <= maxAudioDistance;

                    if (withinRange)
                    {
                        // Enable AudioSource if it was disabled
                        if (!audioSource.enabled)
                        {
                            audioSource.enabled = true;
                        }

                        if (!audioSource.isPlaying)
                        {
                            audioSource.Play();
                            Debug.Log($"[AtmosphericFlicker] Electricity sound started for {gameObject.name}");
                        }

                        // Modulate volume and pitch based on flicker value (voltage fluctuation simulation)
                        float volumeTarget = baseVolume;
                        if (syncVolumeWithFlicker)
                        {
                            // Modulate volume target slightly based on intensity drops to sound more organic
                            volumeTarget = baseVolume * (0.3f + 0.7f * targetValue);
                            audioSource.pitch = 0.9f + 0.2f * targetValue;
                        }
                        else
                        {
                            audioSource.pitch = 1.0f;
                        }

                        audioSource.volume = volumeTarget;
                    }
                    else
                    {
                        // Pause/Disable AudioSource to save performance when far away
                        if (audioSource.isPlaying)
                        {
                            audioSource.Pause();
                            Debug.Log($"[AtmosphericFlicker] Electricity sound paused (culled) for {gameObject.name}");
                        }
                        if (audioSource.enabled)
                        {
                            audioSource.enabled = false;
                        }
                    }
                }
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
