            #if BFUN_INSTALLED_TRUE
using UnityEngine;
#if BFUN_INSTALLED_TRUE
using TMPro;
#endif

namespace BFunCoreKit
{
    public class BFunDebugInfo : MonoBehaviour
    {
        [Header("UI Text Fields")]
                    #if BFUN_INSTALLED_TRUE
        public TMP_Text versionText;
        public TMP_Text timeText;
        #endif

        void Start()
        {
            versionText.text = "version " + GameManager.BFUN_VERSION;
            //UpdateDebugInfo();
        }

        private void UpdateDebugInfo()
        {
            var info = BuildInfo.Get();
            // Platform

            // Version
            if (versionText) versionText.text = $"Build Version : {info.version}";

            // Build Time
            if (timeText) timeText.text = $"Build Time : {info.buildTime}";

            // Battery
            int battery = Mathf.Abs(Mathf.RoundToInt(SystemInfo.batteryLevel * 100));
            string batteryStatus = SystemInfo.batteryStatus.ToString();

            // Connection
            string connection = Application.internetReachability switch
            {
                NetworkReachability.NotReachable => "Offline",
                NetworkReachability.ReachableViaCarrierDataNetwork => "Mobile Data",
                NetworkReachability.ReachableViaLocalAreaNetwork => "Wifi",
                _ => "Unknown"
            };
                        #if BFUN_INSTALLED_TRUE
            #endif
        }
    }
}
#endif
