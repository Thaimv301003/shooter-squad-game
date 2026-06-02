// File: LocalizationData.cs
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector; // Nếu bạn dùng Odin
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BFunCoreKit
{
    [System.Serializable]
    public class Translation
    {
        public LanguageType Language;
        [TextArea]
        public string Text;
    }

    [System.Serializable]
    public class LocalizationEntry
    {
        [ReadOnly]
        public string Key;
        public List<Translation> Translations = new List<Translation>();
    }

    public class LocalizationData : ScriptableObject
    {
        // --- DATA LƯU TRỮ (Ẩn khỏi Inspector cho gọn) ---
        // Biến này vẫn cần tồn tại để Dashboard lưu dữ liệu vào, nhưng ta ẩn nó đi.
        [HideInInspector]
        public List<string> ignoredFolders = new List<string>()
        {
            "Assets/Plugins",
            "Assets/TextMesh Pro"
        };

        // --- DATA CHÍNH (List từ vựng) ---
        [SerializeField]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Key", HideAddButton = true, HideRemoveButton = true)]
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();

        /// <summary>
        /// Hàm này dùng để Code thêm key mới vào
        /// </summary>
        public void AddOrUpdateEntry(string key, string defaultText, LanguageType defaultLanguage)
        {
            if (entries.Exists(e => e.Key == key)) return;

            LocalizationEntry entry = new LocalizationEntry { Key = key };
            entry.Translations.Add(new Translation { Language = defaultLanguage, Text = defaultText });
            entries.Add(entry);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        // --- BUTTON DUY NHẤT CÒN LẠI ---
        // Chỉ giữ lại nút mở Dashboard, xóa hết nút Scan/Update cũ đi.
        [Button(ButtonSizes.Large, Name = "Open Localization Dashboard")]
        void OpenDashboard()
        {
            MultiLanguageClipboardTool.ShowWindow();
        }
#endif
    }
}