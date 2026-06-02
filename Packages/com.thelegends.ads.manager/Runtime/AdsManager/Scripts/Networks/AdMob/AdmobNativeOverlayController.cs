#if USE_ADMOB
using System;
using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public class AdmobNativeOverlayController : AdsPlacementBase
    {
        private NativeOverlayAd _nativeOverlayAd;

        private Action OnClose;

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
            return AdsType.NativeOverlay;
#else
            return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_ADMOB
            return _nativeOverlayAd != null;
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

            if (!IsReady)
            {
                NativeDestroy();

                base.LoadAds();

                AdRequest request = new AdRequest();

                var options = new NativeAdOptions
                {
                    AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
                    MediaAspectRatio = MediaAspectRatio.Any,
                    VideoOptions = new VideoOptions()
                    {
                        ClickToExpandRequested = true,
                        CustomControlsRequested = true,
                        StartMuted = true
                    }
                };


                NativeOverlayAd.Load(adsUnitID.Trim(), request, options,
                    (NativeOverlayAd ad, LoadAdError error) =>
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
                            OnNativeOverlayLoadFailed(error);
                            return;
                        }

                        if (ad == null)
                        {
                            AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "Unexpected error: load event fired with null ad and null error.");
                            OnNativeOverlayLoadFailed(error);
                            return;
                        }

                        networkName = ad.GetResponseInfo().GetMediationAdapterClassName();

                        AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + "ad loaded with response : " + ad.GetResponseInfo());

                        _nativeOverlayAd = ad;

                        OnAdsLoadAvailable();

                    });
            }

#if UNITY_EDITOR
            OnAdsLoadAvailable();
#endif

#endif

        }

        public void ShowAds(NativeTemplateStyle style, AdsPos position, Vector2Int size, Vector2Int offset, string showPosition, Action OnShow = null, Action OnClose = null)
        {
#if USE_ADMOB
            if (Status == AdsEvents.ShowSuccess)
            {
                AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "is showing --> return");
                return;
            }

            this.OnClose = OnClose;
            base.ShowAds(showPosition);



#if UNITY_EDITOR
            if (IsAvailable)
            {
                OnAdsShowSuccess();
                OnShow?.Invoke();
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }

#else
            if (IsReady && IsAvailable)
            {
                RenderAd(style, position, size, offset);
                _nativeOverlayAd.OnAdClicked += OnNativeOverlayClick;
                _nativeOverlayAd.OnAdPaid += OnNativeOverlayPaid;
                _nativeOverlayAd.OnAdImpressionRecorded += OnNativeOverlayImpression;
                _nativeOverlayAd.OnAdFullScreenContentClosed += OnNativeOverlayClosed;
                _nativeOverlayAd.OnAdFullScreenContentOpened += OnNativeOverlayShowSuccess;
                _nativeOverlayAd.Show();
                OnShow?.Invoke();
                OnAdsShowSuccess();
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }
#endif
#endif
        }

        private void RenderAd(NativeTemplateStyle style, AdsPos position, Vector2Int size, Vector2Int offset)
        {
#if USE_ADMOB
            if (_nativeOverlayAd != null)
            {
                Debug.Log("Rendering Native Overlay ad.");

                // Define a native template style with a custom style.

                var deviceScale = MobileAds.Utils.GetDeviceScale();

                Debug.Log("AAAAAAAAAAAA deviceScale: " + deviceScale);

                //Đéo hiểu sao phải nhân deviceScale mới ra được dp đúng như banner. NativeOverlay lol
                var adSizeDp = new AdSize(Mathf.RoundToInt(size.x * deviceScale), Mathf.RoundToInt(size.y * deviceScale));

                //Còn đây là công thức chuẩn tính từ pixel sang dp nhưng lại lệch kích thước vl
                // var adSizeDp = new AdSize((int)(size.x / deviceScale), (int)(size.y / deviceScale));

                // var adSizeDp = new AdSize(size.x, size.y);

                var safeAreaWidth = Mathf.RoundToInt(Screen.safeArea.width * deviceScale);
                var safeAreaHeight = Mathf.RoundToInt(Screen.safeArea.height * deviceScale);

                if (adSizeDp.Width > safeAreaWidth)
                {
                    adSizeDp = new AdSize(safeAreaWidth, adSizeDp.Height);
                }
                if (adSizeDp.Height > safeAreaHeight)
                {
                    adSizeDp = new AdSize(adSizeDp.Width, safeAreaHeight);
                }

                _nativeOverlayAd.RenderTemplate(style, new AdSize(adSizeDp.Width, adSizeDp.Height), AdPosition.Center);

                SetAdCustomPosition(position, new Vector2Int(adSizeDp.Width, adSizeDp.Height), offset);
            }
#endif

        }

        public void SetAdCustomPosition(AdsPos position, Vector2Int size, Vector2Int offset)
        {

            if (!IsAdsReady())
            {
                return;
            }

            var deviceScale = MobileAds.Utils.GetDeviceScale();

            float adWidth = size.x / deviceScale;
            float adHeight = size.y / deviceScale;

            var safeAreaWidth = Screen.safeArea.width / deviceScale;
            var safeAreaHeight = Screen.safeArea.height / deviceScale;

            int xMax = Mathf.RoundToInt(safeAreaWidth - adWidth);
            int yMax = Mathf.RoundToInt(safeAreaHeight - adHeight);
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


            _nativeOverlayAd.SetTemplatePosition(newPos.x, newPos.y);
        }

        #region Internal

        private void OnNativeOverlayClick()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClick();
            });
        }

        private void OnNativeOverlayImpression()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnImpression();
            });
        }

        private void OnNativeOverlayShowSuccess()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsShowSuccess();
            });
        }

        private void OnNativeOverlayLoadFailed(AdError error)
        {
            var errorDescription = error?.GetMessage();
            OnAdsLoadFailed(errorDescription);
        }

        private void OnNativeOverlayClosed()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                HideAds();
            });

        }

        private void OnNativeOverlayPaid(AdValue value)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, value);
            });
        }

        public void HideAds()
        {
#if USE_ADMOB
            if (Status != AdsEvents.ShowSuccess && Status != AdsEvents.Click)
            {
                AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + " is not showing --> return");
                return;
            }

            if (_nativeOverlayAd != null)
            {
                _nativeOverlayAd.Hide();
                OnClose?.Invoke();
                OnClose = null;
                NativeDestroy();
                OnAdsClosed();
            }
#if UNITY_EDITOR
            OnClose?.Invoke();
            OnClose = null;
            NativeDestroy();
            OnAdsClosed();
#endif
#endif
        }

        public void NativeDestroy()
        {
#if USE_ADMOB
            if (_nativeOverlayAd != null)
            {
                try
                {
                    _nativeOverlayAd.OnAdClicked -= OnNativeOverlayClick;
                    _nativeOverlayAd.OnAdPaid -= OnNativeOverlayPaid;
                    _nativeOverlayAd.OnAdImpressionRecorded -= OnNativeOverlayImpression;
                    _nativeOverlayAd.OnAdFullScreenContentClosed -= OnNativeOverlayClosed;
                    _nativeOverlayAd.OnAdFullScreenContentOpened -= OnNativeOverlayShowSuccess;

                    _nativeOverlayAd.Destroy();
                    _nativeOverlayAd = null;
                }
                catch (Exception ex)
                {
                    AdsManager.Instance.LogException(ex);
                }
            }
#endif
        }

        #endregion
    }

}

#endif
