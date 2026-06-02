#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BFunCoreKit
{
    public class BFunSetupWindow : OdinEditorWindow
    {
        [MenuItem("Tools/BFun Core Kit/Setup Wizard")]
        public static void OpenWindow()
        {
            var window = GetWindow<BFunSetupWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(650, 750);
            window.titleContent = new GUIContent("BFun Setup");
        }

        [InitializeOnLoadMethod]
        private static void AutoOpen()
        {
            // 1. Kích hoạt lại quy trình cài đặt nếu đang chạy dở (sau khi compile)
            BFunInstallerCore.ResumeInstallProcess();

            // 2. Mở window nếu chưa xong (Kiểm tra file Marker)
            if (File.Exists(GlobalConst.MarkerPath)) return;
            EditorApplication.delayCall += () => OpenWindow();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshPackageStatus();
        }

        // --- UI HEADER ---
        [Title("Welcome to BFun Core Kit", "Hệ thống cài đặt tự động", TitleAlignments.Centered)]

        [PropertySpace(10)]
        [BoxGroup("Environment Info", CenterLabel = true)]
        [DisplayAsString, HideLabel, GUIColor(0.6f, 1f, 0.6f)]
        [PropertyOrder(-1)]
        public string CurrentRenderPipeline;

        [PropertySpace(15)]

        // --- PACKAGE LIST ---
        [BoxGroup("System Status", CenterLabel = true)]
        [TableList(ShowIndexLabels = false, IsReadOnly = true, AlwaysExpanded = true)]
        [ShowInInspector, HideLabel]
        private List<PackageStatus> _packages = new List<PackageStatus>();

        // ================================================================================================
        //                                      LOGIC TRẠNG THÁI
        // ================================================================================================

        // Step 1 Xong khi: Không còn gói nào thiếu VÀ Define Symbol đã được add vào PlayerSettings
        private bool IsStep1Complete => !HasMissingPackages && IsDefineSymbolSet(GlobalConst.BFunDefineSympol);

        // Step 2 Xong khi: File Marker tồn tại (File này được tạo sau khi bấm nút Step 2)
        private bool IsStep2Complete => File.Exists(GlobalConst.MarkerPath);

        private bool IsAutoInstalling => EditorPrefs.GetBool(BFunInstallerCore.Key_AutoInstall, false);
        private bool HasMissingPackages => _packages.Any(x => !x.IsInstalled);


        // ================================================================================================
        //                                      BUTTONS (STEP 1 -> STEP 2)
        // ================================================================================================

        // --- BUTTON STEP 1: INSTALL PACKAGES ---
        // Chỉ hiện khi Step 1 chưa xong.
        [Button(ButtonSizes.Large, Name = "@InstallButtonName", Icon = SdfIconType.Download)]
        [HideIf("IsStep1Complete")]
        [DisableIf("IsAutoInstalling")] // Disable nút khi đang chạy tiến trình
        [PropertySpace(20)]
        [GUIColor("@IsAutoInstalling ? Color.yellow : new Color(1f, 0.6f, 0.6f)")]
        public void StartStep1()
        {
            // Gọi hàm cài đặt bên Core. Core sẽ cài gói -> Add Define -> Recompile.
            BFunInstallerCore.StartAutoInstall();
            Repaint();
        }

        // --- BUTTON STEP 2: GENERATE ASSETS ---
        // Chỉ hiện khi Step 1 đã XONG và Step 2 CHƯA xong.
        [Button(ButtonSizes.Large, Name = "Step 2: Generate Assets & Config", Icon = SdfIconType.Magic)]
        [ShowIf("@IsStep1Complete && !IsStep2Complete")]
        [PropertySpace(20)]
        [GUIColor(0.4f, 0.8f, 1f)] // Màu xanh dương
        public void StartStep2()
        {
            try
            {
                EditorUtility.DisplayProgressBar("BFun Setup", "Generating Assets...", 0.5f);

                // 1. Load BFun Setting Asset
                string settingPath = GlobalConst.SettingFolder + "/BFun Setting.asset";
                BFunSetting setting = AssetDatabase.LoadAssetAtPath<BFunSetting>(settingPath);

                if (setting == null)
                {
                    EditorUtility.DisplayDialog("Error", $"Không tìm thấy file Setting tại: {settingPath}", "OK");
                    return;
                }

                // 2. Gọi hàm tạo Asset
                setting.GenerateAllAssets();

                // 3. Tạo file Marker để xác nhận hoàn tất
                File.WriteAllText(GlobalConst.MarkerPath, $"Installed on {System.DateTime.Now}");

                // 4. Clean up & Refresh
                EditorPrefs.SetBool(BFunInstallerCore.Key_AutoInstall, false); // Đảm bảo tắt cờ
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("BFun Setup", "Cài đặt và cấu hình hoàn tất!", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BFun Setup Error] {ex.Message}");
                EditorUtility.DisplayDialog("Error", "Lỗi khi tạo Assets. Xem Console.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // --- COMPLETED STATE ---
        [Button(ButtonSizes.Large, Name = "All Setup Completed ✔", Icon = SdfIconType.CheckCircleFill)]
        [ShowIf("IsStep2Complete")]
        [PropertySpace(20)]
        [GUIColor(0.6f, 1f, 0.6f)]
        public void SetupDone()
        {
            // Nút này chỉ để hiển thị trạng thái, bấm vào không làm gì hoặc có thể đóng window
            // Close(); 
        }

        // ================================================================================================
        //                                      HELPERS
        // ================================================================================================

        private string InstallButtonName => IsAutoInstalling ? "Installing... (Wait for Recompile)" : "Step 1: Install Packages & Defines";

        public void RefreshPackageStatus()
        {
            CurrentRenderPipeline = "Detected: " + BFunInstallerCore.GetCurrentRenderPipelineName();

            _packages.Clear();
            var required = BFunInstallerCore.GetRequiredPackages();
            var manifestContent = BFunInstallerCore.GetManifestContent();

            foreach (var pkg in required)
            {
                bool installed = manifestContent.Contains($"\"{pkg.Key}\"");
                _packages.Add(new PackageStatus
                {
                    Name = pkg.Key,
                    Version = pkg.Value,
                    IsInstalled = installed
                });
            }

            if (IsAutoInstalling)
            {
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            if (IsAutoInstalling)
            {
                RefreshPackageStatus();
            }
        }

        // Helper check define symbol (Dùng trực tiếp PlayerSettings để không phụ thuộc access modifier bên Core)
        private bool IsDefineSymbolSet(string define)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            return defines.Contains(define);
        }
    }

    [System.Serializable]
    public class PackageStatus
    {
        [TableColumnWidth(300, Resizable = false)]
        [DisplayAsString]
        [LabelText("Package Name")]
        public string Name;

        [TableColumnWidth(100, Resizable = false)]
        [DisplayAsString]
        public string Version;

        [ShowInInspector]
        [TableColumnWidth(130, Resizable = false)]
        [GUIColor("@IsInstalled ? Color.green : new Color(1f, 0.4f, 0.4f)")]
        [DisplayAsString]
        [LabelText("Status")]
        public string StatusStr => IsInstalled ? "✔ INSTALLED" : "✘ MISSING";

        [HideInInspector]
        public bool IsInstalled;
    }
}
#endif