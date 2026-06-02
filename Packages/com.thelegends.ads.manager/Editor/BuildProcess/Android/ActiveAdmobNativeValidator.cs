using UnityEditor.Android;
using UnityEditor.Build;
using System.IO;
using System.Xml;
using UnityEngine;
using TheLegends.Base.Ads;

/// <summary>
/// A post-build script that adds a meta-data tag to the AndroidManifest.xml
/// to disable the AdMob Native Ad Validator in builds.
/// </summary>
public class ActiveAdmobNativeValidator : IPostGenerateGradleAndroidProject
{
    // A high callback order to run after default scripts.
    public int callbackOrder { get { return 990; } }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        

        Debug.Log("--- DisableAdmobNativeValidator: Executing... ---");

        // Use the shared utility method to find the manifest path.
        string manifestPath = AndroidBuildUtils.GetManifestPath(path);
        if (string.IsNullOrEmpty(manifestPath))
        {
            Debug.LogError("DisableAdmobNativeValidator: AndroidManifest.xml not found.");
            return;
        }

        XmlDocument manifest = new XmlDocument();
        manifest.Load(manifestPath);

        XmlNamespaceManager nsManager = new XmlNamespaceManager(manifest.NameTable);
        nsManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");

        XmlNode applicationNode = manifest.SelectSingleNode("/manifest/application", nsManager);
        if (applicationNode == null)
        {
            Debug.LogError("DisableAdmobNativeValidator: <application> tag not found in the manifest.");
            return;
        }

        string metaDataName = "com.google.android.gms.ads.flag.NATIVE_AD_DEBUGGER_ENABLED";
        string xPath = $"/manifest/application/meta-data[@android:name='{metaDataName}']";
        XmlNode metaDataNode = manifest.SelectSingleNode(xPath, nsManager);

        if (AdsSettings.Instance.isShowAdmobNativeValidator)
        {
            if (metaDataNode != null)
            {
                applicationNode.RemoveChild(metaDataNode);
                Debug.Log("AdmobNativeValidator Processor: Removed NATIVE_AD_DEBUGGER_ENABLED tag to ENABLE the validator.");
            }
            else
            {
                Debug.Log("AdmobNativeValidator Processor: Validator is already enabled (tag not present). No changes needed.");
            }
        }
        else
        {
            if (metaDataNode != null)
            {
                // If the tag already exists, ensure its value is "false".
                XmlAttribute valueAttribute = metaDataNode.Attributes["android:value", "http://schemas.android.com/apk/res/android"];
                if (valueAttribute != null)
                {
                    if (valueAttribute.Value != "false")
                    {
                        valueAttribute.Value = "false";
                        Debug.Log("DisableAdmobNativeValidator: Updated NATIVE_AD_DEBUGGER_ENABLED to 'false'.");
                    }
                }
                else
                {
                    XmlAttribute newValueAttr = manifest.CreateAttribute("android", "value", "http://schemas.android.com/apk/res/android");
                    newValueAttr.Value = "false";
                    metaDataNode.Attributes.Append(newValueAttr);
                    Debug.Log("DisableAdmobNativeValidator: Added value 'false' to NATIVE_AD_DEBUGGER_ENABLED.");
                }
            }
            else
            {
                // If the tag does not exist, create it completely.
                XmlElement newMetaData = manifest.CreateElement("meta-data");

                XmlAttribute nameAttr = manifest.CreateAttribute("android", "name", "http://schemas.android.com/apk/res/android");
                nameAttr.Value = metaDataName;
                newMetaData.Attributes.Append(nameAttr);

                XmlAttribute valueAttr = manifest.CreateAttribute("android", "value", "http://schemas.android.com/apk/res/android");
                valueAttr.Value = "false";
                newMetaData.Attributes.Append(valueAttr);

                applicationNode.AppendChild(newMetaData);
                Debug.Log("DisableAdmobNativeValidator: Added meta-data tag to disable Native Ad Validator.");
            }
        }

        manifest.Save(manifestPath);
        Debug.Log("--- DisableAdmobNativeValidator: Manifest modification successful! ---");
    }
}