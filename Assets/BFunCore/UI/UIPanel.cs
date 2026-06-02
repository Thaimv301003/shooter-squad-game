#if BFUN_INSTALLED_TRUE
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using System.Collections;

namespace BFunCoreKit
{
    public class UIPanel : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] GameObject basePanelPrefab;
#endif
        public Transform screen;
        [SerializeField][ReadOnly] List<GUI> allGUI = new List<GUI>();
        Dictionary<string, Panel> panelDics = new Dictionary<string, Panel>();

#if UNITY_EDITOR
        bool IsNotInPrefabMode() => PrefabStageUtility.GetCurrentPrefabStage() == null;

        [OnInspectorInit]
        void OnInspectorInitEditor()
        {
            ReUpdatePanelEditor();
            GetGUI();
        }

        void GetGUI()
        {
            allGUI.Clear();
            Panel[] panels = GetComponentsInChildren<Panel>();
            foreach (Panel gUIFunction in panels)
            {
                GUI gui = new GUI();
                gui.GUIName = gUIFunction.transform.name.Replace("-", "").Replace("<", "").Replace(">", "").Replace(" ", "");
                gui.panel = gUIFunction.GetComponent<Panel>();
                allGUI.Add(gui);
            }
            EditorUtility.SetDirty(this);
        }

        [Button(ButtonSizes.Large)]
        void AddPanelEditor()
        {
            PanelNameWindow.Open(this);
        }

        public void AddPanelByName(string newPanelName)
        {
            GameObject panelGO = (GameObject)PrefabUtility.InstantiatePrefab(basePanelPrefab, transform);
            panelGO.name = "---------> " + newPanelName + " <---------";
            RectTransform rect = panelGO.GetComponent<RectTransform>();

            int valuePos = 2500 * transform.childCount;

            BFunHelper.SetRectTop(rect, 0);
            BFunHelper.SetRectBottom(rect, 0);
            BFunHelper.SetRectRight(rect, -valuePos);
            BFunHelper.SetRectLeft(rect, valuePos);

            BFun.LogEditor("New Panel Added!");
            GetGUI();
        }

        public void SaveCanvasNameToScript()
        {
            CleanupMissingPrefabClasses();

            string fileName = "Panel" + transform.parent.parent.name.Replace("Canvas", "");
            GetGUI();
            string filePathAndName = GlobalConst.CanvasFolder + "/" + fileName + ".cs";

            using (StreamWriter sw = new StreamWriter(filePathAndName))
            {
                sw.WriteLine("namespace BFunCoreKit");
                sw.WriteLine("{");
                sw.WriteLine("        public enum " + fileName);
                sw.WriteLine("        {");

                foreach (GUI gui in allGUI)
                {
                    sw.WriteLine("\n" + gui.GUIName + ",");
                    gui.panel.SavePanelEffectGroupName();
                }

                sw.WriteLine("        }");
                sw.WriteLine("}");
            }
            AssetDatabase.Refresh();
        }

        private void CleanupMissingPrefabClasses()
        {
            if (!Directory.Exists(GlobalConst.CanvasFolder))
                return;

            var prefabNames = Directory.GetFiles(GlobalConst.CanvasFolder, "*.prefab", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet();

            var classFiles = Directory.GetFiles(GlobalConst.CanvasFolder, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(f => f != "CanvasEnum")
                .ToList();

            var extraClasses = classFiles.Where(c => !prefabNames.Contains(c.Replace("Panel", "Canvas"))).ToList();

            foreach (var cls in extraClasses)
            {
                string path = Path.Combine(GlobalConst.CanvasFolder, cls + ".cs");
                File.Delete(path);
            }
        }

        void ReUpdatePanelEditor()
        {
            foreach (Transform tp in transform)
            {
                int valuePos = 2500 * (tp.GetSiblingIndex() + 1);
                RectTransform rt = tp.GetComponent<RectTransform>();

                BFunHelper.SetRectTop(rt, 0);
                BFunHelper.SetRectBottom(rt, 0);
                BFunHelper.SetRectRight(rt, -valuePos);
                BFunHelper.SetRectLeft(rt, valuePos);
            }
        }
#endif

        private void Awake()
        {
            foreach (GUI gui in allGUI)
            {
                panelDics.Add(gui.GUIName, gui.panel);
                gui.panel.Content.gameObject.SetActive(false);
            }
        }

        public IEnumerator ShowPanel(string panelName, string effectOption = "Default")
        {
            yield return panelDics[panelName].Show(effectOption);
        }

        public IEnumerator ClosePanel(string panelName, string effectOption = "Default")
        {
            yield return panelDics[panelName].Close(effectOption);
        }
    }

#if UNITY_EDITOR
    public class PanelNameWindow : EditorWindow
    {
        string panelName = "";
        UIPanel targetPanel;
        bool hasFocus = false;

        public static void Open(UIPanel panel)
        {
            var window = GetWindow<PanelNameWindow>();
            window.titleContent = new GUIContent("Create New Panel");
            window.targetPanel = panel;

            // Center window on screen
            float w = 350;
            float h = 150;
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float x = main.x + (main.width - w) * 0.5f;
            float y = main.y + (main.height - h) * 0.5f;
            window.position = new Rect(x, y, w, h);

            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Enter New Panel Name", EditorStyles.boldLabel);
            UnityEngine.GUI.SetNextControlName("PanelInput");
            panelName = EditorGUILayout.TextField("Panel Name", panelName);

            // Focus once
            if (!hasFocus)
            {
                EditorGUI.FocusTextInControl("PanelInput");
                hasFocus = true;
            }

            // Check if name exists
            bool nameExists = targetPanel != null && targetPanel.gameObject.GetComponentsInChildren<Panel>()
                .Any(p => p.name.Replace("-", "").Replace("<", "").Replace(">", "").Replace(" ", "") == panelName);

            if (nameExists)
            {
                EditorGUILayout.HelpBox("A panel with this name already exists!", MessageType.Error);
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
                Close();

            UnityEngine.GUI.enabled = !string.IsNullOrEmpty(panelName) && !nameExists;
            if (GUILayout.Button("Create") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && UnityEngine.GUI.enabled))
            {
                if (!string.IsNullOrEmpty(panelName))
                {
                    targetPanel.AddPanelByName(panelName);
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
            UnityEngine.GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }


        [MenuItem("BFun/Test/Open Add Panel _n")] // Ctrl+Shift+N
        public static void TestOpen()
        {
            // Must be in prefab editing mode
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;

            // Find UIPanel in prefab root
            UIPanel uiPanel = stage.prefabContentsRoot.GetComponentInChildren<UIPanel>(true);
            if (uiPanel == null) return;

            // Open the Panel Name window
            PanelNameWindow.Open(uiPanel);
        }
}

#endif
}
#endif