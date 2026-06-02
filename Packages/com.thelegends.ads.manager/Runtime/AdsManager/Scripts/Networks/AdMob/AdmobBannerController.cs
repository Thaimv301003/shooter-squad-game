#if USE_ADMOB
using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public class AdmobBannerController : AdsPlacementBase
    {
        protected BannerView _bannerView;
        private float timeAutoReload;
        private bool isShowOnLoaded = false;

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
            return AdsType.Banner;
#else
        return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_ADMOB
            return _bannerView != null;
#else
        return false;
#endif
        }

        void Awake()
        {
            timeAutoReload = AdsManager.Instance.adsConfigs.adTimeReload;
        }

        public override void LoadAds()
        {
#if USE_ADMOB
            if (!IsCanLoadAds())
            {
                return;
            }

            BannerDestroy();

            if (!IsReady)
            {
                CreateBanner();

                _bannerView.OnAdClicked += OnBannerClick;
                _bannerView.OnAdPaid += OnBannerPaid;
                _bannerView.OnAdImpressionRecorded += OnBannerImpression;
                _bannerView.OnBannerAdLoadFailed += OnBannerLoadFailed;
                _bannerView.OnBannerAdLoaded += OnBannerLoaded;

                base.LoadAds();
                AdRequest request = new AdRequest();

                _bannerView.LoadAd(request);
            }
#endif
        }

        public override void ShowAds(string showPosition)
        {
            base.ShowAds(showPosition);
            isShowOnLoaded = true;
#if USE_ADMOB
            if (IsReady && IsAvailable)
            {
                PreShow();
                _bannerView.Show();
                OnAdsShowSuccess();
                SetShowedConfig();
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }
#endif
        }

        protected virtual void SetShowedConfig()
        {
            AdsManager.Instance.RegisterBannerConfig(new BannerShowedConfig
            {
                order = this.Order,
                position = position,
            });
        }


        public virtual void HideAds()
        {
#if USE_ADMOB
            if (Status != AdsEvents.ShowSuccess && Status != AdsEvents.Click)
            {
                AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + " is not showing --> return");
                return;
            }

            if (IsReady)
            {
                _bannerView.Hide();
                isShowOnLoaded = false;
                BannerDestroy();
                OnAdsClosed();
            }
#endif
        }

        #region Internal

        protected virtual void PreShow()
        {

        }

        protected virtual void CreateBanner()
        {
#if USE_ADMOB
            AdPosition adPosition = AdsManager.Instance.SettingsAds.bannerPosition == BannerPos.Top
                ? AdPosition.Top
                : AdPosition.Bottom;

            if (AdsManager.Instance.SettingsAds.fixBannerSmallSize)
            {
                _bannerView = new BannerView(adsUnitID.Trim(), AdSize.Banner, adPosition);
            }
            else
            {
                AdSize adaptiveSize =
                    AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                _bannerView = new BannerView(adsUnitID.Trim(), adaptiveSize, adPosition);
            }
#endif
        }

        private void OnBannerClick()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClick();
            });
        }

        private void OnBannerPaid(AdValue value)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, value);
            });
        }

        private void OnBannerImpression()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnImpression();
            });
        }

        public void OnBannerLoaded()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                _bannerView.Hide();
                
                if (_loadRequestId != _currentLoadRequestId)
                {
                    // If the load request ID does not match, this callback is from a previous request
                    return;
                }

#if UNITY_EDITOR
                _bannerView.Hide();
#endif

                StopHandleTimeout();

                OnAdsLoadAvailable();

                networkName = _bannerView.GetResponseInfo().GetMediationAdapterClassName();

                AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + "ad loaded with response : " + _bannerView.GetResponseInfo());

                if (isShowOnLoaded)
                {
                    ShowAds(position);
                }
            });

        }

        private void OnBannerLoadFailed(AdError error)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_loadRequestId != _currentLoadRequestId)
                {
                    // If the load request ID does not match, this callback is from a previous request
                    return;
                }

                StopHandleTimeout();

                var errorDescription = error?.GetMessage();
                OnAdsLoadFailed(errorDescription);
            });

        }

        protected override void OnAdsLoadFailed(string message)
        {
            base.OnAdsLoadFailed(message);
            if (Status == AdsEvents.LoadNotAvailable)
            {
                DelayReloadAd(timeAutoReload);
            }
        }

        protected void BannerDestroy()
        {
#if USE_ADMOB
            if (IsReady)
            {
                try
                {
                    _bannerView.OnAdClicked -= OnBannerClick;
                    _bannerView.OnAdPaid -= OnBannerPaid;
                    _bannerView.OnAdImpressionRecorded -= OnBannerImpression;
                    _bannerView.OnBannerAdLoadFailed -= OnBannerLoadFailed;
                    _bannerView.OnBannerAdLoaded -= OnBannerLoaded;

                    _bannerView.Destroy();
                    _bannerView = null;
                    CancelReloadAds();
                }
                catch (Exception ex)
                {
                    AdsManager.Instance.LogException(ex);
                }
            }
#endif
        }

        private void DelayReloadAd(float time)
        {
            Invoke(nameof(LoadAds), time);
        }

        protected void CancelReloadAds()
        {
            CancelInvoke(nameof(LoadAds));
        }


        #endregion
    }
}

#endif