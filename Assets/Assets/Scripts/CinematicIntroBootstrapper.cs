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

            // 4. Create and configure the MissionCoordinator
            GameObject coordGo = new GameObject("MissionCoordinator");
            MissionCoordinator coordinator = coordGo.AddComponent<MissionCoordinator>();
            coordinator.Dialogue1Text = "Welcome to mission agent, I am your mission coordinator.";
            coordinator.Dialogue2Text = "There should be a generator and electric switch, power on the Bunker!";
            coordinator.ObjectiveHUDText = "Locate the generator and power on the Bunker!";
            coordinator.HUDTypewriterSpeed = 0.035f;

            // 5. Create and configure the MissionCoordinatorHUD
            GameObject hudGo = new GameObject("MissionCoordinatorHUD");
            hudGo.AddComponent<MissionCoordinatorHUD>();

            // 6. Create and configure the BunkerPowerManager
            GameObject powerGo = new GameObject("BunkerPowerManager");
            BunkerPowerManager powerManager = powerGo.AddComponent<BunkerPowerManager>();
            powerManager.IsPowerOn = false; // Start in dark power-off state

            // 7. Locate Tz-ExteriorElectricBox2 and attach BunkerElectricBoxInteraction interaction script
            GameObject electricBox = GameObject.Find("Tz-ExteriorElectricBox2");
            if (electricBox != null)
            {
                if (electricBox.GetComponent<BunkerElectricBoxInteraction>() == null)
                {
                    electricBox.AddComponent<BunkerElectricBoxInteraction>();
                    Debug.Log("[CinematicIntroBootstrapper] Dynamic BunkerElectricBoxInteraction component attached to Tz-ExteriorElectricBox2.");
                }
            }
            else
            {
                Debug.LogWarning("[CinematicIntroBootstrapper] Tz-ExteriorElectricBox2 not found in scene on start. Proximity/click interaction bootstrap skipped.");
            }

            Debug.Log("[CinematicIntroBootstrapper] Dynamic intro, coordinator, HUD, and Power Manager initialized successfully for runtime play.");
        }
    }
}
