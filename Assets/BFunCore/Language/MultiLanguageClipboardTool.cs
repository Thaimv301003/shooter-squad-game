// File: MultiLanguageClipboardTool.cs (Editor folder)
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System;

namespace BFunCoreKit
{
    public class MultiLanguageClipboardTool : EditorWindow
    {
        private const string DataPath = "Localization Setting";
        private LocalizationData data;
        private SerializedObject serializedData;
        private Vector2 scrollPos;

        // --- PROMPT TỰ ĐỘNG (STRICT TSV MODE) ---
        private const string AI_PROMPT =
@"I need you to act as a Professional Localization Expert for a Casual Mobile Game.
I will provide a dataset in TSV (Tab-Separated Values) format.

CONTEXT & STYLE:
- Context: Video Game UI (Buttons, HUD, Menus, Dialogs).
- Tone: Casual, Fun, Short, and Simple. Easy for players of all ages to understand.
- Length: Keep translations concise to fit in small UI buttons.

FONT & CHARACTER SAFETY (CRITICAL):
To prevent 'Tofu' (square boxes) in Unity, follow these strict rules:
1. Chinese (zh-CN/zh-TW): 
   - Target Font: 'WenQuanYi Micro Hei'. 
   - Rule: Use ONLY common, standard characters (常用字). 
   - Do NOT use rare, archaic, or classical characters.
2. Arabic (ar): 
   - Target Font: 'Noto Sans Arabic'. 
   - Rule: Use Modern Standard Arabic. Keep it simple. Avoid complex decorative ligatures.
3. Thai (th): 
   - Target Font: 'Noto Sans Thai'. 
   - Rule: Use standard contemporary Thai. Avoid complex Sanskrit-rooted formal words if a simpler synonym exists.
4. Korean (ko): 
   - Target Font: 'Spoqa Han Sans Neo'. 
   - Rule: Use standard Hangul. Avoid Hanja (Chinese characters inside Korean text) completely.
5. General: 
   - NO Emojis. 
   - NO Special Math Symbols. 
   - If the English source is a proper name (like 'Excalibur'), translate the MEANING if possible, or use a standard phonetic translation using COMMON characters.

YOUR TASK:
1. Fill in ALL empty cells.
2. Use the 'English' column as the source context.
3. Keep placeholders like {0}, {1} exactly as they are.
4. OUTPUT: Raw TSV format only. No markdown.

DATA TO TRANSLATE:
";
        // ----------------------------------------

        public static void ShowWindow()
        {
            var window = GetWindow<MultiLanguageClipboardTool>(true, "Localization Dashboard", true);
            window.minSize = new Vector2(450, 650); // Tăng chiều cao một chút
        }

        private void OnEnable()
        {
            data = Resources.Load<LocalizationData>(DataPath);
            if (data == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
                if (guids.Length > 0)
                    data = AssetDatabase.LoadAssetAtPath<LocalizationData>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (data != null)
                serializedData = new SerializedObject(data);
        }

        void OnGUI()
        {
            if (data == null)
            {
                EditorGUILayout.HelpBox("Could not find 'Localization Setting' asset!", MessageType.Error);
                return;
            }

            serializedData.Update();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // --- SECTION 1: SCANNER ---
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("1. Project Scanner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scan project for keys. Does not affect AI export directly, just updates the key list.", MessageType.None);

            EditorGUILayout.PropertyField(serializedData.FindProperty("ignoredFolders"), true);
            GUILayout.Space(5);

            UnityEngine.GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Scan Project & Update Keys", GUILayout.Height(30)))
            {
#if BFUN_INSTALLED_TRUE
                LocalizationScanner.ScanAllWithIgnore(data, data.ignoredFolders);
#endif
                GUIUtility.ExitGUI();
            }
            UnityEngine.GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // --- SECTION 2: AI EXPORT ---
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("2. Export Missing/Incomplete to AI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Export data to clipboard for AI processing.", MessageType.Info);

            // Nút 1: Chỉ export cái thiếu (Mặc định)
            if (GUILayout.Button("Copy Prompt & MISSING Data Only", GUILayout.Height(35)))
            {
                ExportKeysToClipboard(false); // false = Chỉ lấy cái thiếu
            }

            GUILayout.Space(5);

            // Nút 2: Hard Reset (Lấy tất cả)
            Color originalColor = UnityEngine.GUI.backgroundColor;
            UnityEngine.GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Màu đỏ nhạt cảnh báo

            if (GUILayout.Button("Copy Prompt & ALL Data (Hard Reset)", GUILayout.Height(35)))
            {
                ExportKeysToClipboard(true); // true = Lấy tất cả
            }

            UnityEngine.GUI.backgroundColor = originalColor; // Trả lại màu cũ
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // --- SECTION 3: IMPORT ---
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("3. Import Translated Data", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Copy the raw TSV from AI, then click below.", MessageType.Info);

            UnityEngine.GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Import From Clipboard", GUILayout.Height(40)))
            {
                ImportFromClipboard();
            }
            UnityEngine.GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            serializedData.ApplyModifiedProperties();
        }

        /// <summary>
        /// Logic Export Data
        /// </summary>
        /// <param name="exportAll">
        /// false: Chỉ export dòng thiếu (Mặc định).
        /// true: Export TẤT CẢ dòng (Hard Reset).
        /// </param>
        private void ExportKeysToClipboard(bool exportAll)
        {
            // 1. Lấy danh sách tất cả ngôn ngữ cần thiết
            var allLanguageTypes = Enum.GetValues(typeof(LanguageType)).Cast<LanguageType>().ToList();
            var languageNames = allLanguageTypes.Select(l => l.ToString()).ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(AI_PROMPT);
            sb.AppendLine(); // Dòng trống ngăn cách prompt và data

            // Header TSV
            sb.AppendLine($"key\t{string.Join("\t", languageNames)}");

            int skippedCount = 0;
            int exportCount = 0;

            foreach (var entry in data.entries)
            {
                // -- LOGIC CHECK: ĐÃ FULL CHƯA? --
                bool isFullyTranslated = true;
                var currentTrans = entry.Translations.ToDictionary(t => t.Language, t => t.Text);

                foreach (var lang in allLanguageTypes)
                {
                    // Nếu thiếu ngôn ngữ này HOẶC text rỗng/chỉ có khoảng trắng -> Chưa full
                    if (!currentTrans.ContainsKey(lang) || string.IsNullOrWhiteSpace(currentTrans[lang]))
                    {
                        isFullyTranslated = false;
                        break;
                    }
                }

                // --- SỬA LOGIC: LỌC DATA ---
                // Nếu KHÔNG phải chế độ lấy tất cả (exportAll = false) VÀ dòng này đã dịch đủ -> Thì mới bỏ qua.
                if (!exportAll && isFullyTranslated)
                {
                    skippedCount++;
                    continue; // Bỏ qua item này
                }

                // -- THÊM VÀO EXPORT --
                exportCount++;
                var line = new List<string> { entry.Key };

                foreach (var lang in allLanguageTypes)
                {
                    if (currentTrans.TryGetValue(lang, out string text) && !string.IsNullOrEmpty(text))
                    {
                        // Escape xuống dòng để tránh vỡ format TSV
                        line.Add(text.Replace("\n", "\\n").Replace("\r", "").Replace("\t", "    "));
                    }
                    else
                    {
                        line.Add(""); // Để trống cho AI điền
                    }
                }
                sb.AppendLine(string.Join("\t", line));
            }

            // Copy vào clipboard
            EditorGUIUtility.systemCopyBuffer = sb.ToString();

            // Thông báo kết quả
            string modeText = exportAll ? "ALL DATA (Hard Reset)" : "MISSING DATA Only";

            if (exportCount == 0)
            {
                EditorUtility.DisplayDialog("Info", "No keys matched criteria to export.", "OK");
            }
            else
            {
                Debug.Log($"<color=cyan><b>[{modeText}] COPIED TO CLIPBOARD!</b></color>\n" +
                          $"Exported: {exportCount} keys.\n" +
                          $"Skipped: {skippedCount} keys.");
            }
        }

        private void ImportFromClipboard()
        {
            string clipboardContent = EditorGUIUtility.systemCopyBuffer;

            if (string.IsNullOrWhiteSpace(clipboardContent))
            {
                EditorUtility.DisplayDialog("Error", "Clipboard is empty!", "OK");
                return;
            }

            Undo.RecordObject(data, "Import Localization");

            // Tách dòng, xử lý cả \r\n và \n
            string[] lines = clipboardContent.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Tìm dòng Header
            int headerIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                string lineLower = lines[i].Trim().ToLower();
                // Header phải chứa 'key' và ít nhất một dấu tab
                if (lineLower.StartsWith("key") && lineLower.Contains("\t"))
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex == -1)
            {
                EditorUtility.DisplayDialog("Error",
                    "Could not find valid Header (starts with 'key' + tabs).\n" +
                    "Make sure AI output is strictly TSV.", "OK");
                return;
            }

            // Parse Header
            string[] headers = lines[headerIndex].Split('\t');
            var langMap = new Dictionary<LanguageType, int>();

            for (int i = 1; i < headers.Length; i++)
            {
                string headerName = headers[i].Trim();
                if (Enum.TryParse<LanguageType>(headerName, out LanguageType lang))
                {
                    langMap[lang] = i;
                }
            }

            var entryDict = data.entries.ToDictionary(e => e.Key, e => e);
            int updatedCount = 0;

            // Duyệt data
            for (int i = headerIndex + 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split('\t');
                if (parts.Length == 0) continue;

                string key = parts[0].Trim();
                if (string.IsNullOrEmpty(key)) continue;

                // Bỏ qua dòng phân cách Markdown nếu AI lỡ tay thêm vào (vd: ---|---|---)
                if (key.StartsWith("-")) continue;

                if (!entryDict.TryGetValue(key, out LocalizationEntry entry))
                {
                    // Nếu key chưa có (trường hợp hiếm), tạo mới luôn
                    entry = new LocalizationEntry { Key = key };
                    data.entries.Add(entry);
                    entryDict[key] = entry;
                }

                bool entryUpdated = false;

                // Update các cột
                foreach (var pair in langMap)
                {
                    LanguageType lang = pair.Key;
                    int colIndex = pair.Value;

                    if (colIndex < parts.Length)
                    {
                        string newVal = parts[colIndex].Trim();

                        // Khôi phục ký tự đặc biệt
                        newVal = newVal.Replace("\\n", "\n");

                        if (!string.IsNullOrEmpty(newVal))
                        {
                            // Tìm bản dịch hiện tại của ngôn ngữ này
                            var existingTrans = entry.Translations.FirstOrDefault(t => t.Language == lang);

                            if (existingTrans != null)
                            {
                                // Nếu text khác nhau thì update
                                if (existingTrans.Text != newVal)
                                {
                                    existingTrans.Text = newVal;
                                    entryUpdated = true;
                                }
                            }
                            else
                            {
                                // Chưa có thì thêm mới
                                entry.Translations.Add(new Translation { Language = lang, Text = newVal });
                                entryUpdated = true;
                            }
                        }
                    }
                }

                if (entryUpdated) updatedCount++;
            }

            // Sort lại danh sách theo Key cho đẹp
            data.entries = data.entries.OrderBy(e => e.Key).ToList();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", $"Import complete!\nUpdated/Filled: {updatedCount} keys.", "OK");
        }
    }
}
#endif