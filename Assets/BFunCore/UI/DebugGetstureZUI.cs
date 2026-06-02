using BFunCoreKit;
using UnityEngine;

namespace BFunCoreKit
{
    public class DebugGestureToggle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _requiredTaps = 4;
        [SerializeField] private float _timeLimit = 2.0f;
        [SerializeField] private float _cornerSize = 200f;

        private int _currentTaps = 0;
        private float _lastTapTime = 0f;

        // void Update()
        // {
        //     // --- PC: Backquote = TOGGLE (Bật/Tắt) ---
        //     // if (Input.GetKeyDown(KeyCode.BackQuote))
        //     // {
        //     //     ToggleDebugPanel(); // Gọi hàm bật tắt
        //     //     return;
        //     // }

        //     // --- Mobile: Tap góc màn hình = SHOW ONLY (Chỉ mở) ---
        //     // Dùng GetMouseButtonDown(0) để bắt cả click chuột lẫn touch
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         Vector2 clickPos = Input.mousePosition;

        //         // Kiểm tra góc trên bên trái
        //         // (Input.mousePosition gốc tọa độ là dưới-trái)
        //         bool inCorner = (clickPos.x <= _cornerSize) && (clickPos.y >= Screen.height - _cornerSize);

        //         if (inCorner)
        //         {
        //             float currentTime = Time.unscaledTime;

        //             // Nếu gõ quá chậm -> reset đếm lại từ đầu
        //             if (currentTime - _lastTapTime > _timeLimit)
        //             {
        //                 _currentTaps = 0;
        //             }

        //             _currentTaps++;
        //             _lastTapTime = currentTime;

        //             // Đủ số lần thì kích hoạt
        //             if (_currentTaps >= _requiredTaps)
        //             {
        //                 ToggleDebugPanel();
        //                 _currentTaps = 0;
        //             }
        //         }
        //         else
        //         {
        //             // Bấm ra ngoài khu vực -> Reset luôn
        //             _currentTaps = 0;
        //         }
        //     }
        // }

        // Hàm 1: Bật / Tắt (Dùng cho PC)
        private void ToggleDebugPanel()
        {
            // GUIManager.ShowDebugConsole() bản chất là gọi DebugConsole.ToggleConsole()
            if (GUIManager.Instance != null)
            {
                GUIManager.Instance.ShowDebugConsole();
            }
        }

        // Hàm 2: Chỉ Mở (Dùng cho Mobile - để tránh lỡ tay tap nhầm bị tắt mất)
        private void OpenDebugPanelOnly()
        {
            // Gọi thẳng vào DebugConsole.Instance.ShowConsole() (Hàm mới thêm ở bước trước)
            if (DebugConsole.Instance != null)
            {
                DebugConsole.Instance.ShowConsole();
            }
        }

//#if UNITY_EDITOR
//        void OnGUI()
//        {
//            // Vẽ ô màu đỏ mờ để dễ căn chỉnh vùng bấm trên Editor
//            UnityEngine.GUI.color = new Color(1, 0, 0, 0.3f);
//            UnityEngine.GUI.DrawTexture(new Rect(0, 0, _cornerSize, _cornerSize), Texture2D.whiteTexture);
//        }
//#endif
}
}