#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraTakeScreenShot))]
public class CameraTakeScreenShotEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var myScript = (CameraTakeScreenShot)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Take Screenshot"))
        {
            myScript.SaveScreenshot();
        }

        if (GUILayout.Button("Take All Screenshots"))
        {
            myScript.SaveAllScreenshot();
        }
    }
}
#endif