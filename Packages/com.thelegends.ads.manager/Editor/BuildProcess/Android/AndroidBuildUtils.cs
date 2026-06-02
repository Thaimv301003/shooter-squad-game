// File: Assets/Editor/AndroidBuildUtils.cs
using System.IO;
using UnityEngine;

/// <summary>
/// A static utility class containing helper methods for the Android build process.
/// </summary>
public static class AndroidBuildUtils
{
    /// <summary>
    /// Finds the path to the main AndroidManifest.xml in the exported Gradle project.
    /// It checks common locations for different Unity versions.
    /// </summary>
    /// <param name="exportedProjectPath">The root path of the exported Gradle project.</param>
    /// <returns>The full path to the AndroidManifest.xml, or null if not found.</returns>
    public static string GetManifestPath(string exportedProjectPath)
    {
        // Path for modern Unity versions (using a launcher module)
        string manifestPath = Path.Combine(exportedProjectPath, "launcher/src/main/AndroidManifest.xml");
        if (File.Exists(manifestPath))
        {
            return manifestPath;
        }

        // Fallback path for older Unity versions or different configurations
        string legacyManifestPath = Path.Combine(exportedProjectPath, "src/main/AndroidManifest.xml");
        if (File.Exists(legacyManifestPath))
        {
            return legacyManifestPath;
        }

        // Another common fallback path
        string unityLibraryPath = Path.Combine(exportedProjectPath, "unityLibrary/src/main/AndroidManifest.xml");
        if (File.Exists(unityLibraryPath))
        {
            return unityLibraryPath;
        }

        return null;
    }
}