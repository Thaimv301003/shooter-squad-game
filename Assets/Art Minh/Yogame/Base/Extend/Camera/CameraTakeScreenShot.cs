using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class CameraTakeScreenShot : MonoBehaviour
{
    protected int UpScale = 4;
    [SerializeField]
    protected Camera cameraMain = null;
    [SerializeField]
    protected float waitToTake = 1f;

    [SerializeField]
    protected GameObject[] gameObjects = null;

    private Texture2D Screenshot()
    {
        int w = cameraMain.pixelWidth * UpScale;
        int h = cameraMain.pixelHeight * UpScale;
        var rt = new RenderTexture(w, h, 32);
        cameraMain.targetTexture = rt;
        var screenShot = new Texture2D(w, h, TextureFormat.ARGB32, false);
        cameraMain.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        screenShot.Apply();
        cameraMain.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        return screenShot;
    }

    [ContextMenu("Capture Screenshot")]
    public void SaveScreenshot(string fileName = null)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (string.IsNullOrEmpty(fileName))
            fileName = "SS-" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss") + ".png";
        File.WriteAllBytes(Path.Combine(path, fileName), Screenshot().EncodeToPNG());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveScreenshot("icon.png");
        }
    }

    [ContextMenu("Capture All Screenshots")]
    public void SaveAllScreenshot()
    {
        StartCoroutine(DOTakeAllScreenshot());
    }

    public IEnumerator DOTakeAllScreenshot()
    {
        for (int i = 0; i < gameObjects.Length; i++)
        {
            gameObjects[i].SetActive(false);
        }

        for (int i = 0; i < gameObjects.Length; i++)
        {
            for (int ii = 0; ii < gameObjects.Length; ii++)
            {
                gameObjects[ii].SetActive(i == ii);
            }
            yield return new WaitForSeconds(waitToTake);
            yield return new WaitForEndOfFrame();
            SaveScreenshot(gameObjects[i].name + ".png");
            yield return new WaitForEndOfFrame();
        }
    }
}
