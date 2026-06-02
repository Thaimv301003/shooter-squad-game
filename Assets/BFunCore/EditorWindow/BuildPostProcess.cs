#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;
using UnityEngine;

namespace BFunCoreKit
{
    public class BuildPostProcess
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            // Lấy version từ Project Settings
            string version = PlayerSettings.bundleVersion;

            // Lấy thời gian build
            string buildTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

            // Tạo object info
            BuildInfoData data = new BuildInfoData()
            {
                version = version,
                buildTime = buildTime
            };

            string json = JsonUtility.ToJson(data, true);

            // Lưu vào Resources để game load được
            string filePath = "Assets/Resources/BuildInfo.json";
            File.WriteAllText(filePath, json);
            Debug.Log($"[BuildPostProcess] Build info saved: {json}");

            AssetDatabase.Refresh();
        }
    }
}
#endif

