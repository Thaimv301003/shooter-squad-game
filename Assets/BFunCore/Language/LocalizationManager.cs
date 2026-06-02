// File: LocalizationManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BFunCoreKit;
using Sirenix.OdinInspector;

namespace BFunCoreKit
{
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        [ReadOnly] public LocalizationData localizationData;
        private Dictionary<string, Dictionary<LanguageType, string>> _lookupTable;

        private LanguageType _currentLanguage;
        [ShowInInspector]
        public LanguageType CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        public static event System.Action OnLanguageChanged;

        public override void Awake()
        {
            base.Awake();
            localizationData = Resources.Load<LocalizationData>("Localization Setting");
            BuildLookupTable();
            _ = SetLanguage();
        }

        // Giữ nguyên logic SetLanguage cũ của bạn ở đây...
        public async Task SetLanguage()
        {
            // Logic check IP/Language cũ...
            CurrentLanguage = LanguageType.English; // Mặc định
            await Task.Yield();
        }

        private void BuildLookupTable()
        {
            _lookupTable = new Dictionary<string, Dictionary<LanguageType, string>>();
            if (localizationData == null) return;
            foreach (var entry in localizationData.entries)
            {
                var translations = entry.Translations.ToDictionary(t => t.Language, t => t.Text);
                _lookupTable[entry.Key] = translations;
            }
        }

        public string GetLocalizedText(string key)
        {
            if (string.IsNullOrEmpty(key) || _lookupTable == null) return $"INVALID_KEY: {key}";
            if (_lookupTable.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(CurrentLanguage, out var text)) return text;
                if (translations.TryGetValue(LanguageType.English, out var en)) return en;
            }
            return $"UNTRANSLATED: {key}";
        }

        // --- [NEW] HÀM DỊCH TỪ CODE (HỖ TRỢ BIẾN SỐ) ---
        public static string Translate(string defaultText, params object[] args)
        {
            if (string.IsNullOrEmpty(defaultText)) return "";
            string key = GenerateKeyFromText(defaultText);

            // Lấy text mẫu (Template)
            string template = Instance != null ? Instance.GetLocalizedText(key) : defaultText;
            if (template.StartsWith("INVALID_KEY") || template.StartsWith("UNTRANSLATED")) template = defaultText;

            // Format dữ liệu
            if (args != null && args.Length > 0)
            {
                try { return string.Format(template, args); }
                catch { return template; }
            }
            return template;
        }

        public static string GenerateKeyFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "EMPTY";
            string key = text.ToUpperInvariant();
            key = key.Replace(" ", "_").Replace("-", "_").Replace(".", "");
            key = System.Text.RegularExpressions.Regex.Replace(key, @"[^A-Z0-9_]", "");
            key = System.Text.RegularExpressions.Regex.Replace(key, @"_{2,}", "_");
            if (key.Length > 50) key = key.Substring(0, 50);
            return key.TrimEnd('_');
        }
    }
}