#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Sirenix.Utilities.Editor;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum BugType { Editor, UI, Visual, Sound, Crash, Ads, CoreKit }
public enum BugStatus
{
    ReOpened,
    New,
    PendingReview,
    Fixed,
    WontFix
}

[Serializable]
public class BugItem
{
    [TableColumnWidth(60, Resizable = false)]
    [Button("OPEN", ButtonSizes.Small), GUIColor(0.4f, 0.8f, 1)]
    public void Detail()
    {
#if UNITY_EDITOR
        BugDetailWindow.Open(this);
#endif
    }

    [HideInTables] public string ID;
    [TableColumnWidth(200), DisplayAsString(false)] public string BugName;
    [HideInTables] public string Description;
    [TableColumnWidth(80), DisplayAsString(false)] public BugType Type;
    [TableColumnWidth(100), DisplayAsString(false), GUIColor("GetStatusColor")] public BugStatus Status;

    private UnityEngine.Color GetStatusColor()
    {
        switch (Status)
        {
            case BugStatus.New: return new UnityEngine.Color(1f, 0.5f, 0.5f);
            case BugStatus.ReOpened: return new UnityEngine.Color(1f, 0.3f, 0.3f);
            case BugStatus.PendingReview: return new UnityEngine.Color(1f, 0.8f, 0.2f);
            case BugStatus.Fixed: return new UnityEngine.Color(0.4f, 1f, 0.4f);
            case BugStatus.WontFix: return UnityEngine.Color.gray;
            default: return UnityEngine.Color.white;
        }
    }

    [HideInTables] public string UnityVersion;
    [TableColumnWidth(150), DisplayAsString(false)] public string Uploader;
    [TableColumnWidth(120), DisplayAsString(false)] public string ReportTime;
    [HideInTables] public string FixSolution;
    [HideInTables] public string Fixer;
    [HideInTables] public int RowIndex;
    [HideInTables] public string AttachmentIDs;
}

[CreateAssetMenu(fileName = "BugReportManager", menuName = "Tools/Bug Report Manager")]
public class BugReportManager : ScriptableObject
{
    [HideInInspector]
    public GoogleSheetsConfig configSource;

    [OnInspectorInit] private void InitBugSearch() => UpdateBugSearch();
    private void OnEnable() => UpdateBugSearch();

    [HideInInspector] public string BugSearchTerm;

    // List gốc (Ẩn đi)
    [HideInInspector]
    public List<BugItem> bugList = new List<BugItem>();

    // List hiển thị (Vẽ Toolbar thủ công)
    [Space(10)]
    [OnInspectorGUI("DrawBugHeader", append: false)]
    [TableList(DrawScrollView = true, IsReadOnly = true)]
    [ListDrawerSettings(IsReadOnly = true, HideAddButton = true, HideRemoveButton = true, DraggableItems = false, Expanded = true, ShowPaging = false, NumberOfItemsPerPage = 20)]
    [HideReferenceObjectPicker]
    [HideLabel]
    [NonSerialized]
    public List<BugItem> displayBugList = new List<BugItem>();

    private void DrawBugHeader()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Bug Reports", EditorStyles.boldLabel);
        if (displayBugList != null) GUILayout.Label($"({displayBugList.Count})", EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
        try
        {
            // Code giống hệt bên Package Manager
            string newTerm = SirenixEditorGUI.SearchField(GUILayoutUtility.GetRect(200, 16), BugSearchTerm);
            if (newTerm != BugSearchTerm) { BugSearchTerm = newTerm; UpdateBugSearch(); }
        }
        catch { }
        GUILayout.EndHorizontal();
    }

    public void UpdateBugSearch()
    {
        if (displayBugList == null) displayBugList = new List<BugItem>();
        displayBugList.Clear();
        if (bugList == null) return;
        if (string.IsNullOrEmpty(BugSearchTerm)) displayBugList.AddRange(bugList);
        else
        {
            foreach (var b in bugList)
            {
                // Tìm theo Tên, ID, hoặc Người up
                if ((b.BugName != null && b.BugName.IndexOf(BugSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.ID != null && b.ID.IndexOf(BugSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.Uploader != null && b.Uploader.IndexOf(BugSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0))
                    displayBugList.Add(b);
            }
        }
    }


    public void SyncData()
    {
#if UNITY_EDITOR
        _ = FetchFromGoogleSheetAsync();
#endif
    }

    [Button(ButtonSizes.Large), PropertyOrder(1)]
    public void ReportBug()
    {
#if UNITY_EDITOR
        BugReporterWindow.OpenWindow(this);
#endif
    }

    [OnInspectorInit]
    void OnInitGUI()
    {
        configSource = AssetDatabase.LoadAssetAtPath<GoogleSheetsConfig>("Assets/BFunCore/BugReport/GoogleSheetsConfig.asset");
        SyncData();
    }

    private DriveService GetDriveService(GoogleCredential credential)
    {
        return new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = credential, ApplicationName = "Unity Bug Reporter" });
    }

    // 1. UPLOAD ASYNC
    public async Task<string> UploadFileToDriveAsync(string localPath, string targetFileName)
    {
        if (!System.IO.File.Exists(localPath)) return "";
        string jsonPath = configSource.GetCredentialPath();
        string folderId = configSource.DriveFolderId;

        return await Task.Run(() => {
            try
            {
                GoogleCredential credential;
                using (var stream = new System.IO.FileStream(jsonPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(DriveService.Scope.DriveFile);
                }
                var driveService = GetDriveService(credential);
                var fileMetadata = new Google.Apis.Drive.v3.Data.File() { Name = targetFileName, Parents = new List<string> { folderId } };
                using (var stream = new System.IO.FileStream(localPath, FileMode.Open))
                {
                    var request = driveService.Files.Create(fileMetadata, stream, "");
                    request.Fields = "id"; request.SupportsAllDrives = true;
                    var result = request.Upload();
                    if (result.Status == Google.Apis.Upload.UploadStatus.Failed) return "";
                    return request.ResponseBody?.Id ?? "";
                }
            }
            catch (Exception ex) { Debug.LogError(ex); return ""; }
        });
    }

    // 2. GET NAME ASYNC
    public async Task<string> GetFileNameFromDriveAsync(string fileId)
    {
        if (string.IsNullOrEmpty(fileId)) return "Unknown";
        string jsonPath = configSource.GetCredentialPath();
        return await Task.Run(async () => {
            try
            {
                GoogleCredential credential;
                using (var stream = new System.IO.FileStream(jsonPath, FileMode.Open, FileAccess.Read)) credential = GoogleCredential.FromStream(stream).CreateScoped(DriveService.Scope.DriveReadonly);
                var driveService = GetDriveService(credential);
                var request = driveService.Files.Get(fileId);
                request.Fields = "name"; request.SupportsAllDrives = true;
                var file = await request.ExecuteAsync();
                return file.Name;
            }
            catch { return "Unknown"; }
        });
    }

    // 🔥 3. DOWNLOAD TO PATH ASYNC (Hàm mới) 🔥
    public async Task<bool> DownloadFileToPathAsync(string fileId, string savePath)
    {
        if (string.IsNullOrEmpty(fileId)) return false;
        string jsonPath = configSource.GetCredentialPath();

        return await Task.Run(async () => {
            try
            {
                GoogleCredential credential;
                using (var stream = new System.IO.FileStream(jsonPath, FileMode.Open, FileAccess.Read)) credential = GoogleCredential.FromStream(stream).CreateScoped(DriveService.Scope.DriveReadonly);
                var driveService = GetDriveService(credential);

                var request = driveService.Files.Get(fileId);
                request.SupportsAllDrives = true;

                // Ghi vào đường dẫn người dùng chọn
                using (var stream = new System.IO.FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    await request.DownloadAsync(stream);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Download Failed: {ex.Message}");
                return false;
            }
        });
    }

    // 4. DOWNLOAD BYTES ASYNC (For Preview)
    public async Task<byte[]> DownloadBytesFromDriveAsync(string fileId)
    {
        if (string.IsNullOrEmpty(fileId)) return null;
        string jsonPath = configSource.GetCredentialPath();
        return await Task.Run(async () => {
            try
            {
                GoogleCredential credential;
                using (var stream = new System.IO.FileStream(jsonPath, FileMode.Open, FileAccess.Read)) credential = GoogleCredential.FromStream(stream).CreateScoped(DriveService.Scope.DriveReadonly);
                var driveService = GetDriveService(credential);
                using (var stream = new MemoryStream())
                {
                    var request = driveService.Files.Get(fileId);
                    request.SupportsAllDrives = true;
                    await request.DownloadAsync(stream);
                    return stream.ToArray();
                }
            }
            catch { return null; }
        });
    }

    // 5. FETCH ASYNC
    private async Task FetchFromGoogleSheetAsync()
    {
        if (configSource == null || !System.IO.File.Exists(configSource.GetCredentialPath())) return;
        string jsonPath = configSource.GetCredentialPath();
        string sheetId = configSource.SpreadsheetId;
        string sheetName = configSource.SheetName;
        await Task.Run(() => {
            try
            {
                GoogleCredential credential;
                using (var stream = new System.IO.FileStream(jsonPath, FileMode.Open, FileAccess.Read)) credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
                var service = new SheetsService(new BaseClientService.Initializer() { HttpClientInitializer = credential, ApplicationName = "Unity Bug Reporter" });
                string range = $"'{sheetName}'!A:K";
                var values = service.Spreadsheets.Values.Get(sheetId, range).Execute().Values;
                EditorApplication.delayCall += () => { if (values != null && values.Count > 0) { ParseData(values); } };
            }
            catch (Exception e) { Debug.LogError($"Sheet Error: {e.Message}"); }
        });
    }

    private void ParseData(IList<IList<object>> rawData)
    {
        bugList.Clear();
        for (int i = 0; i < rawData.Count; i++)
        {
            if (i == 0) continue;
            var row = rawData[i];
            BugItem item = new BugItem();
            item.RowIndex = i + 1;
            item.ID = (row.Count > 0) ? row[0].ToString() : "";
            item.BugName = (row.Count > 1) ? row[1].ToString() : "";
            item.Description = (row.Count > 2) ? row[2].ToString() : "";
            string typeStr = (row.Count > 3) ? row[3].ToString() : "Gameplay";
            if (!Enum.TryParse(typeStr, true, out item.Type)) item.Type = BugType.Editor;
            string statusStr = (row.Count > 4) ? row[4].ToString() : "New";
            if (!Enum.TryParse(statusStr, true, out item.Status)) item.Status = BugStatus.New;
            item.UnityVersion = (row.Count > 5) ? row[5].ToString() : "";
            item.Uploader = (row.Count > 6) ? row[6].ToString() : "";
            item.FixSolution = (row.Count > 7) ? row[7].ToString() : "";
            item.Fixer = (row.Count > 8) ? row[8].ToString() : "";

            // --- SỬA Ở ĐÂY: Xóa PM và AM ---
            string rawTime = (row.Count > 9) ? row[9].ToString() : "";
            item.ReportTime = rawTime.Replace(" PM", "").Replace(" AM", "").Trim();
            // -------------------------------

            item.AttachmentIDs = (row.Count > 10) ? row[10].ToString() : "";
            bugList.Add(item);
        }
        bugList.Sort((a, b) => a.Status.CompareTo(b.Status));
        UpdateBugSearch();
        EditorUtility.SetDirty(this);
    }

    // 6. UPDATE ASYNC
    public async Task UpdateBugRowAsync(int rowIndex, string solution, string fixer, BugStatus newStatus, string currentAttachmentIDs, string newAttachmentIDs)
    {
        if (configSource == null) return;
        string jsonPath = configSource.GetCredentialPath();
        string sheetId = configSource.SpreadsheetId;
        string sheetName = configSource.SheetName;

        await Task.Run(() => {
            try
            {
                GoogleCredential credential;
                using (var stream = new System.IO.FileStream(jsonPath, FileMode.Open, FileAccess.Read)) credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                var service = new SheetsService(new BaseClientService.Initializer() { HttpClientInitializer = credential, ApplicationName = "Unity Bug Reporter" });

                var statusValue = new ValueRange() { Values = new List<IList<object>> { new List<object> { newStatus.ToString() } } };
                var statusReq = service.Spreadsheets.Values.Update(statusValue, sheetId, $"'{sheetName}'!E{rowIndex}");
                statusReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                statusReq.Execute();

                var fixValues = new ValueRange() { Values = new List<IList<object>> { new List<object> { solution, fixer } } };
                var fixReq = service.Spreadsheets.Values.Update(fixValues, sheetId, $"'{sheetName}'!H{rowIndex}:I{rowIndex}");
                fixReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                fixReq.Execute();

                if (!string.IsNullOrEmpty(newAttachmentIDs))
                {
                    string finalIDs = string.IsNullOrEmpty(currentAttachmentIDs) ? newAttachmentIDs : currentAttachmentIDs + "," + newAttachmentIDs;
                    var attValues = new ValueRange() { Values = new List<IList<object>> { new List<object> { finalIDs } } };
                    var attReq = service.Spreadsheets.Values.Update(attValues, sheetId, $"'{sheetName}'!K{rowIndex}");
                    attReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    attReq.Execute();
                }
            }
            catch (Exception ex) { Debug.LogError($"Update Failed: {ex.Message}"); }
        });
    }
}
#endif