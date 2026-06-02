            #if BFUN_INSTALLED_TRUE
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using System;

using BFunCoreKit;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BFunCoreKit
{
    [Serializable]
    public class GUIColorStyle
    {
        public string colorStyle;
        [OnValueChanged("OnColorChanged")]
        [ShowIf("@this.colorStyle != \"Default\"")]
        public Color color;
        [HideInInspector] public GUIColorStyleSheet gUIColorStyleSheet;
#if UNITY_EDITOR
        void OnColorChanged()
        {
            if (gUIColorStyleSheet) gUIColorStyleSheet.NotifyUpdate();
        }
#endif

        public GUIColorStyle(string colorStyle, Color color)
        {
            this.colorStyle = colorStyle;
            this.color = color;
        }
    }

    public class GUIColorStyleSheet : ScriptableObject
    {
        public GUIColorStyle[] styles;

#if UNITY_EDITOR
        List<GUIColorBinder> allGUIColorBinders = new List<GUIColorBinder>();

        public void NotifyUpdate()
        {
            UpdateAllBinders();

        }

        [OnInspectorInit]
        void FindAllBindersInPrefabs()
        {
            allGUIColorBinders.Clear();
            var allBinders = Resources.FindObjectsOfTypeAll<GUIColorBinder>();
            foreach (var binder in allBinders)
            {
                if (EditorUtility.IsPersistent(binder.gameObject))
                    continue;
                allGUIColorBinders.Add(binder);
            }
        }

        private void UpdateAllBinders()
        {
#if UNITY_EDITOR && BFUN_INSTALLED_TRUE
            var binders = Resources.FindObjectsOfTypeAll<GUIColorBinder>();
            foreach (var b in allGUIColorBinders)
            {
        #if BFUN_TEXT_TRUE        
                b.Apply();
                #endif
                EditorUtility.SetDirty(b);
            }
#endif
        }


        public void Save()
        {
            // ✅ Đảm bảo thư mục tồn tại
            string directory = Path.GetDirectoryName(GlobalConst.ColorSheetClass);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"📁 Created missing directory: {directory}");
            }

            // ✅ Nếu file chưa có, tạo mới trước
            if (!File.Exists(GlobalConst.ColorSheetClass))
            {
                File.Create(GlobalConst.ColorSheetClass).Close();
                Debug.Log($"🆕 Created new color sheet file: {GlobalConst.ColorSheetClass}");
            }

            // ✅ Ghi file enum
            using (StreamWriter sw = new StreamWriter(GlobalConst.ColorSheetClass, false))
            {
                sw.WriteLine("namespace BFunCoreKit");
                sw.WriteLine("{");
                sw.WriteLine("    public enum GUIColorStyleType");
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
                        string style = styles[i].colorStyle;
                        if (!string.IsNullOrEmpty(style))
                        {
                            sw.WriteLine($"        {style},");
                        }

                        // Gán ngược reference lại SO hiện tại
                        styles[i].gUIColorStyleSheet = this;
                    }
                }

                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
            AssetDatabase.Refresh();
        }
#endif
#if BFUN_TEXT_TRUE
        public void ApplyTo(Image image, GUIColorStyleType styleType, Color initColor)
        {
            if (image == null) return;
            var style = System.Array.Find(styles, s => s.colorStyle == styleType.ToString());
            if (style == null) return;

            bool isChange = false;
            if (styleType == GUIColorStyleType.Default)
            {
                if (image.color != initColor)
                {
                    image.color = initColor;
                    isChange = true;
                }
            }
            else
            {
                if (image.color != style.color)
                {
                    image.color = style.color;
                    isChange = true;
                }
            }
#if UNITY_EDITOR
            if (isChange)
                EditorUtility.SetDirty(image);
#endif
        }

        public void ApplyTo(TextMeshProUGUI tmp, GUIColorStyleType styleType, Color initColor)
        {
            if (tmp == null) return;
            var style = System.Array.Find(styles, s => s.colorStyle == styleType.ToString());
            if (style == null) return;

            bool isChange = false;

            if (styleType == GUIColorStyleType.Default)
            {
                if (tmp.color != initColor)
                {
                    tmp.color = initColor;
                    isChange = true;
                }
            }
            else
            {
                if (tmp.color != style.color)
                {
                    tmp.color = style.color;
                    isChange = true;
                }
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
