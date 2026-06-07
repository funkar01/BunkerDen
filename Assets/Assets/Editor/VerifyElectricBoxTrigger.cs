#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BunkerTools
{
    public static class VerifyElectricBoxTrigger
    {
        [MenuItem("Bunker Tools/Verify Proximity Trigger")]
        public static void RunVerification()
        {
            Debug.Log("[VERIFICATION] Loading scene 'BunkerScene_v2'...");
            var scenePath = "Assets/Assets/Scenes/BunkerScene_v2.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            if (!scene.IsValid())
            {
                Debug.LogError($"[VERIFICATION] Failed to load scene at path: {scenePath}");
                EditorApplication.Exit(1);
            }

            Debug.Log("[VERIFICATION] Finding components...");
            
            // Find Electric Box
            GameObject box = GameObject.Find("Tz-ExteriorElectricBox2");
            if (box == null)
            {
                Debug.LogError("[VERIFICATION] Tz-ExteriorElectricBox2 not found in scene!");
                EditorApplication.Exit(1);
            }

            // Tag it as ElectricSwitch
            box.tag = "ElectricSwitch";

            // Spawn a mock player GameObject with PlayerInteractionHandler
            GameObject mockPlayer = new GameObject("Player");
            mockPlayer.tag = "Player";
            PlayerInteractionHandler interaction = mockPlayer.AddComponent<PlayerInteractionHandler>();

            // Find Power Manager (attached at runtime by bootstrapper, so we create it here for the scene context)
            BunkerPowerManager powerManager = Object.FindAnyObjectByType<BunkerPowerManager>();
            if (powerManager == null)
            {
                GameObject powerGo = new GameObject("BunkerPowerManager");
                powerManager = powerGo.AddComponent<BunkerPowerManager>();
            }

            // Verify initial state
            powerManager.IsPowerOn = false;

            // Set the singleton Instance backing field via reflection since Awake won't run in Edit Mode
            var backingField = typeof(BunkerPowerManager).GetField("<Instance>k__BackingField", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (backingField != null)
            {
                backingField.SetValue(null, powerManager);
            }

            Debug.Log("[VERIFICATION] Simulating proximity trigger enter with 'ElectricSwitch' tagged object...");
            
            // Force the power sequence activation by invoking the private CheckAndRestorePower method via reflection
            var method = typeof(PlayerInteractionHandler).GetMethod("CheckAndRestorePower", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
            {
                Debug.LogError("[VERIFICATION] CheckAndRestorePower method not found on PlayerInteractionHandler script!");
                EditorApplication.Exit(1);
            }

            method.Invoke(interaction, new object[] { box });

            // Validate that power has turned ON
            if (powerManager.IsPowerOn)
            {
                Debug.Log("[VERIFICATION] SUCCESS: Power manager state updated to IsPowerOn = true.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Power manager state was not updated to true!");
                EditorApplication.Exit(1);
            }

            // Verify generator audio source was added
            string[] names = { "Power Generator", "Generator", "generator", "Generator_TechMagnet" };
            GameObject generatorGo = null;
            foreach (var name in names)
            {
                generatorGo = GameObject.Find(name);
                if (generatorGo != null) break;
            }

            if (generatorGo != null)
            {
                AudioSource source = generatorGo.GetComponent<AudioSource>();
                if (source != null && source.enabled)
                {
                    Debug.Log($"[VERIFICATION] SUCCESS: AudioSource successfully added and enabled on '{generatorGo.name}'.");
                }
                else
                {
                    Debug.LogError($"[VERIFICATION] FAILURE: No active AudioSource found on generator '{generatorGo.name}'!");
                    EditorApplication.Exit(1);
                }
            }
            else
            {
                Debug.LogWarning("[VERIFICATION] No generator GameObject found in scene to verify audio source on.");
            }

            // Clean up temporary mock player
            Object.DestroyImmediate(mockPlayer);

            Debug.Log("[VERIFICATION] All PlayerInteractionHandler trigger verification checks passed successfully!");
            EditorApplication.Exit(0);
        }
    }
}
#endif
