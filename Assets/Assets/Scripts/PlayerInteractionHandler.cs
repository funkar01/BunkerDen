using System;
using System.Collections;
using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// PlayerInteractionHandler handles collision and trigger events on the player.
    /// It detects hits on objects tagged "ElectricSwitch" and restores the bunker's electrical systems
    /// while printing the confirmation message to the HUD.
    /// </summary>
    public class PlayerInteractionHandler : MonoBehaviour
    {
        private bool _triggered = false;
        private AudioClip _voiceChirpSFX;

        private void Start()
        {
            _voiceChirpSFX = CreateVoiceChirpClip();
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckAndRestorePower(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckAndRestorePower(collision.gameObject);
        }

        private void CheckAndRestorePower(GameObject targetGo)
        {
            if (_triggered) return;

            // Detect collision/trigger with game object having tag "ElectricSwitch"
            if (targetGo.CompareTag("ElectricSwitch"))
            {
                _triggered = true;
                Debug.Log($"[PlayerInteractionHandler] Collision/Trigger with tagged 'ElectricSwitch' object '{targetGo.name}' detected.");

                // 1. Play heavy mechanical/industrial click switch sound at the switch position
                PlaySwitchClickSFX(targetGo);

                // 2. Restore all lights, flickers, and sound effects via the Power Manager
                if (BunkerPowerManager.Instance != null)
                {
                    BunkerPowerManager.Instance.RestorePower();
                }

                // 3. Display completion dialogue message on the HUD
                if (MissionCoordinatorHUD.Instance != null)
                {
                    MissionCoordinatorHUD.Instance.ShowTransmission(
                        "COORDINATOR", 
                        "Well done, power is switched ON!", 
                        0.035f, 
                        _voiceChirpSFX
                    );
                    StartCoroutine(CompleteObjectiveRoutine());
                }
            }
        }

        private IEnumerator CompleteObjectiveRoutine()
        {
            // Wait for dialogue printing to finish (approx 1.5s)
            yield return new WaitForSeconds(1.5f);
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "OBJECTIVE COMPLETED", 
                    "Well done, power is switched ON!"
                );
                MissionCoordinatorHUD.Instance.FadeOutHUD(3.0f);
            }
        }

        private void PlaySwitchClickSFX(GameObject targetGo)
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];
            System.Random rand = new System.Random();

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                // Mechanical clang switch sound
                float click = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-35f * t) * 0.6f;
                float metallicClang = Mathf.Sin(2f * Mathf.PI * 450f * t) * Mathf.Exp(-80f * t) * 0.3f;
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.15f * Mathf.Exp(-120f * t);
                sampleArray[i] = (click + metallicClang + noise) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SwitchClick", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);

            AudioSource source = targetGo.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1.0f; // 3D sound
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
