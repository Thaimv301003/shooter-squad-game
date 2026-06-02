#if USE_MAX

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace TheLegends.Base.Ads
{
    public class MaxMediationController : AdsMediationBase
    {
        private InitiationStatus status = InitiationStatus.NotInitialized;

        private List<MaxInterstitialController> interList = new List<MaxInterstitialController>();
        private List<MaxInterstitialOpenController> interOpenList = new List<MaxInterstitialOpenController>();
        private List<MaxRewardedController> rewardList = new List<MaxRewardedController>();
        private List<MaxAppOpenController> appOpenList = new List<MaxAppOpenController>();
        private List<MaxBannerController> bannerList = new List<MaxBannerController>();
        private List<MaxMrecController> mrecList = new List<MaxMrecController>();
        private List<MaxMrecOpenController> mrecOpenList = new List<MaxMrecOpenController>();

        public override IEnumerator DoInit()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX

            status = InitiationStatus.Initializing;

            var platform = Application.platform;
            var isIOS = platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.OSXPlayer;

            var bannerIds = GetAdUnitIds(isIOS, AdsManager.Instance.SettingsAds.MAX_iOS.bannerIds, AdsManager.Instance.SettingsAds.MAX_Android.bannerIds);
            CreateAdController(bannerIds, bannerList);

            var interIds = GetAdUnitIds(isIOS, AdsManager.Instance.SettingsAds.MAX_iOS.interIds, AdsManager.Instance.SettingsAds.MAX_Android.interIds);
            CreateAdController(interIds, interList);

            var interOpenIds = GetAdUnitIds(isIOS, AdsManager.Instance.SettingsAds.MAX_iOS.interOpenIds, AdsManager.Instance.SettingsAds.MAX_Android.interOpenIds);
            CreateAdController(interOpenIds, interOpenList);

            var rewardedIds = GetAdUnitIds(isIOS, AdsManager.Instance.SettingsAds.MAX_iOS.rewardIds, AdsManager.Instance.SettingsAds.MAX_Android.rewardIds);
            CreateAdController(rewardedIds, rewardList);

            var mrecIds = GetAdUnitIds(isIOS, AdsManager.Instance.SettingsAds.MAX_iOS.mrecIds, AdsManager.Instance.SettingsAds.MAX_Android.mrecIds);
            CreateAdController(mrecIds, mrecList);

            var mrecOpenIds = GetAdUnitIds(isIOS, AdsManager.Instance.SettingsAds.MAX_iOS.mrecOpenIds, AdsManager.Instance.SettingsAds.MAX_Android.mrecOpenIds);
            CreateAdController(mrecOpenIds, mrecOpenList);

            var appOpenIds = GetAdUnitIds(isIOS, AdsManager.Instance.SettingsAds.MAX_iOS.appOpenIds, AdsManager.Instance.SettingsAds.MAX_Android.appOpenIds);
            CreateAdController(appOpenIds, appOpenList);


            MaxSdkCallbacks.OnSdkInitializedEvent += (sdkConfiguration) =>
            {
                PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (sdkConfiguration.IsSuccessfullyInitialized)
                    {
                        status = InitiationStatus.Initialized;
                        AdsManager.Instance.Log($"{TagLog.MAX} " + "Max SDK initialized");

                        if (AdsManager.Instance.SettingsAds.isShowMediationDebugger)
                        {
                            MaxSdk.ShowMediationDebugger();
                        }
                    }
                    else
                    {
                        status = InitiationStatus.Failed;
                        AdsManager.Instance.Log($"{TagLog.MAX} " + "Max SDK initialization failed");
                    }
                });

            };

            if (AdsManager.Instance.SettingsAds.isTest)
            {
                string testDeviceID = "";
#if UNITY_ANDROID
                testDeviceID = GetAndroidAdvertiserId();
#elif UNITY_IOS
                testDeviceID = GetIOSAdvertiserId();
#endif
                MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[] { testDeviceID });
            }

            MaxSdk.InitializeSdk();

            while (status == InitiationStatus.Initializing)
            {
                yield return null;
            }

#endif
            yield break;
        }

        private List<Placement> GetAdUnitIds(bool isIOS, List<Placement> iosIds, List<Placement> androidIds)
        {
            return isIOS ? iosIds : androidIds;
        }

        private void CreateAdController<T>(List<Placement> placements, List<T> adList) where T : AdsPlacementBase
        {
            if (placements.Count <= 0)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {typeof(T).Name} IDs NULL or Empty --> return");
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

        public override AdsEvents GetAdsStatus(AdsType type, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX            
            var listPlacement = new List<AdsPlacementBase>();

            switch (type)
            {
                case AdsType.Banner:
                    listPlacement = bannerList.Cast<AdsPlacementBase>().ToList();
                    break;
                case AdsType.Interstitial:
                    listPlacement = interList.Cast<AdsPlacementBase>().ToList();
                    break;
                case AdsType.Rewarded:
                    listPlacement = rewardList.Cast<AdsPlacementBase>().ToList();
                    break;
                case AdsType.Mrec:
                    listPlacement = mrecList.Cast<AdsPlacementBase>().ToList();
                    break;
                case AdsType.AppOpen:
                    listPlacement = appOpenList.Cast<AdsPlacementBase>().ToList();
                    break;
                case AdsType.MrecOpen:
                    listPlacement = mrecOpenList.Cast<AdsPlacementBase>().ToList();
                    break;
                case AdsType.InterOpen:
                    listPlacement = interOpenList.Cast<AdsPlacementBase>().ToList();
                    break;
                default:
                    return AdsEvents.None;
            }

            if (!IsListExist(listPlacement))
            {
                return AdsEvents.None;
            }

            var index = GetPlacementIndex((int)order, listPlacement.Count);

            if (index == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {type} {order} is not exist");
                return AdsEvents.None;
            }

            return listPlacement[index].Status;
#else
            return AdsEvents.None;
#endif
        }

        private bool IsListExist<T>(List<T> list) where T : AdsPlacementBase
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
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

        private int GetPlacementIndex(int order, int listCount)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
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

        public override AdsMediation GetMediationType()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            return AdsMediation.Max;
#else
            return AdsMediation.None;
#endif
        }

        public override void HideAllBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            foreach (var banner in bannerList)
            {
                banner.HideAds();
            }
#endif
        }

        public override void HideAllMrec()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            foreach (var mrec in mrecList)
            {
                mrec.HideAds();
            }
#endif
        }

        public override void LoadInterstitial(AdsType interType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            var list = interType == AdsType.InterOpen ? (new List<MaxInterstitialController>(interOpenList)) : interList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {interType} {order} is not exist");
                return;
            }

            list[placementIndex].LoadAds();
#endif
        }

        public override void ShowInterstitial(AdsType interType, PlacementOrder order, string position, Action OnClose = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            var list = interType == AdsType.InterOpen ? (new List<MaxInterstitialController>(interOpenList)) : interList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {interType} {order} is not exist");
                return;
            }

            list[placementIndex].ShowAds(position, OnClose);
#endif
        }

        public override void LoadRewarded(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            if (!IsListExist(rewardList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, rewardList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"Rewarded"} {order} is not exist");
                return;
            }

            rewardList[placementIndex].LoadAds();
#endif
        }

        public override void ShowRewarded(PlacementOrder order, string position, Action OnRewarded = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            if (!IsListExist(rewardList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, rewardList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"Rewarded"} {order} is not exist");
                return;
            }

            rewardList[placementIndex].ShowAds(position, OnRewarded);
#endif
        }

        public override void LoadAppOpen(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            if (!IsListExist(appOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, appOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"AppOpen"} {order} is not exist");
                return;
            }

            appOpenList[placementIndex].LoadAds();
#endif
        }

        public override void ShowAppOpen(PlacementOrder order, string position, Action OnClose = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            if (!IsListExist(appOpenList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, appOpenList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"AppOpen"} {order} is not exist");
                return;
            }

            appOpenList[placementIndex].ShowAds(position);
#endif
        }

        public override void LoadBanner(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            if (!IsListExist(bannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, bannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"Banner"} {order} is not exist");
                return;
            }

            bannerList[placementIndex].LoadAds();
#endif
        }

        public override void ShowBanner(PlacementOrder order, string position)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            if (!IsListExist(bannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, bannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"Banner"} {order} is not exist");
                return;
            }

            bannerList[placementIndex].ShowAds(position);
#endif
        }

        public override void HideBanner(PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            if (!IsListExist(bannerList))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, bannerList.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"Banner"} {order} is not exist");
                return;
            }

            bannerList[placementIndex].HideAds();
#endif
        }

        public override void LoadMrec(AdsType mrecType, PlacementOrder order)
        {

        }

        public override void ShowMrec(AdsType mrecType, PlacementOrder order, AdsPos mrecPosition, Vector2Int offset, string position)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX

            var list = mrecType == AdsType.MrecOpen ? (new List<MaxMrecController>(mrecOpenList)) : mrecList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"Mrec"} {order} is not exist");
                return;
            }

            list[placementIndex].ShowAds(mrecPosition, offset, position);
#endif
        }

        public override void HideMrec(AdsType mrecType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX

            var list = mrecType == AdsType.MrecOpen ? (new List<MaxMrecController>(mrecOpenList)) : mrecList;

            if (!IsListExist(list))
            {
                return;
            }

            var placementIndex = GetPlacementIndex((int)order, list.Count);

            if (placementIndex == -1)
            {
                AdsManager.Instance.LogError($"{TagLog.MAX} {"Mrec"} {order} is not exist");
                return;
            }

            list[placementIndex].HideAds();
#endif
        }

        public override void RemoveAds()
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
            foreach (var ad in bannerList)
            {
                ad.HideAds();
            }

            foreach (var ad in mrecList)
            {
                ad.HideAds();
            }
#endif
        }

        public override bool IsAdsReady(AdsType adsType, PlacementOrder order)
        {
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
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

            if (orderIndex <= -1)
            {
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
#if (UNITY_ANDROID || UNITY_IOS) && USE_MAX
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

            return orderIndex != -1;
#else
            return false;
#endif
        }

        private string GetAndroidAdvertiserId()
        {
            string advertisingID = "";
            try
            {
                AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);

                advertisingID = adInfo.Call<string>("getId").ToString();
            }
            catch (Exception)
            {
            }
            return advertisingID;
        }

#if UNITY_IOS
        private string GetIOSAdvertiserId()
        {
            return Device.advertisingIdentifier;
        }
#endif
    }
}

#endif
