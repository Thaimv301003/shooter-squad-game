using System.Collections;
using UnityEngine;
using BFunCoreKit;

public class GameState : MonoBehaviour
{
    [SerializeField] private string nameScene;
    private void Start()
    {
        // Yêu cầu GameManager (đối tượng không bị hủy) thực thi Coroutine chuyển scene
        //GameManager.Instance.StartCoroutine(LoadHomeCoroutine());
    }

    private IEnumerator LoadHomeCoroutine()
    {
        // 1. Chờ 1 frame để đảm bảo các hệ thống đã sẵn sàng
        yield return null;

        // 2. Load scene Home
        yield return LoadManager.LoadScene(nameScene);

        // 3. Chuyển Canvas sang Home trong GUIManager
        yield return GUIManager.Instance.SwitchCanvas(CanvasName.CanvasHome);

        // 4. Tắt Background trong GUIManager
        yield return GUIManager.Instance.CloseBackGround();

        // 5. Hiển thị Ads
        TheLegends.Base.Ads.AdsManager.Instance.ShowInterstitial(
            TheLegends.Base.Ads.AdsType.Interstitial, 
            TheLegends.Base.Ads.PlacementOrder.One, 
            "inter_level"
        );
    }
}
