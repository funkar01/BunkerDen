using System;
using System.Collections;
using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// BunkerElectricBoxInteraction is attached to Tz-ExteriorElectricBox2.
    /// It detects the player entering its trigger collider and automatically initiates
    /// the generator, lighting restoration, and displays HUD feedback without clicking.
    /// </summary>
    public class BunkerElectricBoxInteraction : MonoBehaviour
    {
        private bool _triggered = false;
        private AudioClip _voiceChirpSFX;

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

            // Ensure the existing BoxCollider is configured as a trigger
            BoxCollider col = gameObject.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Procedurally synthesize a short radio/voice chirp clip for HUD dialogue audio feedback
            _voiceChirpSFX = CreateVoiceChirpClip();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only trigger once when the power is off
            if (!_triggered && BunkerPowerManager.Instance != null && !BunkerPowerManager.Instance.IsPowerOn)
            {
                // Verify if the entering collider is the player
                if (other.CompareTag("Player") || 
                    other.GetComponentInParent<StarterAssets.FirstPersonController>() != null || 
                    other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null)
                {
                    RestorePowerSequence();
                }
            }
        }

        private void RestorePowerSequence()
        {
            _triggered = true;
            Debug.Log("[BunkerElectricBoxInteraction] Player triggered electric box collider. Restoring power.");

            // 1. Play heavy mechanical/industrial click switch sound
            PlaySwitchClickSFX();

            // 2. Trigger the power restoration via the Power Manager
            if (BunkerPowerManager.Instance != null)
            {
                BunkerPowerManager.Instance.RestorePower();
            }

            // 3. Update HUD to transmission dialogue and then transition to completion state
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "COORDINATOR", 
                    "Well done, the power is on!", 
                    0.035f, 
                    _voiceChirpSFX
                );
                StartCoroutine(CompleteObjectiveRoutine());
            }

            // 4. Disable this component to prevent duplicate runs
            enabled = false;
        }

        private IEnumerator CompleteObjectiveRoutine()
        {
            // Wait for dialogue typing to complete (approx 1.2 seconds)
            yield return new WaitForSeconds(1.5f);
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "OBJECTIVE COMPLETED", 
                    "Well done, the power is on!"
                );
                MissionCoordinatorHUD.Instance.FadeOutHUD(3.0f);
            }
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
                // Heavy mechanical switch sound
                float click = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-35f * t) * 0.6f;
                float metallicClang = Mathf.Sin(2f * Mathf.PI * 450f * t) * Mathf.Exp(-80f * t) * 0.3f;
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.15f * Mathf.Exp(-120f * t);
                sampleArray[i] = (click + metallicClang + noise) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SwitchClick", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1.0f; // 3D
            source.minDistance = 1.0f;
            source.maxDistance = 12.0f;
            source.volume = 0.8f;
            source.Play();

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(source);
            }
            else
            #endif
            {
                Destroy(source, duration + 0.1f);
            }
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
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                float wave = Mathf.Sin(2f * Mathf.PI * 280f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 840f * t) * 0.4f;
                sampleArray[i] = wave * env * 0.12f;
            }

            AudioClip clip = AudioClip.Create("VoiceChirp", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }
    }
}
