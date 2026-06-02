using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BFunCoreKit
{
    public class SoundSettingData : ScriptableObject
    {
        [Title("------ Audio Volume ------", TitleAlignment = TitleAlignments.Centered, HorizontalLine = false)]
        [HideLabel]
        [ProgressBar(0, 1, r: 1, g: 1, b: 1, Height = 25)]
        public float AudioVolume = 1f;

        [Space(10)]
        public AudioMixer audioMixer;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup musicGroup;

        [Header("Sound List")]
        public List<SoundData> sounds = new List<SoundData>();

#if UNITY_EDITOR
        public void Save()
        {
            string enumName = "SoundName";
            string filePathAndName = GlobalConst.SettingFolder + "/" + enumName + ".cs";

            using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
            {
                streamWriter.WriteLine("namespace BFunCoreKit");
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("public enum " + enumName);
                streamWriter.WriteLine("{");
                for (int i = 0; i < sounds.Count; i++)
                {
                    streamWriter.WriteLine("\t" + sounds[i].soundName + ",");
                }
                streamWriter.WriteLine("}");
                streamWriter.WriteLine("}");
            }
            AssetDatabase.Refresh();
        }
#endif
    }
}
