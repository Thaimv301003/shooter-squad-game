            #if BFUN_INSTALLED_TRUE
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Text;
#if USE_ADMOB
using TheLegends.Base.Ads;
#endif

namespace BFunCoreKit
{
    public static class BFunHelper
    {
        // ===============================
        // 🔹 TRANSFORM HELPERS
        // ===============================
        public static void ResetLocal(Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        public static void SetX(Transform t, float x)
        {
            var pos = t.position;
            pos.x = x;
            t.position = pos;
        }

        public static void SetY(Transform t, float y)
        {
            var pos = t.position;
            pos.y = y;
            t.position = pos;
        }

        public static void SetZ(Transform t, float z)
        {
            var pos = t.position;
            pos.z = z;
            t.position = pos;
        }

        public static void SetRectHeight(this RectTransform rt, float height)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        }

        public static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        // ===============================
        // 🔹 MATH / VECTOR HELPERS
        // ===============================
        public static Vector3 ClampMagnitude(Vector3 v, float min, float max)
        {
            float mag = v.magnitude;
            if (mag < min) return v.normalized * min;
            if (mag > max) return v.normalized * max;
            return v;
        }

        public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }

        public static Vector3 RandomInsideCircle(float radius)
        {
            return UnityEngine.Random.insideUnitCircle * radius;
        }

        public static Vector3 RandomInsideSphere(float radius)
        {
            return UnityEngine.Random.insideUnitSphere * radius;
        }

        public static float Clamp0360(float angle)
        {
            float result = angle % 360f;
            if (result < 0) result += 360f;
            return result;
        }

        // ===============================
        // 🔹 UI HELPERS
        // ===============================

        public static IEnumerator TextAddEffect(TextMeshProUGUI text, string world, float delay = 0.05f, Action onDone = null)
        {
            text.text = "";
            foreach (char letter in world)
            {
                text.text += letter;
                yield return WaitCache.Get(delay);
            }
            onDone?.Invoke();
        }

        public static IEnumerator TextAddEffect(TextMeshPro text, string world, float delay = 0.05f, Action onDone = null)
        {
            text.text = "";
            foreach (char letter in world)
            {
                text.text += letter;
                yield return WaitCache.Get(delay);
            }
            onDone?.Invoke();
        }

        // ===============================
        // 🔹 GAMEOBJECT HELPERS
        // ===============================
        public static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        public static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        // ===============================
        // 🔹 RECTTRANSFORM HELPERS
        // ===============================
        public static void SetRectLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRectRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetRectTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetRectBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        // ===============================
        // 🔹 MISC
        // ===============================
        public static Vector3 WorldToUISpace(Canvas parentCanvas, Vector3 worldPos, Camera mainCamera = null)
        {
            Vector3 screenPos = mainCamera ? mainCamera.WorldToScreenPoint(worldPos) : Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPos,
                parentCanvas.worldCamera,
                out Vector2 movePos);
            return movePos;
        }

        public static bool IsPositionOutsideCamera(Vector3 worldPos, Camera camera = null)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return true; // Không có cam thì coi như không thấy

            Vector3 viewPos = camera.WorldToViewportPoint(worldPos);

            // Viewport: (0,0) là góc dưới trái, (1,1) là góc trên phải.
            // z < 0 nghĩa là nằm sau lưng camera.
            bool isOutside = viewPos.x < 0 || viewPos.x > 1 ||
                             viewPos.y < 0 || viewPos.y > 1 ||
                             viewPos.z < 0;

            return isOutside;
        }

        /// <summary>
        /// Kiểm tra xem vật thể có nằm ngoài camera với một khoảng đệm (offset) không.
        /// Dùng khi vật thể to, tâm ở ngoài nhưng rìa vẫn còn trong màn hình.
        /// </summary>
        public static bool IsPositionOutsideCamera(Vector3 worldPos, float offsetPadding, Camera camera = null)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return true;

            Vector3 viewPos = camera.WorldToViewportPoint(worldPos);

            // Mở rộng hoặc thu hẹp phạm vi check bằng offsetPadding (ví dụ: -0.1f để check rộng hơn màn hình một chút)
            bool isOutside = viewPos.x < -offsetPadding || viewPos.x > (1 + offsetPadding) ||
                             viewPos.y < -offsetPadding || viewPos.y > (1 + offsetPadding) ||
                             viewPos.z < 0;

            return isOutside;
        }

        // --- In danh sách ID cho từng network ---
#if USE_ADMOB
    public static void AppendAdIds(StringBuilder sb, object idObject)
        {
            if (idObject is MaxUnitID max)
            {
                sb.Append(FormatCategory("Banner", max.bannerIds));
                sb.Append(FormatCategory("Inter", max.interIds));
                sb.Append(FormatCategory("Reward", max.rewardIds));
                sb.Append(FormatCategory("AppOpen", max.appOpenIds));
                sb.Append(FormatCategory("MREC", max.mrecIds));
            }
            else if (idObject is AdmobUnitID admob)
            {
                sb.Append(FormatCategory("Banner", admob.bannerIds));
                sb.Append(FormatCategory("Inter", admob.interIds));
                sb.Append(FormatCategory("Reward", admob.rewardIds));
                sb.Append(FormatCategory("AppOpen", admob.appOpenIds));
                sb.Append(FormatCategory("MREC", admob.mrecIds));
            }
        }

    // --- Format từng loại ID ---
    public static string FormatCategory(string title, System.Collections.Generic.List<Placement> list)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"   • <b>{title}</b> :");
        sb.Append(FormatList(list));
        return sb.ToString();
    }

    // --- Format danh sách ID chi tiết ---
    public static string FormatList(System.Collections.Generic.List<Placement> list)
    {
        if (list == null || list.Count == 0) return "      (None)\n";

        var ids = list.SelectMany(p => p.stringIDs).ToList();
        if (ids.Count == 0) return "      (None)\n";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < ids.Count; i++)
            sb.AppendLine($"      <color=#CCCCCC>[ID {i + 1}]</color> {ids[i]}");

        return sb.ToString();
    }
#endif
    }
}
#endif
