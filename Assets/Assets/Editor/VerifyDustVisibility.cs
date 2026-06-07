using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace BunkerTools
{
    [InitializeOnLoad]
    public class VerifyDustVisibility
    {
        private const string StateKey = "VerifyDust_Waiting";
        private static string screenshotPath = @"C:\Users\bhanu\.gemini\antigravity\brain\e1ee5d7e-6286-47ff-8fa7-2271cd6f3d17\dust_verification.png";

        static VerifyDustVisibility()
        {
            EditorApplication.update += EditorUpdate;
        }

        [MenuItem("Bunker Tools/Verify Dust Visibility")]
        public static void Run()
        {
            string scenePath = "Assets/Assets/Scenes/BunkerScene_v2.unity";
            Debug.Log($"Opening scene: {scenePath}");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Debug.Log("Setting state and starting Play Mode...");
            SessionState.SetBool(StateKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void EditorUpdate()
        {
            if (!SessionState.GetBool(StateKey, false)) return;

            if (EditorApplication.isPlaying)
            {
                // We are playing! Wait 4 seconds for particles to spawn
                if (Time.timeSinceLevelLoad > 4.0f)
                {
                    SessionState.SetBool(StateKey, false);
                    CaptureAndExit();
                }
            }
        }

        private static void CaptureAndExit()
        {
            Debug.Log($"Capturing screenshot to: {screenshotPath}");
            
            // Ensure the directory exists
            string dir = Path.GetDirectoryName(screenshotPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            ScreenCapture.CaptureScreenshot(screenshotPath);
            Debug.Log("Screenshot command sent.");

            double exitTime = EditorApplication.timeSinceStartup + 3.0; // Wait 3 seconds for screenshot to write
            EditorApplication.CallbackFunction exitWait = null;
            exitWait = () =>
            {
                if (EditorApplication.timeSinceStartup > exitTime)
                {
                    EditorApplication.update -= exitWait;
                    Debug.Log("Stopping Play Mode...");
                    EditorApplication.isPlaying = false;
                    
                    // Exit Unity editor
                    Debug.Log("Exiting Unity...");
                    EditorApplication.Exit(0);
                }
            };
            EditorApplication.update += exitWait;
        }
    }
}
