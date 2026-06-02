using System;
using System.Collections.Generic;
#if USE_DATABUCKETS
using TheLegends.Base.Databuckets;
#endif
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public abstract class AdsPlacementBase : MonoBehaviour
    {
        protected Placement placement;
        public Placement Placement { get => placement; set => placement = value; }

        public PlacementOrder Order { get; set; }
        protected int adsUnitIDIndex = 0;

        protected string adsUnitID = string.Empty;
        protected string position = "default";

        protected int reloadCount = 0;

        protected string _loadRequestId = "";

        protected string _currentLoadRequestId = "";
        protected string networkName = "";
        private DateTime loadStartTime = DateTime.MinValue;
        private DateTime loadEndTime = DateTime.MinValue;
        protected float loadTime
        {
            get
            {
                return (float)(loadEndTime - loadStartTime).TotalSeconds;
            }
        }

        protected AdsEvents status;
        public AdsEvents Status
        {
            get => status;
            protected set
            {
                if (status != value)
                {
                    status = value;
                    AdsManager.Instance.SetStatus(AdsMediation, AdsType, adsUnitID, position, value);
                }
            }
        }

        public bool IsReady { get => IsAdsReady(); }

        public abstract bool IsAdsReady();

        public bool IsAvailable { get => IsAdsAvailable(); }

        public AdsMediation AdsMediation { get => GetAdsMediation(); }

        public AdsType AdsType { get => GetAdsType(); }

        protected float timeOut = 10f;

        public virtual void Init(Placement placement, PlacementOrder order)
        {
            SetTimeOut();
            this.Placement = placement;
            this.Order = order;
        }

        protected virtual void SetTimeOut()
        {
            timeOut = AdsManager.Instance.adsConfigs.adLoadTimeOut;
        }

        public abstract AdsMediation GetAdsMediation();

        public abstract AdsType GetAdsType();

        public virtual void LoadAds()
        {
            Status = AdsEvents.LoadRequest;

            loadStartTime = DateTime.UtcNow;

            _currentLoadRequestId = Guid.NewGuid().ToString();
            _loadRequestId = _currentLoadRequestId;

            StartHandleTimeout();
        }

        protected void StartHandleTimeout()
        {
            Invoke(nameof(HandleTimeOut), AdsManager.Instance.adsConfigs.adLoadTimeOut);
        }

        protected void StopHandleTimeout()
        {
            CancelInvoke(nameof(HandleTimeOut));
        }

        protected bool IsCanLoadAds()
        {
            if (!AdsManager.Instance.IsCanShowAds && AdsType != AdsType.Rewarded)
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not can show ads --> return");
                return false;
            }

            if (IsInvoking(nameof(LoadAds)))
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is scheduled loading --> return");
                return false;
            }

            if (Status == AdsEvents.LoadRequest)
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is loading --> return");
                return false;
            }

            if (IsAvailable)
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is available --> return");
                return false;
            }

            if (placement.stringIDs != null && placement.stringIDs.Count > 0)
            {
                adsUnitIDIndex %= placement.stringIDs.Count;
                adsUnitID = placement.stringIDs[adsUnitIDIndex];
                AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + "Startting LoadAds " + adsUnitID);
            }

            if (string.IsNullOrEmpty(adsUnitID))
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "UnitId NULL or Empty --> return");
                return false;
            }

            return true;
        }

        public virtual void OnAdsLoadAvailable()
        {
            Status = AdsEvents.LoadAvailable;
            reloadCount = 0;

            loadEndTime = DateTime.UtcNow;

#if USE_DATABUCKETS

            // DatabucketsManager.Instance.RecordEvent("ad_request", new Dictionary<string, object>
            // {
            //     { "ad_format", AdsType.ToString() },
            //     { "ad_platform", AdsMediation.ToString() },
            //     { "ad_network", networkName},
            //     { "ad_unit_id", adsUnitID },
            //     { "is_load", 1 },
            //     { "load_time", loadTime }
            // });
#endif
        }

        public bool IsAdsAvailable()
        {
            return Status == AdsEvents.LoadAvailable && (AdsType == AdsType.Rewarded || AdsManager.Instance.IsCanShowAds);
        }

        protected virtual void OnAdsLoadFailed(string message)
        {

            Status = AdsEvents.LoadFail;
            _currentLoadRequestId = "";

            loadEndTime = DateTime.UtcNow;

#if USE_DATABUCKETS
            // DatabucketsManager.Instance.RecordEvent("ad_request", new Dictionary<string, object>
            // {
            //     { "ad_format", AdsType.ToString() },
            //     { "ad_platform", AdsMediation.ToString() },
            //     { "ad_network", "Unavailable"},
            //     { "ad_unit_id", adsUnitID },
            //     { "is_load", 0 },
            //     { "load_time", loadTime }
            // });
#endif

            float timeWait = 5f;

            switch (GetAdsType())
            {
                case AdsType.InterOpen:
                case AdsType.MrecOpen:
                case AdsType.NativeMrecOpen:
                case AdsType.NativeInterOpen:
                    timeWait = 0.125f;
                    break;
            }

            string extendString = "";

            if (reloadCount < AdsManager.Instance.SettingsAds.autoReLoadMax && timeWait > 0)
            {
                extendString = " re-trying in " + (timeWait * (reloadCount + 1)) + " seconds " + (reloadCount + 1) + "/" + AdsManager.Instance.SettingsAds.autoReLoadMax;
            }


            AdsManager.Instance.LogError($"{AdsMediation.ToString()}_{AdsType.ToString()} " +
                                         "OnAdsLoadFailed " + adsUnitID + " Error: " + message + extendString);

            if (reloadCount < AdsManager.Instance.SettingsAds.autoReLoadMax)
            {
                adsUnitIDIndex++;
                reloadCount++;
                Invoke(nameof(LoadAds), timeWait * reloadCount);
            }
            else
            {
                Status = AdsEvents.LoadNotAvailable;
                adsUnitIDIndex = 0;
                reloadCount = 0;
            }

        }

        protected void HandleTimeOut()
        {
            if (Status == AdsEvents.LoadRequest)
            {
                OnAdsLoadTimeOut();
            }
        }

        protected virtual void OnAdsLoadTimeOut()
        {
            Status = AdsEvents.LoadTimeOut;
            StopHandleTimeout();
            OnAdsLoadFailed("TimeOut");
        }

        public virtual void ShowAds(string showPosition)
        {
            position = showPosition;
        }


        public virtual void OnAdsShowSuccess()
        {
            Status = AdsEvents.ShowSuccess;
        }

        public virtual void OnAdsShowFailed(string message)
        {
            Status = AdsEvents.ShowFail;

            AdsManager.Instance.LogError($"{AdsMediation.ToString()}_{AdsType.ToString()} " + "OnAdsShowFailed " +
                                         adsUnitID + " Error: " + message);
        }


        public virtual void OnAdsClosed()
        {
            Status = AdsEvents.Close;
            adsUnitIDIndex = 0;

            bool isPreload = false;
            var settings = AdsManager.Instance.SettingsAds.preloadSettings;

            switch (AdsType)
            {
                case AdsType.Banner:
                    isPreload = settings.preloadBanner;
                    break;
                case AdsType.Interstitial:
                    isPreload = settings.preloadInterstitial;
                    break;
                case AdsType.Rewarded:
                    isPreload = settings.preloadRewarded;
                    break;
                case AdsType.Mrec:
                    isPreload = settings.preloadMREC;
                    break;
                case AdsType.AppOpen:
                    isPreload = settings.preloadAppOpen;
                    break;
                case AdsType.NativeBanner:
                    isPreload = settings.nativeAds.preloadNativeBanner;
                    break;
                case AdsType.NativeInter:
                    isPreload = settings.nativeAds.preloadNativeInter;
                    break;
                case AdsType.NativeReward:
                    isPreload = settings.nativeAds.preloadNativeReward;
                    break;
                case AdsType.NativeMrec:
                    isPreload = settings.nativeAds.preloadNativeMrec;
                    break;
                case AdsType.NativeAppOpen:
                    isPreload = settings.nativeAds.preloadNativeAppOpen;
                    break;
                case AdsType.NativeVideo:
                    isPreload = settings.nativeAds.preloadNativeVideo;
                    break;
            }

            if (isPreload)
            {
                LoadAds();
            }

#if USE_DATABUCKETS
            if (AdsType == AdsType.Interstitial
            || AdsType == AdsType.InterOpen
            || AdsType == AdsType.AppOpen
            || AdsType == AdsType.NativeInter
            || AdsType == AdsType.NativeInterOpen
            || AdsType == AdsType.NativeAppOpen)
            {
                DatabucketsManager.Instance.RecordEvent("ad_complete", new Dictionary<string, object>
                {
                    { "ad_format", AdsType.ToString() },
                    { "ad_platform", AdsMediation.ToString() },
                    { "ad_network", networkName},
                    { "ad_unit_id", adsUnitID },
                    { "placement", position}
                });
            }
#endif
        }

        public virtual void OnAdsClick()
        {
            Status = AdsEvents.Click;

#if USE_DATABUCKETS
            DatabucketsManager.Instance.RecordEvent("ad_click", new Dictionary<string, object>
            {
                { "ad_format", AdsType.ToString() },
                { "ad_platform", AdsMediation.ToString() },
                { "ad_network", networkName},
                { "ad_unit_id", adsUnitID },
                { "placement", position}
            });
#endif
        }

        public virtual void OnAdsCancel()
        {
            Status = AdsEvents.Cancel;
        }

        public virtual void OnImpression()
        {
            AdsManager.Instance.Log($"{AdsType} " + "ad recorded an impression.");
        }

    }
}

