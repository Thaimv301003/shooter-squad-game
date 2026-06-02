using BFunCoreKit;
using UnityEngine;

public static class DeviceProfiler
{
    // Enum này khớp với index trong GraphicSettingsDatabaseSO (0=Low, 1=Medium, 2=High)
    public enum PerformanceTier
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public static PerformanceTier AutoDetectTier()
    {
        int ram = SystemInfo.systemMemorySize;
        string gpuName = SystemInfo.graphicsDeviceName.ToLower();

        // Ưu tiên 1: Phân loại dựa trên RAM trước
        // Các máy có RAM cực thấp chắc chắn là Low-end, không cần xét GPU
        if (ram <= 3072) // <= 3GB
        {
            BFun.Log($"[DeviceProfiler] Detected Low Tier (Reason: RAM <= 3GB)");
            return PerformanceTier.Low;
        }

        // Ưu tiên 2: Phân loại dựa trên GPU cho các máy có RAM khá hơn
        // Đây là ví dụ, bạn có thể thêm nhiều model GPU hơn
        if (IsHighEndGpu(gpuName))
        {
            BFun.Log($"[DeviceProfiler] Detected High Tier (Reason: High-end GPU '{gpuName}')");
            return PerformanceTier.High;
        }

        if (IsLowEndGpu(gpuName))
        {
            BFun.Log($"[DeviceProfiler] Detected Low Tier (Reason: Low-end GPU '{gpuName}')");
            return PerformanceTier.Low;
        }

        // Ưu tiên 3: Dùng RAM làm tiêu chí cuối cùng cho các trường hợp không xác định
        if (ram >= 6144) // >= 6GB
        {
            BFun.Log($"[DeviceProfiler] Detected High Tier (Reason: RAM >= 6GB)");
            return PerformanceTier.High;
        }

        if (ram >= 4096) // >= 4GB
        {
            BFun.Log($"[DeviceProfiler] Detected Medium Tier (Reason: RAM >= 4GB)");
            return PerformanceTier.Medium;
        }

        // Mặc định an toàn nhất là Low
        BFun.Log($"[DeviceProfiler] Detected Low Tier (Reason: Fallback)");
        return PerformanceTier.Low;
    }

    private static bool IsHighEndGpu(string gpuName)
    {
        // Các dòng GPU cao cấp (Adreno 650/7xx, Mali G78/G7xx, Apple A14+)
        return gpuName.Contains("adreno 650") || gpuName.Contains("adreno 660") || gpuName.Contains("adreno 7") || // Adreno 7xx series
               gpuName.Contains("mali-g78") || gpuName.Contains("mali-g710") || gpuName.Contains("mali-g715") ||
               gpuName.Contains("apple a14") || gpuName.Contains("apple a15") || gpuName.Contains("apple a16") || gpuName.Contains("apple m"); // Apple M series
    }

    private static bool IsLowEndGpu(string gpuName)
    {
        // Các dòng GPU cũ hoặc cấp thấp (Adreno 61x, Mali G5x)
        return gpuName.Contains("adreno 610") || gpuName.Contains("adreno 612") || gpuName.Contains("adreno 616") || gpuName.Contains("adreno 618") ||
               gpuName.Contains("mali-g52") || gpuName.Contains("mali-g57");
    }
}