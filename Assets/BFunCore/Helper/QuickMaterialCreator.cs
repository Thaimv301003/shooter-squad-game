#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class QuickMaterialCreator : Editor
{
    // Đường dẫn hiển thị khi bấm chuột phải
    const string MENU_PATH = "Assets/Create Materials from Textures";

    [MenuItem(MENU_PATH)]
    public static void CreateMaterials()
    {
        // 1. Xác định Render Pipeline hiện tại để chọn Shader
        string shaderName = "Standard"; // Mặc định là Built-in
        string texturePropertyName = "_MainTex"; // Tên biến texture trong Standard shader

        if (GraphicsSettings.currentRenderPipeline != null)
        {
            // Nếu phát hiện có Scriptable Render Pipeline (thường là URP hoặc HDRP)
            // Ở đây mình set mặc định cho URP Lit theo yêu cầu của bạn
            shaderName = "Universal Render Pipeline/Lit";
            texturePropertyName = "_BaseMap"; // Tên biến texture trong URP Lit
        }

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            // Fallback nếu không tìm thấy URP Lit (ví dụ dùng bản Unity quá cũ hoặc tên shader khác)
            shader = Shader.Find("Mobile/Diffuse");
            Debug.LogWarning($"Không tìm thấy shader '{shaderName}', đang dùng Mobile/Diffuse thay thế.");
        }

        // 2. Duyệt qua tất cả các file đang chọn
        foreach (Object obj in Selection.objects)
        {
            // Chỉ xử lý nếu object là Texture
            if (obj is Texture2D texture)
            {
                CreateMaterialForTexture(texture, shader, texturePropertyName);
            }
        }

        // Lưu lại các thay đổi trong Project
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateMaterialForTexture(Texture2D texture, Shader shader, string propertyName)
    {
        // Lấy đường dẫn của texture
        string texturePath = AssetDatabase.GetAssetPath(texture);

        // Tạo đường dẫn cho Material (thay đuôi file thành .mat)
        string materialPath = System.IO.Path.ChangeExtension(texturePath, ".mat");

        // Kiểm tra xem Material đã tồn tại chưa để tránh ghi đè nhầm
        materialPath = AssetDatabase.GenerateUniqueAssetPath(materialPath);

        // Tạo Material mới
        Material newMat = new Material(shader);
        newMat.SetTexture(propertyName, texture);

        // Tạo file asset thật
        AssetDatabase.CreateAsset(newMat, materialPath);

        // Đăng ký Undo để có thể Ctrl+Z
        Undo.RegisterCreatedObjectUndo(newMat, "Create Material from Texture");

        Debug.Log($"Đã tạo Material: {materialPath}", newMat);
    }

    // Hàm Validation: Chỉ hiện menu khi click chuột phải nếu có chọn ít nhất 1 Texture
    [MenuItem(MENU_PATH, true)]
    public static bool ValidateCreateMaterials()
    {
        if (Selection.objects.Length == 0) return false;

        foreach (Object obj in Selection.objects)
        {
            if (obj is Texture2D) return true;
        }
        return false;
    }
}
#endif