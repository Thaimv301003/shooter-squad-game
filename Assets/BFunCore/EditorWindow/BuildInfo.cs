using UnityEngine;

namespace BFunCoreKit
{
    [System.Serializable]
    public class BuildInfoData
    {
        public string version;
        public string buildTime;

        public override string ToString()
        {
            return $"Version: {version}, Time: {buildTime}";
        }
    }

    public static class BuildInfo
    {
        private static BuildInfoData cached;

        public static BuildInfoData Get()
        {
            if (cached == null)
            {
                TextAsset txt = Resources.Load<TextAsset>("BuildInfo");
                if (txt != null)
                {
                    cached = JsonUtility.FromJson<BuildInfoData>(txt.text);
                }
                else
                {
                    cached = new BuildInfoData()
                    {
                        version = Application.version,
                        buildTime = "Unknown"
                    };
                }
            }
            return cached;
        }
    }
}
