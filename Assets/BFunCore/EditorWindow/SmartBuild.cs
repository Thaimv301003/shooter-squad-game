#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor.Build.Reporting;

namespace BFunCoreKit
{
    public class SmartBuild : EditorWindow
    {
        // --- BIẾN CHO CỬA SỔ NHẬP PASSWORD ANDROID ---
        private string keystorePass = "";
        private string keyaliasPass = "";
        private bool buildAABForWindow = false;

        // ------------------------------------------------------------------------
        // --- ĐIỂM BẮT ĐẦU CHÍNH (MENU ITEM) ---
        // ------------------------------------------------------------------------

        public static void ShowSmartBuildWindow()
        {
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;

            if (activeTarget == BuildTarget.Android)
            {
                HandleAndroidBuild();
            }
            else if (activeTarget == BuildTarget.iOS)
            {
                HandleIOSBuild();
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Unsupported Build Target",
                    $"⚠️ Platform hiện tại ({activeTarget}) không được hỗ trợ.\n\nVui lòng chuyển sang Android hoặc iOS.",
                    "OK"
                );
            }
        }

        // ------------------------------------------------------------------------
        // --- LUỒNG XỬ LÝ BUILD CHO ANDROID ---
        // ------------------------------------------------------------------------

        private static void HandleAndroidBuild()
        {
            if (!CheckAndroidKeystore()) return;

            if (!string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) &&
                !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            {
                PromptAndExecuteAndroidBuild(PlayerSettings.Android.keystorePass, PlayerSettings.Android.keyaliasPass);
            }
            else
            {
                GetWindow<SmartBuild>("Enter Keystore Passwords");
            }
        }

        private static void PromptAndExecuteAndroidBuild(string ksPass, string aliasPass)
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Chọn loại Build",
                "Bạn muốn build ra file APK hay AAB?",
                "APK",    // button 0
                "Hủy",   // button 1
                "AAB"     // button 2
            );

            if (choice == 1) return; // Người dùng nhấn Hủy

            bool buildAAB = (choice == 2);
            string extension = buildAAB ? "aab" : "apk";

            // Tạo tên file mặc định
            string productName = PlayerSettings.productName.Replace(" ", "");
            string version = PlayerSettings.bundleVersion;
            int bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            string defaultName = $"{productName}-v{version}-{bundleVersionCode}";

            // Hiển thị cửa sổ chọn nơi lưu file
            string buildPath = EditorUtility.SaveFilePanel(
                $"Chọn nơi lưu file .{extension}",
                "Builds/Android", // Thư mục mặc định
                defaultName,
                extension
            );

            // Nếu người dùng không chọn đường dẫn (nhấn cancel) thì dừng lại
            if (string.IsNullOrEmpty(buildPath))
            {
                Debug.Log("Hủy build vì người dùng không chọn đường dẫn.");
                return;
            }

            DoBuildAndroid(ksPass, aliasPass, buildAAB, buildPath);
        }

        private static void DoBuildAndroid(string ksPass, string aliasPass, bool buildAAB, string buildPath)
        {
            Debug.Log($"Bắt đầu build Android tại: {buildPath}");

            PlayerSettings.Android.keystorePass = ksPass;
            PlayerSettings.Android.keyaliasPass = aliasPass;
            EditorUserBuildSettings.buildAppBundle = buildAAB;

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            HandleBuildReport(report, buildPath, () =>
            {
                Debug.LogError("❌ Sai mật khẩu Keystore!");
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                GetWindow<SmartBuild>("Enter Keystore Passwords");
            });
        }


        // ------------------------------------------------------------------------
        // --- LUỒNG XỬ LÝ BUILD CHO IOS ---
        // ------------------------------------------------------------------------

        private static void HandleIOSBuild()
        {
            if (!CheckIOSSettings()) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Xác nhận Build iOS",
                "Quá trình này sẽ tạo ra một project Xcode.\n\nBạn có muốn tiếp tục không?",
                "Tiếp tục",
                "Hủy"
            );

            if (confirm)
            {
                PromptAndExecuteIOSBuild();
            }
        }

        private static void PromptAndExecuteIOSBuild()
        {
            // Hiển thị cửa sổ chọn thư mục để lưu project
            string buildPath = EditorUtility.SaveFolderPanel(
                "Chọn thư mục để lưu Project Xcode",
                "Builds", // Thư mục mặc định
                ""
            );

            // Nếu người dùng không chọn đường dẫn thì dừng lại
            if (string.IsNullOrEmpty(buildPath))
            {
                Debug.Log("Hủy build vì người dùng không chọn đường dẫn.");
                return;
            }

            DoBuildIOS(buildPath);
        }

        private static void DoBuildIOS(string buildPath)
        {
            Debug.Log($"Bắt đầu tạo project Xcode tại: {buildPath}");

            // Xóa thư mục cũ nếu tồn tại để đảm bảo build sạch
            if (Directory.Exists(buildPath))
            {
                Directory.Delete(buildPath, true);
            }
            Directory.CreateDirectory(buildPath);

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            HandleBuildReport(report, buildPath);
        }


        // ------------------------------------------------------------------------
        // --- CÁC HÀM KIỂM TRA VÀ TIỆN ÍCH CHUNG ---
        // ------------------------------------------------------------------------

        static bool CheckAndroidKeystore()
        {
            string keystorePath = PlayerSettings.Android.keystoreName;
            string aliasName = PlayerSettings.Android.keyaliasName;
            bool usingDefault = string.IsNullOrEmpty(keystorePath) || keystorePath.EndsWith("user.keystore") || string.IsNullOrEmpty(aliasName) || aliasName == "androiddebugkey";
            if (usingDefault)
            {
                EditorUtility.DisplayDialog("Keystore không hợp lệ", "⚠️ Bạn đang dùng keystore mặc định. Vui lòng cấu hình custom keystore trong Project Settings > Player > Publishing Settings.", "OK");
                return false;
            }
            return true;
        }

        static bool CheckIOSSettings()
        {
            string bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            if (string.IsNullOrEmpty(bundleId) || bundleId == "com.Company.ProductName")
            {
                EditorUtility.DisplayDialog("Cài đặt iOS không hợp lệ", "⚠️ Bundle Identifier chưa được thiết lập. Vui lòng vào Project Settings > Player > Other Settings.", "OK");
                return false;
            }
            if (string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID))
            {
                EditorUtility.DisplayDialog("Cài đặt iOS không hợp lệ", "⚠️ Apple Developer Team ID chưa được thiết lập. Vui lòng vào Project Settings > Player > Other Settings.", "OK");
                return false;
            }
            return true;
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        }

        private static void HandleBuildReport(BuildReport report, string outputPath, System.Action onPasswordError = null)
        {
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"✅ Build thành công! Output tại: {outputPath}");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                // Lấy tất cả các message có type là Error từ tất cả các step
                var errorMessages = report.steps
                    .SelectMany(step => step.messages)
                    .Where(msg => msg.type == LogType.Error)
                    .Select(msg => msg.content)
                    .ToList();

                // Kiểm tra xem trong các message lỗi có chứa lỗi sai password không
                bool wrongKey = errorMessages.Any(e =>
                    e.Contains("password was incorrect") ||
                    e.Contains("Keystore was tampered") ||
                    e.Contains("Password verification failed"));

                if (wrongKey && onPasswordError != null)
                {
                    onPasswordError.Invoke();
                }
                else
                {
                    Debug.LogError($"❌ Build thất bại. Vui lòng kiểm tra lỗi trong Console.");
                }
            }
        }
    }
}
#endif

    // ------------------------------------------------------------------------
    // --- GIAO DIỆN C