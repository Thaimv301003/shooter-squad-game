#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

public class BugActionPopup : OdinEditorWindow
{
    public enum ActionType { SubmitFix, Approve, Reopen }
    private BugItem _targetBug;
    private BugReportManager _manager;
    private ActionType _actionType;
    private string _fixerName;

    [Title("$TitleString")]
    [LabelText("@_actionType == ActionType.SubmitFix ? \"Fix Description\" : \"Reason / Note\"")]
    [TextArea(5, 10), Required]
    public string Note;

    [BoxGroup("Attachments"), LabelText("Attach Files")]
    [ListDrawerSettings(Expanded = true, IsReadOnly = true, ShowIndexLabels = false)]
    public List<string> AttachmentPaths = new List<string>();

    [BoxGroup("Attachments"), OnInspectorGUI]
    private void DrawDropZone()
    {
        var style = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
        GUILayout.Box("\n📂 DRAG FILES HERE", style, GUILayout.Height(50), GUILayout.ExpandWidth(true));
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

    private string TitleString => _actionType == ActionType.SubmitFix ? "SUBMIT FIX" : (_actionType == ActionType.Approve ? "APPROVE FIX" : "REOPEN BUG");
    private Color ButtonColor => _actionType == ActionType.SubmitFix ? new Color(1f, 0.64f, 0f) : (_actionType == ActionType.Approve ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.5f));
    private string ButtonText => _actionType == ActionType.SubmitFix ? "SUBMIT FIX" : (_actionType == ActionType.Approve ? "CONFIRM APPROVE" : "CONFIRM REOPEN");

    public static void Open(BugItem bug, BugReportManager manager, ActionType type)
    {
        var window = GetWindow<BugActionPopup>();
        window._targetBug = bug; window._manager = manager; window._actionType = type;
        window.minSize = new Vector2(450, 500); window.titleContent = new GUIContent(type.ToString()); window.ShowUtility();
    }

    protected override void OnEnable() { base.OnEnable(); IdentifyUser(); }
    private async void IdentifyUser()
    {
        _fixerName = CloudProjectSettings.userName;
        try { string[] guids = AssetDatabase.FindAssets("t:PackageManagerSettings"); if (guids.Length > 0) { var settings = AssetDatabase.LoadAssetAtPath<PackageManagerSettings>(AssetDatabase.GUIDToAssetPath(guids[0])); if (settings != null) { string mail = await settings.GetCurrentUserEmailAsync(); if (!string.IsNullOrEmpty(mail)) { _fixerName = mail; Repaint(); } } } } catch { }
    }

    [Button("$ButtonText", ButtonSizes.Large), GUIColor("$ButtonColor"), PropertySpace(20)]
    public async void SubmitAction()
    {
        if (string.IsNullOrEmpty(Note)) return;

        try
        {
            EditorUtility.DisplayProgressBar("Processing...", "Uploading & Updating...", 0.1f);

            // 1. Upload Files
            List<string> newFileIDs = new List<string>();
            if (AttachmentPaths.Count > 0)
            {
                for (int i = 0; i < AttachmentPaths.Count; i++)
                {
                    string path = AttachmentPaths[i];
                    EditorUtility.DisplayProgressBar("Uploading...", $"File {i + 1}/{AttachmentPaths.Count}...", 0.2f + (float)i / AttachmentPaths.Count * 0.4f);
                    string uniqueName = $"{_targetBug.ID}_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(path)}";
                    string id = await _manager.UploadFileToDriveAsync(path, uniqueName);
                    if (!string.IsNullOrEmpty(id)) newFileIDs.Add(id);
                }
            }
            string newIDsString = string.Join(",", newFileIDs);

            // 2. FORMAT HEADER LOG
            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm tt");

            // 🔥 Date: Trắng nhạt (#cccccc), bỏ in đậm
            string prefix = $"<color=#cccccc>[{timestamp}]</color>";

            // 🔥 Email: Trắng tinh (#ffffff), in đậm <b>
            string emailPart = $"<color=#ffffff><b>{_fixerName}</b></color>";

            string header = "";
            string statusColor = "";
            BugStatus newStatus = BugStatus.New;

            if (_actionType == ActionType.SubmitFix)
            {
                statusColor = "#ffa500"; // Cam
                newStatus = BugStatus.PendingReview;
                header = $"{prefix} <color={statusColor}><b>SUBMITTED</b></color> by {emailPart}";
            }
            else if (_actionType == ActionType.Approve)
            {
                statusColor = "#00ff00"; // Xanh
                newStatus = BugStatus.Fixed;
                header = $"{prefix} <color={statusColor}><b>APPROVED</b></color> by {emailPart}";
            }
            else
            {
                statusColor = "#ff6b6b"; // Đỏ
                newStatus = BugStatus.ReOpened;
                header = $"{prefix} <color={statusColor}><b>REOPENED</b></color> by {emailPart}";
            }

            string newEntry = $"{header}:\n";
            if (newFileIDs.Count > 0) newEntry += $"[[ATTACH:{newIDsString}]]\n";
            newEntry += Note;

            // 3. APPEND LOG
            string separator = "\n\n----------------------------------------\n";
            string finalLog = string.IsNullOrEmpty(_targetBug.FixSolution) ? newEntry : _targetBug.FixSolution + separator + newEntry;

            string finalFixer = _targetBug.Fixer;
            if (string.IsNullOrEmpty(finalFixer)) finalFixer = _fixerName;
            else if (!finalFixer.Contains(_fixerName)) finalFixer += $", {_fixerName}";

            await _manager.UpdateBugRowAsync(_targetBug.RowIndex, finalLog, finalFixer, newStatus, _targetBug.AttachmentIDs, newIDsString);

            EditorUtility.ClearProgressBar();
            _targetBug.FixSolution = finalLog;
            _targetBug.Status = newStatus;
            _targetBug.Fixer = finalFixer;
            if (!string.IsNullOrEmpty(newIDsString))
            {
                if (string.IsNullOrEmpty(_targetBug.AttachmentIDs)) _targetBug.AttachmentIDs = newIDsString;
                else _targetBug.AttachmentIDs += "," + newIDsString;
            }

            _manager.SyncData();
            if (EditorWindow.HasOpenInstances<BugDetailWindow>())
            {
                var win = GetWindow<BugDetailWindow>();
                win.RefreshData(_targetBug);
            }
            Close();
        }
        catch (Exception e) { EditorUtility.ClearProgressBar(); Debug.LogError(e); Close(); }
    }
}
#endif