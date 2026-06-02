using System.Collections;
using System.Collections.Generic;
#if USE_ADMOB
using GoogleMobileAds.Api;
#endif
using TheLegends.Base.Ads;
#if USE_APPSFLYER
using TheLegends.Base.AppsFlyer;
#endif
#if USE_FIREBASE
using TheLegends.Base.Firebase;
#endif
using UnityEngine;
using UnityEngine.UI;

public class DemoManager : MonoBehaviour
{
    public PlacementOrder order = PlacementOrder.One;
    public Button initBtn;
    public Button loadInterstitialBtn;
    public Button showInterstitialBtn;
    public Button loadRewardedBtn;
    public Button showRewardedBtn;
    public Button loadAppOpenBtn;
    public Button showAppOpenBtn;
    public Button loadBannerBtn;
    public Button showBannerBtn;
    public Button hideBannerBtn;
    public Button loadMrecBtn;
    public Button showMrecBtn;
    public Button hideMrecBtn;
    public Button loadNativeOverlayBtn;
    public Button showNativeOverlayBtn;
    public Button hideNativeOverlayBtn;
    public Button testBtn;
    public Dropdown MrecPosDropdown;
    public Button loadNativeBtn;
    public Button showNativeBtn;
    public Button hideNativeBtn;
    public Button loadNativeVideoPlatformBtn;
    public Button showNativeVideoPlatformBtn;
    public Button hideNativeVideoPlatformBtn;
    public Button loadNativeBannerPlatformBtn;
    public Button showNativeBannerPlatformBtn;
    public Button hideNativeBannerPlatformBtn;
    public Button adjustLayoutForNativeBannerBtn;
    public Button removeAdsBtn;

#if USE_ADMOB && USE_ADMOB_NATIVE_UNITY
    public AdmobNativeController nativeAdsMrec;
    public AdmobNativeController nativeAdsBanner;
#endif


    public Button nativeOverlayCloseBtn;
    public GameObject nativeOverlayBG;

    public AdLayoutHelper adLayoutHelper;


    private void OnEnable()
    {
        initBtn.onClick.AddListener(InitAdsManager);
        loadInterstitialBtn.onClick.AddListener(LoadInterstitial);
        showInterstitialBtn.onClick.AddListener(ShowInterstitial);
        loadRewardedBtn.onClick.AddListener(Loadrewarded);
        showRewardedBtn.onClick.AddListener(ShowRewarded);
        loadAppOpenBtn.onClick.AddListener(LoadAppOpen);
        showAppOpenBtn.onClick.AddListener(ShowAppOpen);
        loadBannerBtn.onClick.AddListener(LoadBanner);
        showBannerBtn.onClick.AddListener(ShowBanner);
        hideBannerBtn.onClick.AddListener(HideBanner);
        loadMrecBtn.onClick.AddListener(LoadMrec);
        showMrecBtn.onClick.AddListener(ShowMrec);
        hideMrecBtn.onClick.AddListener(HideMrec);
        loadNativeOverlayBtn.onClick.AddListener(LoadNativeOverlay);
        showNativeOverlayBtn.onClick.AddListener(ShowNativeOverlay);
        hideNativeOverlayBtn.onClick.AddListener(HideNativeOverlay);
        loadNativeBtn.onClick.AddListener(LoadNative);
        showNativeBtn.onClick.AddListener(ShowNative);
        hideNativeBtn.onClick.AddListener(HideNative);
        nativeOverlayCloseBtn.onClick.AddListener(HideNativeOverlay);
        loadNativeVideoPlatformBtn.onClick.AddListener(LoadNativeVideoPlatform);
        showNativeVideoPlatformBtn.onClick.AddListener(ShowNativeVideoPlatform);
        hideNativeVideoPlatformBtn.onClick.AddListener(HideNativeVideoPlatform);
        loadNativeBannerPlatformBtn.onClick.AddListener(LoadNativeBannerPlatform);
        showNativeBannerPlatformBtn.onClick.AddListener(ShowNativeBannerPlatform);
        hideNativeBannerPlatformBtn.onClick.AddListener(HideNativeBannerPlatform);
        adjustLayoutForNativeBannerBtn.onClick.AddListener(AdjustLayoutForNativeBanner);
        removeAdsBtn.onClick.AddListener(RemoveAds);
    }


    private void InitAdsManager()
    {
        // AdsManager.Instance.Init();
        // FirebaseManager.Instance.Init();
        // AppsFlyerManager.Instance.Init();
        StartCoroutine(DoInit());
    }

    private IEnumerator DoInit()
    {
        yield return AdsManager.Instance.DoInit();

#if USE_FIREBASE
        var defaultRemoteConfig = new Dictionary<string, object>
        {
            {"adInterOnComplete", AdsManager.Instance.adsConfigs.adInterOnComplete},
            {"adInterOnStart", AdsManager.Instance.adsConfigs.adInterOnStart},
            {"timePlayToShowAds", AdsManager.Instance.adsConfigs.timePlayToShowAds},
            {"adNativeBannerHeight", AdsManager.Instance.adsConfigs.adNativeBannerHeight},
            {"adTimeReload", AdsManager.Instance.adsConfigs.adTimeReload}
        };

        yield return FirebaseManager.Instance.DoInit(defaultRemoteConfig);

        FirebaseManager.Instance.FetchRemoteData(() =>
        {
            // var testBool = FirebaseManager.Instance.RemoteGetValueBoolean("testBool", false);
            // var testFloat = FirebaseManager.Instance.RemoteGetValueFloat("testFloat", 1.0f);
            // var testInt = FirebaseManager.Instance.RemoteGetValueInt("testInt", 2);
            // var testString = FirebaseManager.Instance.RemoteGetValueString("testString", "test");
            AdsManager.Instance.adsConfigs.adInterOnComplete = FirebaseManager.Instance.RemoteGetValueBoolean("adInterOnComplete", AdsManager.Instance.adsConfigs.adInterOnComplete);
            AdsManager.Instance.adsConfigs.adInterOnStart = FirebaseManager.Instance.RemoteGetValueBoolean("adInterOnStart", AdsManager.Instance.adsConfigs.adInterOnStart);
            AdsManager.Instance.adsConfigs.timePlayToShowAds = FirebaseManager.Instance.RemoteGetValueFloat("timePlayToShowAds", AdsManager.Instance.adsConfigs.timePlayToShowAds);
            AdsManager.Instance.adsConfigs.adNativeBannerHeight = FirebaseManager.Instance.RemoteGetValueFloat("adNativeBannerHeight", AdsManager.Instance.adsConfigs.adNativeBannerHeight);
            AdsManager.Instance.adsConfigs.adTimeReload = FirebaseManager.Instance.RemoteGetValueFloat("adTimeReload", AdsManager.Instance.adsConfigs.adTimeReload);
        }, null);
#endif

#if USE_APPSFLYER
        yield return AppsFlyerManager.Instance.DoInit();
#endif
    }

    private void LoadInterstitial()
    {
        AdsManager.Instance.LoadInterstitial(AdsType.Interstitial, order);
        // #if USE_ADMOB
        //         AdsManager.Instance.LoadNativeInter(order);
        // #endif
    }

    private void ShowInterstitial()
    {
        AdsManager.Instance.ShowInterstitial(AdsType.Interstitial, order, "Default", () =>
        {
            AdsManager.Instance.Log("Interstitial closed");
        });
        // #if USE_ADMOB
        //         AdsManager.Instance.ShowNativeInter(PlacementOrder.One, "Default", NativeName.Native_Inter, () =>
        //         {
        //             AdsManager.Instance.Log("NativeInter show");
        //             HideNativeBannerPlatform();
        //         }, () =>
        //         {
        //             AdsManager.Instance.Log("NativeInter closed");
        //             ShowNativeBannerPlatform();
        //         }, () =>
        //         {
        //             AdsManager.Instance.Log("NativeInter full screen content closed");
        //         })
        //         ?.WithCountdown(AdsManager.Instance.adsConfigs.nativeVideoCountdownTimerDuration, AdsManager.Instance.adsConfigs.nativeVideoDelayBeforeCountdown, AdsManager.Instance.adsConfigs.nativeVideoCloseClickableDelay)
        //         ?.Execute();
        // #endif
    }

    private void Loadrewarded()
    {
        AdsManager.Instance.LoadRewarded(order);
    }

    private void ShowRewarded()
    {
        AdsManager.Instance.ShowRewarded(order, "Default", () =>
        {
            AdsManager.Instance.Log("Rewarded successfully");
        });

    }

    private void LoadAppOpen()
    {
        AdsManager.Instance.LoadAppOpen(order);
    }

    private void ShowAppOpen()
    {
        AdsManager.Instance.ShowAppOpen(order, "Default");
    }

    private void LoadBanner()
    {
        AdsManager.Instance.LoadBanner(order);
        // AdsManager.Instance.LoadNativeBanner(order);
    }

    private void ShowBanner()
    {
        AdsManager.Instance.ShowBanner(order, "Default");
        // ShowNativeBannerPlatform();
    }

    private void HideBanner()
    {
        AdsManager.Instance.HideBanner(order);
        // HideNativeBannerPlatform();
    }

    private void LoadMrec()
    {
        AdsManager.Instance.LoadMrec(AdsType.Mrec, order);
        // #if USE_ADMOB
        //         AdsManager.Instance.LoadNativeMrec(PlacementOrder.One);
        // #endif
    }

    private void ShowMrec()
    {
        var mrecPos = (AdsPos)MrecPosDropdown.value;
        AdsManager.Instance.ShowMrec(AdsType.Mrec, order, mrecPos, new Vector2Int(0, 0), "Default");
        // AdsManager.Instance.ShowNativeMrec(PlacementOrder.One, "Default", NativeName.Native_Mrec, null, null, null)
        // ?.WithPosition(mrecPos, new Vector2Int(0, 0))
        // ?.WithAutoReload(AdsManager.Instance.adsConfigs.nativeBannerTimeReload)
        // ?.WithShowOnLoaded(true)
        // ?.Execute();
    }

    private void HideMrec()
    {
        AdsManager.Instance.HideMrec(AdsType.Mrec, order);
        // #if USE_ADMOB
        //         AdsManager.Instance.HideNativeMrec(PlacementOrder.One);
        // #endif
    }

    private void LoadNativeOverlay()
    {
#if USE_ADMOB
        AdsManager.Instance.LoadNativeOverlay(order);
#endif
    }

    private void ShowNativeOverlay()
    {
#if USE_ADMOB
        var pos = (AdsPos)MrecPosDropdown.value;
        var deviceScale = MobileAds.Utils.GetDeviceScale();

        AdsManager.Instance.ShowNativeOverlay(order, new NativeTemplateStyle
        {
            TemplateId = NativeTemplateId.Medium,
            MainBackgroundColor = Color.red,
            CallToActionText = new NativeTemplateTextStyle()
            {
                BackgroundColor = Color.green,
                TextColor = Color.black,
                FontSize = 20,
                Style = NativeTemplateFontStyle.Bold
            }
        }, pos, new Vector2Int(Mathf.RoundToInt(Screen.safeArea.width / deviceScale / 1.5f), Mathf.RoundToInt(Screen.safeArea.height / deviceScale)), new Vector2Int(0, 0), "Default", () =>
        {
            nativeOverlayBG.SetActive(true);
            AdsManager.Instance.Log("NativeOverlay show");
        }, () =>
        {
            AdsManager.Instance.Log("NativeOverlay closed");
            nativeOverlayBG.SetActive(false);
        });

        // new Vector2Int(Mathf.RoundToInt(Screen.safeArea.width / deviceScale / 3), Mathf.RoundToInt(Screen.safeArea.height / deviceScale))
#endif
    }


    private void HideNativeOverlay()
    {
#if USE_ADMOB
        AdsManager.Instance.HideNativeOverlay(order);
#endif
    }

    public void LoadNative()
    {
#if USE_ADMOB && USE_ADMOB_NATIVE_UNITY
        nativeAdsMrec.LoadAds();
        nativeAdsBanner.LoadAds();
#endif
    }

    public void ShowNative()
    {
#if USE_ADMOB && USE_ADMOB_NATIVE_UNITY
        nativeAdsMrec.ShowAds();
        nativeAdsBanner.ShowAds("Default");
#endif
    }

    public void HideNative()
    {
#if USE_ADMOB && USE_ADMOB_NATIVE_UNITY
        nativeAdsMrec.HideAds();
        nativeAdsBanner.HideAds();
#endif
    }

    public void AAAAA()
    {
        AdsManager.Instance.GetAdsStatus(AdsType.NativeUnity, order);
    }

    public void LoadNativeVideoPlatform()
    {
#if USE_ADMOB
        AdsManager.Instance.LoadNativeVideo(PlacementOrder.One);
#endif
    }



    public void ShowNativeVideoPlatform()
    {
#if USE_ADMOB
        AdsManager.Instance.ShowNativeVideo(PlacementOrder.One, "Default", NativeName.Native_Video, () =>
        {
            AdsManager.Instance.Log("NativeVideoPlatform show");
            HideNativeBannerPlatform();
        }, () =>
        {
            AdsManager.Instance.Log("NativeVideoPlatform closed");
            ShowNativeBannerPlatform();
        }, () =>
        {
            AdsManager.Instance.Log("NativeVideoPlatform full screen content closed");
        })
        ?.WithCountdown(AdsManager.Instance.adsConfigs.nativeVideoCountdownTimerDuration, AdsManager.Instance.adsConfigs.nativeVideoDelayBeforeCountdown, AdsManager.Instance.adsConfigs.nativeVideoCloseClickableDelay)
        ?.Execute();
#endif
    }

    public void HideNativeVideoPlatform()
    {
#if USE_ADMOB
        AdsManager.Instance.HideNativeVideo(PlacementOrder.One);
#endif
    }


    public void LoadNativeBannerPlatform()
    {
#if USE_ADMOB
        AdsManager.Instance.LoadNativeBanner(PlacementOrder.One);
#endif
    }

    public void ShowNativeBannerPlatform()
    {
#if USE_ADMOB
        AdsManager.Instance.ShowNativeBanner(PlacementOrder.One, "Default", NativeName.Native_Banner, () =>
        {
            AdsManager.Instance.Log("NativeBannerPlatform show");
        }, () =>
        {
            AdsManager.Instance.Log("NativeBannerPlatform closed");
        }, () =>
        {
            AdsManager.Instance.Log("NativeBannerPlatform full screen content closed");
        })
        ?.WithAutoReload(AdsManager.Instance.adsConfigs.nativeBannerTimeReload)
        ?.WithShowOnLoaded(true)
        ?.Execute();
#endif
    }

    public void HideNativeBannerPlatform()
    {
#if USE_ADMOB
        AdsManager.Instance.HideNativeBanner(PlacementOrder.One);
#endif
    }

    public void AdjustLayoutForNativeBanner()
    {
        adLayoutHelper.AdjustLayoutForNativeBanner(60);
    }

    public void RemoveAds()
    {
        AdsManager.Instance.IsCanShowAds = false;
    }
}
