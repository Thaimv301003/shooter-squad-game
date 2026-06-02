#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace BFunCoreKit
{
    [InitializeOnLoad]
    public static class PackageDefineManager
    {
        private static readonly (string packageName, string defineSymbol)[] PackageDefinePairs =
        {
        ("com.thelegends.firebase.manager", "USE_FIREBASE"),
        ("com.thelegends.ads.manager", "USE_ADMOB"),
    };

        static PackageDefineManager()
        {
            // Lắng nghe khi package thay đổi
            Events.registeredPackages += OnPackagesChanged;

            // Kiểm tra đồng bộ khi khởi động hoặc recompile
            CheckAllPackagesState();
        }

        private static void OnPackagesChanged(PackageRegistrationEventArgs e)
        {
            foreach (var (packageName, defineSymbol) in PackageDefinePairs)
            {
                // Khi thêm package
                if (e.added.Any(p => p.name == packageName))
                {
                    AddDefine(defineSymbol);
                    Debug.Log($"✅ Added Scripting Define Symbol: {defineSymbol} (Detected {packageName})");
                }

                // Khi gỡ package
                if (e.removed.Any(p => p.name == packageName))
                {
                    RemoveDefine(defineSymbol);
                    Debug.Log($"🗑 Removed Scripting Define Symbol: {defineSymbol} (Removed {packageName})");
                }
            }
        }

        private static async void CheckAllPackagesState()
        {
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
                await System.Threading.Tasks.Task.Delay(100);

            if (listRequest.Status != StatusCode.Success)
                return;

            var installed = listRequest.Result.Select(p => p.name).ToList();

            foreach (var (packageName, defineSymbol) in PackageDefinePairs)
            {
                bool isInstalled = installed.Contains(packageName);
                bool isDefined = HasDefine(defineSymbol);

                if (isInstalled && !isDefined)
                {
                    Debug.Log($"[PackageDefineManager] Sync: {packageName} installed → adding {defineSymbol}");
                    AddDefine(defineSymbol);
                }
                //else if (!isInstalled && isDefined)
                //{
                //    Debug.Log($"[PackageDefineManager] Sync: {packageName} missing → removing {defineSymbol}");
                //    RemoveDefine(defineSymbol);
                //}
            }
        }

        private static void AddDefine(string symbol)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

            if (!defines.Contains(symbol))
            {
                defines.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines));
            }
        }

        private static void RemoveDefine(string symbol)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

            if (defines.Contains(symbol))
            {
                defines.Remove(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines));
            }
        }

        private static bool HasDefine(string symbol)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';');
            return defines.Contains(symbol);
        }
    }
}
#endif
