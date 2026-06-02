using UnityEngine;
using Unity.Profiling;

public class SystemMonitor : MonoBehaviour
{
    [Header("--- CẤU HÌNH ---")]
    public bool showMonitor = true;
    [Range(0.5f, 2.0f)] public float uiScale = 1.0f;

    [Header("--- FPS SETTINGS ---")]
    public float updateInterval = 0.5f;

    [Header("--- GIỚI HẠN ---")]
    public float maxRamMB = 1500f;
    public float maxGcKB = 100f;

    ProfilerRecorder renderRec, scriptRec, physicsRec, gcRec;
    int frames = 0;
    float timer = 0f;
    float currentFPS = 60f;

    // Biến hiển thị (Đã bỏ dBat)
    float dRender, dScript, dRam, dGc, dThermal;
    Texture2D bgTex, fillTex;
    GUIStyle labelStyle, valueStyle;

    void OnEnable()
    {
        renderRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Camera.Render");
        scriptRec = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "Update");
        physicsRec = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "FixedUpdate");
        gcRec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

        bgTex = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        fillTex = MakeTex(2, 2, Color.white);
    }

    void OnDisable()
    {
        renderRec.Dispose(); scriptRec.Dispose();
        physicsRec.Dispose(); gcRec.Dispose();
        if (bgTex) Destroy(bgTex);
        if (fillTex) Destroy(fillTex);
    }

    void Update()
    {
        if (!showMonitor) return;

        // FPS Logic
        frames++;
        timer += Time.unscaledDeltaTime;
        if (timer >= updateInterval)
        {
            currentFPS = frames / timer;
            frames = 0; timer = 0f;
        }

        // Smoothing Logic
        float lerp = Time.unscaledDeltaTime * 10f;
        dRender = Mathf.Lerp(dRender, GetMs(renderRec), lerp);
        dScript = Mathf.Lerp(dScript, GetMs(scriptRec), lerp);
        dRam = Mathf.Lerp(dRam, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f), lerp);
        dGc = Mathf.Lerp(dGc, gcRec.Valid ? gcRec.LastValue / 1024f : 0, lerp);

        // Thermal Logic (Dựa trên FPS Load)
        float loadFactor = Mathf.Clamp01(1.0f - (currentFPS / GetCurrentTargetFPS()));
        dThermal = Mathf.Lerp(dThermal, loadFactor, lerp * 0.5f);
    }

    void OnGUI()
    {
        if (!showMonitor) return;

        if (bgTex == null) bgTex = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        if (fillTex == null) fillTex = MakeTex(2, 2, Color.white);

        SetupStyles();

        float targetFrameRate = GetCurrentTargetFPS();
        float msBudget = 1000f / targetFrameRate;

        float w = 300 * uiScale;
        float h = 22 * uiScale;
        float x = 20;
        float y = 20;
        float gap = 6 * uiScale;

        // --- VẼ CÁC THANH (Đã bỏ Battery) ---

        DrawBar(new Rect(x, y, w, h), "Total FPS", currentFPS, targetFrameRate, true, $"{currentFPS:0} FPS");
        y += h + gap;

        // 2. Render
        DrawBar(new Rect(x, y, w, h), "Render", dRender, msBudget, false, $"{dRender:F1} ms");
        y += h + gap;

        // 3. Scripts
        DrawBar(new Rect(x, y, w, h), "Scripts", dScript, msBudget, false, $"{dScript:F1} ms");
        y += h + gap;

        // 4. RAM
        y += 4;
        DrawBar(new Rect(x, y, w, h), "RAM", dRam, maxRamMB, false, $"{dRam:0} MB");
        y += h + gap;

        // 5. GC Alloc
        DrawBar(new Rect(x, y, w, h), "GC Alloc", dGc, maxGcKB, false, $"{dGc:F1} KB");
        y += h + gap;

        // 6. FPS Total
        y += 4;
        // 1. Thermal (Lên đầu)
        string tText = dThermal > 0.5f ? (dThermal > 0.8f ? "HOT!" : "Warm") : "Cool";
        DrawBar(new Rect(x, y, w, h), "Thermal", dThermal, 1f, false, tText);
    }

    void DrawBar(Rect rect, string label, float val, float max, bool invert, string valText)
    {
        float pct = Mathf.Clamp01(val / max);

        // Nền
        GUI.color = Color.white;
        GUI.DrawTexture(rect, bgTex);

        // Thanh màu
        Color c = invert
            ? Color.Lerp(Color.red, Color.green, pct)
            : Color.Lerp(Color.green, Color.red, pct);

        GUI.color = c;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * pct, rect.height), fillTex);

        // --- TEXT (Shadow Style) ---
        Rect leftRect = new Rect(rect.x + 5, rect.y, rect.width, rect.height);
        Rect rightRect = new Rect(rect.x, rect.y, rect.width - 5, rect.height);

        // Bóng đen
        GUI.color = Color.black;
        GUI.Label(new Rect(leftRect.x + 1, leftRect.y + 1, leftRect.width, leftRect.height), label, labelStyle);
        GUI.Label(new Rect(rightRect.x + 1, rightRect.y + 1, rightRect.width, rightRect.height), valText, valueStyle);

        // Chữ trắng
        GUI.color = Color.white;
        GUI.Label(leftRect, label, labelStyle);
        GUI.Label(rightRect, valText, valueStyle);
    }

    float GetCurrentTargetFPS()
    {
        return (Application.targetFrameRate <= 0) ? 60f : (float)Application.targetFrameRate;
    }

    void SetupStyles()
    {
        if (labelStyle == null) labelStyle = new GUIStyle();
        if (valueStyle == null) valueStyle = new GUIStyle();

        int s = (int)(13 * uiScale);

        labelStyle.normal.background = null;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = s;

        valueStyle.normal.background = null;
        valueStyle.fontStyle = FontStyle.Bold;
        valueStyle.alignment = TextAnchor.MiddleRight;
        valueStyle.normal.textColor = Color.white;
        valueStyle.fontSize = s;
    }

    float GetMs(ProfilerRecorder r) { return r.Valid ? r.LastValue * 1e-6f : 0; }

    Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D t = new Texture2D(w, h); t.SetPixels(pix); t.Apply(); return t;
    }
}