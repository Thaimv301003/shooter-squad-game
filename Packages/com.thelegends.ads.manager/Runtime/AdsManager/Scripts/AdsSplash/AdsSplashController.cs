using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
#if USE_APPSFLYER
using TheLegends.Base.AppsFlyer;
#endif
#if USE_FIREBASE
using TheLegends.Base.Firebase;
#endif
using TheLegends.Base.UI;
#if USE_DATABUCKETS
using TheLegends.Base.Databuckets;
#endif
#if USE_FACEBOOK
using TheLegends.Base.Facebook;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


namespace TheLegends.Base.Ads
{
    public class AdsSplashController : MonoBehaviour
    {
        [SerializeField]
        private bool isUseSelectBrand = true;

        private readonly WaitForSeconds _loadDelay = new WaitForSeconds(0.5f);

        [Space(10)]
        [SerializeField, ShowField(nameof(isUseSelectBrand))]
        private AdsPos mrecOpenPos = AdsPos.CenterLeft;
        [SerializeField, ShowField(nameof(isUseSelectBrand))]
        private Vector2Int mrecOpenOffset = Vector2Int.zero;

        [Space(10)]
        [SerializeField, ShowField(nameof(isUseSelectBrand))]
        private BrandScreenController brandScreen;



        [Space(10)]
        [SerializeField]
        private string sceneName;

        private bool canShowSelectBrand
        {
            get
            {
                var temp = PlayerPrefs.GetInt("canShowSelectBrand", 1);
                return temp == 1;
            }
        }

        private Dictionary<string, object> conversionDataDictionary = new Dictionary<string, object>();

        [Space(10)]
        [SerializeField]
        private UnityEvent OnInitFirebaseDone = new UnityEvent();

        [Space(10)]
        [SerializeField]
        private UnityEvent OnInitAdsDone = new UnityEvent();

        [Space(10)]
        [SerializeField]
        private UnityEvent OnCompleteSplash = new UnityEvent();

        public void Start()
        {
            brandScreen.gameObject.SetActive(false);
            StartCoroutine(IELoad());
        }

        private IEnumerator IELoad()
        {
            UILoadingController.SetProgress(0.2f, null);

#if USE_FIREBASE
            // Initialize Firebase with remote config
            yield return InitializeFirebase();
#endif

#if USE_APPSFLYER
            // Initialize AppsFlyer
            yield return InitAppsFlyer();
#endif

#if USE_FIREBASE
            // Fetch remote data and update configs
            yield return FetchAndUpdateRemoteConfigs();
            OnInitFirebaseDone?.Invoke();
#endif

#if USE_DATABUCKETS
            InitDatabuckets();
#endif

#if USE_FACEBOOK
            yield return InitFacebook();
#endif

            // Initialize Ads Manager
            yield return AdsManager.Instance.DoInit();

            UILoadingController.SetProgress(0.4f, null);
            yield return _loadDelay;

            // Load ads based on brand selection settings
            if (isUseSelectBrand)
            {
                yield return LoadInitialAds();
            }

            UILoadingController.SetProgress(0.6f, null);

            // Load the target scene
            yield return IELoadScene();

            // Complete initialization
            CompleteInitialization();
        }

#if USE_APPSFLYER
        private IEnumerator InitAppsFlyer()
        {
            bool isFetching = true;

            yield return AppsFlyerManager.Instance.DoInit((conversionData) =>
            {
                OnGetAppsFlyerConversionData(conversionData);
                isFetching = false;
            }, () =>
            {
                isFetching = false;
            });

            while (isFetching)
            {
                yield return null;
            }

        }

        private void OnGetAppsFlyerConversionData(string conversionData)
        {
            if (string.IsNullOrEmpty(conversionData))
            {
                AdsManager.Instance.Log("AppsFlyer conversion data is null or empty.");
                return;
            }

#if USE_FIREBASE
            conversionDataDictionary = AppsFlyerSDK.AppsFlyer.CallbackStringToDictionary(conversionData);

            try
            {
                var campaign_id = conversionDataDictionary.FirstOrDefault(k => k.Key == "campaign_id").Value as string;

                if (!string.IsNullOrEmpty(campaign_id))
                {
                    FirebaseManager.Instance.SetUserProperty("af_campaign_id", campaign_id);
                }
                else
                {
                    if (AdsManager.Instance.SettingsAds.isTest)
                    {
                        campaign_id = "campaign_id_test";
                        FirebaseManager.Instance.SetUserProperty("af_campaign_id", "campaign_id_test");
                    }
                    else
                    {
                        campaign_id = "organic";
                    }
                }

                AdsManager.Instance.Log($"AppsFlyer Campaign ID: {campaign_id}");
                FirebaseManager.Instance.LogEvent("af_campaign_id", new Dictionary<string, object>
                {
                    { "campaign_id", campaign_id }
                });
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                AdsManager.Instance.LogWarning("Cannot get AppsFlyer Caimpaign: Not work on Unity Editor.");
#else
                AdsManager.Instance.LogError("Cannot get AppsFlyer Caimpaign: " + e.Message);
#endif
            }
#endif
        }
#endif

#if USE_FIREBASE
        private IEnumerator InitializeFirebase()
        {
            var defaultRemoteConfig = CreateDefaultRemoteConfig();
            yield return FirebaseManager.Instance.DoInit(defaultRemoteConfig);
        }

        private Dictionary<string, object> CreateDefaultRemoteConfig()
        {
            var config = new Dictionary<string, object>
            {
                {"isUseAdInterOpen", AdsManager.Instance.adsConfigs.isUseAdInterOpen},
                {"adInterOpenTimeOut", AdsManager.Instance.adsConfigs.adInterOpenTimeOut},
                {"isUseAdMrecOpen", AdsManager.Instance.adsConfigs.isUseAdMrecOpen},
                {"adMrecOpenTimeOut", AdsManager.Instance.adsConfigs.adMrecOpenTimeOut},
                {"isUseAdAppOpenOpen", AdsManager.Instance.adsConfigs.isUseAdAppOpenOpen},
                {"adInterOnComplete", AdsManager.Instance.adsConfigs.adInterOnComplete},
                {"adInterOnStart", AdsManager.Instance.adsConfigs.adInterOnStart},
                {"timePlayToShowAds", AdsManager.Instance.adsConfigs.timePlayToShowAds},
                {"adNativeBannerHeight", AdsManager.Instance.adsConfigs.adNativeBannerHeight},
                {"adTimeReload", AdsManager.Instance.adsConfigs.adTimeReload},
                {"adLoadTimeOut", AdsManager.Instance.adsConfigs.adLoadTimeOut},
                {"isUseAdNative", AdsManager.Instance.adsConfigs.isUseAdNative},
                {"nativeVideoCountdownTimerDuration", AdsManager.Instance.adsConfigs.nativeVideoCountdownTimerDuration},
                {"nativeVideoDelayBeforeCountdown", AdsManager.Instance.adsConfigs.nativeVideoDelayBeforeCountdown},
                {"nativeVideoCloseClickableDelay", AdsManager.Instance.adsConfigs.nativeVideoCloseClickableDelay},
                {"nativeBannerTimeReload", AdsManager.Instance.adsConfigs.nativeBannerTimeReload}
            };

            return config;
        }

        private IEnumerator FetchAndUpdateRemoteConfigs()
        {
            bool isFetching = true;
            FirebaseManager.Instance.FetchRemoteData(() =>
            {
                UpdateCommonConfigs();
                isFetching = false;
            }, () =>
            {
                isFetching = false;
            });

            while (isFetching)
            {
                yield return null;
            }
        }


        private void UpdateCommonConfigs()
        {
            var configs = AdsManager.Instance.adsConfigs;
            configs.adInterOnComplete = FirebaseManager.Instance.RemoteGetValueBoolean("adInterOnComplete", configs.adInterOnComplete);
            configs.adInterOnStart = FirebaseManager.Instance.RemoteGetValueBoolean("adInterOnStart", configs.adInterOnStart);
            configs.timePlayToShowAds = FirebaseManager.Instance.RemoteGetValueFloat("timePlayToShowAds", configs.timePlayToShowAds);
            configs.adNativeBannerHeight = FirebaseManager.Instance.RemoteGetValueFloat("adNativeBannerHeight", configs.adNativeBannerHeight);
            configs.adTimeReload = FirebaseManager.Instance.RemoteGetValueFloat("adTimeReload", configs.adTimeReload);
            configs.adLoadTimeOut = FirebaseManager.Instance.RemoteGetValueFloat("adLoadTimeOut", configs.adLoadTimeOut);
            configs.isUseAdNative = FirebaseManager.Instance.RemoteGetValueBoolean("isUseAdNative", configs.isUseAdNative);
            configs.nativeVideoCountdownTimerDuration = FirebaseManager.Instance.RemoteGetValueFloat("nativeVideoCountdownTimerDuration", configs.nativeVideoCountdownTimerDuration);
            configs.nativeVideoDelayBeforeCountdown = FirebaseManager.Instance.RemoteGetValueFloat("nativeVideoDelayBeforeCountdown", configs.nativeVideoDelayBeforeCountdown);
            configs.nativeVideoCloseClickableDelay = FirebaseManager.Instance.RemoteGetValueFloat("nativeVideoCloseClickableDelay", configs.nativeVideoCloseClickableDelay);
            configs.nativeBannerTimeReload = FirebaseManager.Instance.RemoteGetValueFloat("nativeBannerTimeReload", configs.nativeBannerTimeReload);
            configs.isUseAdInterOpen = FirebaseManager.Instance.RemoteGetValueBoolean("isUseAdInterOpen", configs.isUseAdInterOpen);
            configs.adInterOpenTimeOut = FirebaseManager.Instance.RemoteGetValueFloat("adInterOpenTimeOut", configs.adInterOpenTimeOut);
            configs.isUseAdMrecOpen = FirebaseManager.Instance.RemoteGetValueBoolean("isUseAdMrecOpen", configs.isUseAdMrecOpen);
            configs.adMrecOpenTimeOut = FirebaseManager.Instance.RemoteGetValueFloat("adMrecOpenTimeOut", configs.adMrecOpenTimeOut);
            configs.isUseAdAppOpenOpen = FirebaseManager.Instance.RemoteGetValueBoolean("isUseAdAppOpenOpen", configs.isUseAdAppOpenOpen);
        }
#endif

        private void InitDatabuckets()
        {
#if USE_DATABUCKETS

            DatabucketsManager.Instance.Init();

            try
            {
                var ua_network = conversionDataDictionary.FirstOrDefault(k => k.Key == "media_source").Value as string;
                var ua_campaign = conversionDataDictionary.FirstOrDefault(k => k.Key == "campaign").Value as string;
                var ua_adgroup = conversionDataDictionary.FirstOrDefault(k => k.Key == "adgroup").Value as string;
                var ua_creative = conversionDataDictionary.FirstOrDefault(k => k.Key == "adset").Value as string;

                DatabucketsManager.Instance.SetCommonProperties(new Dictionary<string, object>
                {
                    { "ua_network", ua_network ?? "Unavailable" },
                    { "ua_campaign", ua_campaign ?? "Unavailable" },
                    { "ua_adgroup", ua_adgroup ?? "Unavailable" },
                    { "ua_creative", ua_creative ?? "Unavailable" }
                });
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                AdsManager.Instance.LogWarning("Cannot get AppsFlyer Property: Not work on Unity Editor.");
#else
                AdsManager.Instance.LogError("Cannot get AppsFlyer Property: " + e.Message);
#endif
            }

#endif
        }

#if USE_FACEBOOK
        private IEnumerator InitFacebook()
        {
            yield return FacebookManager.Instance.DoInit();
        }
#endif

        private IEnumerator LoadInitialAds()
        {

            if (!AdsManager.Instance.IsCanShowAds)
            {
                yield break;
            }

            // Load MREC for brand selection if needed
            if (canShowSelectBrand && AdsManager.Instance.adsConfigs.isUseAdMrecOpen)
            {

#if USE_ADMOB
                AdsManager.Instance.LoadNativeMrecOpen(PlacementOrder.One);
                yield return AdsManager.Instance.WaitAdLoaded(AdsType.NativeMrecOpen, PlacementOrder.One);
#endif


                if (AdsManager.Instance.GetAdsStatus(AdsType.NativeMrecOpen, PlacementOrder.One) != AdsEvents.LoadAvailable)
                {
                    AdsManager.Instance.LoadMrec(AdsType.MrecOpen, PlacementOrder.One);
                    yield return AdsManager.Instance.WaitAdLoaded(AdsType.MrecOpen, PlacementOrder.One);
                }
            }

            // Load interstitial for app open if enabled
            if (AdsManager.Instance.adsConfigs.isUseAdInterOpen)
            {
#if USE_ADMOB
                AdsManager.Instance.LoadNativeInterOpen(PlacementOrder.One);
                yield return AdsManager.Instance.WaitAdLoaded(AdsType.NativeInterOpen, PlacementOrder.One);
#endif

                if (AdsManager.Instance.GetAdsStatus(AdsType.NativeInterOpen, PlacementOrder.One) != AdsEvents.LoadAvailable)
                {
                    AdsManager.Instance.LoadInterstitial(AdsType.InterOpen, PlacementOrder.One);
                    yield return AdsManager.Instance.WaitAdLoaded(AdsType.InterOpen, PlacementOrder.One);
                }

            }

            if (AdsManager.Instance.adsConfigs.isUseAdAppOpenOpen
                && !AdsManager.Instance.adsConfigs.isUseAdInterOpen)
            {
                AdsManager.Instance.LoadAppOpen(PlacementOrder.One);
                yield return AdsManager.Instance.WaitAdLoaded(AdsType.AppOpen, PlacementOrder.One);
            }
        }

        private void CompleteInitialization()
        {
            float finishProgress = isUseSelectBrand ? 1f : 0.9f;

            UILoadingController.SetProgress(finishProgress, () =>
            {
                if (isUseSelectBrand)
                {
                    StartCoroutine(HandleBrandSelectionFlow());
                }

                OnInitAdsDone?.Invoke();

                if (!isUseSelectBrand)
                {
                    CompleteSplash();
                }
            });
        }

        public void CompleteSplash()
        {

            AdsManager.Instance.HideMrec(AdsType.MrecOpen, PlacementOrder.One);
#if USE_ADMOB
            AdsManager.Instance.HideNativeMrecOpen(PlacementOrder.One);
#endif

            brandScreen.OnClose -= CompleteSplash;

            OnCompleteSplash?.Invoke();
        }

        private IEnumerator HandleBrandSelectionFlow()
        {
            // Show interstitial if available

            bool isShowAdOpen = false;

            if (AdsManager.Instance.adsConfigs.isUseAdInterOpen)
            {
                if (AdsManager.Instance.GetAdsStatus(AdsType.NativeInterOpen, PlacementOrder.One) == AdsEvents.LoadAvailable)
                {
#if USE_ADMOB
                    AdsManager.Instance.ShowNativeInterOpen(PlacementOrder.One, "native_inter_open", NativeName.Native_FullScreen, null, () =>
                    {
                        isShowAdOpen = false;
                    }, null)
                    .WithCountdown(AdsManager.Instance.adsConfigs.nativeVideoCountdownTimerDuration, AdsManager.Instance.adsConfigs.nativeVideoDelayBeforeCountdown, AdsManager.Instance.adsConfigs.nativeVideoCloseClickableDelay)
                    .Execute();

                    isShowAdOpen = true;
#endif
                }
                else
                {
                    if (AdsManager.Instance.GetAdsStatus(AdsType.InterOpen, PlacementOrder.One) == AdsEvents.LoadAvailable)
                    {
                        AdsManager.Instance.ShowInterstitial(AdsType.InterOpen, PlacementOrder.One, "Inter Open", () =>
                        {
                            isShowAdOpen = false;
                        });
                        isShowAdOpen = true;
                    }
                }
            }

            if (AdsManager.Instance.adsConfigs.isUseAdAppOpenOpen
                && !AdsManager.Instance.adsConfigs.isUseAdInterOpen)
            {
                if (AdsManager.Instance.GetAdsStatus(AdsType.AppOpen, PlacementOrder.One) == AdsEvents.LoadAvailable)
                {
                    AdsManager.Instance.ShowAppOpen(PlacementOrder.One, "App Open", () =>
                    {
                        isShowAdOpen = false;
                    });
                    isShowAdOpen = true;
                }
            }


            while (isShowAdOpen)
            {
                yield return null;
            }

            // Show brand selection screen if can show
            if (canShowSelectBrand && AdsManager.Instance.adsConfigs.isUseAdMrecOpen)
            {
                ShowBrandScreen();
            }
            else
            {
                CompleteSplash();
            }

            UILoadingController.Hide();
        }


        private void ShowBrandScreen()
        {
            if (!AdsManager.Instance.adsConfigs.isUseAdMrecOpen) return;

            if (AdsManager.Instance.GetAdsStatus(AdsType.NativeMrecOpen, PlacementOrder.One) == AdsEvents.LoadAvailable)
            {
#if USE_ADMOB
                AdsManager.Instance.ShowNativeMrecOpen(PlacementOrder.One, "native_mrec_open", NativeName.Native_Mrec, null, null, null)
                .WithPosition(mrecOpenPos, mrecOpenOffset)
                .Execute();
                brandScreen.Show();
                brandScreen.OnClose += CompleteSplash;
#endif
            }
            else if (AdsManager.Instance.GetAdsStatus(AdsType.MrecOpen, PlacementOrder.One) == AdsEvents.LoadAvailable)
            {
                AdsManager.Instance.ShowMrec(AdsType.MrecOpen, PlacementOrder.One, mrecOpenPos, mrecOpenOffset, "Mrec Open");
                brandScreen.Show();
                brandScreen.OnClose += CompleteSplash;
            }
            else
            {
                CompleteSplash();
            }
        }

        private IEnumerator IELoadScene()
        {
            Debug.Log("LoadScene");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            StartCoroutine(IEPreloadAds());

            // Wait until the asynchronous scene fully loads
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        private IEnumerator IEPreloadAds()
        {
            var settings = AdsManager.Instance.SettingsAds.preloadSettings;

            if (settings.preloadBanner)
            {
                yield return IEPreloadByType(AdsType.Banner, order => AdsManager.Instance.LoadBanner(order));
            }

            if (settings.preloadInterstitial)
            {
                yield return IEPreloadByType(AdsType.Interstitial, order => AdsManager.Instance.LoadInterstitial(AdsType.Interstitial, order));
            }

            if (settings.preloadRewarded)
            {
                yield return IEPreloadByType(AdsType.Rewarded, order => AdsManager.Instance.LoadRewarded(order));
            }

            if (settings.preloadMREC)
            {
                yield return IEPreloadByType(AdsType.Mrec, order => AdsManager.Instance.LoadMrec(AdsType.Mrec, order));
            }

            if (settings.preloadAppOpen)
            {
                yield return IEPreloadByType(AdsType.AppOpen, order => AdsManager.Instance.LoadAppOpen(order));
            }



#if USE_ADMOB
            if (settings.nativeAds != null && settings.nativeAds.preloadNativeOverlay)
            {
                yield return IEPreloadByType(AdsType.NativeOverlay, order => AdsManager.Instance.LoadNativeOverlay(order));
            }
            if (settings.nativeAds != null && settings.nativeAds.preloadNativeBanner)
            {
                yield return IEPreloadByType(AdsType.NativeBanner, order => AdsManager.Instance.LoadNativeBanner(order));
            }
            if (settings.nativeAds != null && settings.nativeAds.preloadNativeInter)
            {
                yield return IEPreloadByType(AdsType.NativeInter, order => AdsManager.Instance.LoadNativeInter(order));
            }
            if (settings.nativeAds != null && settings.nativeAds.preloadNativeReward)
            {
                yield return IEPreloadByType(AdsType.NativeReward, order => AdsManager.Instance.LoadNativeReward(order));
            }
            if (settings.nativeAds != null && settings.nativeAds.preloadNativeMrec)
            {
                yield return IEPreloadByType(AdsType.NativeMrec, order => AdsManager.Instance.LoadNativeMrec(order));
            }
            if (settings.nativeAds != null && settings.nativeAds.preloadNativeAppOpen)
            {
                yield return IEPreloadByType(AdsType.NativeAppOpen, order => AdsManager.Instance.LoadNativeAppOpen(order));
            }
            if (settings.nativeAds != null && settings.nativeAds.preloadNativeVideo)
            {
                yield return IEPreloadByType(AdsType.NativeVideo, order => AdsManager.Instance.LoadNativeVideo(order));
            }
#endif
        }

        private IEnumerator IEPreloadByType(AdsType adsType, Action<PlacementOrder> preloadAction)
        {
            AdsManager.Instance.GetPlacementInfo(adsType, out var placementOrders);

            if (placementOrders == null || placementOrders.Count <= 0)
            {
                yield break;
            }

            foreach (var order in placementOrders.Distinct().OrderBy(order => (int)order))
            {
                preloadAction?.Invoke(order);
                yield return null;
            }
        }


    }
}
