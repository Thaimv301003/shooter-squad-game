namespace TheLegends.Base.Ads.Tracking
{
    public interface IImpressionTracker
    {
        void Initialize(AdsSettings settings);
        bool CanTrack(AdsType adsType);
        void Track(ImpressionData data);
    }
}
