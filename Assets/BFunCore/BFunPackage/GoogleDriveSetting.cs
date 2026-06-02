#if UNITY_EDITOR
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic; // Cần cho Dictionary
using System.ComponentModel;

[CreateAssetMenu(fileName = "GoogleDriveSettings", menuName = "Unity Package Manager/Google Drive Settings")]
public class GoogleDriveSettings : ScriptableObject
{
    [Title("Google Drive API Configuration")]
    [Tooltip("ID của thư mục trên Google Drive chứa các file .unitypackage.")]
    public string googleDriveFolderId;

    [Tooltip("Client ID từ Google Cloud Console.")]
    public string googleClientId;

    [Tooltip("Client Secret từ Google Cloud Console. Sẽ được hiển thị dưới dạng ẩn.")]
    [PasswordPropertyText(true)]
    public string googleClientSecret;

    [Title("Package Installation Check")]
    [InfoBox("Ánh xạ Tên Package (như trên Drive, không có version) tới đường dẫn thư mục mà nó tạo ra để kiểm tra đã cài đặt hay chưa.")]
    [DictionaryDrawerSettings(KeyLabel = "Package Base Name", ValueLabel = "Install Path (e.g., Assets/Plugins/...)")]
    public Dictionary<string, string> packageInstallPaths = new Dictionary<string, string>();
}
#endif