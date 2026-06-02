#if BFUN_INSTALLED_TRUE && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using LitMotion.Animation;
using BFunCoreKit;

[InitializeOnLoad]
public static class PanelSceneToolbar
{
    // --- STATE ---
    private static Panel currentPanel;
    private static int selectedShowIndex = 0;
    private static int selectedCloseIndex = 0;

    private static List<string> cachedShowOptions = new List<string>();
    private static List<string> cachedCloseOptions = new List<string>();
    private static int lastObjectId = 0;

    // Timer cho Play
    private static double animStartTime;
    private static float estimatedDuration = 1f;
    private static bool isTrackingTime = false;

    // Timer cho Stop (Cơ chế mới: Refresh liên tiếp vài frame sau khi Stop)
    private static int stopRefreshFrames = 0;

    // --- STYLES ---
    private static GUIStyle buttonStyle;
    private static GUIStyle headerStyle;
    private static GUIStyle statusStyle;
    private static bool stylesInitialized = false;

    static PanelSceneToolbar()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    static void InitializeStyles()
    {
        if (stylesInitialized) return;

        buttonStyle = new GUIStyle(EditorStyles.miniButton);
        buttonStyle.fixedHeight = 22;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontStyle = FontStyle.Normal;
        buttonStyle.fontSize = 10;

        headerStyle = new GUIStyle(EditorStyles.label);
        headerStyle.normal.textColor = Color.white;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontStyle = FontStyle.Normal;
        headerStyle.fontSize = 12;

        statusStyle = new GUIStyle(EditorStyles.boldLabel);
        statusStyle.normal.textColor = Color.white;
        statusStyle.alignment = TextAnchor.MiddleCenter;
        statusStyle.fontStyle = FontStyle.Normal;
        statusStyle.fontSize = 12;

        stylesInitialized = true;
    }

    static void OnEditorUpdate()
    {
        // 1. Xử lý khi đang Play (Update liên tục)
        if (isTrackingTime && currentPanel != null)
        {
            if (!currentPanel.isPlaying && (EditorApplication.timeSinceStartup - animStartTime > estimatedDuration))
            {
                isTrackingTime = false;
                // Khi tự động hết giờ -> Chuyển sang chế độ Stop Refresh để chốt hạ frame cuối
                stopRefreshFrames = 5;
            }
            ForceRepaint();
        }

        // 2. Xử lý khi vừa bấm Stop (Update thêm vài frame nữa cho chắc ăn)
        if (stopRefreshFrames > 0)
        {
            stopRefreshFrames--;
            ForceRepaint();

            // Ép Canvas tính toán lại Layout mỗi frame trong giai đoạn này
            if (stopRefreshFrames % 2 == 0) // Mỗi 2 frame update canvas 1 lần
            {
                UnityEngine.Canvas.ForceUpdateCanvases();
            }
        }
    }

    // Hàm vẽ lại mạnh mẽ nhất
    static void ForceRepaint()
    {
        // Ép Editor tính toán logic
        EditorApplication.QueuePlayerLoopUpdate();

        // Vẽ lại Scene View
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Repaint();

        // Vẽ lại Game View và Inspector (đề phòng)
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        InitializeStyles();

        GameObject go = Selection.activeGameObject;
        if (go != null)
        {
            Panel p = go.GetComponentInParent<Panel>();
            if (p != null && currentPanel != p)
            {
                currentPanel = p;
                UpdateOptionsData(p);
            }
        }

        if (currentPanel == null) return;

        DrawToolbar(sceneView);
    }

    static void UpdateOptionsData(Panel panel)
    {
        cachedShowOptions.Clear();
        cachedCloseOptions.Clear();
        selectedShowIndex = 0;
        selectedCloseIndex = 0;
        isTrackingTime = false;
        stopRefreshFrames = 0;

        SerializedObject so = new SerializedObject(panel);
        SerializedProperty popupProp = so.FindProperty("effectPopup");

        if (popupProp != null)
        {
            ExtractOptions(popupProp, "showEffects", cachedShowOptions);
            ExtractOptions(popupProp, "closeEffects", cachedCloseOptions);
        }
    }

    static void ExtractOptions(SerializedProperty root, string arrayName, List<string> list)
    {
        SerializedProperty arrayProp = root.FindPropertyRelative(arrayName);
        if (arrayProp != null && arrayProp.isArray)
        {
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
                SerializedProperty optionName = element.FindPropertyRelative("showOption");
                if (optionName != null) list.Add(optionName.stringValue);
            }
        }
        if (list.Count == 0) list.Add("Default");
    }

    static string GetCleanName(string originalName)
    {
        return originalName.Replace("-", "").Replace(">", "").Replace("<", "").Trim();
    }

    // --- TÍNH TOÁN THỜI GIAN (Giữ nguyên logic cũ) ---
    static float CalculateAccurateDuration(Panel panel, string optionName, bool isShow)
    {
        EffectGroup[] groups = isShow ? GetEffectGroups(panel, "showEffects") : GetEffectGroups(panel, "closeEffects");
        if (groups == null) return 1f;

        EffectGroup targetGroup = new EffectGroup();
        bool found = false;
        foreach (var g in groups) { if (g.showOption == optionName) { targetGroup = g; found = true; break; } }
        if (!found && groups.Length > 0) targetGroup = groups[0];

        float totalDuration = 0f;
        if (targetGroup.effect != null) totalDuration += CalculateLitMotionDuration(targetGroup.effect);

        if (targetGroup.effectGroup != null)
        {
            float cursor = totalDuration;
            for (int i = 0; i < targetGroup.effectGroup.Length; i++)
            {
                var item = targetGroup.effectGroup[i];
                float itemDur = 0f;
                if (item.litMotionAnimation != null) itemDur = CalculateLitMotionDuration(item.litMotionAnimation);

                if (item.panelDelay == PANELDELAY.Sequence) cursor += item.delayTime + itemDur;
                else
                {
                    float branch = cursor + item.delayTime + itemDur;
                    if (branch > totalDuration) totalDuration = branch;
                }
            }
            if (cursor > totalDuration) totalDuration = cursor;
        }
        return Mathf.Max(totalDuration, 0.5f);
    }

    static float CalculateLitMotionDuration(LitMotionAnimation anim)
    {
        if (anim == null) return 0f;
        SerializedObject so = new SerializedObject(anim);
        SerializedProperty componentsProp = so.FindProperty("components");
        if (componentsProp == null || !componentsProp.isArray) return 0f;

        float timeline = 0f;
        float maxDuration = 0f;

        for (int i = 0; i < componentsProp.arraySize; i++)
        {
            SerializedProperty comp = componentsProp.GetArrayElementAtIndex(i);
            if (comp == null) continue;

            SerializedProperty joinTypeProp = comp.FindPropertyRelative("joinType");
            SerializedProperty joinDelayProp = comp.FindPropertyRelative("joinDelay");
            int joinType = joinTypeProp != null ? joinTypeProp.enumValueIndex : 0;
            float joinDelay = joinDelayProp != null ? joinDelayProp.floatValue : 0f;

            SerializedProperty settings = comp.FindPropertyRelative("settings");
            float duration = 0f; float delay = 0f; int loops = 1;

            if (settings != null)
            {
                SerializedProperty d = settings.FindPropertyRelative("duration");
                SerializedProperty dl = settings.FindPropertyRelative("delay");
                SerializedProperty l = settings.FindPropertyRelative("loops");
                if (d != null) duration = d.floatValue;
                if (dl != null) delay = dl.floatValue;
                if (l != null) loops = l.intValue;
            }

            float singleItemTotal = joinDelay + delay + (duration * loops);
            if (joinType == 1) { timeline += singleItemTotal; if (timeline > maxDuration) maxDuration = timeline; }
            else { float branchEnd = timeline + singleItemTotal; if (branchEnd > maxDuration) maxDuration = branchEnd; }
        }
        return Mathf.Max(timeline, maxDuration);
    }

    static EffectGroup[] GetEffectGroups(Panel p, string fieldName)
    {
        var field = typeof(PopupStruct).GetField(fieldName);
        if (field == null) return null;
        var popupField = typeof(Panel).GetField("effectPopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (popupField == null) return null;
        var popupVal = popupField.GetValue(p);
        return field.GetValue(popupVal) as EffectGroup[];
    }

    // --- HÀM STOP CỰC MẠNH (MULTI-FRAME REFRESH) ---
    static void ForceStopEverything()
    {
        if (currentPanel != null)
        {
#if UNITY_EDITOR
            // 1. Dừng Logic
            currentPanel.StopEditor();

            // 2. Tắt tracking thời gian
            isTrackingTime = false;

            // 3. Mark Dirty
            EditorUtility.SetDirty(currentPanel);

            // 4. Reset Canvas ngay lập tức
            UnityEngine.Canvas.ForceUpdateCanvases();

            // 5. QUAN TRỌNG: Kích hoạt chế độ "Refresh liên tục" trong 5 frame tiếp theo
            // Điều này đảm bảo nếu Canvas chưa kịp cập nhật frame này thì frame sau nó sẽ cập nhật
            stopRefreshFrames = 10;

            // 6. Vẽ lại ngay frame này
            ForceRepaint();
#endif
        }
    }

    static void DrawToolbar(SceneView sceneView)
    {
        Handles.BeginGUI();

        float width = 420f;
        float height = 75f;
        float paddingBottom = 40f;

        float x = (sceneView.position.width - width) / 2f;
        float y = sceneView.position.height - height - paddingBottom;

        Rect mainRect = new Rect(x, y, width, height);

        // Background
        EditorGUI.DrawRect(mainRect, new Color(0.08f, 0.08f, 0.08f, 1f));

        // Progress Bar
        if (isTrackingTime)
        {
            float elapsed = (float)(EditorApplication.timeSinceStartup - animStartTime);
            float progress = Mathf.Clamp01(elapsed / estimatedDuration);
            Rect progressRect = new Rect(x, y, width * progress, 3);
            EditorGUI.DrawRect(progressRect, Color.white);
        }
        else
        {
            EditorGUI.DrawRect(new Rect(x, y, width, 2), new Color(0.3f, 0.3f, 0.3f));
        }

        GUILayout.BeginArea(mainRect);

        // Header
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (isTrackingTime)
        {
            float t = (float)(EditorApplication.timeSinceStartup - animStartTime);
            GUILayout.Label($"Playing... {t:F1}s", statusStyle);
        }
        else
        {
            string displayName = GetCleanName(currentPanel.name);
            GUILayout.Label(displayName, headerStyle);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // Controls
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);

        // SHOW
        UnityEngine.GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("► SHOW", buttonStyle, GUILayout.Height(25), GUILayout.Width(60)))
        {
            string option = cachedShowOptions.Count > 0 ? cachedShowOptions[selectedShowIndex] : "Default";
            estimatedDuration = CalculateAccurateDuration(currentPanel, option, true);
            animStartTime = EditorApplication.timeSinceStartup;
            isTrackingTime = true;
            stopRefreshFrames = 0; // Reset stop counter
            currentPanel.PlayEditor(option, true);
        }
        UnityEngine.GUI.backgroundColor = Color.white;
        selectedShowIndex = EditorGUILayout.Popup(selectedShowIndex, cachedShowOptions.ToArray(), GUILayout.Width(80));

        GUILayout.FlexibleSpace();

        // STOP
        UnityEngine.GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("■ STOP", buttonStyle, GUILayout.Height(25), GUILayout.Width(60)))
        {
            ForceStopEverything();
        }
        UnityEngine.GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();

        // CLOSE
        UnityEngine.GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
        if (GUILayout.Button("► CLOSE", buttonStyle, GUILayout.Height(25), GUILayout.Width(60)))
        {
            string option = cachedCloseOptions.Count > 0 ? cachedCloseOptions[selectedCloseIndex] : "Default";
            estimatedDuration = CalculateAccurateDuration(currentPanel, option, false);
            animStartTime = EditorApplication.timeSinceStartup;
            isTrackingTime = true;
            stopRefreshFrames = 0; // Reset stop counter
            currentPanel.PlayEditor(option, false);
        }
        UnityEngine.GUI.backgroundColor = Color.white;
        selectedCloseIndex = EditorGUILayout.Popup(selectedCloseIndex, cachedCloseOptions.ToArray(), GUILayout.Width(80));

        GUILayout.Space(10);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
#endif