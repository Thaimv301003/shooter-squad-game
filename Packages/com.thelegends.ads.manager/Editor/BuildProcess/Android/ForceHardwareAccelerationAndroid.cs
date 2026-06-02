using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Xml;
using UnityEngine;

/// <summary>
/// This script runs after Unity generates the Android project
/// to ensure the hardwareAccelerated attribute is always set to "true" for the main Activity.
/// </summary>
public class ForceHardwareAccelerationAndroid : IPostGenerateGradleAndroidProject
{
    public int callbackOrder { get { return 999; } }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log("--- ForceHardwareAcceleration: Starting execution ---");

        // Find the path to the AndroidManifest.xml file in the exported project.
        string manifestPath = AndroidBuildUtils.GetManifestPath(path); ;

        if (string.IsNullOrEmpty(manifestPath))
        {
            Debug.LogError("ForceHardwareAcceleration: AndroidManifest.xml not found at: " + manifestPath);
            return;
        }

        Debug.Log("ForceHardwareAcceleration: Found AndroidManifest.xml. Starting modification...");

        // Use XmlDocument to safely read and modify the XML file.
        XmlDocument manifest = new XmlDocument();
        manifest.Load(manifestPath);

        // Create an XmlNamespaceManager to work with the 'android:' namespace.
        XmlNamespaceManager nsManager = new XmlNamespaceManager(manifest.NameTable);
        nsManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");

        // Find the correct <activity> tag for UnityPlayerActivity.
        string xPath = "/manifest/application/activity[@android:name='com.unity3d.player.UnityPlayerActivity']";
        XmlNode activityNode = manifest.SelectSingleNode(xPath, nsManager);

        if (activityNode == null)
        {
            Debug.LogWarning("ForceHardwareAcceleration: Activity tag for UnityPlayerActivity not found.");
            return;
        }

        // Get the hardwareAccelerated attribute.
        XmlAttribute attribute = activityNode.Attributes["android:hardwareAccelerated"];

        if (attribute != null)
        {
            // If the attribute already exists, update its value.
            Debug.Log("ForceHardwareAcceleration: The hardwareAccelerated attribute already exists with value '" + attribute.Value + "'. Overwriting to 'true'.");
            attribute.Value = "true";
        }
        else
        {
            // If the attribute does not exist, create and add it.
            Debug.Log("ForceHardwareAcceleration: The hardwareAccelerated attribute does not exist. Creating and setting to 'true'.");
            XmlAttribute newAttribute = manifest.CreateAttribute("android", "hardwareAccelerated", "http://schemas.android.com/apk/res/android");
            newAttribute.Value = "true";
            activityNode.Attributes.Append(newAttribute);
        }

        // Save the changes to the manifest file.
        manifest.Save(manifestPath);
        Debug.Log("--- ForceHardwareAcceleration: Modification successful! ---");
    }
}