#if USE_ADMOB && UNITY_IOS && !UNITY_EDITOR

using GoogleMobileAds.Common;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// iOS implementation của IAdErrorClient
    /// Simplified approach: Chỉ lưu error message (không có code)
    /// </summary>
    public class AdmobNativePlatformIOSAdErrorClient : ILoadAdErrorClient
    {
        private readonly string _message;

        public AdmobNativePlatformIOSAdErrorClient(string errorMessage)
        {
            _message = errorMessage ?? "Unknown error";
        }

        /// <summary>
        /// iOS không có error code riêng như Android
        /// Return 0 để indicate không có code
        /// </summary>
        public int GetCode()
        {
            return 0;
        }

        /// <summary>
        /// iOS không có error domain trong simplified approach
        /// Return empty string
        /// </summary>
        public string GetDomain()
        {
            return string.Empty;
        }

        /// <summary>
        /// Return error message từ native
        /// </summary>
        public string GetMessage()
        {
            return _message;
        }

        /// <summary>
        /// iOS không implement cause chain
        /// Return null như Android
        /// </summary>
        public IAdErrorClient GetCause()
        {
            return null;
        }

        /// <summary>
        /// iOS không có response info trong error
        /// Return null như Android
        /// </summary>
        public IResponseInfoClient GetResponseInfoClient()
        {
            return null;
        }

        public override string ToString()
        {
            return $"AdmobNativePlatformIOSAdErrorClient: {_message}";
        }
    }
}

#endif
