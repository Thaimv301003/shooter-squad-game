using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using BFunCoreKit;

public class PrefabQuickSelector : EditorWindow
{
    private const float WINDOW_WIDTH = 250f;
    private const float HEADER_HEIGHT = 40f;
    private const float ITEM_HEIGHT = 22f; // Tăng nhẹ cho đẹp
    private const float MIN_WINDOW_HEIGHT = 50f;

    private List<PrefabInfo> prefabList = new List<PrefabInfo>();
    private Vector2 scrollPosition;

    // --- MULTI DIGIT INPUT ---
    private string numberBuffer = "";
    private double lastInputTime = 0;
    private const double INPUT_TIMEOUT = 0.15f; // Tăng timeout xíu cho dễ bấm

    private class PrefabInfo
    {
        public string name;
        public string path;
        public int index;
    }

    [MenuItem("Tools/Prefab Quick Selector... #a")]
    public static void ShowWindow()
    {
        if (EditorApplication.isPlaying)
            return;

        // --- [MỚI] ĐÓNG CỬA SỔ SCENE NẾU ĐANG MỞ ---
        if (HasOpenInstances<SceneQuickSelector>())
        {
            GetWindow<SceneQuickSelector>().Close();
        }
        // -------------------------------------------

        // Check xem Prefab Selector đã mở chưa để đóng (Toggle) hoặc mở mới
        if (HasOpenInstances<PrefabQuickSelector>())
        {
            GetWindow<PrefabQuickSelector>().Close();
        }
        else
        {
            var window = GetWindow<PrefabQuickSelector>(true, "Prefab Selector", true);
            window.RefreshPrefabList();
        }
    }

    private void OnEnable()
    {
        RefreshPrefabList();
    }

    private void OnDisable()
    {
        EditorApplication.update -= CheckBufferTimeout;
    }

    private void RefreshPrefabList()
    {
        prefabList.Clear();

        if (!Directory.Exists(GlobalConst.CanvasFolder))
        {
            // Nếu folder chưa tồn tại, chỉ hiện thông báo
            AdjustWindowSize();
            Repaint();
            return;
        }

        string[] prefabPaths = Directory.GetFiles(GlobalConst.CanvasFolder, "*.prefab", SearchOption.TopDirectoryOnly);

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            string path = prefabPaths[i].Replace("\\", "/");
            prefabList.Add(new PrefabInfo
            {
                name = Path.GetFileNameWithoutExtension(path),
                path = path,
                index = i
            });
        }

        AdjustWindowSize();
        Repaint();
    }

    private void AdjustWindowSize()
    {
        // Tính chiều cao dựa trên số lượng item
        float totalItemHeight = prefabList.Count * ITEM_HEIGHT;
        float desiredHeight = HEADER_HEIGHT + totalItemHeight + 10f; // Padding bottom

        if (prefabList.Count == 0)
            desiredHeight = MIN_WINDOW_HEIGHT;

        // Giới hạn chiều cao tối đa (ví dụ 600px) để không tràn màn hình
        float finalHeight = Mathf.Clamp(desiredHeight, MIN_WINDOW_HEIGHT, 600f);

        // --- KHÓA KÍCH THƯỚC ---
        Vector2 finalSize = new Vector2(WINDOW_WIDTH, finalHeight);
        this.minSize = finalSize;
        this.maxSize = finalSize;
    }

    private void OnGUI()
    {
        // Header
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Type Number to Open", EditorStyles.boldLabel);

        // Hiển thị buffer đang nhập
        if (!string.IsNullOrEmpty(numberBuffer))
        {
            GUIStyle inputStyle = new GUIStyle(EditorStyles.label);
            inputStyle.normal.textColor = Color.yellow;
            inputStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField($"Goto: {numberBuffer}...", inputStyle);
        }
        else
        {
            EditorGUILayout.LabelField("", EditorStyles.miniLabel); // Hack giữ layout
        }
        GUILayout.EndVertical();

        HandleKeyboardInput();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (prefabList.Count == 0)
        {
            EditorGUILayout.HelpBox($"No prefabs in:\n{GlobalConst.CanvasFolder}", MessageType.Warning);
        }
        else
        {
            foreach (var prefab in prefabList)
            {
                // Highlight nếu đang gõ số trùng khớp
                bool isHighlight = !string.IsNullOrEmpty(numberBuffer) &&
                                   prefab.index.ToString().StartsWith(numberBuffer);

                UnityEngine.GUI.backgroundColor = isHighlight ? Color.yellow : Color.white;

                string label = $"[{prefab.index}]  {prefab.name}";

                // Style căn trái cho dễ nhìn
                GUIStyle btnStyle = new GUIStyle(EditorStyles.toolbarButton);
                btnStyle.alignment = TextAnchor.MiddleLeft;
                btnStyle.fixedHeight = 20f;

                if (GUILayout.Button(label, btnStyle))
                {
                    TryOpenPrefab(prefab);
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

        // Top row 0-9
        if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
            digit = e.keyCode - KeyCode.Alpha0;

        // Numpad 0-9
        else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
            digit = e.keyCode - KeyCode.Keypad0;

        if (digit == -1)
            return;

        double now = EditorApplication.timeSinceStartup;

        // Reset buffer nếu timeout
        if (now - lastInputTime > INPUT_TIMEOUT)
            numberBuffer = "";

        lastInputTime = now;
        numberBuffer += digit.ToString();

        EditorApplication.update -= CheckBufferTimeout;
        EditorApplication.update += CheckBufferTimeout;

        Repaint(); // Cập nhật GUI để hiện số đang gõ
        e.Use();
    }

    private void CheckBufferTimeout()
    {
        if (EditorApplication.timeSinceStartup - lastInputTime >= INPUT_TIMEOUT)
        {
            if (int.TryParse(numberBuffer, out int prefabIndex))
            {
                PrefabInfo targetPrefab = prefabList.Find(p => p.index == prefabIndex);
                if (targetPrefab != null)
                {
                    TryOpenPrefab(targetPrefab);
                }
                else
                {
                    // Nhập sai thì clear và repaint để tắt highlight
                    numberBuffer = "";
                    Repaint();
                }
            }

            numberBuffer = "";
            EditorApplication.update -= CheckBufferTimeout;
        }
    }

    private void TryOpenPrefab(PrefabInfo prefab)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Cannot open prefabs while in Play Mode.");
            return;
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefab.path);
        if (prefabAsset == null)
        {
            Debug.LogError("Prefab not found: " + prefab.path);
            return;
        }

        BFun.LogEditor("Opened Prefab : " + prefab.name);
        AssetDatabase.OpenAsset(prefabAsset); // Mở Prefab Mode
        this.Close();
    }
}