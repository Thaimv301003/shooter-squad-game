using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheLegends.Base.Ads.Editor
{
    public class AdsServicesDrawer
    {
        public void Draw(AdsSettings instance, UnityEditor.Editor editor, GUIStyle titleStyle)
        {
            EditorGUILayout.LabelField("Tracking Services", titleStyle);
            EditorGUILayout.Space(2);
            DrawServicesGrid(instance);
            EditorGUILayout.Space(4);

            if (instance.useFirebase)
                DrawAdsTypeMultiSelect(instance.firebaseTrackedTypes, "Firebase Tracked Types", editor);
            if (instance.useAppsFlyer)
                DrawAdsTypeMultiSelect(instance.appsFlyerTrackedTypes, "AppsFlyer Tracked Types", editor);
            if (instance.useDatabuckets)
                DrawAdsTypeMultiSelect(instance.databucketsTrackedTypes, "Databuckets Tracked Types", editor);
            if (instance.useFacebook)
                DrawAdsTypeMultiSelect(instance.facebookTrackedTypes, "Facebook Tracked Types", editor);
        }

        private void DrawServicesGrid(AdsSettings instance)
        {
            var services = new (string label, bool enabled, System.Action<bool> setter)[]
            {
                ("Firebase",   instance.useFirebase,  v => instance.useFirebase  = v),
                ("AppsFlyer",  instance.useAppsFlyer, v => instance.useAppsFlyer = v),
                ("Databuckets",instance.useDatabuckets,v => instance.useDatabuckets = v),
                ("Facebook",   instance.useFacebook,  v => instance.useFacebook = v),
            };

            const int columns = 3;
            int col = 0;

            for (int i = 0; i < services.Length; i++)
            {
                if (col == 0) EditorGUILayout.BeginHorizontal();

                var (label, enabled, setter) = services[i];
                bool newEnabled = GUILayout.Toggle(enabled, label, "Button",
                    GUILayout.Height(24), GUILayout.MinWidth(90));

                if (newEnabled != enabled) setter(newEnabled);

                col++;
                if (col == columns || i == services.Length - 1)
                {
                    while (col < columns) { GUILayout.Label(GUIContent.none, GUILayout.MinWidth(90)); col++; }
                    EditorGUILayout.EndHorizontal();
                    col = 0;
                }
            }
        }

        private void DrawAdsTypeMultiSelect(List<AdsType> trackedTypes, string labelStr, UnityEditor.Editor editor)
        {
            if (trackedTypes == null) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(labelStr);

            string buttonText = "Select Ads Types...";
            if (trackedTypes.Count > 0)
            {
                int totalTypes = System.Enum.GetValues(typeof(AdsType)).Length - 1; // Exclude None
                if (trackedTypes.Count >= totalTypes)
                    buttonText = "Everything";
                else
                    buttonText = $"{trackedTypes.Count} Selected";
            }
            else
            {
                buttonText = "None Selected";
            }

            Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(buttonText), EditorStyles.popup);
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(buttonText), FocusType.Keyboard))
            {
                PopupWindow.Show(buttonRect, new MultiSelectPopup(trackedTypes, editor));
            }
            EditorGUILayout.EndHorizontal();
        }

        private class MultiSelectPopup : PopupWindowContent
        {
            private List<AdsType> _trackedTypes;
            private UnityEditor.Editor _editor;
            private Vector2 _scrollPos;

            public MultiSelectPopup(List<AdsType> trackedTypes, UnityEditor.Editor editor)
            {
                _trackedTypes = trackedTypes;
                _editor = editor;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(250, 300);
            }

            public override void OnGUI(Rect rect)
            {
                var allValues = (AdsType[])System.Enum.GetValues(typeof(AdsType));

                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft))
                {
                    Undo.RecordObject(_editor.target, "Select All Tracking Types");
                    _trackedTypes.Clear();
                    foreach (var val in allValues)
                    {
                        if (val != AdsType.None) _trackedTypes.Add(val);
                    }
                    EditorUtility.SetDirty(_editor.target);
                    _editor.Repaint();
                }
                if (GUILayout.Button("Clear", EditorStyles.miniButtonRight))
                {
                    Undo.RecordObject(_editor.target, "Clear Tracking Types");
                    _trackedTypes.Clear();
                    EditorUtility.SetDirty(_editor.target);
                    _editor.Repaint();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                foreach (var adsType in allValues)
                {
                    if (adsType == AdsType.None) continue;

                    bool isSelected = _trackedTypes.Contains(adsType);

                    EditorGUI.BeginChangeCheck();
                    bool newSelected = EditorGUILayout.ToggleLeft($" {adsType}", isSelected);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_editor.target, "Change Tracking Type");

                        if (newSelected && !isSelected)
                        {
                            _trackedTypes.Add(adsType);
                        }
                        else if (!newSelected && isSelected)
                        {
                            _trackedTypes.Remove(adsType);
                        }

                        EditorUtility.SetDirty(_editor.target);
                        _editor.Repaint();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
