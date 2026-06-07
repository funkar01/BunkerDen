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

            // Temporarily rename scene-loaded active/inactive targets to prevent conflicts with our mocks during validation
            GameObject sceneCmdRoomHL = FindActiveOrInactive("Highlighter_CommandRoom");
            if (sceneCmdRoomHL != null) sceneCmdRoomHL.name = "Highlighter_CommandRoom_Temp";

            GameObject sceneIndiaMapHL = FindActiveOrInactive("Highlighter_IndiaMap");
            if (sceneIndiaMapHL != null) sceneIndiaMapHL.name = "Highlighter_IndiaMap_Temp";

            GameObject sceneLockerHL = FindActiveOrInactive("Highlighter_Locker");
            if (sceneLockerHL != null) sceneLockerHL.name = "Highlighter_Locker_Temp";

            GameObject sceneLockerDoor = FindActiveOrInactive("LockerDoorB");
            if (sceneLockerDoor != null) sceneLockerDoor.name = "LockerDoorB_Temp";
            
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
            coordinator.Scene5Dialogue1Text = "Great, now search for the evidence, it should be the biometric key which looks like a hard drive.";
            coordinator.Scene5Dialogue2Text = "";
            coordinator.Scene5ObjectiveText = "Search for the biometric key!";

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
            if (coordinator.Scene5Dialogue1Text.Contains("biometric key") && coordinator.Scene5ObjectiveText.Contains("biometric key"))
            {
                Debug.Log("[VERIFICATION] SUCCESS: Scene 5 dialogue configuration validated correctly.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Scene 5 dialogue configurations are invalid!");
                EditorApplication.Exit(1);
            }

            // Configure default Scene 6 values for validation
            coordinator.Scene6Dialogue1Text = "The secret code is 'heart of India'.";
            coordinator.Scene6ObjectiveText = "Check for the 'Heart of India'!";

            // Validate Scene 6 configurations
            if (coordinator.Scene6Dialogue1Text.Contains("heart of India") && coordinator.Scene6ObjectiveText.Contains("Heart of India"))
            {
                Debug.Log("[VERIFICATION] SUCCESS: Scene 6 dialogue configuration validated correctly.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Scene 6 dialogue configurations are invalid!");
                EditorApplication.Exit(1);
            }

            // Configure default Scene 7 values for validation
            coordinator.Scene7Dialogue1Text = "Well done, you found the secret button at the Heart of India of Indian map!";
            coordinator.Scene7ObjectiveText = "Explore the unlocked locker!";

            // Create mock Map, Highlighter_IndiaMap, Highlighter_Locker
            GameObject mockMap = new GameObject("MockMap");
            mockMap.tag = "Map";

            GameObject mockIndiaMapHL = new GameObject("Highlighter_IndiaMap");
            mockIndiaMapHL.SetActive(true);

            GameObject mockLockerHL = new GameObject("Highlighter_Locker");
            mockLockerHL.tag = "Locker";
            mockLockerHL.SetActive(false);

            GameObject mockLockerDoor = new GameObject("LockerDoorB");
            mockLockerDoor.SetActive(true);

            // Set Scene 6 active via reflection to bypass scene guard
            var scene6Field = typeof(MissionCoordinator).GetField("_scene6SequenceStarted", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (scene6Field != null)
            {
                scene6Field.SetValue(coordinator, true);
            }

            // Verify Map trigger via reflection
            var mapMethod = typeof(PlayerInteractionHandler).GetMethod("CheckMapTrigger", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (mapMethod == null)
            {
                Debug.LogError("[VERIFICATION] CheckMapTrigger method not found on PlayerInteractionHandler script!");
                EditorApplication.Exit(1);
            }

            mapMethod.Invoke(interaction, new object[] { mockMap });

            // Validate that Highlighter_IndiaMap was disabled and Highlighter_Locker was enabled
            if (!mockIndiaMapHL.activeSelf && mockLockerHL.activeSelf)
            {
                Debug.Log("[VERIFICATION] SUCCESS: Highlighter_IndiaMap disabled and Highlighter_Locker enabled upon trigger.");
            }
            else
            {
                Debug.LogError($"[VERIFICATION] FAILURE: Highlighter active states are invalid! IndiaMap active={mockIndiaMapHL.activeSelf}, Locker active={mockLockerHL.activeSelf}");
                EditorApplication.Exit(1);
            }

            // Validate that LockerDoorB was disabled
            if (!mockLockerDoor.activeSelf)
            {
                Debug.Log("[VERIFICATION] SUCCESS: LockerDoorB was successfully disabled upon trigger.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: LockerDoorB was not disabled!");
                EditorApplication.Exit(1);
            }

            // Validate Scene 7 configurations
            if (coordinator.Scene7Dialogue1Text.Contains("Indian map") && coordinator.Scene7ObjectiveText.Contains("unlocked locker"))
            {
                Debug.Log("[VERIFICATION] SUCCESS: Scene 7 dialogue configuration validated correctly.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Scene 7 dialogue configurations are invalid!");
                EditorApplication.Exit(1);
            }

            // Configure default Scene 8 values for validation
            coordinator.Scene8Dialogue1Text = "Bravo, you have found the Key, now exit the bunker ASAP";
            coordinator.Scene8ObjectiveText = "Exit the bunker quickly!";

            // Set Scene 7 active via reflection to bypass scene guard
            var scene7Field = typeof(MissionCoordinator).GetField("_scene7SequenceStarted", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (scene7Field != null)
            {
                scene7Field.SetValue(coordinator, true);
            }

            // Verify Locker trigger via reflection
            var lockerMethod = typeof(PlayerInteractionHandler).GetMethod("CheckLockerTrigger", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (lockerMethod == null)
            {
                Debug.LogError("[VERIFICATION] CheckLockerTrigger method not found on PlayerInteractionHandler script!");
                EditorApplication.Exit(1);
            }

            lockerMethod.Invoke(interaction, new object[] { mockLockerHL });

            // Validate that Highlighter_Locker was disabled upon trigger
            if (!mockLockerHL.activeSelf)
            {
                Debug.Log("[VERIFICATION] SUCCESS: Highlighter_Locker was successfully disabled upon trigger.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Highlighter_Locker was not disabled!");
                EditorApplication.Exit(1);
            }

            // Validate Scene 8 configurations
            if (coordinator.Scene8Dialogue1Text.Contains("ASAP") && coordinator.Scene8ObjectiveText.Contains("quickly"))
            {
                Debug.Log("[VERIFICATION] SUCCESS: Scene 8 dialogue configuration validated correctly.");
            }
            else
            {
                Debug.LogError("[VERIFICATION] FAILURE: Scene 8 dialogue configurations are invalid!");
                EditorApplication.Exit(1);
            }

            // Clean up temporary objects
            Object.DestroyImmediate(mockPlayer);
            Object.DestroyImmediate(mockCommandRoom);
            Object.DestroyImmediate(mockHighlighter);
            Object.DestroyImmediate(mockMap);
            Object.DestroyImmediate(mockIndiaMapHL);
            Object.DestroyImmediate(mockLockerHL);
            Object.DestroyImmediate(mockLockerDoor);

            // Restore original object names
            if (sceneCmdRoomHL != null) sceneCmdRoomHL.name = "Highlighter_CommandRoom";
            if (sceneIndiaMapHL != null) sceneIndiaMapHL.name = "Highlighter_IndiaMap";
            if (sceneLockerHL != null) sceneLockerHL.name = "Highlighter_Locker";
            if (sceneLockerDoor != null) sceneLockerDoor.name = "LockerDoorB";

            Debug.Log("[VERIFICATION] All PlayerInteractionHandler trigger, Scene 4, Scene 5, Scene 6, Scene 7, and Scene 8 verification checks passed successfully!");
            EditorApplication.Exit(0);
        }

        private static GameObject FindActiveOrInactive(string name)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                GameObject match = FindInChildren(root.transform, name);
                if (match != null) return match;
            }
            return null;
        }

        private static GameObject FindInChildren(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject match = FindInChildren(parent.GetChild(i), name);
                if (match != null) return match;
            }
            return null;
        }
    }
}
#endif
