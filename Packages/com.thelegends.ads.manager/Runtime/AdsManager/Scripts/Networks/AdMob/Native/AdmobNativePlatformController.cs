#if USE_ADMOB

using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public class AdmobNativePlatformController : AdsPlacementBase
    {
        private AdmobNativePlatform _nativePlatformAd;
        protected string _layoutName;

        protected Action OnClose;
        protected Action OnShow;
        private Action OnAdDismissedFullScreenContent;

        // Unity-side config storage for persistence (only Countdown needs storage for native)
        protected NativePlatformShowBuilder.CountdownConfig _storedCountdown;

        // Unity AutoReload & ShowOnLoaded management (exactly like AdmobNativeController)
        protected float _autoReloadTime = 0f; // Like timeAutoReload in AdmobNativeController
        protected bool _isShowOnLoaded = false; // Like isShowOnLoaded in AdmobNativeController
        protected NativePlatformShowBuilder.PositionConfig _storedPosition;

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
            return AdsType.NativePlatform;
#else
            return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_ADMOB
            return _nativePlatformAd != null && _nativePlatformAd.IsAdAvailable();
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

            if (IsReady && Status == AdsEvents.LoadAvailable)
            {
                return;
            }

            // NativePlatformDestroy();

            base.LoadAds();

            // Unity flags already set in StoreConfigs (simple like AdmobNativeController)

            AdRequest request = new AdRequest();

            Debug.Log("LoadAds: " + adsUnitID);

            AdmobNativePlatform.Load(adsUnitID.Trim(), request, (native, error) =>
            {
                PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
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
                        OnNativePlatformLoadFailed(error);
                        return;
                    }

                    if (native == null)
                    {
                        AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "Unexpected error: load event fired with null ad and null error.");
                        OnNativePlatformLoadFailed(error);
                        return;
                    }

                    var responseInfo = native.GetResponseInfo();
                    networkName = responseInfo.GetMediationAdapterClassName();

                    AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + "ad loaded with response : " + responseInfo);

                    if (_nativePlatformAd != null)
                    {
                        NativePlatformDestroy();
                    }

                    _nativePlatformAd = native;

                    OnAdsLoadAvailable();

                    adsUnitIDIndex = 0;

                    if (_isShowOnLoaded)
                    {
                        ShowAds(position, _layoutName, OnShow, OnClose); // Use default layout like AdmobNativeController
                    }

                });
            });
#endif
        }


        public void ShowAds(string showPosition, string layoutName, Action OnShow = null, Action OnClose = null, Action OnAdDismissedFullScreenContent = null)
        {

#if USE_ADMOB
            position = showPosition;
            _layoutName = layoutName;

            if (Status == AdsEvents.ShowSuccess)
            {
                AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "is showing --> return");
                return;
            }

            this.OnClose = OnClose;
            this.OnShow = OnShow;
            this.OnAdDismissedFullScreenContent = OnAdDismissedFullScreenContent;
            base.ShowAds(showPosition);

            if (IsReady && IsAvailable)
            {
                RegisterAdEvents();

                if (_storedCountdown != null)
                {
                    _nativePlatformAd.WithCountdown(_storedCountdown.InitialDelaySeconds,
                                                  _storedCountdown.CountdownDurationSeconds,
                                                  _storedCountdown.CloseButtonDelaySeconds);
                }

                if (_storedPosition != null)
                {
                    SetAdCustomPosition(_storedPosition.AdsPos, _storedPosition.Offset);
                }

                _nativePlatformAd.Show(layoutName);

                CancelReloadAds();
                if (_autoReloadTime > 0)
                {
                    DelayReloadAd(_autoReloadTime);
                }
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }
#endif
        }

        public void HideAds()
        {
            OnShow = null;
            OnClose = null;
            OnAdDismissedFullScreenContent = null;

            ClearStoredConfigs();
            NativePlatformDestroy();
            OnNativePlatformClosed();
        }

        protected void DelayReloadAd(float time)
        {
            Invoke(nameof(LoadAds), time);
        }

        private void CancelReloadAds()
        {
            CancelInvoke(nameof(LoadAds));
        }

        #region Internal


        private void NativePlatformDestroy()
        {
#if USE_ADMOB
            try
            {
                if (_nativePlatformAd != null)
                {
                    CancelReloadAds();

                    UnregisterAdEvents();
                    _nativePlatformAd.Destroy();
                    _nativePlatformAd = null;
                }
            }
            catch (Exception ex)
            {
                AdsManager.Instance.LogException(ex);
            }
#endif
        }

        private void OnNativePlatformLoadFailed(LoadAdError error)
        {
#if USE_ADMOB
            var errorDescription = error?.GetMessage();
            OnAdsLoadFailed(errorDescription);
#endif
        }

        private void RegisterAdEvents()
        {
#if USE_ADMOB
            if (_nativePlatformAd == null) return;

            _nativePlatformAd.OnAdPaid += OnAdsPaid;
            _nativePlatformAd.OnAdClicked += OnNativePlatformClick;
            _nativePlatformAd.OnAdDidRecordImpression += OnNativePlatformImpression;
            _nativePlatformAd.OnVideoStart += OnVideoStart;
            _nativePlatformAd.OnVideoEnd += OnVideoEnd;
            _nativePlatformAd.OnVideoMute += OnVideoMute;
            _nativePlatformAd.OnVideoPlay += OnVideoPlay;
            _nativePlatformAd.OnVideoPause += OnVideoPause;
            _nativePlatformAd.OnAdClosed += OnNativePlatformClosed;
            _nativePlatformAd.OnAdShow += OnNativePlatformShow;
            _nativePlatformAd.OnAdShowedFullScreenContent += OnNativePlatformShowedFullScreenContent;
            _nativePlatformAd.OnAdDismissedFullScreenContent += OnNativePlatformDismissedFullScreenContent;
#endif
        }

        private void UnregisterAdEvents()
        {
#if USE_ADMOB
            if (_nativePlatformAd == null) return;

            _nativePlatformAd.OnAdPaid -= OnAdsPaid;
            _nativePlatformAd.OnAdClicked -= OnNativePlatformClick;
            _nativePlatformAd.OnAdDidRecordImpression -= OnNativePlatformImpression;
            _nativePlatformAd.OnVideoStart -= OnVideoStart;
            _nativePlatformAd.OnVideoEnd -= OnVideoEnd;
            _nativePlatformAd.OnVideoMute -= OnVideoMute;
            _nativePlatformAd.OnVideoPlay -= OnVideoPlay;
            _nativePlatformAd.OnVideoPause -= OnVideoPause;
            _nativePlatformAd.OnAdClosed -= OnNativePlatformClosed;
            _nativePlatformAd.OnAdShow -= OnNativePlatformShow;
            _nativePlatformAd.OnAdShowedFullScreenContent -= OnNativePlatformShowedFullScreenContent;
            _nativePlatformAd.OnAdDismissedFullScreenContent -= OnNativePlatformDismissedFullScreenContent;
#endif
        }

        private void OnAdsPaid(AdValue adValue)
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, adValue);
            });
#endif
        }

        private void OnNativePlatformClick()
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClick();
            });
#endif
        }

        private void OnNativePlatformImpression(object sender, EventArgs args)
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnImpression();
            });
#endif
        }

        private void OnVideoStart()
        {
#if USE_ADMOB
            AdsManager.Instance.Log($"{AdsMediation}_{AdsType} Video started");
#endif
        }

        private void OnVideoEnd()
        {
#if USE_ADMOB
            AdsManager.Instance.Log($"{AdsMediation}_{AdsType} Video ended");
#endif
        }

        private void OnVideoMute(object sender, bool isMuted)
        {
#if USE_ADMOB
            AdsManager.Instance.Log($"{AdsMediation}_{AdsType} Video mute state: {isMuted}");
#endif
        }

        private void OnVideoPlay()
        {
#if USE_ADMOB
            AdsManager.Instance.Log($"{AdsMediation}_{AdsType} Video playing");
#endif
        }

        private void OnVideoPause()
        {
#if USE_ADMOB
            AdsManager.Instance.Log($"{AdsMediation}_{AdsType} Video paused");
#endif
        }

        protected virtual void OnNativePlatformClosed()
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClosed();
                OnClose?.Invoke();
            });
#endif
        }

        protected virtual void OnNativePlatformShow()
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsShowSuccess();
                OnShow?.Invoke();
            });
#endif
        }

        private void OnNativePlatformShowedFullScreenContent()
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsShowSuccess();
            });
#endif
        }

        private void OnNativePlatformDismissedFullScreenContent()
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdDismissedFullScreenContent?.Invoke();
            });
#endif
        }

        #endregion

        #region Config Persistence Management


        /// <summary>
        /// Store configs from builder for persistence across LoadFails
        /// </summary>
        internal void StoreConfigs(NativePlatformShowBuilder.CountdownConfig countdown,
                                   NativePlatformShowBuilder.AutoReloadConfig autoReload,
                                   NativePlatformShowBuilder.ShowOnLoadedConfig showOnLoaded,
                                   NativePlatformShowBuilder.PositionConfig position)
        {
            _storedCountdown = countdown?.Clone();
            _autoReloadTime = autoReload?.IntervalSeconds ?? 0f;
            _isShowOnLoaded = showOnLoaded?.Enabled ?? false;
            _storedPosition = position?.Clone() ?? null;

            var configsInfo = new System.Collections.Generic.List<string>();
            if (_storedCountdown != null) configsInfo.Add($"Countdown({_storedCountdown})");
            if (_autoReloadTime > 0) configsInfo.Add($"AutoReload({_autoReloadTime}s)");
            if (_isShowOnLoaded) configsInfo.Add($"ShowOnLoaded({_isShowOnLoaded})");
            if (_storedPosition != null) configsInfo.Add($"Position({_storedPosition})");

            Debug.Log($"[{AdsMediation}_{AdsType}] Stored configs: [{string.Join(", ", configsInfo)}]");
        }


        /// <summary>
        /// Clear stored configs (called on Hide)
        /// </summary>
        private void ClearStoredConfigs()
        {
            _storedCountdown = null;
            _autoReloadTime = 0f;
            _isShowOnLoaded = false;
            _storedPosition = null;
        }

        #endregion

        private void SetAdCustomPosition(AdsPos position, Vector2Int offset)
        {
            if (!IsAdsReady())
            {
                return;
            }

            var deviceScale = MobileAds.Utils.GetDeviceScale();

            float adWidth = AdSize.MediumRectangle.Width;
            float adHeight = AdSize.MediumRectangle.Height;

            Debug.Log("AAAAA " + "adWidthNative: " + adWidth + " adHeightNative: " + adHeight);

            var safeAreaWidth = Screen.width / deviceScale;
            var safeAreaHeight = Screen.height / deviceScale;

            int xMax = (int)(safeAreaWidth - adWidth);
            int yMax = (int)(safeAreaHeight - adHeight);
            int xCenter = xMax / 2;
            int yCenter = yMax / 2;

            Vector2Int newPos = Vector2Int.zero;

            switch (position)
            {
                case AdsPos.Top:
                    newPos = new Vector2Int(xCenter + offset.x, offset.y);

                    break;
                case AdsPos.TopLeft:
                    newPos = new Vector2Int(offset.x, offset.y);

                    break;
                case AdsPos.TopRight:
                    newPos = new Vector2Int(xMax + offset.x, offset.y);

                    break;
                case AdsPos.Center:
                    newPos = new Vector2Int(xCenter + offset.x, yCenter + offset.y);

                    break;
                case AdsPos.CenterLeft:
                    newPos = new Vector2Int(offset.x, yCenter + offset.y);

                    break;
                case AdsPos.CenterRight:
                    newPos = new Vector2Int(xMax + offset.x, yCenter + offset.y);

                    break;
                case AdsPos.Bottom:
                    newPos = new Vector2Int(xCenter + offset.x, yMax + offset.y);

                    break;
                case AdsPos.BottomLeft:
                    newPos = new Vector2Int(offset.x, yMax + offset.y);

                    break;
                case AdsPos.BottomRight:
                    newPos = new Vector2Int(xMax + offset.x, yMax + offset.y);

                    break;
            }

            _nativePlatformAd.WithPosition(Mathf.RoundToInt(newPos.x * deviceScale), Mathf.RoundToInt(newPos.y * deviceScale));
        }

    }
}

#endif