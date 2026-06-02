// File: GUITextBinder.cs
#if BFUN_INSTALLED_TRUE
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

namespace BFunCoreKit
{
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class GUITextBinder : MonoBehaviour
    {
        [Header("Styling")]
        [SerializeField] GUITextStyleSheet styleSheet;
#if BFUN_TEXT_TRUE
        [SerializeField] GUITextStyleType textType;
#endif
        [Header("Localization")]
        [ReadOnly, SerializeField] public string localizationKey;

        [Tooltip("Nếu TRUE: Scanner sẽ bỏ qua, và Runtime cũng sẽ không dịch text này.")]
        public bool ignoreTranslate; // <--- [NEW] Biến mới thêm vào

        public bool ignoreFont, ignoreFontSize, ignoreFontStyle;

        private TextMeshProUGUI tmp;
        private object[] formatArgs;

        // --- LIFECYCLE ---

        void Awake()
        {
            if (!styleSheet) styleSheet = Resources.Load<GUITextStyleSheet>("Font Setting");
            GetComponents();
            Apply();
        }

        void Start()
        {
            if (Application.isPlaying)
            {
                LocalizationManager.OnLanguageChanged += ApplyLocalization;
            }
        }

        void OnDestroy()
        {
            if (Application.isPlaying)
            {
                LocalizationManager.OnLanguageChanged -= ApplyLocalization;
            }
        }

        void OnValidate()
        {
            GetComponents();
            ApplyStyle();
        }

        private void GetComponents()
        {
            if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();
        }

        // --- PUBLIC API ---

        public void SetKey(string key, params object[] args)
        {
            this.localizationKey = key;
            this.formatArgs = args;
            Apply();
        }

        public void Apply()
        {
            ApplyStyle();
            ApplyLocalization();
        }

        // --- INTERNAL LOGIC ---

        private void ApplyStyle()
        {
            if (tmp == null) return;

#if BFUN_TEXT_TRUE
            if (styleSheet != null)
                styleSheet.ApplyTo(tmp, textType, ignoreFont, ignoreFontSize, ignoreFontStyle);
#endif
        }

        private void ApplyLocalization()
        {
            if (!Application.isPlaying) return;

            // --- [NEW] Logic Ignore ---
            // Nếu đánh dấu ignore, thoát luôn không dịch
            if (ignoreTranslate) return;

            if (tmp == null) return;
            if (LocalizationManager.Instance == null) return;
            if (string.IsNullOrEmpty(localizationKey)) return;

            string finalString = LocalizationManager.Instance.GetLocalizedText(localizationKey);

            if (finalString.Contains("UNTRANSLATED") || finalString.Contains("INVALID_KEY")) return;

            if (formatArgs != null && formatArgs.Length > 0)
            {
                try { tmp.text = string.Format(finalString, formatArgs); }
                catch { tmp.text = finalString; }
            }
            else
            {
                tmp.text = finalString;
            }
        }
    }
}
#endif