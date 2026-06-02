#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class BugReporterWindow : OdinEditorWindow
{
    // ... (Giữ nguyên các biến khai báo cũ) ...
    private BugReportManager _manager;

    [Title("Bug Name")]
    [BoxGroup("Info"), PropertyOrder(1), TextArea(2, 10), Required, HideLabel] public string BugName;
    [Title("Bug Description")]
    [BoxGroup("Info"), PropertyOrder(2), TextArea(5, 10), Required, HideLabel] public string Description;

    [BoxGroup("Attachments"), PropertyOrder(2.5f), LabelText("Files to Upload")]
    [ListDrawerSettings(Expanded = true, DraggableItems = false, ShowIndexLabels = false)]
    public List<string> AttachmentPaths = new List<string>();

    // ... (Giữ nguyên hàm DrawDropZone và ClearFiles) ...
    [BoxGroup("Attachments"), PropertyOrder(2.6f), OnInspectorGUI]
    private void DrawDropZone()
    {
        // ... (Code cũ giữ nguyên) ...
        var style = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
        GUILayout.Box("\n📂 DRAG FILES HERE", style, GUILayout.Height(60), GUILayout.ExpandWidth(true));
        Rect dropArea = GUILayoutUtility.GetLastRect();
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (!dropArea.Contains(evt.mousePosition)) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (string path in DragAndDrop.paths)
                {
                    if (File.Exists(path) && !AttachmentPaths.Contains(path)) AttachmentPaths.Add(path);
                }
            }
        }
    }

    [BoxGroup("Attachments"), PropertyOrder(2.7f), Button("❌ CLEAR FILES", ButtonSizes.Small), GUIColor(1f, 0.5f, 0.5f), ShowIf("@AttachmentPaths.Count > 0")]
    public void ClearFiles() { AttachmentPaths.Clear(); }

    // ... (Các biến Enum Type, Status...) ...
    [Title("Bug Type")]
    [BoxGroup("@AutoID"), PropertyOrder(1), EnumToggleButtons][HideLabel] public BugType Type = BugType.Editor;
    [HideInInspector]
    public BugStatus Status = BugStatus.New;
    [BoxGroup("Info"), HorizontalGroup("Info/Split"), PropertyOrder(4), ReadOnly, LabelText("Uploader")] public string UploaderEmail;
    [BoxGroup("Info"), HorizontalGroup("Info/Split"), PropertyOrder(5), ReadOnly] public string Version;
    [HideInInspector] public string AutoID;

    // --- CẬP NHẬT HÀM OPEN WINDOW ---
    public static void OpenWindow(BugReportManager manager)
    {
        // Để tiêu đề tạm là "Loading..." hoặc gì cũng được, nó sẽ bị đè ngay lập tức
        var window = GetWindow<BugReporterWindow>(true, "Bug Reporter", true);
        window._manager = manager;

        window.minSize = new Vector2(450, 750);
        window.maxSize = new Vector2(450, 750);

        // [MỚI] Cập nhật tiêu đề cửa sổ thành ID (vì OnEnable đã chạy rồi)
        window.titleContent = new GUIContent(window.AutoID);

        window.Show();
    }

    // --- CẬP NHẬT HÀM ON ENABLE ---
    protected override void OnEnable()
    {
        base.OnEnable();
        Version = Application.unityVersion;

        GenerateNewID(); // Sinh ID mới
        IdentifyUploader();

        if (_manager == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:BugReportManager");
            if (guids.Length > 0) _manager = AssetDatabase.LoadAssetAtPath<BugReportManager>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }

    // --- CẬP NHẬT HÀM GENERATE ID ---
    private void GenerateNewID()
    {
        // Sinh ID ngẫu nhiên
        AutoID = $"BUG-{UnityEngine.Random.Range(1000, 9999)}";

        // [MỚI] Gán ID này làm tiêu đề cửa sổ luôn
        this.titleContent = new GUIContent(AutoID);
    }

    // ... (Giữ nguyên phần IdentifyUploader, UploadBug, PushToGoogleSheetAsync) ...
    private async void IdentifyUploader()
    {
        UploaderEmail = CloudProjectSettings.userName;
        try
        {
            string[] guids = AssetDatabase.FindAssets("t:PackageManagerSettings");
            if (guids.Length > 0)
            {
                var settings = AssetDatabase.LoadAssetAtPath<PackageManagerSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (settings != null) { string mail = await settings.GetCurrentUserEmailAsync(); if (!string.IsNullOrEmpty(mail)) { UploaderEmail = mail; Repaint(); } }
            }
        }
        catch { }
    }

    [PropertyOrder(10), Button(ButtonSizes.Large, Icon = SdfIconType.CloudUpload), GUIColor(0, 1, 0), PropertySpace(20), LabelText(" UPLOAD BUG")]
    public async void UploadBug()
    {
        if (_manager == null || _manager.configSource == null) { EditorUtility.DisplayDialog("Error", "Config missing!", "OK"); return; }
        if (string.IsNullOrEmpty(BugName) || string.IsNullOrEmpty(Description)) return;

        try
        {
            EditorUtility.DisplayProgressBar("Uploading...", "Processing...", 0.1f);
            List<string> uploadedIDs = new List<string>();
            if (AttachmentPaths.Count > 0)
            {
                for (int i = 0; i < AttachmentPaths.Count; i++)
                {
                    string path = AttachmentPaths[i];
                    if (!File.Exists(path)) continue;
                    float progress = 0.2f + ((float)i / AttachmentPaths.Count) * 0.6f;
                    string fileName = Path.GetFileName(path);
                    string remoteName = $"{AutoID}_{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}";
                    EditorUtility.DisplayProgressBar("Uploading...", $"Uploading: {fileName}...", progress);
                    string id = await _manager.UploadFileToDriveAsync(path, remoteName);
                    if (string.IsNullOrEmpty(id)) { EditorUtility.ClearProgressBar(); EditorUtility.DisplayDialog("Error", $"Failed to upload {fileName}", "OK"); return; }
                    uploadedIDs.Add(id);
                }
            }
            string finalIDs = string.Join(",", uploadedIDs);
            EditorUtility.DisplayProgressBar("Uploading...", "Writing to Sheet...", 0.9f);
            await PushToGoogleSheetAsync(finalIDs);

            EditorUtility.ClearProgressBar();
            _manager.SyncData();
            EditorUtility.DisplayDialog("Success", "Bug uploaded!", "OK");
            Close();
        }
        catch (Exception e) { EditorUtility.ClearProgressBar(); EditorUtility.DisplayDialog("Error", e.Message, "OK"); Debug.LogError(e); }
    }

    private async Task PushToGoogleSheetAsync(string attachmentIDs)
    {
        var config = _manager.configSource;
        string jsonPath = config.GetCredentialPath();
        string sheetId = config.SpreadsheetId;
        string sheetName = config.SheetName;

        string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm tt");
        string prefix = $"<color=#cccccc>[{timestamp}]</color>";
        string emailPart = $"<color=#ffffff><b>{UploaderEmail}</b></color>";

        string initialLog = $"{prefix} <color=#ffffff><b>CREATED</b></color> by {emailPart}:\nNew issue reported.";

        var listObj = new List<object>() {
            AutoID, BugName, Description, Type.ToString(), Status.ToString(),
            Application.unityVersion, UploaderEmail, initialLog, UploaderEmail, timestamp, attachmentIDs
        };

        await Task.Run(() => {
            GoogleCredential credential;
            using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read)) credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            var service = new SheetsService(new BaseClientService.Initializer() { HttpClientInitializer = credential, ApplicationName = "Unity Bug Reporter" });
            var req = service.Spreadsheets.Values.Append(new ValueRange() { Values = new List<IList<object>> { listObj } }, sheetId, $"'{sheetName}'!A:K");
            req.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            req.Execute();
        });
    }
}
#endif