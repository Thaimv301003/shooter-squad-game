using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using BFunCoreKit;

public class SceneQuickSelector : EditorWindow
{
    private const float WINDOW_WIDTH = 250f;
    private const float HEADER_HEIGHT = 40f;
    private const float ITEM_HEIGHT = 22f; // Tăng nhẹ chiều cao item cho đẹp
    private const float MIN_WINDOW_HEIGHT = 50f;

    private List<SceneInfo> buildScenes = new List<SceneInfo>();
    private Vector2 scrollPosition;

    // --- MULTI DIGIT INPUT ---
    private string numberBuffer = "";
    private double lastInputTime = 0;
    private const double INPUT_TIMEOUT = 0.15; // Tăng thời gian chờ xíu cho dễ bấm

    private class SceneInfo
    {
        public string name;
        public string path;
        public int buildIndex;
    }

    [MenuItem("Tools/Scene Quick Selector... #s")]
    public static void ShowWindow()
    {
        if (EditorApplication.isPlaying)
            return;

        // --- [MỚI] ĐÓNG CỬA SỔ PREFAB NẾU ĐANG MỞ ---
        if (HasOpenInstances<PrefabQuickSelector>())
        {
            GetWindow<PrefabQuickSelector>().Close();
        }
        // ---------------------------------------------

        // Check xem Scene Selector đã mở chưa để đóng (Toggle) hoặc mở mới
        if (HasOpenInstances<SceneQuickSelector>())
        {
            GetWindow<SceneQuickSelector>().Close();
        }
        else
        {
            var window = GetWindow<SceneQuickSelector>(true, "Scene Selector", true);
            window.RefreshSceneList();
        }
    }

    private void OnEnable()
    {
        RefreshSceneList();
        EditorBuildSettings.sceneListChanged += RefreshSceneList;
    }

    private void OnDisable()
    {
        EditorBuildSettings.sceneListChanged -= RefreshSceneList;
        EditorApplication.update -= CheckBufferTimeout;
    }

    private void RefreshSceneList()
    {
        buildScenes.Clear();

        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];

            if (scene.enabled)
            {
                buildScenes.Add(new SceneInfo
                {
                    name = Path.GetFileNameWithoutExtension(scene.path),
                    path = scene.path,
                    buildIndex = i
                });
            }
        }

        AdjustWindowSize();
        Repaint();
    }

    private void AdjustWindowSize()
    {
        // Tính toán chiều cao cần thiết dựa trên số lượng scene
        float totalItemHeight = buildScenes.Count * ITEM_HEIGHT;
        float desiredHeight = HEADER_HEIGHT + totalItemHeight + 10f; // +10 padding bottom

        // Giới hạn chiều cao tối đa (ví dụ 600px) để không bị tràn màn hình nếu quá nhiều scene
        float finalHeight = Mathf.Clamp(desiredHeight, MIN_WINDOW_HEIGHT, 600f);

        // --- KHÓA KÍCH THƯỚC ---
        // Set min và max bằng nhau
        Vector2 finalSize = new Vector2(WINDOW_WIDTH, finalHeight);
        minSize = finalSize;
        maxSize = finalSize;
    }

    private void OnGUI()
    {
        // Header
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Type Number to Open", EditorStyles.boldLabel);

        // Hiển thị buffer đang nhập (nếu có)
        if (!string.IsNullOrEmpty(numberBuffer))
        {
            GUIStyle inputStyle = new GUIStyle(EditorStyles.label);
            inputStyle.normal.textColor = Color.yellow;
            inputStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField($"Goto: {numberBuffer}...", inputStyle);
        }
        else
        {
            // Hack layout để giữ vị trí khi không có text
            EditorGUILayout.LabelField("", EditorStyles.miniLabel);
        }
        GUILayout.EndVertical();

        HandleKeyboardInput();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (buildScenes.Count == 0)
        {
            EditorGUILayout.HelpBox("No enabled scenes in Build Settings.", MessageType.Warning);
        }
        else
        {
            foreach (var sceneInfo in buildScenes)
            {
                // Highlight nếu scene đang nhập trùng với index
                bool isHighlight = !string.IsNullOrEmpty(numberBuffer) &&
                                   sceneInfo.buildIndex.ToString().StartsWith(numberBuffer);

                UnityEngine.GUI.backgroundColor = isHighlight ? Color.yellow : Color.white;

                string label = $"[{sceneInfo.buildIndex}]  {sceneInfo.name}";

                // Dùng Alignment Left cho dễ nhìn
                GUIStyle btnStyle = new GUIStyle(EditorStyles.toolbarButton);
                btnStyle.alignment = TextAnchor.MiddleLeft;
                btnStyle.fixedHeight = 20f;

                if (GUILayout.Button(label, btnStyle))
                {
                    TryOpenScene(sceneInfo);
                }

                UnityEngine.GUI.backgroundColor = Color.white; // Reset màu
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void HandleKeyboardInput()
    {
        Event e = Event.current;

        if (e.type != EventType.KeyDown)
            return;

        int digit = -1;

        if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
            digit = e.keyCode - KeyCode.Alpha0;
        else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
            digit = e.keyCode - KeyCode.Keypad0;

        if (digit == -1)
            return;

        double now = EditorApplication.timeSinceStartup;

        if (now - lastInputTime > INPUT_TIMEOUT)
            numberBuffer = "";

        lastInputTime = now;
        numberBuffer += digit.ToString();

        EditorApplication.update -= CheckBufferTimeout;
        EditorApplication.update += CheckBufferTimeout;

        Repaint(); // Repaint để hiện số đang gõ
        e.Use();
    }

    private void CheckBufferTimeout()
    {
        if (EditorApplication.timeSinceStartup - lastInputTime >= INPUT_TIMEOUT)
        {
            if (int.TryParse(numberBuffer, out int sceneIndex))
            {
                SceneInfo targetScene = buildScenes.Find(s => s.buildIndex == sceneIndex);
                if (targetScene != null)
                {
                    TryOpenScene(targetScene);
                }
                else
                {
                    // Nếu nhập sai số scene thì clear buffer và repaint để tắt highlight
                    numberBuffer = "";
                    Repaint();
                }
            }

            numberBuffer = "";
            EditorApplication.update -= CheckBufferTimeout;
        }
    }

    private void TryOpenScene(SceneInfo sceneInfo)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Cannot open scenes while in Play Mode.");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            BFun.LogEditor("Opened Scene: " + sceneInfo.name);
            EditorSceneManager.OpenScene(sceneInfo.path, OpenSceneMode.Single);
            Close();
        }
    }
}