// FILE: LogEntryUI.cs
#if BFUN_INSTALLED_TRUE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BFunCoreKit
{
    public class LogEntryUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text stackTraceText; // Kéo cái Text to dùng để hiện StackTrace vào đây
        [SerializeField] private Image logTypeIcon;
        [SerializeField] private Button button; // Button để click expand

        [Header("Assets")]
        [SerializeField] private Sprite logIcon;
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite errorIcon;

        private LogEntry currentLog;

        public void Setup(LogEntry logEntry)
        {
            currentLog = logEntry;

            // 1. Setup nội dung Message
            messageText.text = currentLog.Message;

            // 2. Setup nội dung StackTrace (Sử dụng chuỗi đã cache)
            stackTraceText.text = currentLog.CachedStackTrace;

            // 3. Setup Icon
            UpdateIcon();

            // 4. Setup Button state
            if (button != null)
            {
                // Nếu là lệnh gõ từ console thì không cho click expand
                button.interactable = !logEntry.IsFromCommand;
                // Xóa listener cũ để tránh duplicate khi pooling
            }

            // 5. Cập nhật trạng thái hiển thị (Mở/Đóng)
        }

        private void UpdateIcon()
        {
            switch (currentLog.Type)
            {
                case LogType.Log:
                    logTypeIcon.sprite = logIcon;
                    break;
                case LogType.Warning:
                    logTypeIcon.sprite = warningIcon;
                    break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    logTypeIcon.sprite = errorIcon;
                    break;
            }
        }

        public void OnLogClicked()
        {
            // Nếu không có stacktrace thì ko làm gì cả
            if (currentLog.StackTraceLines == null || currentLog.StackTraceLines.Count == 0) return;

            currentLog.IsExpanded = !currentLog.IsExpanded;
            UpdateDisplay();

            // QUAN TRỌNG: Báo cho Layout Group tính toán lại ngay lập tức
            // Nếu không có dòng này, UI có thể bị giật 1 frame trước khi giãn ra đúng kích thước
           LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }

        private void UpdateDisplay()
        {
            // Chỉ cần Bật/Tắt GameObject chứa StackTrace
            // ContentSizeFitter ở Root sẽ tự lo việc co giãn chiều cao
            if (stackTraceText != null)
            {
                stackTraceText.gameObject.SetActive(currentLog.IsExpanded);
            }
        }
    }
}
#endif