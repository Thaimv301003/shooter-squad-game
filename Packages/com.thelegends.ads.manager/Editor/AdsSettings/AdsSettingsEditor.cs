using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheLegends.Base.Ads.Editor
{
    [CustomEditor(typeof(AdsSettings))]
    public class AdsSettingsEditor : UnityEditor.Editor
    {
        private static AdsSettings instance = null;

        public static AdsSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<AdsSettings>(AdsSettings.FileName);
                }

                if (instance != null)
                {
                    Selection.activeObject = instance;
                }
                else
                {
                    Directory.CreateDirectory(AdsSettings.ResDir);

                    instance = CreateInstance<AdsSettings>();

                    string assetPath = Path.Combine(AdsSettings.ResDir, AdsSettings.FileName);
                    string assetPathWithExtension = Path.ChangeExtension(assetPath, AdsSettings.FileExtension);
                    AssetDatabase.CreateAsset(instance, assetPathWithExtension);
                    AssetDatabase.SaveAssets();
                }

                return instance;
            }
        }

        [MenuItem("TripSoft/Ads Settings")]
        public static void OpenInspector()
        {
            if (Instance == null)
            {
                Debug.Log("Creat new Ads Settings");
            }
        }

        // Drawer Instances
        private AdsMediationDrawer _mediationDrawer = new AdsMediationDrawer();
        private AdsServicesDrawer _servicesDrawer = new AdsServicesDrawer();
        private GoogleSheetSyncTool _googleSheetTool = new GoogleSheetSyncTool();
        private AdsSettingsSaveProcessor _saveProcessor = new AdsSettingsSaveProcessor();
        private AdsGeneralSettingsDrawer _generalSettingsDrawer = new AdsGeneralSettingsDrawer();
        private AdsNetworkConfigsDrawer _networkConfigsDrawer = new AdsNetworkConfigsDrawer();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            // Styles & Colors
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.cyan },
                fontSize = 12,
                margin = new RectOffset(0, 0, 5, 5)
            };

            EditorGUILayout.Space(5);

            // Validation Warnings
            AdsSettingsUIUtils.DrawValidationWarnings(Instance);

            // Draw Sections
            _mediationDrawer.Draw(Instance, titleStyle);
            EditorGUILayout.Space(5);
            
            _servicesDrawer.Draw(Instance, this, titleStyle);
            EditorGUILayout.Space(8);

            // Save Button
            _saveProcessor.DrawSaveButton(Instance, () => Save((AdsSettings)target));
            EditorGUILayout.Space(10);

            // General Settings
            _generalSettingsDrawer.Draw(Instance, serializedObject, titleStyle);
            
            // Google Sheet Tool
            _googleSheetTool.Draw(Instance, serializedObject);
            
            // Network Details
            _networkConfigsDrawer.Draw(Instance, serializedObject, titleStyle);

            if (EditorGUI.EndChangeCheck())
            {
                Save((AdsSettings)target);
            }
        }

        private void Save(Object target)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
