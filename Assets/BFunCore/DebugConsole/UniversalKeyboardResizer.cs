// FILE: UniversalKeyboardResizer.cs
#if BFUN_INSTALLED_TRUE
using BFunCoreKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BFunCoreKit
{
    [RequireComponent(typeof(RectTransform))]
    public class UniversalKeyboardResizer : MonoBehaviour
    {
        [Header("Mobile Fine-Tuning")]
        [Tooltip("Khoảng đệm bổ sung (bằng pixel) để đẩy UI lên cao hơn trên điện thoại.")]
        [SerializeField]
        private float mobileBottomOffset = 0f;
        [SerializeField] Image imageInput;
        [SerializeField] TextMeshProUGUI textInput;
        private Vector2 initialOffsetMin;

        private RectTransform containerToResize;
        private float lastKeyboardHeight;

        private void Start()
        {
            containerToResize = GetComponent<RectTransform>();
            initialOffsetMin = containerToResize.offsetMin;
        }

        private void Update()
        {
#if !UNITY_EDITOR
            float keyboardHeight = GetKeyboardHeight();
            
            // Chỉ cập nhật nếu có thay đổi chiều cao đáng kể
            if (Mathf.Abs(keyboardHeight - lastKeyboardHeight) > 1f)
            {
                lastKeyboardHeight = keyboardHeight;
                ApplyResizing(keyboardHeight);
            }
#endif
        }

        private float GetKeyboardHeight()
        {
#if UNITY_ANDROID
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var window = currentActivity.Call<AndroidJavaObject>("getWindow");
                var decorView = window.Call<AndroidJavaObject>("getDecorView");
                var rect = new AndroidJavaObject("android.graphics.Rect");
                decorView.Call("getWindowVisibleDisplayFrame", rect);

                // 1. Lấy chiều cao thực tế (Native Physical Pixels)
                int nativeDisplayHeight = Display.main.systemHeight; // Chiều cao gốc phần cứng
                int visibleHeightNative = rect.Call<int>("height");  // Chiều cao vùng nhìn thấy (Native)

                // 2. Tính chiều cao bàn phím theo Native Pixels
                int keyboardHeightNative = nativeDisplayHeight - visibleHeightNative;

                // 3. Tính tỷ lệ Scale giữa độ phân giải hiện tại (đã set resolution) và độ phân giải gốc
                // Screen.height là chiều cao hiện tại sau khi đã Screen.SetResolution
                float scaleRatio = (float)Screen.height / nativeDisplayHeight;

                // 4. Quy đổi chiều cao bàn phím về đơn vị màn hình hiện tại
                return keyboardHeightNative * scaleRatio;
            }
#elif UNITY_IOS
            // Trên iOS, TouchScreenKeyboard.area trả về point/pixel đã được scale theo Unity
            // Nên thường không cần nhân chia phức tạp, nhưng cần dùng đúng API
            return TouchScreenKeyboard.area.height; 
#else
            return 0;
#endif
        }

        private void ApplyResizing(float keyboardHeight)
        {
            if (containerToResize == null || GUIManager.Instance.canvas == null) return;

            // Lúc này keyboardHeight đã cùng đơn vị với Screen.height hiện tại
            float totalOffsetPixels = keyboardHeight + mobileBottomOffset;

            // Chia cho scaleFactor để đổi sang đơn vị của Canvas
            float scaledTotalOffset = totalOffsetPixels / GUIManager.Instance.canvas.scaleFactor;

            if (keyboardHeight > 0)
            {
                if(imageInput != null) imageInput.color = new Color(0, 0, 0, 0);
                if(textInput != null) textInput.color = new Color(0, 0, 0, 0);
                containerToResize.offsetMin = new Vector2(initialOffsetMin.x, scaledTotalOffset);
            }
            else
            {
                if(imageInput != null) imageInput.color = new Color(1, 1, 1, 1);
                if(textInput != null) textInput.color = new Color(1, 1, 1, 1);
                containerToResize.offsetMin = new Vector2(initialOffsetMin.x, initialOffsetMin.y);
            }
        }
    }
}
#endif