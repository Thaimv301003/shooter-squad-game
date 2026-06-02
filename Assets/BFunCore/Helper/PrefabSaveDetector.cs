#if BFUN_INSTALLED_TRUE
#if UNITY_EDITOR
using BFunCoreKit;
using UnityEditor;
using UnityEngine;

namespace BFunCoreKit
{
    public class PrefabSaveDetector : AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(".prefab"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        UIPanel uI = prefab.GetComponentInChildren<UIPanel>();
                        Panel panel = prefab.GetComponent<Panel>();
                        if(!panel) panel = prefab.GetComponentInChildren<Panel>();
                        
                        if (uI)
                            uI.SaveCanvasNameToScript();

                        if (panel)
                            panel.SavePanelEffectGroupName();
                    };
                }
            }

            return paths;
        }
    }
}
#endif
#endif