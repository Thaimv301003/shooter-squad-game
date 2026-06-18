using UnityEngine;
using System.Collections;
using System.IO;

public class CarPrefabScreenshot : MonoBehaviour
{
    [Header("References")]
    public Camera captureCamera;
    public Transform carSlot;
    public GameObject[] carPrefabs;

    [Header("Screenshot")]
    public int width = 1024;
    public int height = 1024;
    public float waitBeforeShot = 0.15f;

    [Header("Optional Rotate")]
    public bool rotateCar = true;
    public float rotateDuration = 0.8f;

    void Start()
    {
        StartCoroutine(CaptureAllCars());
    }

    IEnumerator CaptureAllCars()
    {
        for (int i = 0; i < carPrefabs.Length; i++)
        {
            // Spawn xe
            GameObject car = Instantiate(carPrefabs[i], carSlot);
            ResetTransform(car);

            // Replay animation nếu có
            Animator anim = car.GetComponent<Animator>();
            if (anim != null)
                anim.Play(0, 0, 0f);

            // Xoay xe (tuỳ chọn)
            if (rotateCar)
                yield return StartCoroutine(RotateCar(car));

            yield return new WaitForSeconds(waitBeforeShot);
            yield return new WaitForEndOfFrame();

            TakeScreenshot(carPrefabs[i].name);

            Destroy(car);
            yield return null;
        }

        Debug.Log("✅ Done – Chụp xong tất cả xe prefab");
    }

    void ResetTransform(GameObject car)
    {
        car.transform.localPosition = Vector3.zero;
        car.transform.localRotation = Quaternion.identity;
        car.transform.localScale = Vector3.one;
    }

    IEnumerator RotateCar(GameObject car)
    {
        float t = 0f;
        while (t < rotateDuration)
        {
            car.transform.Rotate(Vector3.up, 360f * Time.deltaTime / rotateDuration);
            t += Time.deltaTime;
            yield return null;
        }
    }

    void TakeScreenshot(string carName)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        captureCamera.targetTexture = rt;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        captureCamera.Render();

        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        string dir = Application.dataPath + "/Screenshots/";
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(dir + carName + ".png", tex.EncodeToPNG());
    }
}
