using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Watermelon.SquadShooter
{
    public class SkinnedMeshCombiner : EditorWindow
    {
        [MenuItem("GameObject/Combine Skinned Meshes", false, 0)]
        public static void CombineMeshes(MenuCommand menuCommand)
        {
            GameObject selectedObj = Selection.activeGameObject;
            if (selectedObj == null)
            {
                Debug.LogWarning("Vui lòng chọn một GameObject chứa các SkinnedMeshRenderer (thường là Object gốc của model).");
                return;
            }

            SkinnedMeshRenderer[] smRenderers = selectedObj.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (smRenderers.Length == 0)
            {
                Debug.LogWarning("Không tìm thấy SkinnedMeshRenderer nào trong Object này.");
                return;
            }

            List<CombineInstance> combineInstances = new List<CombineInstance>();
            List<Material> materials = new List<Material>();
            List<Transform> bones = new List<Transform>();
            Transform rootBone = smRenderers[0].rootBone;

            foreach (SkinnedMeshRenderer smr in smRenderers)
            {
                if (smr.sharedMesh == null) continue;

                for (int subMeshIndex = 0; subMeshIndex < smr.sharedMesh.subMeshCount; subMeshIndex++)
                {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = smr.sharedMesh;
                    ci.subMeshIndex = subMeshIndex;
                    // For Skinned Meshes, the vertices are already properly weighted to the bones.
                    // Applying a transform matrix here physically moves the vertices, which causes double-transformations (floating head) when bones apply their own offsets.
                    ci.transform = Matrix4x4.identity;
                    combineInstances.Add(ci);

                    // Add material
                    if (subMeshIndex < smr.sharedMaterials.Length)
                    {
                        materials.Add(smr.sharedMaterials[subMeshIndex]);
                    }
                    else
                    {
                        materials.Add(smr.sharedMaterial); // Fallback
                    }
                }

                // Collect bones
                foreach (Transform bone in smr.bones)
                {
                    bones.Add(bone);
                }
                
                // Hide old renderers instead of deleting them to be safe
                smr.gameObject.SetActive(false);
            }

            // Create new Skinned Mesh
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = selectedObj.name + "_CombinedMesh";
            
            // Combine with preserve vertex attributes to keep bone weights (skinning)
            combinedMesh.CombineMeshes(combineInstances.ToArray(), false, true);

            // Create new GameObject to hold the combined mesh
            GameObject combinedObj = new GameObject("Combined_SkinnedMesh");
            combinedObj.transform.SetParent(selectedObj.transform);
            combinedObj.transform.localPosition = Vector3.zero;
            combinedObj.transform.localRotation = Quaternion.identity;
            combinedObj.transform.localScale = Vector3.one;

            SkinnedMeshRenderer combinedSMR = combinedObj.AddComponent<SkinnedMeshRenderer>();
            combinedSMR.sharedMesh = combinedMesh;
            combinedSMR.sharedMaterials = materials.ToArray();
            combinedSMR.bones = bones.ToArray();
            combinedSMR.rootBone = rootBone;

            // Make sure the new mesh is saved as an asset so it works in Prefabs
            string savePath = "Assets/CombinedMesh_" + selectedObj.name + ".asset";
            // Make path unique if it exists
            savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);
            AssetDatabase.CreateAsset(combinedMesh, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Đã gộp thành công {smRenderers.Length} mesh thành 1. File Mesh được lưu tại: {savePath}. Các mesh cũ đã bị ẩn đi.");
        }
    }
}
