using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace BunkerTools
{
    public class SetupPuddleSystem
    {
        [MenuItem("Bunker Tools/Setup Floor Puddles")]
        public static void Run()
        {
            // 1. Ensure directories exist
            Directory.CreateDirectory("Assets/Assets/Textures");
            Directory.CreateDirectory("Assets/Assets/Materials");

            string texturePath = "Assets/Assets/Textures/WaterPuddleMask.png";
            string materialPath = "Assets/Assets/Materials/WaterPuddleDecal.mat";

            // 2. Generate a highly realistic organic puddle mask texture
            Debug.Log("Generating procedural organic puddle mask...");
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Random-like blob parameters for multiple connected puddle patches
            Vector2 blob1 = new Vector2(0.5f, 0.5f);
            Vector2 blob2 = new Vector2(0.35f, 0.45f);
            Vector2 blob3 = new Vector2(0.6f, 0.6f);
            float radius1 = 0.28f;
            float radius2 = 0.18f;
            float radius3 = 0.22f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    
                    // Normalize to [-1, 1] for calculations
                    float px = (u - 0.5f) * 2f;
                    float py = (v - 0.5f) * 2f;
                    
                    // Multi-frequency Perlin noise to make organic, non-circular borders
                    float noise1 = Mathf.PerlinNoise(u * 5f, v * 5f) * 0.12f;
                    float noise2 = Mathf.PerlinNoise(u * 12f + 10f, v * 12f + 10f) * 0.04f;
                    float noise = noise1 + noise2;

                    // Distance to blob centers
                    float d1 = Vector2.Distance(new Vector2(px, py), (blob1 - new Vector2(0.5f, 0.5f)) * 2f) + noise;
                    float d2 = Vector2.Distance(new Vector2(px, py), (blob2 - new Vector2(0.5f, 0.5f)) * 2f) + noise;
                    float d3 = Vector2.Distance(new Vector2(px, py), (blob3 - new Vector2(0.5f, 0.5f)) * 2f) + noise;

                    // Wetness calculation: value will be > 0 inside puddle
                    float val1 = Mathf.Clamp01((radius1 - d1) / 0.03f);
                    float val2 = Mathf.Clamp01((radius2 - d2) / 0.03f);
                    float val3 = Mathf.Clamp01((radius3 - d3) / 0.03f);
                    
                    // Combine blobs
                    float puddleIntensity = Mathf.Max(val1, Mathf.Max(val2, val3));

                    // Color: Dark grey tint (water absorbs light)
                    // Alpha: Smoothness/Opacity of the puddle
                    tex.SetPixel(x, y, new Color(0.08f, 0.08f, 0.08f, puddleIntensity));
                }
            }
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(texturePath, bytes);
            AssetDatabase.ImportAsset(texturePath);
            Debug.Log($"Puddle mask texture saved and imported at {texturePath}");

            // 3. Configure Texture Import Settings
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }
            Texture2D puddleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            // 4. Create and Configure HDRP Decal Material
            Debug.Log("Configuring HDRP Decal Material...");
            Shader decalShader = Shader.Find("HDRP/Decal");
            if (decalShader == null)
            {
                Debug.LogError("HDRP/Decal shader not found! Make sure you are using HDRP.");
                return;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                mat = new Material(decalShader);
                AssetDatabase.CreateAsset(mat, materialPath);
            }
            else
            {
                mat.shader = decalShader;
            }

            // Standard HDRP Decal Material Properties
            mat.SetTexture("_BaseColorMap", puddleTexture);
            mat.SetFloat("_DecalBlend", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_Metallic", 0f);

            // Enable Albedo, Normal, and Smoothness overrides for the decal
            if (mat.HasProperty("_AffectAlbedo")) mat.SetFloat("_AffectAlbedo", 1f);
            if (mat.HasProperty("_AffectNormal")) mat.SetFloat("_AffectNormal", 1f);
            if (mat.HasProperty("_AffectSmoothness")) mat.SetFloat("_AffectSmoothness", 1f);
            if (mat.HasProperty("_AffectMetal")) mat.SetFloat("_AffectMetal", 0f);
            if (mat.HasProperty("_AffectAO")) mat.SetFloat("_AffectAO", 0f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"Decal Material configured and saved at {materialPath}");

            // 5. Use the currently active open scene
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log($"Using active scene: {scene.name}");

            string targetFloorName = "pbr_sci-fi_modular_flooring (1)";
            GameObject floorObj = FindGameObjectInScene(targetFloorName);

            if (floorObj == null)
            {
                Debug.LogError($"Target floor object '{targetFloorName}' was not found in the scene! Creating at origin...");
            }

            Vector3 floorPos = floorObj != null ? floorObj.transform.position : Vector3.zero;
            Quaternion floorRot = floorObj != null ? floorObj.transform.rotation : Quaternion.identity;
            
            // 6. Create or update Puddle System GameObjects
            GameObject puddleSystemRoot = GameObject.Find("FloorPuddleSystem");
            if (puddleSystemRoot != null)
            {
                Object.DestroyImmediate(puddleSystemRoot);
            }
            
            puddleSystemRoot = new GameObject("FloorPuddleSystem");
            puddleSystemRoot.transform.position = floorPos;
            puddleSystemRoot.transform.rotation = floorRot;

            // Create Decal Projector GameObject
            GameObject decalProjGo = new GameObject("PuddleDecalProjector");
            decalProjGo.transform.parent = puddleSystemRoot.transform;
            
            // Position above the floor, pointing straight down
            decalProjGo.transform.localPosition = new Vector3(0f, 2f, 0f); // 2 meters above
            decalProjGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face down

            // Add HDRP Decal Projector Component
            var projector = decalProjGo.AddComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();
            projector.material = mat;
            // X/Y matches the size of the puddle on the floor; Z is the projection distance/depth
            projector.size = new Vector3(8f, 8f, 4f); 

            // 7. Add Planar Reflection Probe for perfect mirror-like puddle reflections
            GameObject planarProbeGo = new GameObject("PuddlePlanarReflectionProbe");
            planarProbeGo.transform.parent = puddleSystemRoot.transform;
            
            // Position slightly above the floor surface
            planarProbeGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            planarProbeGo.transform.localRotation = Quaternion.identity;

            var planarProbe = planarProbeGo.AddComponent<UnityEngine.Rendering.HighDefinition.PlanarReflectionProbe>();
            
            // Setup the bounding box to cover the floor object
            planarProbe.influenceVolume.shape = UnityEngine.Rendering.HighDefinition.InfluenceShape.Box;
            planarProbe.influenceVolume.boxSize = new Vector3(12f, 5f, 12f); // Covers the flooring area
            
            // Set update mode to OnAwake/Static for maximum performance unless dynamic elements are needed
            planarProbe.mode = UnityEngine.Rendering.HighDefinition.ProbeSettings.Mode.Realtime;
            planarProbe.realtimeMode = UnityEngine.Rendering.HighDefinition.ProbeSettings.RealtimeMode.OnDemand;

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Puddle system successfully configured in the scene with a Decal Projector and Planar Reflection Probe!");
        }

        private static GameObject FindGameObjectInScene(string name)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                var result = FindInChildren(root.transform, name);
                if (result != null) return result.gameObject;
            }
            return null;
        }

        private static Transform FindInChildren(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindInChildren(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
