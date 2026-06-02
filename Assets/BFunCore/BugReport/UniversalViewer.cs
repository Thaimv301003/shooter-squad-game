#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class UniversalViewer : EditorWindow
{
    private Texture2D _image;
    private string _textContent;
    private bool _isImageMode;
    private Vector2 _scrollPos;

    // --- STATIC OPEN METHODS ---

    // Mở xem Ảnh
    public static void OpenImage(Texture2D image, string title)
    {
        var window = GetWindow<UniversalViewer>(true); // true = Utility window
        window.titleContent = new GUIContent(title);
        window._image = image;
        window._isImageMode = true;

        // Tự căn size cửa sổ theo ảnh
        float width = Mathf.Min(image.width, Screen.currentResolution.width * 0.8f);
        float height = Mathf.Min(image.height, Screen.currentResolution.height * 0.8f);
        window.minSize = new Vector2(400, 300);
        window.position = new Rect(100, 100, width, height);
        window.Show();
    }

    // Mở xem Text / Code
    public static void OpenText(string content, string title)
    {
        var window = GetWindow<UniversalViewer>(true);
        window.titleContent = new GUIContent(title);
        window._textContent = content;
        window._isImageMode = false;

        window.minSize = new Vector2(600, 400);
        window.position = new Rect(100, 100, 800, 600);
        window.Show();
    }

    private void OnGUI()
    {
        // Vẽ nền tối cho dễ nhìn
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.15f, 0.15f, 0.15f));

        if (_isImageMode)
        {
            DrawImageMode();
        }
        else
        {
            DrawTextMode();
        }
    }

    private void DrawImageMode()
    {
        if (_image == null) { EditorGUILayout.LabelField("No Image", EditorStyles.whiteLabel); return; }

        // Logic giữ tỉ lệ ảnh (Aspect Ratio)
        float windowAspect = position.width / position.height;
        float imageAspect = (float)_image.width / _image.height;
        Rect drawRect;

        if (imageAspect > windowAspect)
        {
            float h = position.width / imageAspect;
            drawRect = new Rect(0, (position.height - h) * 0.5f, position.width, h);
        }
        else
        {
            float w = position.height * imageAspect;
            drawRect = new Rect((position.width - w) * 0.5f, 0, w, position.height);
        }

        GUI.DrawTexture(drawRect, _image, ScaleMode.ScaleToFit);
    }

    private void DrawTextMode()
    {
        if (string.IsNullOrEmpty(_textContent)) { EditorGUILayout.LabelField("Empty Content", EditorStyles.whiteLabel); return; }

        // Style cho Text (Monospaced cho giống Code)
        GUIStyle textStyle = new GUIStyle(EditorStyles.textArea);
        textStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f); // Chữ trắng
        textStyle.fontSize = 13;
        textStyle.richText = true;
        textStyle.padding = new RectOffset(10, 10, 10, 10);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // Hiển thị toàn bộ nội dung text (Selectable để copy được)
        EditorGUILayout.TextArea(_textContent, textStyle, GUILayout.ExpandHeight(true));

        EditorGUILayout.EndScrollView();
    }
}
#endif