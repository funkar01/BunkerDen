using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BunkerTools
{
    /// <summary>
    /// SetupIntroUISystem is an Editor utility that places the ScreenFadeManager and MissionIntroUI
    /// scripts in the currently active scene, setting up the cinematic experience automatically.
    /// </summary>
    public class SetupIntroUISystem
    {
        [MenuItem("Bunker Tools/Setup Cinematic Intro & Fade System")]
        public static void Run()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorUtility.DisplayDialog("Error", "Please save or open a scene before running this setup.", "OK");
                return;
            }

            Debug.Log($"[BunkerTools] Setting up Cinematic Intro & Fade System in scene: {activeScene.name}");

            // 1. Create or Find ScreenFadeManager
            ScreenFadeManager fadeManager = Object.FindAnyObjectByType<ScreenFadeManager>();
            GameObject fadeManagerGo;
            if (fadeManager == null)
            {
                fadeManagerGo = new GameObject("ScreenFadeManager");
                fadeManager = fadeManagerGo.AddComponent<ScreenFadeManager>();
                Undo.RegisterCreatedObjectUndo(fadeManagerGo, "Create ScreenFadeManager");
                Debug.Log("[BunkerTools] Created new ScreenFadeManager in scene.");
            }
            else
            {
                fadeManagerGo = fadeManager.gameObject;
                Debug.Log("[BunkerTools] ScreenFadeManager already exists in scene.");
            }

            // Ensure StartAlpha is 1 (black screen on start)
            fadeManager.StartAlpha = 1f;
            EditorUtility.SetDirty(fadeManager);

            // 2. Create or Find MissionIntroUI
            MissionIntroUI introUI = Object.FindAnyObjectByType<MissionIntroUI>();
            GameObject introUIGo;
            if (introUI == null)
            {
                introUIGo = new GameObject("MissionIntroUI");
                introUI = introUIGo.AddComponent<MissionIntroUI>();
                Undo.RegisterCreatedObjectUndo(introUIGo, "Create MissionIntroUI");
                Debug.Log("[BunkerTools] Created new MissionIntroUI in scene.");
            }
            else
            {
                introUIGo = introUI.gameObject;
                Debug.Log("[BunkerTools] MissionIntroUI already exists in scene.");
            }

            // Setup default configuration
            introUI.HeaderTitle = "TACTICAL BRIEFING";
            introUI.SubHeader = "SECURITY LEVEL 5 // EYES ONLY";
            introUI.BriefingText = "You are a secret agent serving for the nation, your mission is to explore the old abandoned bunker and retrieve the confidential data which is hidden inside.";
            introUI.TypewriterSpeed = 0.045f;
            EditorUtility.SetDirty(introUI);

            // 3. Create or Find MissionCoordinator
            MissionCoordinator coordinator = Object.FindAnyObjectByType<MissionCoordinator>();
            GameObject coordinatorGo;
            if (coordinator == null)
            {
                coordinatorGo = new GameObject("MissionCoordinator");
                coordinator = coordinatorGo.AddComponent<MissionCoordinator>();
                Undo.RegisterCreatedObjectUndo(coordinatorGo, "Create MissionCoordinator");
                Debug.Log("[BunkerTools] Created new MissionCoordinator in scene.");
            }
            else
            {
                coordinatorGo = coordinator.gameObject;
                Debug.Log("[BunkerTools] MissionCoordinator already exists in scene.");
            }

            coordinator.Dialogue1Text = "Welcome to mission agent, I am your mission coordinator.";
            coordinator.Dialogue2Text = "There should be a generator and electric switch, power on the Bunker!";
            coordinator.ObjectiveHUDText = "Locate the electric switch and power on the Bunker!";
            coordinator.HUDTypewriterSpeed = 0.035f;
            EditorUtility.SetDirty(coordinator);

            // 4. Create or Find MissionCoordinatorHUD
            MissionCoordinatorHUD coordinatorHUD = Object.FindAnyObjectByType<MissionCoordinatorHUD>();
            GameObject coordinatorHUDGo;
            if (coordinatorHUD == null)
            {
                coordinatorHUDGo = new GameObject("MissionCoordinatorHUD");
                coordinatorHUD = coordinatorHUDGo.AddComponent<MissionCoordinatorHUD>();
                Undo.RegisterCreatedObjectUndo(coordinatorHUDGo, "Create MissionCoordinatorHUD");
                Debug.Log("[BunkerTools] Created new MissionCoordinatorHUD in scene.");
            }
            else
            {
                coordinatorHUDGo = coordinatorHUD.gameObject;
                Debug.Log("[BunkerTools] MissionCoordinatorHUD already exists in scene.");
            }
            EditorUtility.SetDirty(coordinatorHUD);

            // 5. Create or Find BunkerPowerManager
            BunkerPowerManager powerManager = Object.FindAnyObjectByType<BunkerPowerManager>();
            GameObject powerManagerGo;
            if (powerManager == null)
            {
                powerManagerGo = new GameObject("BunkerPowerManager");
                powerManager = powerManagerGo.AddComponent<BunkerPowerManager>();
                Undo.RegisterCreatedObjectUndo(powerManagerGo, "Create BunkerPowerManager");
                Debug.Log("[BunkerTools] Created new BunkerPowerManager in scene.");
            }
            else
            {
                powerManagerGo = powerManager.gameObject;
                Debug.Log("[BunkerTools] BunkerPowerManager already exists in scene.");
            }
            powerManager.IsPowerOn = false; // Start in dark power-off state
            powerManager.TorchRange = 25f;
            powerManager.TorchAngle = 30f;
            powerManager.TorchIntensity = 2975.964f;
            powerManager.TorchColor = new Color(0.98f, 0.97f, 0.90f, 1f);
            EditorUtility.SetDirty(powerManager);

            // 6. Mark scene dirty to ensure changes are saved
            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);
            
            if (saved)
            {
                EditorUtility.DisplayDialog("Success", 
                    "Cinematic Intro, Fade, Coordinator & Power Manager System configured successfully!\n\n" +
                    "- 'ScreenFadeManager' handles transitions.\n" +
                    "- 'MissionIntroUI' manages briefing overlays.\n" +
                    "- 'MissionCoordinator' plays dialogues & procedural radio static.\n" +
                    "- 'MissionCoordinatorHUD' displays active transmission & objective status.\n" +
                    "- 'BunkerPowerManager' controls scene dark-mode, flashlight & objective blinking.", 
                    "Awesome");
                Debug.Log("[BunkerTools] Cinematic Experience System configuration saved successfully.");
            }
            else
            {
                Debug.LogWarning("[BunkerTools] Scene setup succeeded but saving the scene was cancelled or failed.");
            }
        }
    }
}
