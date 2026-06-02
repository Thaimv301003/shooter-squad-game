using UnityEditor;
using UnityEngine;

// Attribute này đảm bảo class sẽ được khởi tạo khi Editor load
[InitializeOnLoad]
public class EditorStartup
{
    // Đây là một static constructor, nó sẽ được gọi tự động
    static EditorStartup()
    {
        // Kiểm tra xem chúng ta đã chạy code này trong session hiện tại chưa
        // SessionState sẽ bị xóa khi bạn tắt và mở lại Unity
        if (!SessionState.GetBool("FirstTimeLaunch", false))
        {
            // Đánh dấu là đã chạy lần đầu trong session này
            SessionState.SetBool("FirstTimeLaunch", true);


            PackageManagerSettings packageManagerSettings = AssetDatabase.LoadAssetAtPath<PackageManagerSettings>(GlobalConst.SettingFolder + "/Package Setting.asset");
            packageManagerSettings.RefreshDrivePackages();
        }
    }
}