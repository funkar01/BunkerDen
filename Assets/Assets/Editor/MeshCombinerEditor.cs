using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace BunkerTools
{
    public class MeshCombinerEditor : EditorWindow
    {
        private GameObject targetObject;
        private bool deactivateOriginal = true;
        private string savePath = "Assets/Assets/3DModels/Combined";

        [MenuItem("Bunker Tools/Mesh Combiner")]
        public static void ShowWindow()
        {
            GetWindow<MeshCombinerEditor>("Mesh Combiner");
        }

        private void OnGUI()
        {
            GUILayout.Label("Bunker Mesh Combiner Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
            deactivateOriginal = EditorGUILayout.Toggle("Deactivate Original Parts", deactivateOriginal);
            savePath = EditorGUILayout.TextField("Save Folder", savePath);

            EditorGUILayout.Space();

            if (GUILayout.Button("Combine Meshes"))
            {
                if (targetObject == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select a target GameObject to combine.", "OK");
                    return;
                }

                CombineMeshes(targetObject);
            }
        }

        private void CombineMeshes(GameObject root)
        {
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(false);
            if (meshFilters.Length <= 1)
            {
                EditorUtility.DisplayDialog("Info", "No child meshes to combine or target already has 1 or fewer meshes.", "OK");
                return;
            }

            // Ensure save directory exists
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }

            // Group mesh filters by material
            Dictionary<Material, List<MeshFilter>> materialGroups = new Dictionary<Material, List<MeshFilter>>();

            foreach (var filter in meshFilters)
            {
                // Skip if it's the root itself and has no mesh (or we skip the combined output root)
                if (filter.gameObject == root && root.GetComponent<MeshRenderer>() == null)
                    continue;

                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null)
                    continue;

                Material[] sharedMaterials = renderer.sharedMaterials;
                for (int i = 0; i < sharedMaterials.Length; i++)
                {
                    Material mat = sharedMaterials[i];
                    if (mat == null) continue;

                    if (!materialGroups.ContainsKey(mat))
                    {
                        materialGroups[mat] = new List<MeshFilter>();
                    }
                    materialGroups[mat].Add(filter);
                }
            }

            if (materialGroups.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No valid meshes with materials found.", "OK");
                return;
            }

            // Create a new container for combined meshes
            GameObject combinedContainer = new GameObject(root.name + "_Combined");
            combinedContainer.transform.SetParent(root.transform);
            combinedContainer.transform.localPosition = Vector3.zero;
            combinedContainer.transform.localRotation = Quaternion.identity;
            combinedContainer.transform.localScale = Vector3.one;

            Undo.RegisterCreatedObjectUndo(combinedContainer, "Combine Meshes");

            int combinedCount = 0;

            foreach (var group in materialGroups)
            {
                Material mat = group.Key;
                List<MeshFilter> filters = group.Value;

                List<CombineInstance> combineInstances = new List<CombineInstance>();

                foreach (var filter in filters)
                {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = filter.sharedMesh;
                    
                    // We must calculate the local matrix relative to the root GameObject
                    ci.transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    combineInstances.Add(ci);
                }

                // Create combined mesh
                Mesh combinedMesh = new Mesh();
                combinedMesh.name = $"{root.name}_{mat.name}_CombinedMesh";
                combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

                // Save combined mesh asset
                string assetName = $"{root.name}_{mat.name}_Combined_{System.DateTime.Now.Ticks}.asset";
                string fullPath = Path.Combine(savePath, assetName).Replace("\\", "/");
                AssetDatabase.CreateAsset(combinedMesh, fullPath);

                // Create combined child GameObject
                GameObject combinedObj = new GameObject($"{root.name}_Combined_{mat.name}");
                combinedObj.transform.SetParent(combinedContainer.transform);
                combinedObj.transform.localPosition = Vector3.zero;
                combinedObj.transform.localRotation = Quaternion.identity;
                combinedObj.transform.localScale = Vector3.one;

                MeshFilter combinedFilter = combinedObj.AddComponent<MeshFilter>();
                combinedFilter.sharedMesh = combinedMesh;

                MeshRenderer combinedRenderer = combinedObj.AddComponent<MeshRenderer>();
                combinedRenderer.sharedMaterial = mat;

                combinedCount++;
            }

            // Deactivate original objects
            if (deactivateOriginal)
            {
                foreach (var filter in meshFilters)
                {
                    if (filter.gameObject != root && !filter.transform.IsChildOf(combinedContainer.transform))
                    {
                        Undo.RecordObject(filter.gameObject, "Deactivate Combined Part");
                        filter.gameObject.SetActive(false);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Successfully combined meshes into {combinedCount} sub-mesh(es) grouped by Material!", "OK");
        }
    }
}
