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
            coordinator.Scene4Dialogue1Text = "Mind the poisonous gases trapped inside, you need to hurry and find the evidence before your breath runs out";
            coordinator.Scene4Dialogue2Text = "look for the main command room";
            coordinator.Scene4ObjectiveText = "Look for the main command room";
            coordinator.Scene5Dialogue1Text = "Great, now search for the evidence, it should be the biometric key which looks like a hard drive.";
            coordinator.Scene5Dialogue2Text = "";
            coordinator.Scene5ObjectiveText = "Search the main command arena";
            coordinator.HUDTypewriterSpeed = 0.035f;

            // 5. Create and configure the MissionCoordinatorHUD
            GameObject hudGo = new GameObject("MissionCoordinatorHUD");
            hudGo.AddComponent<MissionCoordinatorHUD>();

            // 6. Create and configure the BunkerPowerManager
            GameObject powerGo = new GameObject("BunkerPowerManager");
            BunkerPowerManager powerManager = powerGo.AddComponent<BunkerPowerManager>();
            powerManager.IsPowerOn = false; // Start in dark power-off state

            // 7. Locate Tz-ExteriorElectricBox2, tag it as "ElectricSwitch", and configure its trigger collider
            GameObject electricBox = GameObject.Find("Tz-ExteriorElectricBox2");
            if (electricBox != null)
            {
                electricBox.tag = "ElectricSwitch";
                
                BoxCollider col = electricBox.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = electricBox.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;

                Rigidbody rb = electricBox.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = electricBox.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true;
                rb.useGravity = false;

                Debug.Log("[CinematicIntroBootstrapper] Tz-ExteriorElectricBox2 tagged as 'ElectricSwitch' and configured as trigger.");
            }
            else
            {
                Debug.LogWarning("[CinematicIntroBootstrapper] Tz-ExteriorElectricBox2 not found on start.");
            }

            // 8. Attach PlayerInteractionHandler to the player character dynamically
            AttachPlayerInteractionHandler();

            Debug.Log("[CinematicIntroBootstrapper] Dynamic intro, coordinator, HUD, and Power Manager initialized successfully for runtime play.");
        }

        private static void AttachPlayerInteractionHandler()
        {
            // First Person Player
            var fpc = Object.FindAnyObjectByType<StarterAssets.FirstPersonController>();
            if (fpc != null)
            {
                if (fpc.GetComponent<PlayerInteractionHandler>() == null)
                {
                    fpc.gameObject.AddComponent<PlayerInteractionHandler>();
                    Debug.Log($"[CinematicIntroBootstrapper] Attached PlayerInteractionHandler to FirstPersonPlayer: {fpc.name}");
                }
                return;
            }

            // Third Person Player
            var tpc = Object.FindAnyObjectByType<StarterAssets.ThirdPersonController>();
            if (tpc != null)
            {
                if (tpc.GetComponent<PlayerInteractionHandler>() == null)
                {
                    tpc.gameObject.AddComponent<PlayerInteractionHandler>();
                    Debug.Log($"[CinematicIntroBootstrapper] Attached PlayerInteractionHandler to ThirdPersonPlayer: {tpc.name}");
                }
                return;
            }

            Debug.LogWarning("[CinematicIntroBootstrapper] No player controller found on start. PlayerInteractionHandler could not be attached.");
        }
    }
}
