#if USE_ADMOB
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public class AdmobMediationController : AdsMediationBase
    {
        private bool isChecking = false;
        private InitiationStatus status = InitiationStatus.NotInitialized;

        [Header("DEBUG")]
        [SerializeField]
        private List<string> testDevicesIDAds = new List<string>();

        [Space(5)]

        [SerializeField]
        private DebugGeography debugGeography = DebugGeography.Disabled;

        [SerializeField]
        private List<string> testDeivesIDConsent = new List<string>();

        private List<AdmobInterstitialController> interList = new List<AdmobInterstitialController>();
        private List<AdmobRewardedController> rewardList = new List<AdmobRewardedController>();
        private List<AdmobAppOpenController> appOpenList = new List<AdmobAppOpenController>();
        private List<AdmobBannerController> bannerList = new List<AdmobBannerController>();
        private List<AdmobMrecController> mrecList = new List<AdmobMrecController>();
        private List<AdmobMrecOpenController> mrecOpenList = new List<AdmobMrecOpenController>();
        private List<AdmobInterstitialOpenController> interOpenList = new List<AdmobInterstitialOpenController>();
        private List<AdmobNativeOverlayController> nativeOverlayList = new List<AdmobNativeOverlayController>();
        private List<AdmobNativeBannerController> nativeBannerList = new List<AdmobNativeBannerController>();
        private List<AdmobNativeInterController> nativeInterList = new List<AdmobNativeInterController>();
        private List<AdmobNativeRewardController> nativeRewardList = new List<AdmobNativeRewardController>();
        private List<AdmobNativeMrecController> nativeMrecList = new List<AdmobNativeMrecController>();
        private List<AdmobNativeAppOpenController> nativeAppOpenList = new List<AdmobNativeAppOpenController>();
        private List<AdmobNativeInterOpenController> nativeInterOpenList = new List<AdmobNativeInterOpenController>();
        private List<AdmobNativeMrecOpenController> nativeMrecOpenList = new List<AdmobNativeMrecOpenController>();
        private List<AdmobNativeVideoController> nativeVideoList = new List<AdmobNativeVideoController>();

        private readonly List<string> excludedIdFields = new List<string>
        {
            "nativeUnityIds"
        };

        public override IEnumerator DoInit()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            status = InitiationStatus.Initializing;

            var platform = Application.platform;
            var isIOS = platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.OSXPlayer;

            var isTest = AdsManager.Instance.SettingsAds.isTest;

            // Lấy tất cả các trường trong AdmobUnitID
            var unitIdFields = typeof(AdmobUnitID).GetFields();

            // Lấy tất cả các trường danh sách controller trong AdmobNetworkController
            var controllerListFields = this.GetType()
                .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(f => f.FieldType.IsGenericType &&
                           f.FieldType.GetGenericTypeDefinition() == typeof(List<>) &&
                           typeof(AdsPlacementBase).IsAssignableFrom(f.FieldType.GetGenericArguments()[0]))
                .ToList();

            foreach (var unitIdField in unitIdFields)
            {
                if (unitIdField.FieldType == typeof(List<Placement>))
                {
                    string fieldName = unitIdField.Name;

                    if (excludedIdFields.Contains(fieldName))
                    {
                        continue;
                    }

                    var controllerField = controllerListFields.FirstOrDefault(f =>
                        fieldName.Replace("Ids", "List").Equals(f.Name, StringComparison.OrdinalIgnoreCase));

                    if (controllerField != null)
                    {
                        var iosIds = AdsManager.Instance.SettingsAds.ADMOB_IOS;
                        var androidIds = AdsManager.Instance.SettingsAds.ADMOB_Android;
                        var iosTestIds = AdsManager.Instance.SettingsAds.ADMOB_IOS_Test;
                        var androidTestIds = AdsManager.Instance.SettingsAds.ADMOB_Android_Test;

                        var placements = GetAdUnitIds(
                            isIOS,
                            isTest,
                            (List<Placement>)unitIdField.GetValue(iosIds),
                            (List<Placement>)unitIdField.GetValue(androidIds),
                            (List<Placement>)unitIdField.GetValue(iosTestIds),
                            (List<Placement>)unitIdField.GetValue(androidTestIds)
                        );

                        var controllerList = controllerField.GetValue(this);

                        var controllerType = controllerField.FieldType.GetGenericArguments()[0];
                        var methodInfo = typeof(AdmobMediationController).GetMethod("CreateAdController", BindingFlags.NonPublic | BindingFlags.Instance);
                        var genericMethod = methodInfo.MakeGenericMethod(controllerType);
                        genericMethod.Invoke(this, new object[] { placements, controllerList });
                    }
                    else
                    {
                        AdsManager.Instance.LogError($"Cannot find controller list for {fieldName} - skipping");
                    }
                }
            }

            yield return RequestUMP();

            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TestDeviceIds = testDevicesIDAds
            });

            MobileAds.SetiOSAppPauseOnBackground(true);

            if (ConsentInformation.CanRequestAds())
            {
                MobileAds.Initialize(initStatus =>
                {
                    PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        if (initStatus == null)
                        {
                            status = InitiationStatus.Failed;

                            AdsManager.Instance.LogError("Google Mobile Ads initialization failed.");
                            return;
                        }

                        if (initStatus != null)
                        {
                            status = InitiationStatus.Initialized;

                            AdsManager.Instance.Log($"{TagLog.ADMOB} " + "Mediations checking status...");
                            var adapterStatusMap = initStatus.getAdapterStatusMap();
                            if (adapterStatusMap != null)
                            {
                                foreach (var item in adapterStatusMap)
                                {
                                    AdsManager.Instance.Log($"{TagLog.ADMOB} " + string.Format(" Google Adapter {0} is {1}",
                                        item.Key,
                                        item.Value.InitializationState));
                                }
                            }
                            AdsManager.Instance.Log($"{TagLog.ADMOB} " + "Mediations checking done.");

                        }

                        AdsManager.Instance.Log($"{TagLog.ADMOB} " + "Initialize: " + initStatus.ToString());

                    });

                });
            }
            else
            {
                AdsManager.Instance.Log($"{TagLog.UMP} " + "UMP ConsentStatus --> " + ConsentInformation.ConsentStatus.ToString() + " CanRequestAds: " + ConsentInformation.CanRequestAds().ToString().ToUpper() + " --> NOT INIT");
                status = InitiationStatus.Failed;
            }



            while (status == InitiationStatus.Initializing)
            {
                yield return null;
            }

#endif
            yield break;
        }



        private IEnumerator RequestUMP()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (isChecking)
            {
                AdsManager.Instance.LogError($"{TagLog.UMP} " + ConsentInformation.ConsentStatus.ToString().ToUpper() + " CHECKING");
                yield break;
            }

            isChecking = true;

            // Create a ConsentRequestParameters object.
            ConsentRequestParameters request = new ConsentRequestParameters
            {
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    TestDeviceHashedIds = testDeivesIDConsent,
                    DebugGeography = debugGeography
                }
            };

            // Check the current consent information status.
            ConsentInformation.Update(request, (updateError =>
            {
                PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (updateError != null)
                    {
                        // Handle the error.
                        AdsManager.Instance.LogError($"{TagLog.UMP} " + ConsentInformation.ConsentStatus.ToString().ToUpper() + " --> " + updateError.Message);
                        isChecking = false;
                        return;
                    }

                    if (ConsentInformation.CanRequestAds()) // Determine the consent-related action to take based on the ConsentStatus.
                    {
                        // Consent has already been gathered or not required.
                        // Return control back to the user.
                        AdsManager.Instance.Log($"{TagLog.UMP} " + "Update " + ConsentInformation.ConsentStatus.ToString().ToUpper() + " -- Consent has already been gathered or not required");
                        isChecking = false;
                        return;
                    }

                    AdsManager.Instance.Log(ConsentInformation.ConsentStatus.ToString().ToUpper() + " --> LOAD AND SHOW ConsentForm If Required");

                    // If the error is null, the consent information state was updated.
                    // You are now ready to check if a form is available.
                    ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
                    {
                        if (formError != null)
                        {
                            // Consent gathering failed.
                            AdsManager.Instance.LogError($"{TagLog.UMP} " + ConsentInformation.ConsentStatus.ToString().ToUpper() + " --> " + formError.Message);
                            return;
                        }
                        else
                        {
                            // Form showing succeeded.
                            AdsManager.Instance.Log($"{TagLog.UMP} " + ConsentInformation.ConsentStatus.ToString().ToUpper() + " --> LOAD AND SHOW SUCCESS");
                        }

                        isChecking = false;
                    });
                });

            }));

            while (isChecking)
            {
                yield return null;
            }
#endif
            yield break;
        }

        private List<Placement> GetAdUnitIds(bool isIOS, bool isTest, List<Placement> iosIds, List<Placement> androidIds, List<Placement> iosTestIds, List<Placement> androidTestIds)
        {
            return isTest ? (isIOS ? iosTestIds : androidTestIds) : (isIOS ? iosIds : androidIds);
        }

        private void CreateAdController<T>(List<Placement> placements, List<T> adList) where T : AdsPlacementBase
        {
            if (placements.Count <= 0)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {typeof(T).Name} IDs NULL or Empty --> return");
                return;
            }

            for (int i = 0; i < placements.Count; i++)
            {
                var adId = placements[i];
                var adController = new GameObject().AddComponent<T>();
                adController.name = typeof(T).Name;
                adController.transform.parent = this.transform;
                adController.Init(adId, (PlacementOrder)(i + 1));
                adList.Add(adController);
            }
        }

        public void OpenAdInspector()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            AdsManager.Instance.Log("Opening ad Inspector.");
            MobileAds.OpenAdInspector((AdInspectorError error) =>
            {
                // If the operation failed, an error is returned.
                if (error != null)
                {
                    AdsManager.Instance.Log("Ad Inspector failed to open with error: " + error);
                    return;
                }

                AdsManager.Instance.Log("Ad Inspector opened successfully.");
            });
#endif
        }

        public override int GetPlacementInfo(AdsType adsType, out List<PlacementOrder> placementOrders)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            var listPlacement = GetPlacementListByType(adsType);

            placementOrders = listPlacement
                .Where(placement => placement != null)
                .Select(placement => placement.Order)
                .ToList();

            return placementOrders.Count;
#else
            placementOrders = new List<PlacementOrder>();
            return 0;
#endif
        }

        public override AdsEvents GetAdsStatus(AdsType type, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            var listPlacement = GetPlacementListByType(type);

            if (!IsListExist(listPlacement))
            {
                return AdsEvents.None;
            }

            var index = GetPlacementIndex((int)order, listPlacement.Count);

            if (index == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {type} {order} is not exist");
                return AdsEvents.None;
            }

            return listPlacement[index].Status;
#else
            return AdsEvents.None;
#endif
        }

        public override void RemoveAds()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            foreach (var ad in bannerList)
            {
                ad.HideAds();
            }

            foreach (var ad in mrecList)
            {
                ad.HideAds();
            }

            foreach (var ad in mrecOpenList)
            {
                ad.HideAds();
            }

            foreach (var ad in nativeOverlayList)
            {
                ad.HideAds();
            }

            foreach (var ad in nativeBannerList)
            {
                ad.HideAds();
            }

            foreach (var ad in nativeMrecList)
            {
                ad.HideAds();
            }

            foreach (var ad in nativeMrecOpenList)
            {
                ad.HideAds();
            }

            foreach (var ad in nativeVideoList)
            {
                ad.HideAds();
            }

#endif
        }

        public override bool IsAdsReady(AdsType adsType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            int orderIndex = -1;
            switch (adsType)
            {
                case AdsType.Banner:
                    orderIndex = GetPlacementIndex((int)order, bannerList.Count);
                    break;
                case AdsType.Interstitial:
                    orderIndex = GetPlacementIndex((int)order, interList.Count);
                    break;
                case AdsType.InterOpen:
                    orderIndex = GetPlacementIndex((int)order, interOpenList.Count);
                    break;
                case AdsType.Rewarded:
                    orderIndex = GetPlacementIndex((int)order, rewardList.Count);
                    break;
                case AdsType.Mrec:
                    orderIndex = GetPlacementIndex((int)order, mrecList.Count);
                    break;
                case AdsType.MrecOpen:
                    orderIndex = GetPlacementIndex((int)order, mrecOpenList.Count);
                    break;
                case AdsType.AppOpen:
                    orderIndex = GetPlacementIndex((int)order, appOpenList.Count);
                    break;
                default:
                    return false;
            }

            if (orderIndex <= -1) {
                return false;
            }

            switch (adsType)
            {
                case AdsType.Banner:
                    return bannerList[orderIndex].IsAdsReady();
                case AdsType.Interstitial:
                    return interList[orderIndex].IsAdsReady();
                case AdsType.InterOpen:
                    return interOpenList[orderIndex].IsAdsReady();
                case AdsType.Rewarded:
                    return rewardList[orderIndex].IsAdsReady();
                case AdsType.Mrec:
                    return mrecList[orderIndex].IsAdsReady();
                case AdsType.MrecOpen:
                    return mrecOpenList[orderIndex].IsAdsReady();
                case AdsType.AppOpen:
                    return appOpenList[orderIndex].IsAdsReady();
                default:
                    return false;
            }
#else
            return false;
#endif
        }

        public override bool IsAdsControllerExist(AdsType adsType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            int orderIndex = -1;
            switch (adsType)
            {
                case AdsType.Banner:
                    orderIndex = GetPlacementIndex((int)order, bannerList.Count);
                    break;
                case AdsType.Interstitial:
                    orderIndex = GetPlacementIndex((int)order, interList.Count);
                    break;
                case AdsType.InterOpen:
                    orderIndex = GetPlacementIndex((int)order, interOpenList.Count);
                    break;
                case AdsType.Rewarded:
                    orderIndex = GetPlacementIndex((int)order, rewardList.Count);
                    break;
                case AdsType.Mrec:
                    orderIndex = GetPlacementIndex((int)order, mrecList.Count);
                    break;
                case AdsType.MrecOpen:
                    orderIndex = GetPlacementIndex((int)order, mrecOpenList.Count);
                    break;
                case AdsType.AppOpen:
                    orderIndex = GetPlacementIndex((int)order, appOpenList.Count);
                    break;
                case AdsType.NativeOverlay:
                    orderIndex = GetPlacementIndex((int)order, nativeOverlayList.Count);
                    break;
                case AdsType.NativeUnity:
                    orderIndex = GetPlacementIndex((int)order, nativeOverlayList.Count);
                    break;
                case AdsType.NativeBanner:
                    orderIndex = GetPlacementIndex((int)order, nativeBannerList.Count);
                    break;
                case AdsType.NativeInter:
                    orderIndex = GetPlacementIndex((int)order, nativeInterList.Count);
                    break;
                case AdsType.NativeReward:
                    orderIndex = GetPlacementIndex((int)order, nativeRewardList.Count);
                    break;
                case AdsType.NativeMrec:
                    orderIndex = GetPlacementIndex((int)order, nativeMrecList.Count);
                    break;
                case AdsType.NativeAppOpen:
                    orderIndex = GetPlacementIndex((int)order, nativeAppOpenList.Count);
                    break;
                case AdsType.NativeInterOpen:
                    orderIndex = GetPlacementIndex((int)order, nativeInterOpenList.Count);
                    break;
                case AdsType.NativeMrecOpen:
                    orderIndex = GetPlacementIndex((int)order, nativeMrecOpenList.Count);
                    break;
                case AdsType.NativeVideo:
                    orderIndex = GetPlacementIndex((int)order, nativeVideoList.Count);
                    break;
                default:
                    return false;
            }

            return orderIndex != -1;
#else
            return false;
#endif
        }

        private int GetPlacementIndex(int order, int listCount)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (listCount <= 0)
            {
                return -1;
            }

            if (order > listCount)
            {
                return -1;
            }

            return Mathf.Clamp(order - 1, 0, listCount - 1);
#else
            return -1;
#endif
        }

        private List<AdsPlacementBase> GetPlacementListByType(AdsType type)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB
            switch (type)
            {
                case AdsType.Banner:
                    return bannerList.Cast<AdsPlacementBase>().ToList();
                case AdsType.Interstitial:
                    return interList.Cast<AdsPlacementBase>().ToList();
                case AdsType.Rewarded:
                    return rewardList.Cast<AdsPlacementBase>().ToList();
                case AdsType.Mrec:
                    return mrecList.Cast<AdsPlacementBase>().ToList();
                case AdsType.AppOpen:
                    return appOpenList.Cast<AdsPlacementBase>().ToList();
                case AdsType.MrecOpen:
                    return mrecOpenList.Cast<AdsPlacementBase>().ToList();
                case AdsType.InterOpen:
                    return interOpenList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeOverlay:
                    return nativeOverlayList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeUnity:
                    return nativeOverlayList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeBanner:
                    return nativeBannerList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeInter:
                    return nativeInterList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeReward:
                    return nativeRewardList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeMrec:
                    return nativeMrecList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeAppOpen:
                    return nativeAppOpenList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeInterOpen:
                    return nativeInterOpenList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeMrecOpen:
                    return nativeMrecOpenList.Cast<AdsPlacementBase>().ToList();
                case AdsType.NativeVideo:
                    return nativeVideoList.Cast<AdsPlacementBase>().ToList();
                default:
                    return null;
            }
#else
            return null;
#endif
        }

        public override void LoadInterstitial(AdsType interType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB
            var list = interType == AdsType.InterOpen ? (new List<AdmobInterstitialController>(interOpenList)) : interList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {interType} {order} is not exist");
                return;
            }

            list[placementIndex].LoadAds();
#endif
        }

        public override void ShowInterstitial(AdsType interType, PlacementOrder order, string position, Action OnClose = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            var list = interType == AdsType.InterOpen ? (new List<AdmobInterstitialController>(interOpenList)) : interList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {interType} {order} is not exist");
                return;
            }

            list[placementIndex].ShowAds(position, OnClose);
#endif
        }

        public override void LoadRewarded(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(rewardList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, rewardList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Rewarded"} {order} is not exist");
                return;
            }

            rewardList[placementIndex].LoadAds();
#endif
        }

        public override void ShowRewarded(PlacementOrder order, string position, Action OnRewarded = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(rewardList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, rewardList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Rewarded"} {order} is not exist");
                return;
            }

            rewardList[placementIndex].ShowAds(position, OnRewarded);
#endif
        }

        public override void LoadAppOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(appOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, appOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"AppOpen"} {order} is not exist");
                return;
            }

            appOpenList[placementIndex].LoadAds();
#endif
        }

        public override void ShowAppOpen(PlacementOrder order, string position, Action OnClose = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(appOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, appOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"AppOpen"} {order} is not exist");
                return;
            }

            appOpenList[placementIndex].ShowAds(position, OnClose);
#endif
        }

        public override void LoadBanner(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(bannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, bannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Banner"} {order} is not exist");
                return;
            }

            bannerList[placementIndex].LoadAds();
#endif
        }

        public override void HideBanner(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(bannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, bannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Banner"} {order} is not exist");
                return;
            }

            bannerList[placementIndex].HideAds();
#endif
        }

        public override void ShowBanner(PlacementOrder order, string position)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(bannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, bannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Banner"} {order} is not exist");
                return;
            }

            bannerList[placementIndex].ShowAds(position);
#endif
        }

        public override void LoadMrec(AdsType mrecType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            var list = mrecType == AdsType.MrecOpen ?(new List<AdmobMrecController>(mrecOpenList)) : mrecList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Mrec"} {order} is not exist");
                return;
            }

            list[placementIndex].LoadAds();
#endif
        }

        public override void ShowMrec(AdsType mrecType, PlacementOrder order, AdsPos mrecPosition, Vector2Int offset, string position)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            var list = mrecType == AdsType.MrecOpen ?(new List<AdmobMrecController>(mrecOpenList)) : mrecList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Mrec"} {order} is not exist");
                return;
            }

            list[placementIndex].ShowAds(mrecPosition, offset, position);
#endif
        }

        public override void HideMrec(AdsType mrecType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            var list = mrecType == AdsType.MrecOpen ?(new List<AdmobMrecController>(mrecOpenList)) : mrecList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"Mrec"} {order} is not exist");
                return;
            }

            list[placementIndex].HideAds();
#endif
        }

        public void LoadNativeOverlay(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeOverlayList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeOverlayList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeOverlay"} {order} is not exist");
                return;
            }

            nativeOverlayList[placementIndex].LoadAds();
#endif
        }

        public void ShowNativeOverlay(PlacementOrder order, NativeTemplateStyle style, AdsPos nativeOverlayposition, Vector2Int size, Vector2Int offset, string position, Action OnShow = null, Action OnClose = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeOverlayList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeOverlayList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeOverlay"} {order} is not exist");
                return;
            }

            nativeOverlayList[placementIndex].ShowAds(style, nativeOverlayposition, size, offset, position, OnShow, OnClose);
#endif
        }

        public void HideNativeOverlay(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeOverlayList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeOverlayList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeOverlay"} {order} is not exist");
                return;
            }

            nativeOverlayList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeBanner(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeBannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeBannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeBanner"} {order} is not exist");
                return;
            }

            nativeBannerList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeBanner(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeBannerList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeBannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativePlatform"} {order} is not exist");
                return null;
            }

            var controller = nativeBannerList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeBanner(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeBannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeBannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeBanner"} {order} is not exist");
                return;
            }

            nativeBannerList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeInter(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeInterList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeInterList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeInter"} {order} is not exist");
                return;
            }

            nativeInterList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeInter(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeInterList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeInterList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeInter"} {order} is not exist");
                return null;
            }

            var controller = nativeInterList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeInter(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeInterList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeInterList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeInter"} {order} is not exist");
                return;
            }

            nativeInterList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeReward(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeRewardList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeRewardList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeReward"} {order} is not exist");
                return;
            }

            nativeRewardList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeReward(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeRewardList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeRewardList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeReward"} {order} is not exist");
                return null;
            }

            var controller = nativeRewardList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeReward(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeRewardList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeRewardList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeReward"} {order} is not exist");
                return;
            }

            nativeRewardList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeMrec(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeMrecList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeMrecList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeMrec"} {order} is not exist");
                return;
            }

            nativeMrecList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeMrec(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeMrecList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeMrecList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeMrec"} {order} is not exist");
                return null;
            }

            var controller = nativeMrecList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeMrec(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeMrecList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeMrecList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeMrec"} {order} is not exist");
                return;
            }

            nativeMrecList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeAppOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeAppOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeAppOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeAppOpen"} {order} is not exist");
                return;
            }

            nativeAppOpenList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeAppOpen(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeAppOpenList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeAppOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeAppOpen"} {order} is not exist");
                return null;
            }

            var controller = nativeAppOpenList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeAppOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeAppOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeAppOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeAppOpen"} {order} is not exist");
                return;
            }

            nativeAppOpenList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeInterOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeInterOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeInterOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeInterOpen"} {order} is not exist");
                return;
            }

            nativeInterOpenList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeInterOpen(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeInterOpenList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeInterOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeInterOpen"} {order} is not exist");
                return null;
            }

            var controller = nativeInterOpenList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeInterOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeInterOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeInterOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeInterOpen"} {order} is not exist");
                return;
            }

            nativeInterOpenList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeMrecOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeMrecOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeMrecOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeMrecOpen"} {order} is not exist");
                return;
            }

            nativeMrecOpenList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeMrecOpen(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeMrecOpenList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeMrecOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeMrecOpen"} {order} is not exist");
                return null;
            }

            var controller = nativeMrecOpenList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeMrecOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeMrecOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeMrecOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeMrecOpen"} {order} is not exist");
                return;
            }

            nativeMrecOpenList[placementIndex].HideAds();
#endif
        }

        public void LoadNativeVideo(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeVideoList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeVideoList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeVideo"} {order} is not exist");
                return;
            }

            nativeVideoList[placementIndex].LoadAds();
#endif
        }

        public NativePlatformShowBuilder ShowNativeVideo(PlacementOrder order, string position, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeVideoList))
            {
                return null;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeVideoList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeVideo"} {order} is not exist");
                return null;
            }

            var controller = nativeVideoList[placementIndex];

            return new NativePlatformShowBuilder(controller, position, layoutName, OnShow, OnClose, OnAdDismissedFullScreenContent);
#endif
            return null;
        }

        public void HideNativeVideo(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            if (!IsListExist(nativeVideoList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, nativeVideoList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.ADMOB} {"NativeVideo"} {order} is not exist");
                return;
            }

            nativeVideoList[placementIndex].HideAds();
#endif
        }

        private bool IsListExist<T>(List<T> list) where T : AdsPlacementBase
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            bool isExist = false;
            isExist = list.Count > 0;

            if (!isExist)
            {
                AdsManager.Instance.LogError($"{typeof(T).Name} is empty");
            }

            return isExist;
#else
            return false;
#endif
        }

        public override void HideAllBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            foreach (var banner in bannerList)
            {
                banner.HideAds();
            }
#endif
        }

        public override void HideAllMrec()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            foreach (var mrec in mrecList)
            {
                mrec.HideAds();
            }
#endif
        }

        public void HideAllNativeBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            foreach (var nativeBanner in nativeBannerList)
            {
                nativeBanner.HideAds();
            }
#endif
        }

        public void HideAllNativeMrec()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            foreach (var nativeMrec in nativeMrecList)
            {
                nativeMrec.HideAds();
            }
#endif
        }


        public override AdsMediation GetMediationType()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_ADMOB

            return AdsMediation.Admob;
#else
            return AdsMediations.None;
#endif
        }
    }
}

#endif