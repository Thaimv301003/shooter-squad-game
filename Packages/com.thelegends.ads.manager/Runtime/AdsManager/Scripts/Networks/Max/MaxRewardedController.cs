#if USE_MAX
using System;
using System.Collections.Generic;
#if USE_DATABUCKETS
using TheLegends.Base.Databuckets;
#endif
using TheLegends.Base.UI;

namespace TheLegends.Base.Ads
{
    public class MaxRewardedController : AdsPlacementBase
    {
        private Action OnRewarded;
        private bool isRewarded = false;

        public override AdsMediation GetAdsMediation()
        {

#if USE_MAX
            return AdsMediation.Max;
#else
            return AdsMediation.None;
#endif
        }

        public override AdsType GetAdsType()
        {
#if USE_MAX
            return AdsType.Rewarded;
#else
            return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_MAX
            return MaxSdk.IsRewardedAdReady(adsUnitID);
#else
            return false;
#endif
        }

        public override void LoadAds()
        {
#if USE_MAX
            if (!IsCanLoadAds())
            {
                return;
            }

            if (IsReady)
            {
                return;
            }

            base.LoadAds();

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;

            MaxSdk.LoadRewardedAd(adsUnitID);
#endif
        }

        public void ShowAds(string showPosition, Action OnRewarded = null)
        {
            this.OnRewarded = OnRewarded;
            base.ShowAds(showPosition);
#if USE_MAX
            if (IsReady && IsAvailable)
            {
                MaxSdk.ShowRewardedAd(adsUnitID);
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }
#endif
        }

        #region Internal

        private void OnRewardedLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                if (_loadRequestId != _currentLoadRequestId) return;

                StopHandleTimeout();

                OnAdsLoadAvailable();
            });
        }

        private void OnRewardedDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                OnAdsShowSuccess();
                AdsManager.Instance.OnFullScreenAdsShow();
            });
        }

        private void OnRewardedRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, adInfo);
            });
        }

        private void OnRewardedLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                if (_loadRequestId != _currentLoadRequestId) return;

                StopHandleTimeout();

                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedLoadedEvent;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedLoadFailedEvent;
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedDisplayedEvent;
                MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedRevenuePaidEvent;
                MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedClickedEvent;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedHiddenEvent;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdFailedToDisplayEvent;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnAdReceivedRewardEvent;

                OnAdsLoadFailed(errorInfo.Message);
            });
        }

        private void OnRewardedClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                OnAdsClick();
            });
        }

        private void OnRewardedHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedLoadedEvent;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedLoadFailedEvent;
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedDisplayedEvent;
                MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedRevenuePaidEvent;
                MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedClickedEvent;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedHiddenEvent;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdFailedToDisplayEvent;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnAdReceivedRewardEvent;

                UILoadingController.Show(1f, () =>
                {
                    if (isRewarded)
                    {
                        OnRewarded?.Invoke();
                    }

                    isRewarded = false;

#if USE_DATABUCKETS
                    DatabucketsManager.Instance.RecordEvent("ad_complete", new Dictionary<string, object>
                    {
                        { "ad_format", AdsType.ToString() },
                        { "ad_platform", AdsMediation.ToString() },
                        { "ad_network", networkName},
                        { "ad_unit_id", adsUnitID },
                        { "end_type", "done"},
                        { "placement", position}
                    });
#endif

                    AdsManager.Instance.OnFullScreenAdsClosed();
                });

                OnAdsClosed();


            });
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                OnAdsShowFailed(errorInfo.Message);
            });
        }

        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + $"{adsUnitID} " + "claimed");
                isRewarded = true;
            });
        }

        #endregion
    }
}

#endif