using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class PackageManagerAssetWatcher : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (deletedAssets.Length == 0) return;
        if (SessionState.GetBool("BFunPackageManager_ShouldRefresh", false)) return;

        var settings = AssetDatabase.FindAssets($"t:{nameof(PackageManagerSettings)}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<PackageManagerSettings>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault();
        if (settings == null) return;

        // Cần phải định nghĩa lại các hàm tiện ích ở đây vì chúng ta đang ở trong một lớp khác
        var normalizedPackageNames = new HashSet<string>(
            settings.bfunPackageLibary.Select(p => GetNormalizedName(GetBasePackageName(p.packageName)))
        );

        foreach (var deletedPath in deletedAssets)
        {
            string deletedFolderName = Path.GetFileName(deletedPath);
            string normalizedDeletedName = GetNormalizedName(deletedFolderName);
            if (normalizedPackageNames.Contains(normalizedDeletedName))
            {
                Debug.Log($"Detected deletion of a package folder: {deletedFolderName}. Scheduling a refresh.");
                SessionState.SetBool("BFunPackageManager_ShouldRefresh", true);
                break;
            }
        }
    }

    private static string GetNormalizedName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return name.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
    }

    private static string GetBasePackageName(string fullPackageName)
    {
        var match = Regex.Match(fullPackageName, @"^(.*?)\s*v\d", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : fullPackageName;
    }
}