#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;

public class BugDetailWindow : OdinEditorWindow
{
    private BugItem _targetBug;
    private BugReportManager _managerFile;

    // --- PHẦN 1: THÔNG TIN CHUNG & FILE GỐC ---
    [BoxGroup(" ")]
    [HideLabel, InlineProperty]
    public BugInfoDisplay Info;

    [Serializable]
    public class BugInfoDisplay
    {
        [CustomValueDrawer("DrawBugHeader"), HideLabel]
        public string BugName;

        [HorizontalGroup("Header"), LabelText("ID")]
        [CustomValueDrawer("DrawBrightBox")]
        public string ID;

        private string DrawBugHeader(string value, GUIContent label)
        {
            if (string.IsNullOrEmpty(value)) return value;

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = false;

            EditorGUILayout.LabelField(value.ToUpper(), style, GUILayout.Height(36));
            return value;
        }

        [HorizontalGroup("Header"), LabelText("Reporter")]
        [CustomValueDrawer("DrawBrightBox")]
        public string Uploader;

        [HorizontalGroup("Header"), LabelText("Date")]
        [CustomValueDrawer("DrawBrightBox")]
        public string ReportTime;

        [HorizontalGroup("Meta")]
        [ShowInInspector, LabelText("Type")]
        [CustomValueDrawer("DrawStatusBox")]
        public string TypeBox => Type.ToString();
        [HideInInspector] public BugType Type;

        [HorizontalGroup("Meta")]
        [ShowInInspector, LabelText("Status")]
        [CustomValueDrawer("DrawStatusBox")]
        [GUIColor("GetStatusColor")]
        public string StatusBox => Status.ToString();
        [HideInInspector] public BugStatus Status;

        private Color GetStatusColor()
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

        [HorizontalGroup("Meta"), LabelText("Unity Ver")]
        [CustomValueDrawer("DrawBrightBox")]
        [PropertyOrder(1)]
        public string UnityVersion;


        [Title("Description", bold: false)]
        [HideLabel]
        [CustomValueDrawer("DrawBrightTextArea")]
        [PropertyOrder(2)]
        public string Description;

        [BoxGroup("Attachments"), ShowIf("HasAttachment"), LabelText("Original Files"), ListDrawerSettings(IsReadOnly = true, Expanded = true)]
        [PropertyOrder(2)]
        public List<AttachmentItem> Attachments = new List<AttachmentItem>();
        public bool HasAttachment => Attachments.Count > 0;

        [HideInInspector] public BugReportManager Manager;

        private string DrawStatusBox(string value, GUIContent label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.textField);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;

            Color targetColor = GUI.color;
            Color savedColor = GUI.color;
            Color savedContentColor = GUI.contentColor;

            GUI.color = Color.white;
            GUI.contentColor = targetColor;
            style.normal.textColor = targetColor;

            EditorGUILayout.LabelField(label, new GUIContent(value), style);

            GUI.color = savedColor;
            GUI.contentColor = savedContentColor;

            return value;
        }

        private string DrawBrightBox(string value, GUIContent label)
        {
            EditorGUILayout.LabelField(label.text, value, EditorStyles.textField);
            return value;
        }
        private string DrawBrightTextArea(string value, GUIContent label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.textArea);
            style.wordWrap = true;
            EditorGUILayout.LabelField(GUIContent.none, new GUIContent(value), style, GUILayout.MinHeight(125));
            return value;
        }

        [Serializable]
        public class AttachmentItem
        {
            [HorizontalGroup("Row"), DisplayAsString, HideLabel, GUIColor(0.6f, 0.8f, 1f)] public string FileName = "Loading...";
            [HideInInspector] public string FileID; [HideInInspector] public BugReportManager Mgr;
            [HideInInspector] public string Extension;

            public async void FetchName()
            {
                if (Mgr != null) { FileName = await Mgr.GetFileNameFromDriveAsync(FileID); Extension = Path.GetExtension(FileName).ToLower(); }
            }

            [HorizontalGroup("Row", Width = 80), Button("VIEW", ButtonSizes.Small), GUIColor(0, 1, 0), ShowIf("CanPreview")]
            public async void Preview()
            {
                if (Mgr == null) return;
                EditorUtility.DisplayProgressBar("Loading", "Preview...", 0.5f);
                byte[] data = await Mgr.DownloadBytesFromDriveAsync(FileID);
                EditorUtility.ClearProgressBar();
                if (data == null) return;
                if (IsImage) { Texture2D tex = new Texture2D(2, 2); tex.LoadImage(data); UniversalViewer.OpenImage(tex, FileName); }
                else if (IsText) { string txt = System.Text.Encoding.UTF8.GetString(data); UniversalViewer.OpenText(txt, FileName); }
            }

            [HorizontalGroup("Row", Width = 40), Button(SdfIconType.Download, ""), GUIColor(1f, 0.8f, 0.4f)]
            public async void DownloadAndSave()
            {
                if (Mgr == null) return;
                string ext = Path.GetExtension(FileName).Replace(".", "");
                string path = EditorUtility.SaveFilePanel("Save File", "", FileName, ext);
                if (string.IsNullOrEmpty(path)) return;
                EditorUtility.DisplayProgressBar("Downloading", "Saving...", 0.5f);
                bool success = await Mgr.DownloadFileToPathAsync(FileID, path);
                EditorUtility.ClearProgressBar();
                if (success) Application.OpenURL(path);
                else EditorUtility.DisplayDialog("Error", "Download Failed", "OK");
            }

            private bool IsImage => Extension == ".png" || Extension == ".jpg" || Extension == ".jpeg";
            private bool IsText => Extension == ".txt" || Extension == ".cs" || Extension == ".log" || Extension == ".json" || Extension == ".xml" || Extension == ".shader";
            private bool CanPreview => IsImage || IsText;
        }

        public BugInfoDisplay(BugItem item, BugReportManager mgr)
        {
            ID = item.ID;
            Uploader = item.Uploader;
            BugName = item.BugName;
            Description = item.Description;
            Type = item.Type;
            Status = item.Status;
            Manager = mgr;
            ReportTime = item.ReportTime;
            UnityVersion = item.UnityVersion;

            if (!string.IsNullOrEmpty(item.AttachmentIDs))
            {
                string[] ids = item.AttachmentIDs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in ids) { var att = new AttachmentItem { FileID = id, Mgr = mgr }; att.FetchName(); Attachments.Add(att); }
            }
        }
    }

    // --- PHẦN 2: HISTORY LOG (CUSTOM DRAW) ---
    [BoxGroup("Solution History"), LabelText("Fix Logs"), CustomValueDrawer("DrawAdvancedLog")]
    public string FixHistory;

    private struct LogBlock { public string Header; public string Body; public List<string> FileIDs; }
    private List<LogBlock> _parsedLogs = new List<LogBlock>();
    private Vector2 _logScroll;

    private void ParseLogHistory(string rawLog)
    {
        _parsedLogs.Clear();
        if (string.IsNullOrEmpty(rawLog)) return;
        string separator = "\n\n----------------------------------------\n";
        string[] entries = rawLog.Split(new string[] { separator }, StringSplitOptions.None);

        foreach (var entry in entries)
        {
            LogBlock block = new LogBlock { FileIDs = new List<string>() };
            string cleanEntry = entry;
            var match = Regex.Match(entry, @"\[\[ATTACH:(.*?)\]\]");
            if (match.Success)
            {
                string[] ids = match.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                block.FileIDs.AddRange(ids);
                cleanEntry = entry.Replace(match.Value, "").Trim();
            }
            int splitIndex = cleanEntry.IndexOf('\n');
            if (splitIndex > 0) { block.Header = cleanEntry.Substring(0, splitIndex).Trim(); block.Body = cleanEntry.Substring(splitIndex).Trim(); }
            else { block.Header = cleanEntry; block.Body = ""; }
            _parsedLogs.Add(block);
        }
    }

    private string DrawAdvancedLog(string value, GUIContent label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true, fontSize = 12 };
        GUIStyle bodyStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 12, richText = true };

        _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.MaxHeight(400), GUILayout.MinHeight(60));
        if (_parsedLogs.Count == 0)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("(No logs yet)", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }
        else
        {
            foreach (var block in _parsedLogs)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(block.Header, headerStyle);
                if (block.FileIDs.Count > 0)
                {
                    GUILayout.Space(3);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("  Files:", EditorStyles.miniBoldLabel, GUILayout.Width(45));
                    foreach (var fileID in block.FileIDs)
                    {
                        GUILayout.BeginHorizontal();
                        var oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0, 1, 0);
                        if (GUILayout.Button("VIEW", GUILayout.Height(20), GUILayout.Width(60))) OpenLogAttachment(fileID, false);
                        GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
                        if (GUILayout.Button("⬇", GUILayout.Height(20), GUILayout.Width(25))) OpenLogAttachment(fileID, true);
                        GUI.backgroundColor = oldColor;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(3);
                }
                if (!string.IsNullOrEmpty(block.Body))
                {
                    if (block.FileIDs.Count > 0) GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    EditorGUILayout.LabelField(block.Body, bodyStyle);
                }
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }
        EditorGUILayout.EndScrollView();
        return value;
    }

    private async void OpenLogAttachment(string fileID, bool forceDownload)
    {
        if (_managerFile == null) return;
        EditorUtility.DisplayProgressBar("Loading", "Checking File...", 0.2f);
        string name = await _managerFile.GetFileNameFromDriveAsync(fileID);
        string ext = Path.GetExtension(name).ToLower();
        EditorUtility.ClearProgressBar();

        if (forceDownload || (!IsImage(ext) && !IsText(ext)))
        {
            string saveExt = ext.Replace(".", "");
            string path = EditorUtility.SaveFilePanel("Save Attachment", "", name, saveExt);
            if (string.IsNullOrEmpty(path)) return;
            EditorUtility.DisplayProgressBar("Downloading", "Saving...", 0.5f);
            bool success = await _managerFile.DownloadFileToPathAsync(fileID, path);
            EditorUtility.ClearProgressBar();
            if (success) Application.OpenURL(path);
            else EditorUtility.DisplayDialog("Error", "Download Failed", "OK");
        }
        else
        {
            EditorUtility.DisplayProgressBar("Loading", "Preview...", 0.5f);
            byte[] data = await _managerFile.DownloadBytesFromDriveAsync(fileID);
            EditorUtility.ClearProgressBar();
            if (data != null)
            {
                if (IsImage(ext)) { Texture2D tex = new Texture2D(2, 2); tex.LoadImage(data); UniversalViewer.OpenImage(tex, name); }
                else if (IsText(ext)) { string content = System.Text.Encoding.UTF8.GetString(data); UniversalViewer.OpenText(content, name); }
            }
        }
    }

    private bool IsImage(string ext) => ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    private bool IsText(string ext) => ext == ".txt" || ext == ".cs" || ext == ".json" || ext == ".xml" || ext == ".log" || ext == ".shader" || ext == ".html";

    // --- SỬA Ở ĐÂY ---
    public static void Open(BugItem item)
    {
        // true: Utility window (Nổi lên trên)
        // true: Focus
        var window = GetWindow<BugDetailWindow>(true, "Bug Detail", true);

        string[] guids = AssetDatabase.FindAssets("t:BugReportManager");
        if (guids.Length > 0) window._managerFile = AssetDatabase.LoadAssetAtPath<BugReportManager>(AssetDatabase.GUIDToAssetPath(guids[0]));

        window.RefreshData(item);

        // Đặt tiêu đề cửa sổ theo ID bug cho chuyên nghiệp (nếu có)
        window.titleContent = new GUIContent(!string.IsNullOrEmpty(item.ID) ? item.ID : "Bug Detail");

        // Khóa size
        window.minSize = new Vector2(550, 800);
        window.maxSize = new Vector2(550, 800);

        window.Show();
    }
    // -----------------

    public void RefreshData(BugItem item)
    {
        _targetBug = item;
        Info = new BugInfoDisplay(item, _managerFile);
        FixHistory = string.IsNullOrEmpty(item.FixSolution) ? "" : item.FixSolution;
        ParseLogHistory(FixHistory);
        Repaint();
    }

    [Button(ButtonSizes.Large, Icon = SdfIconType.Upload), GUIColor(1f, 0.64f, 0f), BoxGroup("Actions"), PropertySpace(10)]
    [ShowIf("CanSubmitFix"), LabelText("@Info.Status == BugStatus.PendingReview ? \"Update Fix Info\" : \"Submit for Review\"")]
    public void SubmitFix() { if (_managerFile != null) BugActionPopup.Open(_targetBug, _managerFile, BugActionPopup.ActionType.SubmitFix); }

    [Button(ButtonSizes.Large, Icon = SdfIconType.CheckCircleFill), GUIColor(0.4f, 1f, 0.4f), BoxGroup("Actions"), PropertySpace(10)]
    [ShowIf("IsPendingReview"), LabelText("✅ APPROVE FIX")]
    public void ApproveFix() { if (_managerFile != null) BugActionPopup.Open(_targetBug, _managerFile, BugActionPopup.ActionType.Approve); }

    [Button(ButtonSizes.Large, Icon = SdfIconType.ArrowRepeat), GUIColor(1f, 0.5f, 0.5f), BoxGroup("Actions"), PropertySpace(10)]
    [ShowIf("IsClosed"), LabelText("⚠️ REOPEN BUG")]
    public void ReopenBug() { if (_managerFile != null) BugActionPopup.Open(_targetBug, _managerFile, BugActionPopup.ActionType.Reopen); }

    private bool CanSubmitFix => Info.Status == BugStatus.New || Info.Status == BugStatus.ReOpened;
    private bool IsPendingReview => Info.Status == BugStatus.PendingReview;
    private bool IsClosed => Info.Status == BugStatus.Fixed || Info.Status == BugStatus.WontFix;
}
#endif