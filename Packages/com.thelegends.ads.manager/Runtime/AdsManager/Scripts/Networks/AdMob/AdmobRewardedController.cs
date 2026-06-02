#if USE_ADMOB

using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
#if USE_DATABUCKETS
using TheLegends.Base.Databuckets;
#endif
using TheLegends.Base.UI;

namespace TheLegends.Base.Ads
{
    public class AdmobRewardedController : AdsPlacementBase
    {
        private RewardedAd _rewardedAd;
        private Action OnRewarded;

        public override AdsMediation GetAdsMediation()
        {
#if USE_ADMOB
            return AdsMediation.Admob;
#else
            return AdsMediation.None;
#endif
        }

        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.Rewarded;
#else
            return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_ADMOB
            if (_rewardedAd != null)
            {
                return _rewardedAd.CanShowAd();
            }

            return false;
#else
            return false;
#endif
        }

        public override void LoadAds()
        {
#if USE_ADMOB
            if (!IsCanLoadAds())
            {
                return;
            }

            if (IsReady)
            {
                return;
            }


            base.LoadAds();
            AdRequest request = new AdRequest();

            RewardedAd.Load(adsUnitID.Trim(), request,
                (RewardedAd ad, LoadAdError error) =>
                {
                    if (_loadRequestId != _currentLoadRequestId)
                    {
                        // If the load request ID does not match, this callback is from a previous request
                        return;
                    }

                    StopHandleTimeout();

                    // if error is not null, the load request failed.
                    if (error != null)
                    {
                        AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "ad failed to load with error : " + error);
                        OnRewardedLoadFailed(error);
                        return;
                    }

                    if (ad == null)
                    {
                        AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "Unexpected error: load event fired with null ad and null error.");
                        OnRewardedLoadFailed(error);
                        return;
                    }

                    networkName = ad.GetResponseInfo().GetMediationAdapterClassName();

                    AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + "ad loaded with response : " + ad.GetResponseInfo());

                    _rewardedAd = ad;

                    OnAdsLoadAvailable();
                });
        }
#endif


        public void ShowAds(string showPosition, Action OnRewarded = null)
        {
            base.ShowAds(showPosition);
#if USE_ADMOB
            if (IsReady && IsAvailable)
            {
                _rewardedAd.OnAdClicked += OnRewardClick;
                _rewardedAd.OnAdPaid += OnRewardPaid;
                _rewardedAd.OnAdImpressionRecorded += OnRewardImpression;
                _rewardedAd.OnAdFullScreenContentClosed += OnRewardedClosed;
                _rewardedAd.OnAdFullScreenContentFailed += OnRewardedShowFailed;
                _rewardedAd.OnAdFullScreenContentOpened += OnRewardShowSuccess;
                _rewardedAd.Show(reward =>
                {
                    if (reward != null)
                    {
                        AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + $"{adsUnitID} " + "claimed");
                        this.OnRewarded = OnRewarded;
                    }
                });
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

        private void OnRewardClick()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClick();
            });
        }

        private void OnRewardPaid(AdValue value)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsPaid(value);
            });
        }

        private void OnRewardImpression()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnImpression();
            });
        }

        private void OnRewardShowSuccess()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsShowSuccess();
                AdsManager.Instance.OnFullScreenAdsShow();
            });
        }

        private void OnRewardedLoadFailed(AdError error)
        {
            var errorDescription = error?.GetMessage();
            OnAdsLoadFailed(errorDescription);
        }

        private void OnRewardedShowFailed(AdError error)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var errorDescription = error?.GetMessage();
                OnAdsShowFailed(errorDescription);
            });
        }

        private void OnRewardedClosed()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UILoadingController.Show(1f, () =>
                {
                    OnRewarded?.Invoke();
                    OnRewarded = null;

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

                _rewardedAd.OnAdClicked -= OnRewardClick;
                _rewardedAd.OnAdPaid -= OnRewardPaid;
                _rewardedAd.OnAdImpressionRecorded -= OnRewardImpression;
                _rewardedAd.OnAdFullScreenContentClosed -= OnRewardedClosed;
                _rewardedAd.OnAdFullScreenContentFailed -= OnRewardedShowFailed;
                _rewardedAd.OnAdFullScreenContentOpened -= OnRewardShowSuccess;

                if (_rewardedAd != null)
                {
                    _rewardedAd.Destroy();
                    _rewardedAd = null;
                }

                OnAdsClosed();
            });
        }

        private void OnAdsPaid(AdValue value)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, value);
            });
        }

        #endregion
    }
}

#endif