using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// CinematicIntroBootstrapper automatically initializes the Cinematic Intro & Fade System
    /// at runtime when the game starts, ensuring it works out-of-the-box in any scene.
    /// </summary>
    public static class CinematicIntroBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            // 1. Prevent duplicate initialization if the components already exist in the scene
            MissionIntroUI existingIntro = Object.FindAnyObjectByType<MissionIntroUI>();
            if (existingIntro != null)
            {
                Debug.Log("[CinematicIntroBootstrapper] MissionIntroUI already exists in the scene. Skipping dynamic bootstrapper.");
                return;
            }

            // 2. Locate or create the ScreenFadeManager
            ScreenFadeManager existingFade = Object.FindAnyObjectByType<ScreenFadeManager>();
            if (existingFade == null)
            {
                GameObject fadeGo = new GameObject("ScreenFadeManager");
                existingFade = fadeGo.AddComponent<ScreenFadeManager>();
                existingFade.StartAlpha = 1f; // Start blacked out
                Debug.Log("[CinematicIntroBootstrapper] ScreenFadeManager not found. Initialized dynamically.");
            }

            // 3. Create and configure the MissionIntroUI
            GameObject introGo = new GameObject("MissionIntroUI");
            MissionIntroUI introUI = introGo.AddComponent<MissionIntroUI>();
            
            // Set default cinematic properties
            introUI.HeaderTitle = "TACTICAL BRIEFING";
            introUI.SubHeader = "SECURITY LEVEL 5 // EYES ONLY";
            introUI.BriefingText = "You are a secret agent serving for the nation, your mission is to explore the old abandoned bunker and retrieve the confidential data which is hidden inside.";
            introUI.TypewriterSpeed = 0.04f;

            Debug.Log("[CinematicIntroBootstrapper] MissionIntroUI dynamically created and configured for runtime play.");
        }
    }
}
