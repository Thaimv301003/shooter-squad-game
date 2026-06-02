#if BFUN_INSTALLED_TRUE
using System.IO;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Callbacks;
#endif
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Linq;

namespace BFunCoreKit
{
    [CreateAssetMenu(fileName = "UI Setting", menuName = "BFunCo/UI Setting")]
    public class GUISetting : ScriptableObject
    {
#if UNITY_EDITOR
        [ShowInInspector, ListDrawerSettings(Expanded = true, DraggableItems = false, ShowPaging = false)]
        [LabelText("All Canvas Prefabs"), ReadOnly]
        private List<GameObject> allCanvas = new List<GameObject>();

        // ================= Figma Importer =================
        [Title("Figma Importer", "Tự động tạo UI từ Figma", titleAlignment: TitleAlignments.Centered)]
        [BoxGroup("Figma Importer Config")]
        [LabelText("Figma URL")]
        public string figmaURL;

        [BoxGroup("Figma Importer Config")]
        [LabelText("Figma Token")]
        public string figmaToken;

        [FolderPath(RequireExistingPath = true)]
        [BoxGroup("Figma Importer Config")]
        [LabelText("Thư mục lưu Ảnh xuất")]
        public string figmaImageSavePath = "Assets/BFunCore/Texture2D/Figma";

        [BoxGroup("Figma Importer Config")]
        [ValueDropdown("AvailableCanvases")]
        [LabelText("Canvas Mục Tiêu")]
        public string targetCanvasToImport = "All Canvases";

        [HideInInspector]
        public List<string> availableCanvases = new List<string> { "All Canvases" };
        private IEnumerable<string> AvailableCanvases() { return availableCanvases; }

        [Button("1. Đọc Canvases từ Figma", ButtonSizes.Medium), GUIColor(0.2f, 0.6f, 0.8f)]
        [BoxGroup("Figma Importer Config")]
        private void FetchAvailableCanvases()
        {
            if (string.IsNullOrEmpty(figmaToken)) figmaToken = EditorPrefs.GetString("BFun_FigmaToken", "");
            else EditorPrefs.SetString("BFun_FigmaToken", figmaToken);
            
            BFunCoreKit.Figma.FigmaUIBuilder.FetchCanvasList(figmaURL, figmaToken, this);
        }

        [Button("2. 📥 Import Canvas Lựa Chọn", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.4f)]
        [BoxGroup("Figma Importer Config")]
        private void ImportFigmaUI()
        {
            if (string.IsNullOrEmpty(figmaToken)) figmaToken = EditorPrefs.GetString("BFun_FigmaToken", "");
            else EditorPrefs.SetString("BFun_FigmaToken", figmaToken);
            
#pragma warning disable CS4014
            BFunCoreKit.Figma.FigmaUIBuilder.ImportFigmaUI(figmaURL, figmaToken, figmaImageSavePath, targetCanvasToImport);
#pragma warning restore CS4014
        }
        // ==================================================
#endif

        [HideLabel]
        public GameObject loadingScreenPanel;
#if UNITY_EDITOR
        private const string CANVAS_PROCESSING_QUEUE_KEY = "CanvasProcessingQueue_GUISetting";
        private const char QUEUE_DELIMITER = ';';

        // ================= OnInspectorInit & Refresh =================
        [OnInspectorInit]
        private void OnInspectorInit()
        {
            RefreshDisplayList();
        }

        public void RefreshDisplayList()
        {
            allCanvas.Clear();
            if (!Directory.Exists(GlobalConst.CanvasFolder)) return;

            string[] files = Directory.GetFiles(GlobalConst.CanvasFolder, "*.prefab", SearchOption.TopDirectoryOnly);
            foreach (var f in files)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(f);
                if (prefab != null) allCanvas.Add(prefab);
            }
            EditorUtility.SetDirty(this);
        }

        // ================= Core Logic (For Setup) =================

        /// <summary>
        /// Chuẩn bị các file cho các canvas cơ bản, không tạo file enum.
        /// </summary>
        public void AddToPrefabToAddresable()
        {
            string[] baseNames = { "CanvasHome", "CanvasGame", "CanvasGlobal" };
            foreach (var name in baseNames)
            {
                string finalPrefabPath = Path.Combine(GlobalConst.CanvasFolder, name + ".prefab");
                AddToAddressables(finalPrefabPath, name);
            }
        }

        /// <summary>
        /// Tạo các file vật lý (prefab, script) cho một canvas.
        /// </summary>
        public void CreateCanvasFilesInternal(string canvasName)
        {
            string basePrefabPath = "Assets/BFunCore/UI/Canvas/CanvasBase.prefab";
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (basePrefab == null)
            {
                Debug.LogError($"[GUISetting] Cannot load Base Prefab at: {basePrefabPath}");
                return;
            }

            string folderPath = GlobalConst.CanvasFolder;
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string finalPrefabPath = Path.Combine(GlobalConst.CanvasFolder, canvasName + ".prefab");
            if (File.Exists(finalPrefabPath))
            {
                Debug.LogWarning($"[GUISetting] Canvas '{canvasName}' already exists, skipping creation.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            instance.name = canvasName;
            PrefabUtility.SaveAsPrefabAsset(instance, finalPrefabPath);
            GameObject.DestroyImmediate(instance);

            string enumName = canvasName.Replace("Canvas", "Panel");
            string enumPath = Path.Combine(folderPath, enumName + ".cs");
            if (!File.Exists(enumPath)) File.WriteAllText(enumPath, GeneratePanelEnum(enumName));

            string scriptPath = Path.Combine(folderPath, canvasName + ".cs");
            if (!File.Exists(scriptPath)) File.WriteAllText(scriptPath, GenerateCanvasClass(canvasName, enumName));

            AddToAddressables(finalPrefabPath, canvasName);

            string currentQueue = EditorPrefs.GetString(CANVAS_PROCESSING_QUEUE_KEY, "");
            string newQueue = string.IsNullOrEmpty(currentQueue) ? canvasName : currentQueue + QUEUE_DELIMITER + canvasName;
            EditorPrefs.SetString(CANVAS_PROCESSING_QUEUE_KEY, newQueue);
        }

        // ================= Post Compilation Callback =================

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            string queue = EditorPrefs.GetString(CANVAS_PROCESSING_QUEUE_KEY, null);
            if (string.IsNullOrEmpty(queue)) return;

            EditorPrefs.DeleteKey(CANVAS_PROCESSING_QUEUE_KEY);

            GenerateLatestCanvasEnum();

            string[] canvasNamesToProcess = queue.Split(QUEUE_DELIMITER);
            foreach (var canvasName in canvasNamesToProcess.Where(s => !string.IsNullOrEmpty(s)))
            {
                AttachComponentToPrefab(canvasName);
            }

            string[] guids = AssetDatabase.FindAssets("t:GUISetting");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GUISetting settings = AssetDatabase.LoadAssetAtPath<GUISetting>(path);
                if (settings != null)
                {
                    settings.RefreshDisplayList();
                }
            }

            Debug.LogWarning("[GUISetting] CanvasName.cs has been updated. Unity will now trigger a second compilation.");
            AssetDatabase.Refresh();
        }

        private static void AttachComponentToPrefab(string canvasName)
        {
            string folderPath = GlobalConst.CanvasFolder;
            string prefabPath = Path.Combine(folderPath, canvasName + ".prefab");
            System.Type newType = System.Type.GetType($"BFunCoreKit.{canvasName}, Assembly-CSharp");

            if (newType != null)
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot != null && prefabRoot.GetComponent(newType) == null)
                {
                    prefabRoot.AddComponent(newType);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
                if (prefabRoot != null) PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            else
            {
                Debug.LogWarning($"[GUISetting] Could not find type 'BFunCoreKit.{canvasName}' after first compilation. This is expected and should resolve after the second compilation.");
            }
        }

        // ================= File Generation Helpers =================

        private static void GenerateLatestCanvasEnum()
        {
            var allPrefabNames = GetPrefabNamesInFolder(GlobalConst.CanvasFolder);
            allPrefabNames.Sort();

            string enumPath = Path.Combine(GlobalConst.SettingFolder, "CanvasName.cs");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("namespace BFunCoreKit");
            sb.AppendLine("{");
            sb.AppendLine("    public enum CanvasName");
            sb.AppendLine("    {");
            for (int i = 0; i < allPrefabNames.Count; i++)
            {
                // ❌ CODE ADDED/MODIFIED HERE: Cắt bỏ tiền tố "Canvas" ❌
                string enumEntry = allPrefabNames[i].Replace("Canvas", "");
                // ❌ END CODE ADDED/MODIFIED HERE ❌

                string line = "        " + enumEntry;
                if (i < allPrefabNames.Count - 1) line += ",";
                sb.AppendLine(line);
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(enumPath, sb.ToString());
        }

        public static List<string> GetPrefabNamesInFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return new List<string>();
            }
            return new DirectoryInfo(folderPath)
                .GetFiles("*.prefab")
                .Select(f => Path.GetFileNameWithoutExtension(f.Name))
                .ToList();
        }

        private static string GeneratePanelEnum(string enumName) { return $@"namespace BFunCoreKit {{ public enum {enumName} {{ }} }}"; }
        private static string GenerateCanvasClass(string className, string enumName)
        {
            return
        $@"using System.Collections;
using System.Collections.Generic;
using BFunCoreKit;
using UnityEngine;

namespace BFunCoreKit
{{
    public class {className} : CanvasBase<{className}>
    {{
        public IEnumerator ShowPanel({enumName} panelName, string effectOption = ""Default"")
        {{
            yield return uiPanel.ShowPanel(panelName.ToString(), effectOption);
        }}

        public IEnumerator ClosePanel({enumName} panelName, string effectOption = ""Default"")
        {{
            yield return uiPanel.ClosePanel(panelName.ToString(), effectOption);
        }}
    }}
}}
";
        }
        public static void AddToAddressables(string assetPath, string addressAndLabel)
        {
            // === Phần kiểm tra và tạo Settings (Giữ nguyên, đã đúng) ===
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("Addressable Settings không được tìm thấy. Đang tạo mới...");
                string settingsPath = "Assets/AddressableAssetsData";
                AddressableAssetSettings.Create(settingsPath, "AddressableAssetSettings", true, true);
                var newSettings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(settingsPath + "/AddressableAssetSettings.asset");
                if (newSettings != null)
                {
                    AddressableAssetSettingsDefaultObject.Settings = newSettings;
                    settings = newSettings;
                    EditorUtility.SetDirty(settings);
                }
                else
                {
                    Debug.LogError("Tạo Addressable Settings thất bại!");
                    return;
                }
            }

            // === Phần logic chính ===
            const string groupName = "UI";

            // Đổi tên tham số để rõ ràng hơn: giá trị này sẽ được dùng cho cả address và label
            string labelName = addressAndLabel;

            // 1. Tìm hoặc tạo group "UI"
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, false, settings.DefaultGroup.Schemas);
            }

            // 2. Lấy GUID của asset
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            // 3. Dùng hàm CreateOrMoveEntry để đơn giản hóa
            var entry = settings.CreateOrMoveEntry(guid, group);

            // 4. GÁN CẢ HAI: ADDRESS VÀ LABEL
            entry.address = addressAndLabel; // Gán Addressable Name

            // Thêm label vào danh sách chung của project nếu nó chưa tồn tại
            if (!settings.GetLabels().Contains(labelName))
            {
                settings.AddLabel(labelName);
            }
            // Gán Label cho entry này
            entry.SetLabel(labelName, true, true);

            // 5. Đánh dấu dirty và lưu lại
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
            AssetDatabase.SaveAssets();
        }

        // ================= Popup with name validation =================
        [Button("➕ Create New Canvas", ButtonSizes.Large)]
        private void ShowCreateCanvasPopup()
        {
            CreateCanvasPopup.ShowWindow(this);
        }

        private class CreateCanvasPopup : OdinEditorWindow
        {
            private string newCanvasName;
            private GUISetting owner;
            private bool isNameDuplicate = false;
            private string errorMessage = "";

            public static void ShowWindow(GUISetting owner)
            {
                var window = GetWindow<CreateCanvasPopup>("Create Canvas");
                window.owner = owner;
                window.position = GUIHelper.GetEditorWindowRect().AlignCenter(400, 200);
                window.Show();
            }

            protected override void OnGUI()
            {
                SirenixEditorGUI.Title("Create New Canvas", null, TextAlignment.Center, true);
                GUILayout.Space(8);

                EditorGUI.BeginChangeCheck();
                newCanvasName = EditorGUILayout.TextField("Prefab Name", newCanvasName);
                if (EditorGUI.EndChangeCheck())
                {
                    ValidateCanvasName();
                }

                if (isNameDuplicate)
                {
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                }

                GUILayout.Space(12);
                GUILayout.BeginHorizontal();

                UnityEngine.GUI.enabled = !isNameDuplicate && !string.IsNullOrWhiteSpace(newCanvasName);

                if (GUILayout.Button("Create", GUILayout.Height(28)))
                {
                    ValidateCanvasName();
                    if (isNameDuplicate) return;

                    // ================== LOGIC ĐÃ ĐƯỢC SỬA LỖI VÀ ĐỒNG BỘ ==================
                    // 1. Xóa hàng đợi cũ để bắt đầu một tác vụ mới.
                    EditorPrefs.DeleteKey(CANVAS_PROCESSING_QUEUE_KEY);

                    // 2. Chỉ gọi hàm tạo file. Hàm này sẽ tự động thêm 'newCanvasName' vào hàng đợi.
                    owner.CreateCanvasFilesInternal(newCanvasName);

                    // 3. Kích hoạt biên dịch. Phần còn lại sẽ do OnScriptsReloaded xử lý.
                    AssetDatabase.Refresh();

                    // =======================================================================

                    Close();
                }

                UnityEngine.GUI.enabled = true;

                if (GUILayout.Button("Cancel", GUILayout.Height(28))) Close();
                GUILayout.EndHorizontal();
            }

            private void ValidateCanvasName()
            {
                string cleanName = newCanvasName.Trim();

                // 1. Rỗng
                if (string.IsNullOrWhiteSpace(cleanName))
                {
                    isNameDuplicate = true;
                    errorMessage = "Tên Canvas không được để trống.";
                    return;
                }

                // 2. Không chứa khoảng trắng
                if (cleanName.Contains(" "))
                {
                    isNameDuplicate = true;
                    errorMessage = "Tên Canvas không được chứa khoảng trắng.";
                    return;
                }

                // 3. Phải bắt đầu bằng 'Canvas'
                if (!cleanName.StartsWith("Canvas"))
                {
                    isNameDuplicate = true;
                    errorMessage = "Tên Canvas phải bắt đầu bằng từ khóa 'Canvas'.";
                    return;
                }

                // 4. Kiểm tra trùng prefab
                string potentialPath = Path.Combine(GlobalConst.CanvasFolder, cleanName + ".prefab");

                if (File.Exists(potentialPath))
                {
                    isNameDuplicate = true;
                    errorMessage = $"Một prefab với tên '{cleanName}' đã tồn tại trong thư mục Canvas.";
                }
                else
                {
                    isNameDuplicate = false;
                    errorMessage = "";
                }
            }

        }
        #endif
    }
}
#endif