using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Text;
using System.IO;

namespace BunkerTools
{
    public class HDRPToURPMaterialConverter : EditorWindow
    {
        private static StringBuilder log = new StringBuilder();

        private static void Log(string message)
        {
            Debug.Log(message);
            log.AppendLine(message);
        }

        private static void SaveLog()
        {
            try
            {
                File.WriteAllText("urp_setup.log", log.ToString());
                Debug.Log("[BunkerTools] Diagnostic log saved to urp_setup.log");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to write diagnostic log: " + ex.Message);
            }
        }

        [MenuItem("Bunker Tools/Complete URP Setup and Material Conversion")]
        public static void SetupURPAndConvert()
        {
            log.Clear();
            Log("=== Starting URP Setup and Material Conversion ===");
            Log("Timestamp: " + System.DateTime.Now.ToString());

            try
            {
                // 1. Create Settings folder if it doesn't exist
                string settingsFolder = "Assets/Assets/Settings";
                if (!AssetDatabase.IsValidFolder(settingsFolder))
                {
                    AssetDatabase.CreateFolder("Assets/Assets", "Settings");
                    Log("Created settings folder Assets/Assets/Settings");
                }

                string rendererPath = settingsFolder + "/URP-RendererData.asset";
                string pipelinePath = settingsFolder + "/URP-PipelineAsset.asset";

                UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);

                // 2. Create Renderer Data if missing
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, rendererPath);
                    Log($"Created URP Renderer Data at: {rendererPath}");
                }

                // 3. Add Decal Renderer Feature if missing
                AddDecalFeatureToRenderer(rendererData);

                // 4. Create URP Pipeline Asset if missing
                if (pipelineAsset == null)
                {
                    pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                    AssetDatabase.CreateAsset(pipelineAsset, pipelinePath);
                    Log($"Created URP Pipeline Asset at: {pipelinePath}");
                }
                else
                {
                    // Ensure the renderer is linked properly
                    SerializedObject serializedPipeline = new SerializedObject(pipelineAsset);
                    SerializedProperty rendererDataProp = serializedPipeline.FindProperty("m_RendererDataList");
                    if (rendererDataProp != null && rendererDataProp.isArray)
                    {
                        rendererDataProp.ClearArray();
                        rendererDataProp.InsertArrayElementAtIndex(0);
                        rendererDataProp.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                    }
                    serializedPipeline.ApplyModifiedProperties();
                    Log("Ensured URP Pipeline links to Renderer Data");
                }

                // 5. Assign URP in Graphics and Quality Settings
                GraphicsSettings.defaultRenderPipeline = pipelineAsset;
                
                // Assign URP to all quality levels to prevent overrides from causing pink shaders
                int qualityLevelsCount = QualitySettings.names.Length;
                for (int i = 0; i < qualityLevelsCount; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipelineAsset;
                }
                Log($"Assigned URP Pipeline Asset to defaultRenderPipeline and {qualityLevelsCount} Quality Levels.");

                // 6. Convert materials
                ConvertMaterials();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("URP Setup", "Successfully created URP Pipeline Assets, assigned them in Project Settings, and converted all materials to URP!", "OK");
            }
            catch (System.Exception ex)
            {
                Log("EXCEPTION IN SETUP: " + ex.ToString());
                EditorUtility.DisplayDialog("URP Setup Exception", "An error occurred during setup. Check urp_setup.log for details.", "OK");
            }
            finally
            {
                SaveLog();
            }
        }

        private static void AddDecalFeatureToRenderer(UniversalRendererData rendererData)
        {
            // Check if feature already exists
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature != null && feature.GetType() == typeof(DecalRendererFeature))
                {
                    Log("DecalRendererFeature already exists in renderer data.");
                    return; // Already exists
                }
            }

            // Create and add Decal Renderer Feature
            DecalRendererFeature decalFeature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            decalFeature.name = "DecalRendererFeature";
            AssetDatabase.AddObjectToAsset(decalFeature, rendererData);

            SerializedObject serializedRenderer = new SerializedObject(rendererData);
            SerializedProperty featuresProp = serializedRenderer.FindProperty("m_RendererFeatures");
            if (featuresProp != null && featuresProp.isArray)
            {
                int index = featuresProp.arraySize;
                featuresProp.InsertArrayElementAtIndex(index);
                featuresProp.GetArrayElementAtIndex(index).objectReferenceValue = decalFeature;
            }
            serializedRenderer.ApplyModifiedProperties();
            EditorUtility.SetDirty(rendererData);
            Log("Added URP Decal Renderer Feature to URP Renderer Data.");
        }

        [MenuItem("Bunker Tools/Convert Materials HDRP -> URP Only")]
        public static void ConvertMaterialsMenu()
        {
            log.Clear();
            Log("=== Starting URP Material Conversion Only ===");
            Log("Timestamp: " + System.DateTime.Now.ToString());
            try
            {
                ConvertMaterials();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Material Converter", "Material conversion completed. Check urp_setup.log for details.", "OK");
            }
            catch (System.Exception ex)
            {
                Log("EXCEPTION IN CONVERSION: " + ex.ToString());
            }
            finally
            {
                SaveLog();
            }
        }

        public static void ConvertMaterials()
        {
            Log("--- Starting Material Conversion ---");
            
            // Search all materials in project
            string[] guids = AssetDatabase.FindAssets("t:Material");
            Log($"Found {guids.Length} total material assets in the project database.");
            
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            Shader urpDecalShader = Shader.Find("Shader Graphs/Decal");
            
            Log("Shader 'Universal Render Pipeline/Lit' found: " + (urpLitShader != null));
            Log("Shader 'Universal Render Pipeline/Unlit' found: " + (urpUnlitShader != null));
            Log("Shader 'Shader Graphs/Decal' found: " + (urpDecalShader != null));
            
            if (urpLitShader == null)
            {
                Log("ERROR: URP Lit Shader ('Universal Render Pipeline/Lit') not found! Aborting conversion.");
                return;
            }

            int litConverted = 0;
            int unlitConverted = 0;
            int decalConverted = 0;
            int skipped = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    Log($"Warning: Material at path '{path}' could not be loaded.");
                    continue;
                }

                // 1. Determine shader name (even if shader is missing/null due to package uninstall)
                string shaderName = "None";
                if (mat.shader != null && mat.shader.name != "Hidden/InternalErrorShader")
                {
                    shaderName = mat.shader.name;
                }
                else
                {
                    // Parse the file text for HDRP shader GUIDs
                    try
                    {
                        string fileText = File.ReadAllText(path);
                        if (fileText.Contains("guid: 6e4ae4064600d784cac1e41a9e6f2e59")) // HDRP/Lit GUID
                        {
                            shaderName = "HDRP/Lit";
                        }
                        else if (fileText.Contains("guid: c28bb37ced75c094faa86ea19e772801")) // HDRP/Unlit GUID
                        {
                            shaderName = "HDRP/Unlit";
                        }
                        else if (fileText.Contains("guid: 1d64af84bdc970c4fae0c1e06dd95b73")) // HDRP/Decal GUID
                        {
                            shaderName = "HDRP/Decal";
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log($"Warning: Failed to parse raw YAML file for {path}: {ex.Message}");
                    }
                }

                // 2. Perform deep serialized data upgrade
                if (shaderName == "HDRP/Lit")
                {
                    Log($"Converting Lit Material: {path}");
                    
                    // Extract properties directly from serialized state (bypasses null shader constraints)
                    Texture baseMap = GetTextureFromSerialized(mat, "_BaseColorMap");
                    Color baseColor = GetColorFromSerialized(mat, "_BaseColor", Color.white);
                    Texture normalMap = GetTextureFromSerialized(mat, "_NormalMap");
                    float normalScale = GetFloatFromSerialized(mat, "_NormalScale", 1.0f);
                    Texture maskMap = GetTextureFromSerialized(mat, "_MaskMap");
                    Texture emissiveMap = GetTextureFromSerialized(mat, "_EmissiveColorMap");
                    Color emissiveColor = GetColorFromSerialized(mat, "_EmissiveColor", Color.black);
                    
                    Vector2 mainTiling = GetScaleFromSerialized(mat, "_BaseColorMap");
                    Vector2 mainOffset = GetOffsetFromSerialized(mat, "_BaseColorMap");
                    float doubleSided = GetFloatFromSerialized(mat, "_DoubleSidedEnable", 0f);
                    float surfaceType = GetFloatFromSerialized(mat, "_SurfaceType", 0f);

                    // Swap shader to URP Lit
                    mat.shader = urpLitShader;

                    // Apply cached textures & parameters to URP slots
                    mat.SetTexture("_BaseMap", baseMap);
                    mat.SetColor("_BaseColor", baseColor);
                    mat.SetTextureScale("_BaseMap", mainTiling);
                    mat.SetTextureOffset("_BaseMap", mainOffset);

                    if (normalMap != null)
                    {
                        mat.SetTexture("_BumpMap", normalMap);
                        mat.SetFloat("_BumpScale", normalScale);
                        mat.EnableKeyword("_NORMALMAP");
                    }

                    if (maskMap != null)
                    {
                        mat.SetTexture("_MetallicGlossMap", maskMap);
                        mat.SetFloat("_Metallic", 1f);
                        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                        
                        mat.SetTexture("_OcclusionMap", maskMap);
                        mat.SetFloat("_OcclusionStrength", 1f);
                    }

                    if (emissiveMap != null || emissiveColor != Color.black)
                    {
                        mat.SetTexture("_EmissionMap", emissiveMap);
                        mat.SetColor("_EmissionColor", emissiveColor);
                        mat.EnableKeyword("_EMISSION");
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    }

                    // Double-sided conversion
                    if (doubleSided == 1f)
                    {
                        mat.SetFloat("_Cull", 0f);
                        if (mat.HasProperty("_RenderFace")) mat.SetFloat("_RenderFace", 0f);
                    }

                    // Transparency conversion
                    if (surfaceType == 1f) // Transparent
                    {
                        mat.SetFloat("_Surface", 1f);
                        mat.SetFloat("_Blend", 0f); // Alpha blend
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = 3000;
                    }

                    EditorUtility.SetDirty(mat);
                    litConverted++;
                }
                else if (shaderName == "HDRP/Unlit" && urpUnlitShader != null)
                {
                    Log($"Converting Unlit Material: {path}");
                    
                    Texture baseMap = GetTextureFromSerialized(mat, "_UnlitColorMap") ?? GetTextureFromSerialized(mat, "_BaseColorMap");
                    Color baseColor = GetColorFromSerialized(mat, "_UnlitColor", GetColorFromSerialized(mat, "_BaseColor", Color.white));
                    Texture emissiveMap = GetTextureFromSerialized(mat, "_EmissiveColorMap");
                    Color emissiveColor = GetColorFromSerialized(mat, "_EmissiveColor", Color.black);

                    mat.shader = urpUnlitShader;
                    mat.SetTexture("_BaseMap", baseMap);
                    mat.SetColor("_BaseColor", baseColor);

                    if (emissiveMap != null || emissiveColor != Color.black)
                    {
                        mat.SetTexture("_EmissionMap", emissiveMap);
                        mat.SetColor("_EmissionColor", emissiveColor);
                        mat.EnableKeyword("_EMISSION");
                    }

                    EditorUtility.SetDirty(mat);
                    unlitConverted++;
                }
                else if (shaderName == "HDRP/Decal" && urpDecalShader != null)
                {
                    Log($"Converting Decal Material: {path}");
                    
                    Texture baseColorMap = GetTextureFromSerialized(mat, "_BaseColorMap");
                    Color baseColor = GetColorFromSerialized(mat, "_BaseColor", Color.white);
                    Texture normalMap = GetTextureFromSerialized(mat, "_NormalMap");
                    Texture maskMap = GetTextureFromSerialized(mat, "_MaskMap");
                    
                    float decalBlend = GetFloatFromSerialized(mat, "_DecalBlend", 1f);
                    float smoothness = GetFloatFromSerialized(mat, "_Smoothness", 1f);
                    float metallic = GetFloatFromSerialized(mat, "_Metallic", 0f);

                    mat.shader = urpDecalShader;
                    mat.SetTexture("_BaseColorMap", baseColorMap);
                    mat.SetColor("_BaseColor", baseColor);
                    
                    if (normalMap != null) mat.SetTexture("_NormalMap", normalMap);
                    if (maskMap != null) mat.SetTexture("_MaskMap", maskMap);
                    
                    mat.SetFloat("_DecalBlend", decalBlend);
                    mat.SetFloat("_Smoothness", smoothness);
                    mat.SetFloat("_Metallic", metallic);

                    EditorUtility.SetDirty(mat);
                    decalConverted++;
                }
                else
                {
                    skipped++;
                }
            }

            Log($"Material conversion execution completed. Lit Converted: {litConverted}, Unlit Converted: {unlitConverted}, Decal Converted: {decalConverted}, Skipped: {skipped}");
        }

        // --- Serialized Property Extraction Helpers (Bypasses null shader constraints) ---

        private static Texture GetTextureFromSerialized(Material mat, string propertyName)
        {
            SerializedObject so = new SerializedObject(mat);
            SerializedProperty texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (texEnvs != null && texEnvs.isArray)
            {
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    SerializedProperty prop = texEnvs.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = prop.FindPropertyRelative("first");
                    if (nameProp != null && nameProp.stringValue == propertyName)
                    {
                        SerializedProperty texProp = prop.FindPropertyRelative("second.m_Texture");
                        if (texProp != null)
                        {
                            return texProp.objectReferenceValue as Texture;
                        }
                    }
                }
            }
            return null;
        }

        private static Color GetColorFromSerialized(Material mat, string propertyName, Color defaultValue)
        {
            SerializedObject so = new SerializedObject(mat);
            SerializedProperty colors = so.FindProperty("m_SavedProperties.m_Colors");
            if (colors != null && colors.isArray)
            {
                for (int i = 0; i < colors.arraySize; i++)
                {
                    SerializedProperty prop = colors.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = prop.FindPropertyRelative("first");
                    if (nameProp != null && nameProp.stringValue == propertyName)
                    {
                        SerializedProperty colorProp = prop.FindPropertyRelative("second");
                        if (colorProp != null)
                        {
                            return colorProp.colorValue;
                        }
                    }
                }
            }
            return defaultValue;
        }

        private static float GetFloatFromSerialized(Material mat, string propertyName, float defaultValue)
        {
            SerializedObject so = new SerializedObject(mat);
            SerializedProperty floats = so.FindProperty("m_SavedProperties.m_Floats");
            if (floats != null && floats.isArray)
            {
                for (int i = 0; i < floats.arraySize; i++)
                {
                    SerializedProperty prop = floats.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = prop.FindPropertyRelative("first");
                    if (nameProp != null && nameProp.stringValue == propertyName)
                    {
                        SerializedProperty floatProp = prop.FindPropertyRelative("second");
                        if (floatProp != null)
                        {
                            return floatProp.floatValue;
                        }
                    }
                }
            }
            return defaultValue;
        }

        private static Vector2 GetScaleFromSerialized(Material mat, string propertyName)
        {
            SerializedObject so = new SerializedObject(mat);
            SerializedProperty texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (texEnvs != null && texEnvs.isArray)
            {
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    SerializedProperty prop = texEnvs.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = prop.FindPropertyRelative("first");
                    if (nameProp != null && nameProp.stringValue == propertyName)
                    {
                        SerializedProperty scaleProp = prop.FindPropertyRelative("second.m_Scale");
                        if (scaleProp != null)
                        {
                            return scaleProp.vector2Value;
                        }
                    }
                }
            }
            return Vector2.one;
        }

        private static Vector2 GetOffsetFromSerialized(Material mat, string propertyName)
        {
            SerializedObject so = new SerializedObject(mat);
            SerializedProperty texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (texEnvs != null && texEnvs.isArray)
            {
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    SerializedProperty prop = texEnvs.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = prop.FindPropertyRelative("first");
                    if (nameProp != null && nameProp.stringValue == propertyName)
                    {
                        SerializedProperty offsetProp = prop.FindPropertyRelative("second.m_Offset");
                        if (offsetProp != null)
                        {
                            return offsetProp.vector2Value;
                        }
                    }
                }
            }
            return Vector2.zero;
        }
    }
}
