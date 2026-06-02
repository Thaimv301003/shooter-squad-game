// File: LocalizationScanner.cs
#if BFUN_INSTALLED_TRUE
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System;

namespace BFunCoreKit
{
    public class LocalizationScanner
    {
        private static readonly HashSet<string> IgnoredTexts = new HashSet<string> { "New Text", "Button", "Text" };

        public static void ScanAllWithIgnore(LocalizationData data, List<string> ignoredPaths)
        {
            if (!EditorUtility.DisplayDialog("Confirm Scan & Clean",
               "This will SCAN the project and REMOVE unused keys.\n\n" +
               "WARNING: Translations for unused keys will be deleted forever!\n" +
               "Make sure you have not accidentally ignored folders containing valid keys.",
               "Scan & Clean", "Cancel")) return;

            var collectedData = new Dictionary<string, string>();
            List<string> normalizedIgnores = new List<string>();
            if (ignoredPaths != null)
                foreach (var path in ignoredPaths) if (!string.IsNullOrEmpty(path)) normalizedIgnores.Add(path.Replace("\\", "/"));

            // 1. Quét Prefab
            ScanPrefabs(collectedData, normalizedIgnores);

            // 2. Quét Code
            ScanSourceCode(collectedData, normalizedIgnores);

            // 3. Quét ScriptableObjects
            ScanScriptableObjects(collectedData, normalizedIgnores);

            Undo.RecordObject(data, "Update Localization Data");

            // --- [LOGIC MỚI: CLEAN UP & SYNC] ---

            // A. Xóa các key cũ không còn tìm thấy trong dự án (Unused Keys)
            int removedCount = data.entries.RemoveAll(e => !collectedData.ContainsKey(e.Key));

            // B. Thêm các key mới & Update key cũ
            int newCount = 0;
            // Tạo HashSet để check nhanh hơn
            var currentKeys = new HashSet<string>(data.entries.Select(e => e.Key));

            foreach (var pair in collectedData)
            {
                if (!currentKeys.Contains(pair.Key))
                {
                    // Nếu là Key mới hoàn toàn -> Thêm vào
                    data.entries.Add(new LocalizationEntry
                    {
                        Key = pair.Key,
                        Translations = new List<Translation>
                        {
                            new Translation { Language = LanguageType.English, Text = pair.Value }
                        }
                    });
                    newCount++;
                }
                // Nếu Key đã tồn tại -> Giữ nguyên bản dịch cũ (không làm gì cả), 
                // hoặc bạn có thể cập nhật lại tiếng Anh nếu muốn (tùy chọn).
            }

            // Sắp xếp lại cho đẹp
            data.entries = data.entries.OrderBy(e => e.Key).ToList();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>Scan Finished!</color>\n" +
                      $"Found Total: {collectedData.Count}\n" +
                      $"Added New: {newCount}\n" +
                      $"Removed Unused: {removedCount}");
        }

        private static bool IsIgnored(string path, List<string> ignores)
        {
            if (ignores == null || ignores.Count == 0) return false;
            string p = path.Replace("\\", "/");
            foreach (var ignore in ignores) if (p.StartsWith(ignore)) return true;
            return false;
        }

        // --- CÁC HÀM DƯỚI GIỮ NGUYÊN ---

        // --- 1. PREFAB SCANNING ---
        private static void ScanPrefabs(Dictionary<string, string> collectedData, List<string> ignores)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            HashSet<object> visited = new HashSet<object>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (IsIgnored(path, ignores)) continue;
                if (i % 50 == 0) EditorUtility.DisplayProgressBar("Scanning Prefabs", path, (float)i / guids.Length);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) continue;

                // A. Quét UI Binder
                foreach (var binder in prefab.GetComponentsInChildren<GUITextBinder>(true))
                {
                    // --- [NEW] CHECK IGNORE TRANSLATE ---
                    // Nếu binder này bật ignoreTranslate -> Bỏ qua vòng lặp này
                    if (binder.ignoreTranslate) continue;
                    // ------------------------------------

                    var tmp = binder.GetComponent<TextMeshProUGUI>();
                    if (tmp == null || string.IsNullOrWhiteSpace(tmp.text) || IgnoredTexts.Contains(tmp.text)) continue;
                    if (Regex.IsMatch(tmp.text, @"^[\+\-]?\d+") || Regex.IsMatch(tmp.text, @"^\d{1,2}:\d{2}$")) continue;

                    string key = LocalizationManager.GenerateKeyFromText(tmp.text);
                    SerializedObject so = new SerializedObject(binder);
                    if (so.FindProperty("localizationKey").stringValue != key)
                    {
                        so.FindProperty("localizationKey").stringValue = key;
                        so.ApplyModifiedProperties();
                    }
                    if (!collectedData.ContainsKey(key)) collectedData[key] = tmp.text.Trim();
                }

                // B. Quét Script thường (Giữ nguyên)
                MonoBehaviour[] scripts = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var script in scripts)
                {
                    if (script == null) continue;

                    Type type = script.GetType();
                    string ns = type.Namespace;
                    if (ns != null && (ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor") || ns.StartsWith("TMPro") || ns.StartsWith("System")))
                        continue;

                    visited.Clear();
                    RecursiveScanObject(script, collectedData, visited);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        // --- 2. SOURCE CODE SCANNING ---
        private static void ScanSourceCode(Dictionary<string, string> collectedData, List<string> ignores)
        {
            Regex regex = new Regex(@"\.Translate\s*\(\s*""([^""]+)""");
            string[] allFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            foreach (var fullPath in allFiles)
            {
                string unityPath = "Assets" + fullPath.Replace(Application.dataPath.Replace("/", "\\"), "").Replace("\\", "/");
                if (IsIgnored(unityPath, ignores)) continue;

                string content = File.ReadAllText(fullPath);
                foreach (Match match in regex.Matches(content))
                {
                    if (match.Groups.Count >= 2)
                    {
                        string txt = match.Groups[1].Value;
                        string key = LocalizationManager.GenerateKeyFromText(txt);
                        if (!collectedData.ContainsKey(key)) collectedData[key] = txt;
                    }
                }
            }
        }

        // --- 3. SCRIPTABLE OBJECT SCANNING ---
        private static void ScanScriptableObjects(Dictionary<string, string> collectedData, List<string> ignores)
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            HashSet<object> visited = new HashSet<object>();
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsIgnored(path, ignores)) continue;

                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null) continue;

                if (count++ % 50 == 0) EditorUtility.DisplayProgressBar("Scanning Data (SO)", path, (float)count / guids.Length);

                visited.Clear();
                RecursiveScanObject(so, collectedData, visited);
            }
            EditorUtility.ClearProgressBar();
        }

        // --- HELPER: REFLECTION SCANNER ---
        // --- HELPER: REFLECTION SCANNER ---
        private static void RecursiveScanObject(object obj, Dictionary<string, string> collectedData, HashSet<object> visited)
        {
            if (obj == null) return;
            if (visited.Contains(obj)) return;
            visited.Add(obj);

            Type type = obj.GetType();

            string ns = type.Namespace;
            // Chặn System, nhưng lưu ý logic này sẽ chặn string nếu gọi đệ quy vào string
            if (ns != null && (ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor") || ns.StartsWith("TMPro") || ns.StartsWith("System")))
                return;

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                Type ft = field.FieldType;

                if (typeof(UnityEngine.Object).IsAssignableFrom(ft))
                    continue;

                // 1. Xử lý string đơn lẻ (CODE CŨ - GIỮ NGUYÊN)
                if (ft == typeof(string))
                {
                    if (Attribute.IsDefined(field, typeof(LocalizeAttribute)))
                    {
                        string text = field.GetValue(obj) as string;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            string key = LocalizationManager.GenerateKeyFromText(text);
                            if (!collectedData.ContainsKey(key)) collectedData[key] = text.Trim();
                        }
                    }
                    continue;
                }

                // Bỏ qua các kiểu primitive, enum... (CODE CŨ - GIỮ NGUYÊN)
                if (ft.IsPrimitive || ft.IsEnum || ft == typeof(Vector2) || ft == typeof(Vector3) || ft == typeof(Color))
                    continue;

                object value = field.GetValue(obj);
                if (value == null) continue;

                // 2. Xử lý List/Array (CODE MỚI - ĐÃ SỬA)
                if (typeof(IEnumerable).IsAssignableFrom(ft))
                {
                    var list = value as IEnumerable;
                    if (list != null)
                    {
                        // [NEW] Kiểm tra xem cái List/Array này có được gắn [Localize] không?
                        // Ví dụ: [Localize] public List<string> myDialogs;
                        bool isListLocalized = Attribute.IsDefined(field, typeof(LocalizeAttribute));

                        foreach (var item in list)
                        {
                            // Nếu List có [Localize] và phần tử là string -> Quét luôn tại đây
                            if (isListLocalized && item is string strVal && !string.IsNullOrWhiteSpace(strVal))
                            {
                                string key = LocalizationManager.GenerateKeyFromText(strVal);
                                if (!collectedData.ContainsKey(key)) collectedData[key] = strVal.Trim();
                            }

                            // Vẫn gọi đệ quy để quét tiếp (dành cho List<Class> có chứa field [Localize] bên trong class đó)
                            // Note: Nếu item là string thì hàm đệ quy sẽ return ngay lập tức vì namespace System, nên không lo bị trùng.
                            RecursiveScanObject(item, collectedData, visited);
                        }
                    }
                }
                else if (ft.IsClass)
                {
                    RecursiveScanObject(value, collectedData, visited);
                }
            }
        
    }
    }
}
#endif
#endif