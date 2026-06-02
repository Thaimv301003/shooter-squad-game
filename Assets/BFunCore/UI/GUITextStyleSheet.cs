            #if BFUN_INSTALLED_TRUE
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using System.IO;
using UnityEditor;
using BFunCoreKit;
using System.Collections.Generic;

namespace BFunCoreKit
{
    [System.Serializable]
    public class GUITextStyle
    {
        public string styleType;
        [OnValueChanged("OnColorChanged")]
        public TMP_FontAsset font;
        [OnValueChanged("OnColorChanged")]
        public FontStyles fontStyles;
        [OnValueChanged("OnColorChanged")]
        public int fontSize = 36;
        [HideInInspector] public GUITextStyleSheet gUITextStyleSheet;
#if UNITY_EDITOR
        void OnColorChanged()
        {
            if (gUITextStyleSheet) gUITextStyleSheet.NotifyUpdate();
        }
#endif

        public GUITextStyle(string styleType, TMP_FontAsset font)
        {
            this.styleType = styleType;
            this.font = font;
        }
    }

    public class GUITextStyleSheet : ScriptableObject
    {
        public GUITextStyle[] styles;

#if UNITY_EDITOR
        List<GUITextBinder> allGUITextBinders = new List<GUITextBinder>();
        public void NotifyUpdate()
        {

            UpdateAllBinders();

        }

        [OnInspectorInit]
        void FindAllBindersInPrefabs()
        {
            allGUITextBinders.Clear();
            var allBinders = Resources.FindObjectsOfTypeAll<GUITextBinder>();
            foreach (var binder in allBinders)
            {
                if (EditorUtility.IsPersistent(binder.gameObject))
                    continue;
                allGUITextBinders.Add(binder);
            }
        }

        private void UpdateAllBinders()
        {
#if UNITY_EDITOR
            var binders = Resources.FindObjectsOfTypeAll<GUITextBinder>();
            foreach (var b in allGUITextBinders)
            {
                b.Apply();
                EditorUtility.SetDirty(b);
            }
#endif
        }

        public void Save()
        {
            // ✅ Đảm bảo thư mục tồn tại
            string directory = Path.GetDirectoryName(GlobalConst.TextSheetClass);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"📁 Created missing directory: {directory}");
            }

            // ✅ Nếu file chưa có, tạo mới trước
            if (!File.Exists(GlobalConst.TextSheetClass))
            {
                File.Create(GlobalConst.TextSheetClass).Close();
                Debug.Log($"🆕 Created new color sheet file: {GlobalConst.TextSheetClass}");
            }

            // ✅ Ghi file enum
            using (StreamWriter sw = new StreamWriter(GlobalConst.TextSheetClass, false))
            {
                sw.WriteLine("namespace BFunCoreKit");
                sw.WriteLine("{");
                sw.WriteLine("    public enum GUITextStyleType");
                sw.WriteLine("    {");

                if (styles == null || styles.Length == 0)
                {
                    // Nếu chưa có style nào, tạo 1 giá trị mặc định
                    sw.WriteLine("        Default,");
                }
                else
                {
                    for (int i = 0; i < styles.Length; i++)
                    {
                        string style = styles[i].styleType;
                        if (!string.IsNullOrEmpty(style))
                        {
                            sw.WriteLine($"        {style},");
                        }

                        // Gán ngược reference lại SO hiện tại
                        styles[i].gUITextStyleSheet = this;
                    }
                }

                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
            AssetDatabase.Refresh();
        }
#endif
#if BFUN_TEXT_TRUE
        public void ApplyTo(TextMeshProUGUI tmp, GUITextStyleType styleType, bool ignoreFont, bool ignoreFontSize, bool ignoreFontStyle)
        {
            if (tmp == null) return;
            var style = System.Array.Find(styles, s => s.styleType == styleType.ToString());
            if (style == null) return;

            bool isChange = false;

            if (!ignoreFont && tmp.font != style.font)
            {
                tmp.font = style.font;
                isChange = true;
            }

            if (!ignoreFontStyle && tmp.fontStyle != style.fontStyles)
            {
                tmp.fontStyle = style.fontStyles;
                isChange = true;
            }

            if (!ignoreFontSize && tmp.fontSize != style.fontSize)
            {
                tmp.fontSize = style.fontSize;
                isChange = true;
            }
#if UNITY_EDITOR
            if (isChange)
                EditorUtility.SetDirty(tmp);

#endif
        }
#endif
    }
}
#endif
