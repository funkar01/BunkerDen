using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// BunkerPowerManager handles the electrical power states of the bunker.
    /// It darkens all environmental lights and flickering buzzing noises on start,
    /// equips the player with a flashlight, and blinks a guide light on the target electric box.
    /// </summary>
    public class BunkerPowerManager : MonoBehaviour
    {
        public static BunkerPowerManager Instance { get; private set; }

        [Header("Power Configuration")]
        [Tooltip("Is the main power currently on?")]
        public bool IsPowerOn = false;

        [Header("Generator Settings")]
        [Tooltip("The audio clip played when the generator turns on.")]
        public AudioClip GeneratorStartClip;
        
        [Tooltip("Delay in seconds before lights and flickering sounds turn on after the generator starts.")]
        public float LightsActivationDelay = 2.0f;

        [Header("Flashlight Configuration")]
        public float TorchRange = 25f; // Set to 25f from screenshot
        public float TorchAngle = 30f; // Outer Spot Angle set to 30f from screenshot
        [Tooltip("Physical light intensity. Set to 2975.964f lumens from screenshot.")]
        public float TorchIntensity = 2975.964f; 
        public Color TorchColor = new Color(0.98f, 0.97f, 0.90f, 1f); // Warm/Off-White color from screenshot

        [Header("Highlight Configuration")]
        public string TargetBoxName = "Tz-ExteriorElectricBox2";
        public Color HighlightColor = new Color(1f, 0.2f, 0.2f); // Blinking red indicator
        public float HighlightRange = 12f; // Expanded to 12m for a massive projecting highlight
        [Tooltip("Physical light intensity. For HDRP, typical values are between 150f and 800f.")]
        public float HighlightIntensity = 600f; // Increased brightness to illuminate a larger area

        private struct LightState
        {
            public Light LightComponent;
            public bool OriginalEnabled;
            public float OriginalIntensity;
        }

        private List<LightState> _darkenedLights = new List<LightState>();
        private List<AtmosphericFlicker> _disabledFlickers = new List<AtmosphericFlicker>();
        private List<AudioSource> _disabledAudioSources = new List<AudioSource>();
        private GameObject _playerTorchGo;
        private BunkerOutlineHighlighter _boxHighlighter;
        private GameObject _staticHighlighterGo;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Force exact flashlight values to override stale serialized values in the scene
            TorchRange = 25f;
            TorchAngle = 30f;
            TorchIntensity = 2975.964f;
            TorchColor = new Color(0.98f, 0.97f, 0.90f, 1f);

            if (!IsPowerOn)
            {
                ApplyPowerOffMode();
            }
        }

        /// <summary>
        /// Darkens the bunker, equips the player flashlight, and highlights the target switch.
        /// </summary>
        public void ApplyPowerOffMode()
        {
            IsPowerOn = false;
            Debug.Log("[BunkerPowerManager] Powering down all lights and buzzing noises. Entering darkness.");

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            }

            Transform camTransform = mainCam != null ? mainCam.transform : null;

            // 1. Find and disable all standard lights (excluding player/torch lights)
            Light[] allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in allLights)
            {
                if (camTransform != null && l.transform.IsChildOf(camTransform)) continue;
                if (l.gameObject.name.Contains("Player") || l.gameObject.name.Contains("Torch") || l.gameObject.name.Contains("Flashlight") || l.gameObject.name.Contains("Highlight")) continue;

                _darkenedLights.Add(new LightState
                {
                    LightComponent = l,
                    OriginalEnabled = l.enabled,
                    OriginalIntensity = l.intensity
                });

                l.enabled = false;
            }

            // 2. Find and disable all AtmosphericFlicker components
            AtmosphericFlicker[] flickers = UnityEngine.Object.FindObjectsByType<AtmosphericFlicker>(FindObjectsSortMode.None);
            foreach (AtmosphericFlicker f in flickers)
            {
                f.enabled = false;
                _disabledFlickers.Add(f);

                // Stop active AudioSources buzzing
                AudioSource source = f.GetComponent<AudioSource>();
                if (source != null)
                {
                    source.Stop();
                    source.enabled = false;
                    _disabledAudioSources.Add(source);
                }

                // Turn off material emission intensity (so lightbulbs look physically turned off)
                if (f.targetRenderer != null)
                {
                    MaterialPropertyBlock pb = new MaterialPropertyBlock();
                    f.targetRenderer.GetPropertyBlock(pb, f.materialIndex);
                    pb.SetColor(Shader.PropertyToID("_EmissiveColor"), Color.black);
                    f.targetRenderer.SetPropertyBlock(pb, f.materialIndex);
                }
            }

            // 3. Equip the player with a flashlight (attached to camera)
            SetupPlayerTorch(camTransform);

            // 4. Locate and highlight the target electric box
            GameObject box = GameObject.Find(TargetBoxName);
            if (box != null)
            {
                SetupElectricBoxHighlight(box);
            }
            else
            {
                Debug.LogWarning($"[BunkerPowerManager] Target electric box '{TargetBoxName}' not found in scene on start.");
                // Retry in 1.5 seconds in case it was loaded asynchronously
                StartCoroutine(DeferredHighlightSearchRoutine());
            }
        }

        /// <summary>
        /// Restores electrical power, enabling environmental lights and buzzing.
        /// </summary>
        public void RestorePower()
        {
            if (IsPowerOn) return;
            IsPowerOn = true;
            Debug.Log("[BunkerPowerManager] Restoring power. Initializing grid.");

            // 1. Remove or disable electric box outline highlight
            if (_boxHighlighter != null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(_boxHighlighter);
                }
                else
                #endif
                {
                    Destroy(_boxHighlighter);
                }
                _boxHighlighter = null;
            }
            if (_staticHighlighterGo != null)
            {
                _staticHighlighterGo.SetActive(false);
            }

            // 2. Switch off the torch light immediately
            if (_playerTorchGo != null)
            {
                _playerTorchGo.SetActive(false);
                Debug.Log("[BunkerPowerManager] Player torch turned off.");
            }

            // 3. Play the generator startup sound immediately
            ActivateGeneratorAudio();

            // 4. Delay the activation of lights and flickering/buzzing sounds
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In edit-mode verification tests, execute synchronously to prevent test hangs
                ActivateLightsAndFlickers();
            }
            else
            #endif
            {
                StartCoroutine(DelayedLightsActivationRoutine());
            }
        }

        private IEnumerator DelayedLightsActivationRoutine()
        {
            yield return new WaitForSeconds(LightsActivationDelay);
            ActivateLightsAndFlickers();
        }

        private void ActivateLightsAndFlickers()
        {
            // Restore all environmental lights
            foreach (var state in _darkenedLights)
            {
                if (state.LightComponent != null)
                {
                    state.LightComponent.enabled = state.OriginalEnabled;
                    state.LightComponent.intensity = state.OriginalIntensity;
                }
            }
            _darkenedLights.Clear();

            // Restore all flickers & buzzing sounds
            foreach (var f in _disabledFlickers)
            {
                if (f != null) f.enabled = true;
            }
            _disabledFlickers.Clear();

            foreach (var s in _disabledAudioSources)
            {
                if (s != null)
                {
                    s.enabled = true;
                    s.Play();
                }
            }
            _disabledAudioSources.Clear();

            Debug.Log("[BunkerPowerManager] Grid activation sequence complete. Environmental lights and flickers turned ON.");

            // Trigger Scene 4 dialogue sequence warning after a few seconds gap
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (MissionCoordinator.Instance != null)
                {
                    MissionCoordinator.Instance.StartScene4DialogueSequence();
                }
            }
            else
            #endif
            {
                StartCoroutine(TriggerScene4AfterDelayRoutine());
            }
        }

        private IEnumerator TriggerScene4AfterDelayRoutine()
        {
            // Wait 4.0 seconds after grid power-up before starting coordinator warning
            yield return new WaitForSeconds(4.0f);
            if (MissionCoordinator.Instance != null)
            {
                MissionCoordinator.Instance.StartScene4DialogueSequence();
            }
        }

        private void SetupPlayerTorch(Transform parentCam)
        {
            if (parentCam == null) return;

            Light torchLight = null;

            // Search if there is already a Light in the camera's children (including sub-children)
            Light[] childLights = parentCam.GetComponentsInChildren<Light>(true);
            foreach (Light l in childLights)
            {
                if (l.gameObject.name.Contains("Highlight") || l.gameObject.name.Contains("Blink")) continue;
                torchLight = l;
                _playerTorchGo = l.gameObject;
                break;
            }

            if (torchLight == null)
            {
                // Create dynamically if not found
                _playerTorchGo = new GameObject("PlayerTorchLight");
                _playerTorchGo.transform.SetParent(parentCam);
                // Z is set to 0.55f (55cm forward) to bypass player capsule/mesh culling and prevent self-shadow blocking
                _playerTorchGo.transform.localPosition = new Vector3(0.28f, -0.25f, 0.55f); 
                _playerTorchGo.transform.localRotation = Quaternion.identity;

                torchLight = _playerTorchGo.AddComponent<Light>();
                torchLight.type = LightType.Spot;
            }
            else
            {
                // Ensure it and its parents are active in hierarchy
                torchLight.gameObject.SetActive(true);
                Transform p = torchLight.transform.parent;
                while (p != null && p != parentCam)
                {
                    p.gameObject.SetActive(true);
                    p = p.parent;
                }
            }

            // Apply the exact settings as shown in the screenshot
            torchLight.type = LightType.Spot;
            torchLight.spotAngle = TorchAngle; // 30f (Outer Spot Angle)
            torchLight.innerSpotAngle = 21.8f; // From screenshot
            torchLight.range = TorchRange; // 25f
            torchLight.intensity = TorchIntensity; // 2975.964f
            torchLight.color = TorchColor;
            torchLight.bounceIntensity = 1f; // Indirect Multiplier set to 1
            torchLight.shadows = LightShadows.None; // Set shadows to None to guarantee no mesh blockages

            // Add or configure HDRP data structure
            var hdLightType = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalLightData, Unity.RenderPipelines.HighDefinition.Runtime");
            if (hdLightType != null)
            {
                Component hdLight = torchLight.GetComponent(hdLightType);
                if (hdLight == null)
                {
                    hdLight = torchLight.gameObject.AddComponent(hdLightType);
                }
                
                // Configure physical properties to match screenshot precisely
                ApplyHDLightSettings(hdLight, TorchIntensity, TorchRange, TorchAngle, 21.8f, 0.025f, 0f);
            }

            Debug.Log($"[BunkerPowerManager] Flashlight configured from screenshot settings on '{torchLight.gameObject.name}'.");
        }

        private void SetupElectricBoxHighlight(GameObject box)
        {
            // First check if there is a static Highlighter object in the scene hierarchy
            Transform staticHL = box.transform.Find("Highlighter");
            if (staticHL != null)
            {
                _staticHighlighterGo = staticHL.gameObject;
                _staticHighlighterGo.SetActive(true);
                Debug.Log($"[BunkerPowerManager] Activated static highlighter in scene hierarchy on '{box.name}'.");
                return;
            }

            if (_boxHighlighter != null) return;

            // Fallback: Attach the boundary/outline highlighter to draw glowing corner brackets around the box
            _boxHighlighter = box.AddComponent<BunkerOutlineHighlighter>();
            _boxHighlighter.OutlineColor = new Color(0f, 1f, 0.95f, 0.85f); // Glowing cyan outline
            _boxHighlighter.BracketLength = 0.15f;
            _boxHighlighter.LineWidth = 0.025f;
            _boxHighlighter.Blink = true;
            _boxHighlighter.BlinkSpeed = 4.5f;

            Debug.Log($"[BunkerPowerManager] Attached outline boundary highlighter to '{box.name}'.");
        }

        private void ApplyHDLightSettings(Component hdLight, float intensity, float range, float outerAngle, float innerAngle, float radius, float temperature)
        {
            Type hdLightType = hdLight.GetType();

            // 1. Set light unit to Lumen first to prevent auto-conversion scaling when intensity is set
            var lightUnitType = Type.GetType("UnityEngine.Rendering.HighDefinition.LightUnit, Unity.RenderPipelines.HighDefinition.Runtime");
            if (lightUnitType != null)
            {
                var lightUnitProp = hdLightType.GetProperty("lightUnit");
                if (lightUnitProp != null)
                {
                    try
                    {
                        object lumenValue = Enum.Parse(lightUnitType, "Lumen");
                        lightUnitProp.SetValue(hdLight, lumenValue);
                    }
                    catch (Exception)
                    {
                        // Fallback silently if Enum.Parse fails on specific configurations
                    }
                }
            }

            // 2. Set intensity (now guaranteed to be set in Lumens)
            var intensityProp = hdLightType.GetProperty("intensity");
            if (intensityProp != null)
            {
                intensityProp.SetValue(hdLight, intensity);
            }

            // 3. Set inner spot angle
            var innerAngleProp = hdLightType.GetProperty("innerSpotAngle");
            if (innerAngleProp != null)
            {
                innerAngleProp.SetValue(hdLight, innerAngle);
            }

            // 4. Set shape radius
            var radiusProp = hdLightType.GetProperty("shapeRadius") ?? hdLightType.GetProperty("radius");
            if (radiusProp != null)
            {
                radiusProp.SetValue(hdLight, radius);
            }

            // 5. Configure color temperature
            var useColorTempProp = hdLightType.GetProperty("useColorTemperature");
            if (useColorTempProp != null)
            {
                useColorTempProp.SetValue(hdLight, temperature > 100f);
            }
            if (temperature > 100f)
            {
                var colorTempProp = hdLightType.GetProperty("colorTemperature");
                if (colorTempProp != null)
                {
                    colorTempProp.SetValue(hdLight, temperature);
                }
            }
        }



        private IEnumerator DeferredHighlightSearchRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            GameObject box = GameObject.Find(TargetBoxName);
            if (box != null)
            {
                SetupElectricBoxHighlight(box);
            }
        }

        private void ActivateGeneratorAudio()
        {
            string[] names = { "Power Generator", "Generator", "generator", "Generator_TechMagnet" };
            GameObject generatorGo = null;
            foreach (var name in names)
            {
                generatorGo = GameObject.Find(name);
                if (generatorGo != null) break;
            }

            if (generatorGo == null)
            {
                Debug.LogWarning("[BunkerPowerManager] No generator GameObject found to play audio on.");
                return;
            }

            // Ensure generator object and its parents are active in the hierarchy
            generatorGo.SetActive(true);
            Transform p = generatorGo.transform.parent;
            while (p != null)
            {
                p.gameObject.SetActive(true);
                p = p.parent;
            }

            // Get or add AudioSource on the generator
            AudioSource source = generatorGo.GetComponent<AudioSource>();
            if (source == null)
            {
                source = generatorGo.AddComponent<AudioSource>();
            }

            source.enabled = true;
            source.spatialBlend = 1.0f; // 3D sound
            source.minDistance = 2.0f;
            source.maxDistance = 25.0f;
            source.volume = 0.85f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.loop = false; // Do not loop continuously, we fade away instead

            // Try to load the specified generator audio clip if not already assigned
            #if UNITY_EDITOR
            if (GeneratorStartClip == null)
            {
                GeneratorStartClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/Audios/freesound_community-generator-synthetic-63166.mp3");
            }
            #endif

            // Play the clip if loaded, otherwise fallback to synthesized startup sound
            if (GeneratorStartClip != null)
            {
                source.clip = GeneratorStartClip;
                source.loop = false;
                source.Play();
                Debug.Log($"[BunkerPowerManager] Playing generator audio asset '{GeneratorStartClip.name}' on '{generatorGo.name}'.");
                StartCoroutine(GeneratorFadeOutRoutine(source, GeneratorStartClip.length));
            }
            else
            {
                source.loop = false;
                
                // Synthesize the startup and looping hum audio clips
                AudioClip startupClip = CreateGeneratorStartupClip();
                AudioClip humClip = CreateGeneratorHumClip();

                source.clip = startupClip;
                source.Play();

                Debug.Log($"[BunkerPowerManager] Generator audio asset not found. Playing synthesized engine startup sound on '{generatorGo.name}'.");

                // Transition to the hum clip and fade it out over 8.0 seconds
                StartCoroutine(GeneratorSynthesizedTransitionAndFadeRoutine(source, humClip, startupClip.length, 8.0f));
            }
        }

        private IEnumerator GeneratorFadeOutRoutine(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (source == null) yield break;
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            if (source != null)
            {
                source.Stop();
                source.volume = startVolume;
            }
        }

        private IEnumerator GeneratorSynthesizedTransitionAndFadeRoutine(AudioSource source, AudioClip humClip, float startupDuration, float fadeDuration)
        {
            yield return new WaitForSeconds(startupDuration - 0.05f); // Soft overlap
            if (source != null)
            {
                source.clip = humClip;
                source.loop = true; // Loop during the active fade-out window
                source.Play();

                float startVolume = source.volume;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    if (source == null) yield break;
                    elapsed += Time.deltaTime;
                    source.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                    yield return null;
                }
                if (source != null)
                {
                    source.Stop();
                    source.loop = false;
                    source.volume = startVolume;
                    Debug.Log("[BunkerPowerManager] Synthesized generator audio faded out to silence.");
                }
            }
        }

        private AudioClip CreateGeneratorStartupClip()
        {
            int sampleRate = 44100;
            float duration = 2.2f;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];
            System.Random rand = new System.Random();

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;

                // 1. Starter motor cranking pulses (0.0s to 1.2s)
                float crank = 0f;
                if (t < 1.2f)
                {
                    // Slow cranking pulses at 8Hz
                    float pulse = Mathf.PingPong(t * 8f, 1f);
                    float crankFreq = 30f + 15f * pulse;
                    crank = Mathf.Sin(2f * Mathf.PI * crankFreq * t) * (0.3f + 0.7f * pulse) * 0.4f;
                    crank += ((float)rand.NextDouble() * 2f - 1f) * 0.08f * pulse;
                }

                // 2. Firing up transition (1.0s to 1.6s)
                float fire = 0f;
                if (t >= 1.0f && t < 1.6f)
                {
                    float fade = (t - 1.0f) / 0.6f;
                    float engineFiredFreq = Mathf.Lerp(45f, 60f, fade);
                    // Exhaust sputter & pops
                    float pop = Mathf.Sin(2f * Mathf.PI * engineFiredFreq * t) * 0.5f;
                    pop += ((float)rand.NextDouble() * 2f - 1f) * 0.25f;
                    fire = pop * Mathf.Sin(Mathf.PI * fade) * 0.6f;
                }

                // 3. Settling into idle hum (1.4s to 2.2s)
                float idle = 0f;
                if (t >= 1.4f)
                {
                    float fade = Mathf.Clamp01((t - 1.4f) / 0.8f);
                    // 60Hz fundamental + harmonics
                    float wave = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.5f +
                                 Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.3f +
                                 Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.15f;
                    float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.05f;
                    idle = (wave + noise) * fade * 0.4f;
                }

                sampleArray[i] = (crank + fire + idle) * 0.6f;
            }

            AudioClip clip = AudioClip.Create("GeneratorStartup", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }

        private AudioClip CreateGeneratorHumClip()
        {
            int sampleRate = 44100;
            float duration = 2.0f; // 2.0s loop
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];
            System.Random rand = new System.Random();

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;

                // 60Hz fundamental (AC power grid hum) + harmonics
                float wave = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.2f +
                             Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.1f;

                // Low combustion vibration noise
                float rumble = ((float)rand.NextDouble() * 2f - 1f) * 0.08f;

                sampleArray[i] = (wave + rumble) * 0.35f;
            }

            AudioClip clip = AudioClip.Create("GeneratorHumLoop", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }
    }
}
