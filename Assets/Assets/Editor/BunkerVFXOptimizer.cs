using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.IO;

namespace BunkerTools
{
    public class BunkerVFXOptimizer : EditorWindow
    {
        private GameObject playerObject;
        private string playerTag = "Player";

        [MenuItem("Bunker Tools/VFX Optimizer")]
        public static void ShowWindow()
        {
            GetWindow<BunkerVFXOptimizer>("VFX Optimizer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Bunker VFX Dust Optimizer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            playerObject = (GameObject)EditorGUILayout.ObjectField("Player GameObject", playerObject, typeof(GameObject), true);
            playerTag = EditorGUILayout.TextField("Player Tag", playerTag);

            EditorGUILayout.Space();
            GUILayout.Label("Choose Optimization Method:", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Setup Proximity Trigger Culling (Room-by-Room)"))
            {
                SetupTriggerCulling();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Setup Player-Relative Dust Halo (Single Emitter)"))
            {
                SetupPlayerHalo();
            }
        }

        private void SetupTriggerCulling()
        {
            VisualEffect[] vfxSystems = FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int optimizedCount = 0;

            foreach (var vfx in vfxSystems)
            {
                // We target dust particle systems
                if (vfx.name.ToLower().Contains("dust"))
                {
                    GameObject go = vfx.gameObject;
                    
                    // Add trigger script if missing
                    CinematicDustTrigger trigger = go.GetComponent<CinematicDustTrigger>();
                    if (trigger == null)
                    {
                        trigger = go.AddComponent<CinematicDustTrigger>();
                    }

                    Undo.RecordObject(trigger, "Setup Proximity Trigger");
                    trigger.mode = CinematicDustTrigger.CullingMode.TriggerVolumeCheck;
                    trigger.vfxComponent = vfx;
                    trigger.playerTag = playerTag;

                    // Ensure there's a trigger collider on the GameObject
                    BoxCollider col = go.GetComponent<BoxCollider>();
                    if (col == null)
                    {
                        col = go.AddComponent<BoxCollider>();
                    }

                    Undo.RecordObject(col, "Setup Collider Trigger");
                    col.isTrigger = true;
                    // Default bounds for room size check (e.g. 15x15x15 meters)
                    col.size = new Vector3(15f, 6f, 15f);

                    // Re-activate GameObject so trigger functions can fire
                    Undo.RecordObject(go, "Activate VFX Object");
                    go.SetActive(true);

                    optimizedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("VFX Optimizer", $"Successfully attached Proximity Trigger Culling and Trigger Colliders to {optimizedCount} dust systems in the scene!", "OK");
        }

        private void SetupPlayerHalo()
        {
            // 1. Find main camera or player
            Transform cameraTransform = null;
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else if (playerObject != null)
            {
                cameraTransform = playerObject.transform;
            }

            if (cameraTransform == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find Main Camera or Player GameObject. Please assign it in the window.", "OK");
                return;
            }

            // 2. Find the FloatingDust VFX asset in the project
            string[] guids = AssetDatabase.FindAssets("FloatingDust t:VisualEffectAsset");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Could not find 'FloatingDust' VFX Graph asset in the project. Please make sure the asset exists.", "OK");
                return;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            VisualEffectAsset dustAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);

            if (dustAsset == null)
            {
                EditorUtility.DisplayDialog("Error", $"Could not load VisualEffectAsset at path: {assetPath}", "OK");
                return;
            }

            // 3. Disable existing static dust emitters in the scene to avoid duplicates
            VisualEffect[] existingVfx = FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int disabledCount = 0;
            foreach (var vfx in existingVfx)
            {
                if (vfx.name.ToLower().Contains("dust") && !vfx.name.Contains("PlayerDustHalo"))
                {
                    Undo.RecordObject(vfx.gameObject, "Disable Static Dust Emitter");
                    vfx.gameObject.SetActive(false);
                    disabledCount++;
                }
            }

            // 4. Create the PlayerDustHalo GameObject
            GameObject haloGo = GameObject.Find("PlayerDustHalo");
            if (haloGo == null)
            {
                haloGo = new GameObject("PlayerDustHalo");
            }

            Undo.RegisterCreatedObjectUndo(haloGo, "Create Player Dust Halo");
            haloGo.transform.SetParent(cameraTransform);
            haloGo.transform.localPosition = new Vector3(0, 0, 5); // offset slightly in front of camera
            haloGo.transform.localRotation = Quaternion.identity;
            
            VisualEffect vfxComp = haloGo.GetComponent<VisualEffect>();
            if (vfxComp == null)
            {
                vfxComp = haloGo.AddComponent<VisualEffect>();
            }
            
            Undo.RecordObject(vfxComp, "Assign VFX Asset");
            vfxComp.visualEffectAsset = dustAsset;

            // 5. Add CinematicDustTrigger script
            CinematicDustTrigger trigger = haloGo.GetComponent<CinematicDustTrigger>();
            if (trigger == null)
            {
                trigger = haloGo.AddComponent<CinematicDustTrigger>();
            }

            Undo.RecordObject(trigger, "Configure Dust Halo Trigger");
            trigger.mode = CinematicDustTrigger.CullingMode.PlayerRelativeHalo;
            trigger.vfxComponent = vfxComp;
            trigger.playerCamera = cameraTransform;
            trigger.positionOffset = new Vector3(0, 0, 5);

            EditorUtility.DisplayDialog("VFX Optimizer", $"Successfully set up Player-Relative Dust Halo on Camera!\nDisabled {disabledCount} static emitters to prevent duplication.", "OK");
        }
    }
}
