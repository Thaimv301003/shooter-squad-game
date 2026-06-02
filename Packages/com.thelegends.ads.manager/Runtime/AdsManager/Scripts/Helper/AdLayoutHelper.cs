using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

namespace TheLegends.Base.Ads
{
    public class AdLayoutHelper : MonoBehaviour
    {
        public Canvas mainCanvas; // Kéo Canvas của bạn vào đây
        public RectTransform mainContentRect; // Kéo RectTransform gốc của bạn vào đây
        public Image adBackground;

        // Bạn có thể gọi hàm này khi cần điều chỉnh layout
        public void AdjustLayoutForNativeBanner(float bannerHeightInDp)
        {
            if (mainCanvas == null || mainContentRect == null)
            {
                Debug.LogError("Canvas or RectTransform is not set!");
                return;
            }

            // Áp dụng công thức
            float bannerHeightInCanvasUnits = GetCanvasUnitsFromDp(bannerHeightInDp);

            // Giả sử mainContentRect được neo vào 4 góc (stretch-stretch)
            // Đẩy cạnh dưới của nó lên một khoảng bằng chiều cao của banner
            Vector2 offsetMin = mainContentRect.offsetMin;
            offsetMin.y = bannerHeightInCanvasUnits;
            mainContentRect.offsetMin = offsetMin;

            if (adBackground != null)
            {
                adBackground.rectTransform.sizeDelta = new Vector2(mainContentRect.rect.width, bannerHeightInCanvasUnits);
            }

            Debug.Log($"Calculated banner height in Canvas Units: {bannerHeightInCanvasUnits}. Adjusting bottom padding.");
        }

        /// <summary>
        /// Chuyển đổi một giá trị DP (Density-independent Pixels) của Android
        /// thành đơn vị tương đương trong hệ thống Canvas của Unity.
        /// </summary>
        /// <param name="dp">Giá trị DP cần chuyển đổi.</param>
        /// <returns>Giá trị tương đương trong đơn vị Canvas.</returns>
        public float GetCanvasUnitsFromDp(float dp)
        {
            // Lấy DPI của màn hình. Trả về một giá trị mặc định nếu không lấy được (ví dụ: trong Editor).
            float dpi = Screen.dpi;
            if (dpi == 0)
            {
                // DPI có thể là 0 trong một số trường hợp trong Editor. 
                // Dùng một giá trị trung bình để test. 320 (xhdpi) là một lựa chọn tốt.
                dpi = 320;
                Debug.LogWarning($"Screen.dpi is 0 (likely in Editor). Using default value: {dpi}");
            }

            // 1. Tính "density" của Android
            float density = dpi / 160f;

            // 2. Chuyển đổi DP sang Pixel
            float pixels = dp * density;

            // 3. Chuyển đổi Pixel sang đơn vị Canvas
            float canvasUnits = pixels / mainCanvas.scaleFactor;

            return canvasUnits;
        }
    }
}