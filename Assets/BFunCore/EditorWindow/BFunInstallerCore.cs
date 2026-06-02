#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BFunCoreKit
{
    public static class BFunInstallerCore
    {
        public const string Key_AutoInstall = "BFun_AutoInstall_Flag";

        private static AddRequest _currentRequest;
        private static bool _isMonitorRunning;
        private static bool _originalRunInBackground;
        private static string _currentInstallingPackageName;

        // --- 1. KHỞI ĐỘNG (CHẾ ĐỘ CHỜ) ---

        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            // Kiểm tra cờ: 
            // - Nếu FALSE (Mặc định khi mới import): Không làm gì cả -> Cửa sổ hiện lên và chờ bấm nút.
            // - Nếu TRUE (User đã bấm nút và Unity vừa Recompile): Tiếp tục chạy quy trình.
            if (EditorPrefs.GetBool(Key_AutoInstall, false))
            {
                EditorApplication.delayCall += ResumeInstallProcess;
            }
            else
            {
                // Dọn dẹp tiến trình nếu có
                if (_isMonitorRunning)
                {
                    EditorApplication.update -= InstallMonitorLoop;
                    _isMonitorRunning = false;
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        // Hàm này được gọi khi bấm nút "Install" trên Window UI
        public static void StartAutoInstall()
        {
            EditorPrefs.SetBool(Key_AutoInstall, true);
            ResumeInstallProcess();
        }

        public static void ResumeInstallProcess()
        {
            // Chỉ chạy nếu cờ đang bật
            if (!EditorPrefs.GetBool(Key_AutoInstall, false)) return;

            ShowBlockingProgressBar("Initializing Setup...", 0.1f);

            if (!_isMonitorRunning)
            {
                _originalRunInBackground = Application.runInBackground;
                Application.runInBackground = true;
                EditorApplication.update += InstallMonitorLoop;
                _isMonitorRunning = true;
            }
        }

        // --- 2. VÒNG LẶP XỬ LÝ CHÍNH ---

        private static void InstallMonitorLoop()
        {
            try
            {
                InternalEditorUtility.RepaintAllViews();

                // A. CHECK REQUEST PACKAGE
                if (_currentRequest != null)
                {
                    ShowBlockingProgressBar($"Installing {_currentInstallingPackageName}...", 0.3f);
                    if (_currentRequest.IsCompleted)
                    {
                        if (_currentRequest.Status == StatusCode.Failure)
                            Debug.LogError($"[BFun Installer] Failed: {_currentRequest.Error.message}");

                        _currentRequest = null;
                        _currentInstallingPackageName = null;
                        EditorUtility.ClearProgressBar();
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    }
                    return;
                }

                // B. CHECK MISSING PACKAGES
                var missingPkg = GetFirstMissingPackage();
                if (missingPkg != null)
                {
                    _currentInstallingPackageName = missingPkg.Name;
                    ShowBlockingProgressBar($"Downloading {_currentInstallingPackageName}...", 0.2f);
                    _currentRequest = Client.Add($"{missingPkg.Name}@{missingPkg.Version}");
                    return;
                }

                // C. PRE-COMPILE CONFIGURATION (STEP 1 FINISH LINE)
                if (!IsDefineSymbolSet(GlobalConst.BFunDefineSympol))
                {
                    ShowBlockingProgressBar("Configuring Defines & TMP...", 0.6f);
                    ImportTMPEssentials();
                    AddDefineSymbol(GlobalConst.BFunDefineSympol);

                    // Dừng tại đây để Unity Recompile. 
                    // Sau khi Recompile, BFunSetupWindow sẽ thấy Define đã có và hiện nút Step 2.
                    StopAndCleanUp();
                    return;
                }

                // Nếu đã đến đây nghĩa là Step 1 đã xong, dừng monitor.
                StopAndCleanUp();
            }
            catch (Exception ex)
            {
                Debug.LogError("❌ [System Error] " + ex.Message);
                StopAndCleanUp();
            }
        }

        private static void ShowBlockingProgressBar(string info, float progress)
        {
            EditorUtility.DisplayProgressBar("BFun Installer (Do not close)", info, progress);
        }

        private static void StopAndCleanUp()
        {
            EditorPrefs.SetBool(Key_AutoInstall, false);
            if (_isMonitorRunning)
            {
                EditorApplication.update -= InstallMonitorLoop;
                _isMonitorRunning = false;
            }
            EditorUtility.ClearProgressBar();
            Application.runInBackground = _originalRunInBackground;
        }

   
        // --- HELPERS (Dành cho Package Manager & Helper) ---

        // Thêm lại hàm bị thiếu này
        public static string GetCurrentRenderPipelineName()
        {
            var rpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (rpAsset == null) return "Built-in Render Pipeline";
            return rpAsset.GetType().Name;
        }

        private class PkgInfo { public string Name; public string Version; }
        private static PkgInfo GetFirstMissingPackage()
        {
            var required = GetRequiredPackages();
            var manifest = GetManifestContent();
            foreach (var kvp in required)
            {
                if (!manifest.Contains($"\"{kvp.Key}\"")) return new PkgInfo { Name = kvp.Key, Version = kvp.Value };
            }
            return null;
        }

        public static Dictionary<string, string> GetRequiredPackages()
        {
            var pkgs = new Dictionary<string, string>();
            bool isUnity6 = false;
#if UNITY_2023_2_OR_NEWER
            isUnity6 = true;
#endif
            pkgs.Add("com.unity.collections", isUnity6 ? "2.5.7" : "2.6.3");
            pkgs.Add("com.unity.mathematics", isUnity6 ? "1.3.3" : "1.2.6");
            pkgs.Add("com.unity.burst", isUnity6 ? "1.8.25" : "1.8.26");
            pkgs.Add("com.unity.addressables", isUnity6 ? "2.7.4" : "1.22.3");
            pkgs.Add("com.unity.textmeshpro", "3.0.9");
            pkgs.Add("com.unity.editorcoroutines", "1.0.0");
            return pkgs;
        }

        public static string GetManifestContent()
        {
            string path = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            return File.Exists(path) ? File.ReadAllText(path) : "";
        }

        private static bool IsDefineSymbolSet(string define)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            return defines.Contains(define);
        }

        private static void AddDefineSymbol(string define)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            if (!defines.Contains(define))
            {
                defines += ";" + define;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
            }
        }

        private static void ImportTMPEssentials()
        {
            string packageName = "com.unity.textmeshpro";
#if UNITY_2023_2_OR_NEWER
            packageName = "com.unity.ugui";
#endif
            try
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
                if (packageInfo != null)
                {
                    string packagePath = Path.Combine(packageInfo.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
                    if (File.Exists(packagePath) && !Directory.Exists("Assets/TextMesh Pro"))
                    {
                        AssetDatabase.ImportPackage(packagePath, false);
                    }
                }
            }
            catch { }
        }
    }
}
#endif