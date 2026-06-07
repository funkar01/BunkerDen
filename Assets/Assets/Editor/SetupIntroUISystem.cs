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

            // 3. Mark scene dirty to ensure changes are saved
            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);
            
            if (saved)
            {
                EditorUtility.DisplayDialog("Success", 
                    "Cinematic Intro & Fade System configured successfully!\n\n" +
                    "- 'ScreenFadeManager' handles smooth transitions.\n" +
                    "- 'MissionIntroUI' manages briefing overlays.\n\n" +
                    "Both components are dynamic and self-constructing at runtime.", 
                    "Awesome");
                Debug.Log("[BunkerTools] Cinematic Intro & Fade System configuration saved successfully.");
            }
            else
            {
                Debug.LogWarning("[BunkerTools] Scene setup succeeded but saving the scene was cancelled or failed.");
            }
        }
    }
}
