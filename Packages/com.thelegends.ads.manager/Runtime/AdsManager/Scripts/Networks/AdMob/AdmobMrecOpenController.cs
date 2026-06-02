#if USE_ADMOB
namespace TheLegends.Base.Ads
{
    public class AdmobMrecOpenController : AdmobMrecController
    {
        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.MrecOpen;
#else
            return AdsType.None;
#endif
        }

        public override void HideAds()
        {
#if USE_ADMOB
            if (Status != AdsEvents.ShowSuccess && Status != AdsEvents.Click)
            {
                AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + " is not showing --> return");

                return;
            }

            if (_mrecView != null)
            {
                _mrecView.Hide();
                MRecDestroy();
                Status = AdsEvents.Close;
            }
#endif
        }

        protected override void SetTimeOut()
        {
#if USE_ADMOB
            timeOut = AdsManager.Instance.adsConfigs.adMrecOpenTimeOut;
#endif
        }

    }
}

#endif
