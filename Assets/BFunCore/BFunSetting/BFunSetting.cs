#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

// QUAN TRỌNG: using Sirenix... và using BFunCoreKit được đặt bên ngoài khối #if 
// vì chúng là các namespace trong chính project của bạn và luôn tồn tại.
using Sirenix.OdinInspector; 
using BFunCoreKit;

// QUAN TRỌNG: Chỉ using TMPro bên trong khối #if.
#if BFUN_INSTALLED_TRUE
using TMPro;
#endif

namespace BFunCoreKit
{
    public class BFunSetting : ScriptableObject
    {
        [SerializeField, ReadOnly] private bool hasInit = false;


        public bool HasInit => hasInit;

#if UNITY_EDITOR
        // Hàm này sẽ được gọi bởi Installer SAU KHI các package đã được cài.
        public void GenerateAllAssets()
        {
#if BFUN_INSTALLED_TRUE
            // 1. Cấu hình UI Setting
            string uiSettingPath = GlobalConst.SettingFolder + "/UI Setting.asset";
            GUISetting gUISetting = AssetDatabase.LoadAssetAtPath<GUISetting>(uiSettingPath);
            if (gUISetting != null)
            {
                gUISetting.AddToPrefabToAddresable();
            }

            // 2. Cấu hình Pipeline
            ConfigureCanvasForRenderPipeline();

            hasInit = true;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
#endif

            private void ConfigureCanvasForRenderPipeline()
        {
            // Bắt đầu khối code chỉ dành cho URP
#if UNITY_PIPELINE_URP

    // 1. Chỉ cần kiểm tra xem có SRP nào đang chạy không, không cần kiểm tra tên nữa vì #if đã làm việc đó
    if (GraphicsSettings.currentRenderPipeline != null)
    {
        Debug.Log("Phát hiện URP. Bắt đầu chuyển đổi Camera của BFun.GUI sang chế độ Overlay...");

        string prefabPath = "Assets/BFunCore/Prefab/UI/BFun.GUI.prefab";
        if (!File.Exists(prefabPath))
        {
            Debug.LogWarning($"Không tìm thấy prefab BFun.GUI tại: {prefabPath}. Bỏ qua bước chuyển đổi.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        Camera uiCamera = prefabRoot.GetComponentInChildren<Camera>();

        if (uiCamera == null)
        {
            Debug.LogError($"Prefab tại '{prefabPath}' không chứa component Camera nào.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // Truy cập vào các lớp của URP một cách an toàn bên trong khối #if
        if (uiCamera.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var urpCameraData))
        {
            if (urpCameraData.renderType != UnityEngine.Rendering.Universal.CameraRenderType.Overlay)
            {
                urpCameraData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
                Debug.Log($"Đã chuyển Camera của '{Path.GetFileName(prefabPath)}' sang chế độ Overlay.");
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            else
            {
                Debug.Log($"Camera của '{Path.GetFileName(prefabPath)}' đã ở chế độ Overlay.");
            }
        }
        else
        {
            Debug.LogWarning($"Camera trong prefab '{Path.GetFileName(prefabPath)}' không có component 'UniversalAdditionalCameraData'. Có thể project chưa được cấu hình URP đúng cách.");
        }
        
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

#else
            // (Tùy chọn) In ra một thông báo nếu không phải URP để dễ debug
            Debug.Log("Project không sử dụng URP. Không cần thay đổi Camera.");
#endif
            // Kết thúc khối code chỉ dành cho URP
        }
    }
}
#endif