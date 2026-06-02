using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace TheLegends.Base.Ads
{
    [CreateAssetMenu(fileName = "AdsSettings", menuName = "DataAsset/AdsSettings")]
    public class AdsSettings : ScriptableObject
    {
        public const string ResDir = "Assets/TripSoft/AdsManager/Resources";
        public const string FileName = "AdsSettingsAsset";
        public const string FileExtension = ".asset";

        [SerializeField]
        private List<AdsMediation> _adsMediations = new List<AdsMediation>();
        public List<AdsMediation> AdsMediations => this._adsMediations;

        private static AdsSettings _instance;
        public static AdsSettings Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = Resources.Load<AdsSettings>(FileName);
                return _instance;
            }
        }

        [SerializeField]
        private AdsMediation _flagMediations;
        public AdsMediation primaryMediation;
        public AdsMediation FlagMediations
        {
            get => this._flagMediations;

            set
            {
                if (this._flagMediations == value)
                {
                    return;
                }

                _flagMediations = value;

                _adsMediations.Clear();

                var mediationsList = Enum.GetValues(typeof(AdsMediation)).Cast<AdsMediation>().ToList();

                foreach (var mediation in mediationsList)
                {
                    if ((_flagMediations & mediation) != 0)
                    {
                        _adsMediations.Add(mediation);
                    }

                    switch (mediation)
                    {
                        case Ads.AdsMediation.Iron:
                            showIRON = (_flagMediations & mediation) != 0;
                            break;
                        case Ads.AdsMediation.Max:
                            showMAX = (_flagMediations & mediation) != 0;
                            break;
                        case Ads.AdsMediation.Admob:
                            showADMOB = (_flagMediations & mediation) != 0;
                            break;
                    }
                }
            }
        }



        [Header("Optional Services")]
        public bool useFirebase = false;
        public List<AdsType> firebaseTrackedTypes = Enum.GetValues(typeof(AdsType)).Cast<AdsType>().Where(t => t != AdsType.None).ToList();

        public bool useAppsFlyer = false;
        public List<AdsType> appsFlyerTrackedTypes = Enum.GetValues(typeof(AdsType)).Cast<AdsType>().Where(t => t != AdsType.None).ToList();

        public bool useDatabuckets = false;
        public List<AdsType> databucketsTrackedTypes = Enum.GetValues(typeof(AdsType)).Cast<AdsType>().Where(t => t != AdsType.None).ToList();

        public bool useFacebook = false;
        public List<AdsType> facebookTrackedTypes = Enum.GetValues(typeof(AdsType)).Cast<AdsType>().Where(t => t != AdsType.None).ToList();

        public string appsFlyerDevKey = "Qhno4yJY6KHmZp9uS9DRe4";
        public string appleAppId = "";

        [Header("Google Sheet Sync")]
        public string admobSheetUrl;
        public string maxSheetUrl;

        public BannerPos bannerPosition = BannerPos.Bottom;
        public bool fixBannerSmallSize;

        public int autoReLoadMax = 2;
        public bool isTest = false;


        [Header("Preload Settings")]
        public PreloadSettings preloadSettings = new PreloadSettings();

        [Header("IRON")]
        public bool showIRON = false;
        public string ironAndroidAppKey = string.Empty;
        public string ironIOSAppKey = string.Empty;
        public bool ironEnableAdmob;
        public string ironAdmobAndroidAppID = string.Empty;
        public string ironAdmobIOSAppID = string.Empty;
        [Header("MAX")]
        public bool showMAX = false;
        public bool isShowMediationDebugger = false;
        public MaxUnitID MAX_Android = new MaxUnitID();
        public MaxUnitID MAX_iOS = new MaxUnitID();
        [Header("ADMOB")]
        public bool showADMOB = false;
        public bool isUseNativeUnity = false;
        public bool isShowAdmobNativeValidator = false;
        public bool isHideWhenFullscreenShowed = false;
        public AdmobUnitID ADMOB_Android = new AdmobUnitID();
        public AdmobUnitID ADMOB_IOS = new AdmobUnitID();
        public AdmobUnitID ADMOB_Android_Test = new AdmobUnitID
        {
            bannerIds = CreatePlacement("ca-app-pub-3940256099942544/6300978111"),
            interIds = CreatePlacement("ca-app-pub-3940256099942544/1033173712"),
            rewardIds = CreatePlacement("ca-app-pub-3940256099942544/5224354917"),
            appOpenIds = CreatePlacement("ca-app-pub-3940256099942544/9257395921"),
            mrecIds = CreatePlacement("ca-app-pub-3940256099942544/6300978111"),
            interOpenIds = CreatePlacement("ca-app-pub-3940256099942544/1033173712"),
            mrecOpenIds = CreatePlacement("ca-app-pub-3940256099942544/6300978111"),
            nativeUnityIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeOverlayIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeBannerIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeInterIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeRewardIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeMrecIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeAppOpenIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeInterOpenIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeMrecOpenIds = CreatePlacement("ca-app-pub-3940256099942544/2247696110"),
            nativeVideoIds = CreatePlacement("ca-app-pub-3940256099942544/1044960115")
        };

        public AdmobUnitID ADMOB_IOS_Test = new AdmobUnitID
        {
            bannerIds = CreatePlacement("ca-app-pub-3940256099942544/2934735716"),
            interIds = CreatePlacement("ca-app-pub-3940256099942544/4411468910"),
            rewardIds = CreatePlacement("ca-app-pub-3940256099942544/1712485313"),
            appOpenIds = CreatePlacement("ca-app-pub-3940256099942544/5575463023"),
            mrecIds = CreatePlacement("ca-app-pub-3940256099942544/2934735716"),
            interOpenIds = CreatePlacement("ca-app-pub-3940256099942544/4411468910"),
            mrecOpenIds = CreatePlacement("ca-app-pub-3940256099942544/2934735716"),
            nativeUnityIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeOverlayIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeBannerIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeInterIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeRewardIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeMrecIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeAppOpenIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeInterOpenIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeMrecOpenIds = CreatePlacement("ca-app-pub-3940256099942544/3986624511"),
            nativeVideoIds = CreatePlacement("ca-app-pub-3940256099942544/2521693316")
        };

        private static List<Placement> CreatePlacement(params string[] adUnitIds)
        {
            var placements = new List<Placement>();

            foreach (string adUnitId in adUnitIds)
            {
                placements.Add(new Placement
                {
                    stringIDs = new List<string> { adUnitId }
                });
            }

            return placements;
        }

    }
}

[System.Serializable]
public class PreloadSettings
{
    [Header("Standard Ads")]
    public bool preloadBanner = false;
    public bool preloadInterstitial = false;
    public bool preloadRewarded = false;
    public bool preloadMREC = false;
    public bool preloadAppOpen = false;

    [Header("Admob Native Ads")]
    public NativePreloadSettings nativeAds;
}

[System.Serializable]
public class NativePreloadSettings
{
    public bool preloadNativeBanner = false;
    public bool preloadNativeOverlay = false;
    public bool preloadNativeInter = false;
    public bool preloadNativeReward = false;
    public bool preloadNativeMrec = false;
    public bool preloadNativeAppOpen = false;
    public bool preloadNativeVideo = false;
}
