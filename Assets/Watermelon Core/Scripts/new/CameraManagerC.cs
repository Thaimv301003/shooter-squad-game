using UnityEngine;
using UnityEngine.Rendering.Universal;
using BFunCoreKit;
using TheLegends.Base.Ads;

public class CameraManagerC : MonoBehaviour
{
    public static CameraManagerC Instance { get; private set; }
    public Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StackSceneInit()
    {
        mainCamera.gameObject.SetActive(true);
        mainCamera.GetUniversalAdditionalCameraData().cameraStack.Clear();  
        mainCamera.GetUniversalAdditionalCameraData().cameraStack.Add(GUIManager.Instance.UICamera);
        mainCamera.GetUniversalAdditionalCameraData().cameraStack.Add(AdsManager.Instance.AdsCamera);
    }

    public void StackCameraAdd(Camera cam)
    {
        cam.GetUniversalAdditionalCameraData().cameraStack.Clear();
        cam.GetUniversalAdditionalCameraData().cameraStack.Add(GUIManager.Instance.UICamera);
        cam.GetUniversalAdditionalCameraData().cameraStack.Add(AdsManager.Instance.AdsCamera);

        mainCamera.gameObject.SetActive(false);
    }



}
