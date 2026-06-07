using System;
using System.Collections;
using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// BunkerElectricBoxInteraction is attached to the target electric box.
    /// It dynamically sets up a proximity trigger collider to transition from Scene 2 to Scene 3,
    /// instructs the player via HUD subtitles, and listens for click raycasts to restore power.
    /// </summary>
    public class BunkerElectricBoxInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Max reach distance in meters for the click interaction.")]
        public float MaxReachDistance = 5.0f;

        private bool _scene3Initiated = false;
        private bool _generatorOn = false;
        private AudioClip _voiceChirpSFX;
        private BoxCollider _proximityTrigger;

        private void Start()
        {
            // Add a Rigidbody and set it to kinematic to guarantee trigger events fire reliably
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Dynamically add a larger trigger BoxCollider around the electric box for proximity detection
            _proximityTrigger = gameObject.AddComponent<BoxCollider>();
            _proximityTrigger.isTrigger = true;

            // Enclose the bounds with a larger margin (roughly 6x4x4m centered on the electric box offset)
            _proximityTrigger.center = new Vector3(-0.1336786f, -0.0132344f, 0.6158089f);
            _proximityTrigger.size = new Vector3(6.0f, 4.0f, 4.0f);

            // Procedurally synthesize a short radio/voice chirp clip for the HUD objective change
            _voiceChirpSFX = CreateVoiceChirpClip();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only trigger if power is currently OFF and Scene 3 is not yet initiated
            if (BunkerPowerManager.Instance != null && !BunkerPowerManager.Instance.IsPowerOn && !_scene3Initiated)
            {
                // Verify if the entering collider is the player
                if (other.CompareTag("Player") || 
                    other.GetComponentInParent<StarterAssets.FirstPersonController>() != null || 
                    other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null)
                {
                    InitiateScene3();
                }
            }
        }

        private void InitiateScene3()
        {
            _scene3Initiated = true;
            Debug.Log("[BunkerElectricBoxInteraction] Proximity trigger activated. Initiating Scene 3.");

            // Instruct the player via the HUD typewriter dialogue sequence
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "COORDINATOR", 
                    "Click on the Electric box to switch on the Generator.", 
                    0.035f, 
                    _voiceChirpSFX
                );
            }
        }

        private void Update()
        {
            // Listen for click input during Scene 3
            if (_scene3Initiated && !_generatorOn)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    AttemptInteraction();
                }
            }
        }

        private void AttemptInteraction()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            }

            if (mainCam == null) return;

            // Cast ray from center of screen (if cursor is locked) or mouse position
            Ray ray;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
            else
            {
                ray = mainCam.ScreenPointToRay(Input.mousePosition);
            }

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, MaxReachDistance))
            {
                // Verify if the ray hit the electric box or any of its child objects
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    RestorePowerSequence();
                }
            }
        }

        private void RestorePowerSequence()
        {
            _generatorOn = true;
            Debug.Log("[BunkerElectricBoxInteraction] Click interaction detected. Restoring power.");

            // 1. Play heavy mechanical/industrial click switch sound
            PlaySwitchClickSFX();

            // 2. Trigger the power restoration via the Power Manager
            if (BunkerPowerManager.Instance != null)
            {
                BunkerPowerManager.Instance.RestorePower();
            }

            // 3. Update HUD to completion state and fade it out
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "OBJECTIVE COMPLETED", 
                    "Bunker power restored successfully."
                );
                MissionCoordinatorHUD.Instance.FadeOutHUD(3.0f);
            }

            // 4. Disable trigger and this script to prevent duplicate runs
            if (_proximityTrigger != null)
            {
                _proximityTrigger.enabled = false;
            }
            enabled = false;
        }

        private void PlaySwitchClickSFX()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];
            System.Random rand = new System.Random();

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                // Heavy mechanical switch sound: low metal impact + metallic spring clang + high frequency click noise
                float click = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-35f * t) * 0.6f;
                float metallicClang = Mathf.Sin(2f * Mathf.PI * 450f * t) * Mathf.Exp(-80f * t) * 0.3f;
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.15f * Mathf.Exp(-120f * t);
                sampleArray[i] = (click + metallicClang + noise) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SwitchClick", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1.0f; // Full 3D
            source.minDistance = 1.0f;
            source.maxDistance = 12.0f;
            source.volume = 0.8f;
            source.Play();

            Destroy(source, duration + 0.1f);
        }

        private AudioClip CreateVoiceChirpClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration)); // Smooth envelope
                float wave = Mathf.Sin(2f * Mathf.PI * 280f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 840f * t) * 0.4f;
                sampleArray[i] = wave * env * 0.12f;
            }

            AudioClip clip = AudioClip.Create("VoiceChirp", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }
    }
}
