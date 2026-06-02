using UnityEngine;
using Sirenix.OdinInspector;
using System.IO;

[CreateAssetMenu(fileName = "GoogleSheetsConfig", menuName = "BFun/Google Sheets Config")]
public class GoogleSheetsConfig : ScriptableObject
{
    [Title("Google Sheets & Drive Config")]

    [BoxGroup("Settings")]
    [LabelText("Spreadsheet ID"), Required]
    public string SpreadsheetId = "";

    [BoxGroup("Settings")]
    [LabelText("Sheet Name"), Required]
    public string SheetName = "BFunBugReport";

    // 🔥 THÊM MỚI 🔥
    [BoxGroup("Settings")]
    [LabelText("Drive Folder ID")]
    [InfoBox("ID của Folder trên Google Drive (Lấy trên URL)")]
    [Required]
    public string DriveFolderId = "";

    [BoxGroup("Settings")]
    [LabelText("Credential Path"), FilePath(Extensions = "json", ParentFolder = "Assets"), Required]
    public string CredentialRelativePath = "Editor/GoogleDriveAPICredentials.json";

    public string GetCredentialPath()
    {
        return Path.Combine(Application.dataPath, CredentialRelativePath);
    }
}