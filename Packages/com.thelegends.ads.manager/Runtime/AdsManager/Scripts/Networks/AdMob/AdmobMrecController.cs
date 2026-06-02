#if USE_ADMOB
using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public class AdmobMrecController : AdsPlacementBase
    {
        protected BannerView _mrecView;
        private Vector2Int offset = new Vector2Int(0, 0);
        private AdsPos mrecPosition = AdsPos.None;

        private bool isShowing = false;
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
            return AdsType.Mrec;
#else
        return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_ADMOB
            return _mrecView != null;
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

            MRecDestroy();

            if (!IsReady)
            {
                CreateMRec();

                _mrecView.OnAdClicked += OnMRecClick;
                _mrecView.OnAdPaid += OnMRecPaid;
                _mrecView.OnAdImpressionRecorded += OnMRecImpression;
                _mrecView.OnBannerAdLoadFailed += OnMRecLoadFailed;
                _mrecView.OnBannerAdLoaded += OnMRecLoaded;

                base.LoadAds();
                AdRequest request = new AdRequest();

                _mrecView.LoadAd(request);
            }
#endif
        }

        public override void ShowAds(string showPosition)
        {
            base.ShowAds(showPosition);
#if USE_ADMOB
            if (IsReady && IsAvailable)
            {
                PreShow();
                _mrecView.Show();
                OnAdsShowSuccess();
                isShowing = true;
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }
#endif
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
                _mrecView.Hide();
                MRecDestroy();
                OnAdsClosed();
                isShowing = false;
            }
#endif
        }


        public void ShowAds(AdsPos position, Vector2Int offset, string showPosition)
        {
            this.offset = offset;
            this.mrecPosition = position;
            ShowAds(showPosition);
        }

        public void SetAdCustomPosition(AdsPos position, Vector2Int offset)
        {
            if (!IsAdsReady())
            {
                return;
            }

            var deviceScale = MobileAds.Utils.GetDeviceScale();

            float adWidth = _mrecView.GetWidthInPixels() / deviceScale;
            float adHeight = _mrecView.GetHeightInPixels() / deviceScale;

            Debug.Log("AAAAA " + "adWidthMrec: " + adWidth + " adHeightMrec: " + adHeight);

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


            _mrecView.SetPosition(newPos.x, newPos.y);
        }

        #region Internal

        private void PreShow()
        {
#if UNITY_EDITOR
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                SetAdCustomPosition(mrecPosition, offset);
            });
#else
            SetAdCustomPosition(mrecPosition, offset);
#endif

        }

        private void CreateMRec()
        {
#if USE_ADMOB
            _mrecView = new BannerView(adsUnitID.Trim(), AdSize.MediumRectangle, AdPosition.Center);
#endif
        }

        private void OnMRecClick()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClick();
            });
        }

        private void OnMRecPaid(AdValue value)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, value);
            });
        }

        private void OnMRecImpression()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnImpression();
            });
        }

        public void OnMRecLoaded()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                _mrecView.Hide();
                
                if (_loadRequestId != _currentLoadRequestId)
                {
                    // If the load request ID does not match, this callback is from a previous request
                    return;
                }

#if UNITY_EDITOR
                _mrecView.Hide();
#endif

                StopHandleTimeout();

                OnAdsLoadAvailable();

                networkName = _mrecView.GetResponseInfo().GetMediationAdapterClassName();

                AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + "ad loaded with response : " + _mrecView.GetResponseInfo());

                if (isShowing)
                {
                    ShowAds(mrecPosition, offset, position);
                }
            });

        }

        private void OnMRecLoadFailed(AdError error)
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

        protected void MRecDestroy()
        {
#if USE_ADMOB
            if (IsReady)
            {
                try
                {
                    _mrecView.OnAdClicked -= OnMRecClick;
                    _mrecView.OnAdPaid -= OnMRecPaid;
                    _mrecView.OnAdImpressionRecorded -= OnMRecImpression;
                    _mrecView.OnBannerAdLoadFailed -= OnMRecLoadFailed;
                    _mrecView.OnBannerAdLoaded -= OnMRecLoaded;

                    _mrecView.Destroy();
                    _mrecView = null;
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