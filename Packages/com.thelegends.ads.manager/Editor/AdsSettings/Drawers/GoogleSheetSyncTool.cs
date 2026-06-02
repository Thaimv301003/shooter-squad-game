using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace TheLegends.Base.Ads.Editor
{
    public class GoogleSheetSyncTool
    {
        private AdsSettings _instance;
        private SerializedObject _serializedObject;

        public void Draw(AdsSettings instance, SerializedObject serializedObject)
        {
            _instance = instance;
            _serializedObject = serializedObject;

            EditorGUILayout.BeginVertical(GUI.skin.window);
            EditorGUILayout.LabelField("Google Sheet Sync", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.yellow } });
            EditorGUILayout.Space(5);

            // Admob
            instance.admobSheetUrl = EditorGUILayout.TextField("Admob CSV URL", instance.admobSheetUrl);
            if (GUILayout.Button("Fetch Admob Data"))
            {
                if (!string.IsNullOrEmpty(instance.admobSheetUrl))
                    FetchDataFromGoogleSheet(instance.admobSheetUrl, "ADMOB");
                else
                    EditorUtility.DisplayDialog("Error", "Admob Sheet URL is empty.", "OK");
            }

            EditorGUILayout.Space(5);

            // MAX
            instance.maxSheetUrl = EditorGUILayout.TextField("MAX CSV URL", instance.maxSheetUrl);
            if (GUILayout.Button("Fetch MAX Data"))
            {
                if (!string.IsNullOrEmpty(instance.maxSheetUrl))
                    FetchDataFromGoogleSheet(instance.maxSheetUrl, "MAX");
                else
                    EditorUtility.DisplayDialog("Error", "MAX Sheet URL is empty.", "OK");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void FetchDataFromGoogleSheet(string url, string mediationPrefix)
        {
            Debug.Log("Fetching data from Google Sheet...");
            UnityWebRequest www = UnityWebRequest.Get(url);
            var operation = www.SendWebRequest();

            EditorApplication.update += () =>
            {
                if (!operation.isDone)
                    return;

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Fetch successful!");
                    ParseAndApplyData(www.downloadHandler.text, mediationPrefix);
                }
                else
                {
                    Debug.LogError("Fetch failed: " + www.error);
                    EditorUtility.DisplayDialog("Error", "Failed to fetch data from URL. Check console for details.", "OK");
                }
                www.Dispose();
            };
        }

        private void ParseAndApplyData(string csvData, string mediationPrefix)
        {
            var lines = csvData.Split('\n');
            if (lines.Length < 2) return;

            var header = lines[0].Split(',');
            var dataRows = new List<string[]>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                dataRows.Add(lines[i].Split(','));
            }

            var newData = new Dictionary<string, List<string>>();
            for (int j = 0; j < header.Length; j++)
            {
                var columnName = header[j].Trim();
                if (string.IsNullOrEmpty(columnName)) continue;

                var idsInColumn = new List<string>();
                for (int i = 0; i < dataRows.Count; i++)
                {
                    if (j < dataRows[i].Length)
                    {
                        string id = dataRows[i][j].Trim();
                        if (!string.IsNullOrEmpty(id))
                            idsInColumn.Add(id);
                    }
                }
                newData[columnName] = idsInColumn;
            }

            Action<object, string> applyDataToUnitId = (unitId, platformPrefix) =>
            {
                if (unitId == null) return;
                var fields = unitId.GetType().GetFields().Where(f => f.FieldType == typeof(List<Placement>));

                foreach (var field in fields)
                {
                    string adType = field.Name.Replace("Ids", "");
                    string columnName = $"{platformPrefix}_{adType}";

                    var idList = (List<Placement>)field.GetValue(unitId);
                    idList.Clear();

                    if (newData.ContainsKey(columnName))
                    {
                        foreach (var id in newData[columnName])
                        {
                            idList.Add(new Placement { stringIDs = new List<string> { id } });
                        }
                    }
                }
            };

            if (_instance != null)
            {
                Undo.RecordObject(_instance, "Sync Google Sheet Data");

                if (mediationPrefix == "ADMOB")
                {
                    applyDataToUnitId(_instance.ADMOB_Android, "A_Android");
                    applyDataToUnitId(_instance.ADMOB_IOS, "A_iOS");
                }
                else if (mediationPrefix == "MAX")
                {
                    applyDataToUnitId(_instance.MAX_Android, "M_Android");
                    applyDataToUnitId(_instance.MAX_iOS, "M_iOS");
                }

                if (_serializedObject != null)
                {
                    _serializedObject.Update();
                    _serializedObject.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(_instance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Data applied successfully!");
            }
        }
    }
}
