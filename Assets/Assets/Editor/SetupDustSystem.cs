using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace BunkerTools
{
    public class SetupDustSystem
    {
        [MenuItem("Bunker Tools/Setup New Dust System")]
        public static void Run()
        {
            // 1. Ensure directories exist
            Directory.CreateDirectory("Assets/Assets/Textures");
            Directory.CreateDirectory("Assets/Assets/Materials");

            string texturePath = "Assets/Assets/Textures/BunkerDustSoftCircle.png";
            string materialPath = "Assets/Assets/Materials/BunkerDustMaterial.mat";
            string scenePath = "Assets/Assets/Scenes/BunkerScene_v2.unity";

            // 2. Create and Save soft circle texture
            Debug.Log("Generating blurred circular texture...");
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - (size - 1) / 2f;
                    float dy = y - (size - 1) / 2f;
                    
                    // Normalize distance relative to radius
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2.2f);
                    float alpha = Mathf.Clamp01(1f - dist);
                    
                    // Apply Gaussian decay * linear falloff for extremely blurry edges (realistic out-of-focus dust)
                    alpha = Mathf.Exp(-dist * dist * 4.5f) * alpha; 
                    
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(texturePath, bytes);
            AssetDatabase.ImportAsset(texturePath);
            Debug.Log($"Texture saved and imported at {texturePath}");

            // 3. Load texture and configure import settings
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true; // Color map
                importer.SaveAndReimport();
            }
            Texture2D dustTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            // 4. Create Material
            Debug.Log("Creating BunkerDustMaterial...");
            Shader particlesShader = Shader.Find("HDRP/ParticlesUnlit");
            if (particlesShader == null)
            {
                particlesShader = Shader.Find("Particles/Standard Unlit");
            }
            if (particlesShader == null)
            {
                particlesShader = Shader.Find("Unlit/Transparent");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                mat = new Material(particlesShader);
                AssetDatabase.CreateAsset(mat, materialPath);
            }
            else
            {
                mat.shader = particlesShader;
            }

            // Configure Material for HDRP Particles Unlit transparency
            mat.SetTexture("_BaseColorMap", dustTexture);
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", dustTexture);
            }

            // Set to transparent surface type in HDRP
            if (mat.HasProperty("_SurfaceType"))
                mat.SetFloat("_SurfaceType", 1); // 1 = Transparent
            
            if (mat.HasProperty("_BlendMode"))
                mat.SetFloat("_BlendMode", 0); // 0 = Alpha blend

            // Setup blending keywords and parameters for standard/HDRP render pipelines
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.renderQueue = 3000;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"Material configured and saved at {materialPath}");

            // 5. Setup Scene
            Debug.Log($"Opening scene: {scenePath}");
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = Object.FindAnyObjectByType<Camera>();
            }

            if (mainCam == null)
            {
                Debug.LogError("No Camera found in the scene! Cannot attach manager to Main Camera.");
                return;
            }

            GameObject camGo = mainCam.gameObject;
            Debug.Log($"Found camera GameObject: {camGo.name}. Attaching BunkerDustManager...");

            BunkerDustManager manager = camGo.GetComponent<BunkerDustManager>();
            if (manager == null)
            {
                manager = camGo.AddComponent<BunkerDustManager>();
            }

            Undo.RecordObject(manager, "Configure Dust Manager");
            manager.dustMaterial = mat;
            manager.density = 250f; // Soft, nicely dense
            manager.minSize = 0.015f;
            manager.maxSize = 0.045f;
            manager.windSpeed = 0.15f; // More fluid motion
            manager.opacity = 0.45f;
            manager.particleColor = new Color(0.68f, 0.68f, 0.7f, 1f); // Ash color
            manager.noiseFrequency = 0.65f; // More volatile direction changes
            manager.noiseStrengthMultiplier = 1.4f; // More turbulence

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Scene saved successfully with BunkerDustManager attached to camera!");
        }
    }
}
