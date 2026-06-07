using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BunkerTools
{
    /// <summary>
    /// SetupElectricBoxHighlighter is an Editor utility that places a static highlighter object
    /// in the scene hierarchy around 'Tz-ExteriorElectricBox2'.
    /// </summary>
    public class SetupElectricBoxHighlighter
    {
        [MenuItem("Bunker Tools/Setup Electric Box Highlighter")]
        public static void Run()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorUtility.DisplayDialog("Error", "Please save or open a scene before running this setup.", "OK");
                return;
            }

            SetupHighlighterInScene();

            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);
            if (saved)
            {
                Debug.Log("[BunkerTools] Scene saved successfully with highlighter.");
            }
            else
            {
                Debug.LogError("[BunkerTools] Failed to save scene with highlighter.");
            }
        }

        public static void RunBatchMode()
        {
            Debug.Log("[BunkerTools] Starting SetupElectricBoxHighlighter in Batch Mode.");
            string scenePath = "Assets/Assets/Scenes/BunkerScene_v2.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[BunkerTools] Failed to open scene: " + scenePath);
                EditorApplication.Exit(1);
                return;
            }

            SetupHighlighterInScene();

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            if (saved)
            {
                Debug.Log("[BunkerTools] Scene saved successfully in Batch Mode.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[BunkerTools] Failed to save scene in Batch Mode.");
                EditorApplication.Exit(1);
            }
        }

        private static void SetupHighlighterInScene()
        {
            string targetName = "Tz-ExteriorElectricBox2";
            GameObject box = GameObject.Find(targetName);
            if (box == null)
            {
                Debug.LogError($"[BunkerTools] Target object '{targetName}' not found in the scene.");
                return;
            }

            // Remove existing Highlighter if any to avoid duplicates
            Transform existing = box.transform.Find("Highlighter");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                Debug.Log("[BunkerTools] Removed existing Highlighter object.");
            }

            // Create Highlighter container
            GameObject highlighterGo = new GameObject("Highlighter");
            highlighterGo.transform.SetParent(box.transform);
            highlighterGo.transform.localPosition = Vector3.zero;
            highlighterGo.transform.localRotation = Quaternion.identity;
            highlighterGo.transform.localScale = Vector3.one;

            // Calculate local bounding box of box
            Bounds localBounds = CalculateLocalBounds(box);
            // Expand bounds slightly to prevent clipping
            localBounds.Expand(0.04f);

            // Create or load glowing cyan material
            Material glowMat = CreateOrGetGlowMaterial();

            // Build brackets (8 corners, each made of 3 perpendicular thin cubes)
            BuildCornerBrackets(highlighterGo, localBounds, glowMat);

            // Add the static highlighter script component so it pulses at runtime
            var staticHLComponent = highlighterGo.AddComponent<BunkerStaticHighlighter>();
            staticHLComponent.OutlineColor = new Color(0f, 1f, 0.95f, 0.85f);
            staticHLComponent.Blink = true;
            staticHLComponent.BlinkSpeed = 4.5f;

            // Deactivate by default so BunkerPowerManager controls it
            highlighterGo.SetActive(false);

            Debug.Log($"[BunkerTools] Successfully added static Highlighter around '{targetName}' in scene hierarchy.");
        }

        private static Bounds CalculateLocalBounds(GameObject target)
        {
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;

                Bounds rBounds = r.bounds;
                Vector3 minWorld = rBounds.min;
                Vector3 maxWorld = rBounds.max;

                Vector3[] worldCorners = new Vector3[]
                {
                    new Vector3(minWorld.x, minWorld.y, minWorld.z),
                    new Vector3(maxWorld.x, minWorld.y, minWorld.z),
                    new Vector3(maxWorld.x, maxWorld.y, minWorld.z),
                    new Vector3(minWorld.x, maxWorld.y, minWorld.z),
                    new Vector3(minWorld.x, minWorld.y, maxWorld.z),
                    new Vector3(maxWorld.x, minWorld.y, maxWorld.z),
                    new Vector3(maxWorld.x, maxWorld.y, maxWorld.z),
                    new Vector3(minWorld.x, maxWorld.y, maxWorld.z)
                };

                foreach (Vector3 corner in worldCorners)
                {
                    Vector3 localCorner = target.transform.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            if (!hasBounds)
            {
                localBounds = new Bounds(Vector3.zero, Vector3.one);
            }
            return localBounds;
        }

        private static Material CreateOrGetGlowMaterial()
        {
            string matPath = "Assets/Assets/Materials/HighlighterMaterial.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("HDRP/Unlit");
                bool isHDRP = (shader != null);
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

                mat = new Material(shader);
                Color glowColor = new Color(0f, 1f, 0.95f, 0.85f); // Neon Cyan

                if (isHDRP)
                {
                    mat.SetColor("_BaseColor", glowColor);
                    
                    // Transparent overlay settings
                    mat.SetInt("_SurfaceType", 1); // Transparent
                    mat.SetInt("_BlendMode", 0); // Alpha
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0); // Off
                    mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always); // Draw on top
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                    // Emission
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissiveColor", glowColor * 3.5f);
                }
                else
                {
                    mat.color = glowColor;
                    mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                }

                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[BunkerTools] Created new glowing highlighter material.");
            }
            return mat;
        }

        private static void BuildCornerBrackets(GameObject parent, Bounds bounds, Material material)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z), // 0: Bottom-Left-Back
                new Vector3(max.x, min.y, min.z), // 1: Bottom-Right-Back
                new Vector3(max.x, max.y, min.z), // 2: Top-Right-Back
                new Vector3(min.x, max.y, min.z), // 3: Top-Left-Back
                new Vector3(min.x, min.y, max.z), // 4: Bottom-Left-Front
                new Vector3(max.x, min.y, max.z), // 5: Bottom-Right-Front
                new Vector3(max.x, max.y, max.z), // 6: Top-Right-Front
                new Vector3(min.x, max.y, max.z)  // 7: Top-Left-Front
            };

            float bracketLength = 0.15f;
            float thickness = 0.025f;

            for (int i = 0; i < 8; i++)
            {
                GameObject cornerGo = new GameObject($"Bracket_Corner_{i}");
                cornerGo.transform.SetParent(parent.transform);
                cornerGo.transform.localPosition = corners[i];
                cornerGo.transform.localRotation = Quaternion.identity;
                cornerGo.transform.localScale = Vector3.one;

                Vector3 corner = corners[i];
                float dirX = (corner.x <= bounds.center.x) ? 1f : -1f;
                float dirY = (corner.y <= bounds.center.y) ? 1f : -1f;
                float dirZ = (corner.z <= bounds.center.z) ? 1f : -1f;

                // X Arm
                GameObject armX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(armX.GetComponent<BoxCollider>());
                armX.name = "Arm_X";
                armX.transform.SetParent(cornerGo.transform);
                armX.transform.localPosition = new Vector3(dirX * bracketLength / 2f, 0f, 0f);
                armX.transform.localScale = new Vector3(bracketLength, thickness, thickness);
                armX.GetComponent<Renderer>().sharedMaterial = material;

                // Y Arm
                GameObject armY = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(armY.GetComponent<BoxCollider>());
                armY.name = "Arm_Y";
                armY.transform.SetParent(cornerGo.transform);
                armY.transform.localPosition = new Vector3(0f, dirY * bracketLength / 2f, 0f);
                armY.transform.localScale = new Vector3(thickness, bracketLength, thickness);
                armY.GetComponent<Renderer>().sharedMaterial = material;

                // Z Arm
                GameObject armZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(armZ.GetComponent<BoxCollider>());
                armZ.name = "Arm_Z";
                armZ.transform.SetParent(cornerGo.transform);
                armZ.transform.localPosition = new Vector3(0f, 0f, dirZ * bracketLength / 2f);
                armZ.transform.localScale = new Vector3(thickness, thickness, bracketLength);
                armZ.GetComponent<Renderer>().sharedMaterial = material;
            }
        }
    }
}
