using System.Collections.Generic;
using UnityEngine;

namespace BFunCoreKit
{
    public static class WaitCache
    {
        private static readonly Dictionary<float, WaitForSeconds> secondsCache = new Dictionary<float, WaitForSeconds>();
        private static readonly Dictionary<float, WaitForSecondsRealtime> realTimeCache = new Dictionary<float, WaitForSecondsRealtime>();

        static WaitCache()
        {
            // Cache sẵn WaitForSeconds từ 0.1 -> 5.0
            for (int i = 1; i <= 50; i++)
            {
                float t = i * 0.1f;
                if (!secondsCache.ContainsKey(t))
                    secondsCache[t] = new WaitForSeconds(t);

                if (!realTimeCache.ContainsKey(t))
                    realTimeCache[t] = new WaitForSecondsRealtime(t);
            }
        }

        /// <summary>
        /// Lấy một đối tượng WaitForSeconds đã được cache hoặc tạo mới nếu chưa có.
        /// </summary>
        public static WaitForSeconds Get(float time)
        {
            if (secondsCache.TryGetValue(time, out var wait))
                return wait;

            var newWait = new WaitForSeconds(time);
            secondsCache[time] = newWait;
            return newWait;
        }

        /// <summary>
        /// Lấy một đối tượng WaitForSecondsRealtime đã được cache hoặc tạo mới nếu chưa có.
        /// </summary>
        public static WaitForSecondsRealtime GetRealtime(float time)
        {
            if (realTimeCache.TryGetValue(time, out var wait))
                return wait;

            var newWait = new WaitForSecondsRealtime(time);
            realTimeCache[time] = newWait;
            return newWait;
        }
    }
}
