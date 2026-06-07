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
        private bool _commandRoomTriggered = false;
        private bool _mapTriggered = false;
        private AudioClip _voiceChirpSFX;

        private void Start()
        {
            _voiceChirpSFX = CreateVoiceChirpClip();
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckAndRestorePower(other.gameObject);
            CheckCommandRoomTrigger(other.gameObject);
            CheckMapTrigger(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckAndRestorePower(collision.gameObject);
            CheckCommandRoomTrigger(collision.gameObject);
            CheckMapTrigger(collision.gameObject);
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

        private void CheckCommandRoomTrigger(GameObject targetGo)
        {
            if (_commandRoomTriggered) return;

            // Detect collision/trigger with game object having tag "CommandRoom"
            if (targetGo.CompareTag("CommandRoom"))
            {
                _commandRoomTriggered = true;
                Debug.Log($"[PlayerInteractionHandler] Collision/Trigger with tagged 'CommandRoom' object '{targetGo.name}' detected.");

                // Disable the "Highlighter_CommandRoom" object
                GameObject commandRoomHL = GameObject.Find("Highlighter_CommandRoom");
                if (commandRoomHL != null)
                {
                    commandRoomHL.SetActive(false);
                    Debug.Log("[PlayerInteractionHandler] Highlighter_CommandRoom disabled.");
                }
                else
                {
                    Debug.LogWarning("[PlayerInteractionHandler] Highlighter_CommandRoom not found in scene.");
                }

                // Initiate the Scene 4 to Scene 5 transition
                if (MissionCoordinator.Instance != null)
                {
                    MissionCoordinator.Instance.StartScene5DialogueSequence();
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

        private void CheckMapTrigger(GameObject targetGo)
        {
            if (_mapTriggered) return;

            // Only allow map trigger during Scene 6
            if (MissionCoordinator.Instance != null && !MissionCoordinator.Instance.IsScene6Active)
            {
                return;
            }

            // Detect collision/trigger with game object having tag "Map"
            if (targetGo.CompareTag("Map"))
            {
                _mapTriggered = true;
                Debug.Log($"[PlayerInteractionHandler] Collision/Trigger with tagged 'Map' object '{targetGo.name}' detected.");

                // Disable "Highlighter_IndiaMap"
                GameObject mapHL = FindGameObjectIncludingInactive("Highlighter_IndiaMap");
                if (mapHL != null)
                {
                    mapHL.SetActive(false);
                    Debug.Log("[PlayerInteractionHandler] Highlighter_IndiaMap deactivated.");
                }
                else
                {
                    Debug.LogWarning("[PlayerInteractionHandler] Highlighter_IndiaMap not found in scene.");
                }

                // Enable "Highlighter_Locker"
                GameObject lockerHL = FindGameObjectIncludingInactive("Highlighter_Locker");
                if (lockerHL != null)
                {
                    lockerHL.SetActive(true);
                    Debug.Log("[PlayerInteractionHandler] Highlighter_Locker activated.");
                }
                else
                {
                    Debug.LogWarning("[PlayerInteractionHandler] Highlighter_Locker not found in scene.");
                }

                // Disable "LockerDoorB"
                GameObject lockerDoor = FindGameObjectIncludingInactive("LockerDoorB");
                if (lockerDoor != null)
                {
                    lockerDoor.SetActive(false);
                    Debug.Log("[PlayerInteractionHandler] LockerDoorB deactivated.");
                }
                else
                {
                    Debug.LogWarning("[PlayerInteractionHandler] LockerDoorB not found in scene.");
                }

                // Initiate Scene 7 dialogues
                if (MissionCoordinator.Instance != null)
                {
                    MissionCoordinator.Instance.StartScene7DialogueSequence();
                }
            }
        }

        private GameObject FindGameObjectIncludingInactive(string name)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return null;

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                GameObject match = FindInChildrenRecursive(root.transform, name);
                if (match != null) return match;
            }
            return null;
        }

        private GameObject FindInChildrenRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject match = FindInChildrenRecursive(parent.GetChild(i), name);
                if (match != null) return match;
            }
            return null;
        }
    }
}
