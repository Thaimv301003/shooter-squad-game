#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class PackageManagerReloadHandler
{
    static PackageManagerReloadHandler()
    {
        if (SessionState.GetBool("BFunPackageManager_ShouldRefresh", false))
        {
            SessionState.EraseBool("BFunPackageManager_ShouldRefresh");
            var settings = AssetDatabase.FindAssets($"t:{nameof(PackageManagerSettings)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<PackageManagerSettings>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();
            if (settings != null)
            {
                // Gọi hàm refresh nhanh thay vì hàm đầy đủ
                settings.RefreshInstallationStatus();
            }
        }
    }
}
#endif