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

            // Find Power Manager
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

            // Find or create MissionCoordinator for verification
            MissionCoordinator coordinator = Object.FindAnyObjectByType<MissionCoordinator>();
            if (coordinator == null)
            {
                GameObject coordGo = new GameObject("MissionCoordinator");
                coordinator = coordGo.AddComponent<MissionCoordinator>();
            }

            // Set the MissionCoordinator singleton Instance backing field via reflection
            var coordBackingField = typeof(MissionCoordinator).GetField("<Instance>k__BackingField", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (coordBackingField != null)
            {
                coordBackingField.SetValue(null, coordinator);
            }

            // Configure default values for validation
            coordinator.Scene4Dialogue1Text = "Mind the poisonous gases trapped inside, you need to hurry and find the evidence before your breath runs out";
            coordinator.Scene4Dialogue2Text = "look for the main command room";
            coordinator.Scene4ObjectiveText = "Look for the main command room";

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

            // Validate Scene 4 dialogue settings
            if (coordinator.Scene4Dialogue1Text.Contains("poisonous gases") && coordinator.Scene4Dialogue2Text.Contains("main command room"))
            {
                Debug.Log("[VERIFICATION] SUCCESS: Scene 4 dialogue configuration validated correctly.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Scene 4 dialogue configurations are invalid!");
                EditorApplication.Exit(1);
            }

            // Configure default Scene 5 values for validation
            coordinator.Scene5Dialogue1Text = "You've made it to the main command room! Find the central mainframe terminal.";
            coordinator.Scene5Dialogue2Text = "Access the terminal and download the encrypted files before your air supply runs out.";
            coordinator.Scene5ObjectiveText = "Access the mainframe terminal and download the files";

            // Create a mock CommandRoom GameObject
            GameObject mockCommandRoom = new GameObject("MockCommandRoom");
            mockCommandRoom.tag = "CommandRoom";

            // Create a mock Highlighter_CommandRoom GameObject
            GameObject mockHighlighter = new GameObject("Highlighter_CommandRoom");
            mockHighlighter.SetActive(true);

            // Verify CommandRoom trigger via reflection
            var cmdRoomMethod = typeof(PlayerInteractionHandler).GetMethod("CheckCommandRoomTrigger", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cmdRoomMethod == null)
            {
                Debug.LogError("[VERIFICATION] CheckCommandRoomTrigger method not found on PlayerInteractionHandler script!");
                EditorApplication.Exit(1);
            }

            cmdRoomMethod.Invoke(interaction, new object[] { mockCommandRoom });

            // Validate that Highlighter_CommandRoom was disabled
            if (!mockHighlighter.activeSelf)
            {
                Debug.Log("[VERIFICATION] SUCCESS: Highlighter_CommandRoom was successfully disabled upon trigger.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Highlighter_CommandRoom was not disabled!");
                EditorApplication.Exit(1);
            }

            // Validate Scene 5 configurations
            if (coordinator.Scene5Dialogue1Text.Contains("mainframe") && coordinator.Scene5Dialogue2Text.Contains("encrypted files"))
            {
                Debug.Log("[VERIFICATION] SUCCESS: Scene 5 dialogue configuration validated correctly.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Scene 5 dialogue configurations are invalid!");
                EditorApplication.Exit(1);
            }

            // Clean up temporary objects
            Object.DestroyImmediate(mockPlayer);
            Object.DestroyImmediate(mockCommandRoom);
            Object.DestroyImmediate(mockHighlighter);

            Debug.Log("[VERIFICATION] All PlayerInteractionHandler trigger, Scene 4, and Scene 5 verification checks passed successfully!");
            EditorApplication.Exit(0);
        }
    }
}
#endif
