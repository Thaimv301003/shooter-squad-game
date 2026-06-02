#if USE_ADMOB

using System;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// Builder for reverse chaining: ShowNativePlatform(...).WithCountdown(...).WithAutoReload(...).Execute()
    /// Must call Execute() to trigger actual ad display with applied configurations
    /// </summary>
    public class NativePlatformShowBuilder
    {
        private readonly AdmobNativePlatformController _controller;
        private readonly string _position;
        private readonly string _layoutName;
        private readonly Action _onShow;
        private readonly Action _onClose;
        private readonly Action _onAdDismissedFullScreenContent;
        private bool _hasExecuted = false;
        
        // Configuration storage for explicit execution
        private CountdownConfig _countdownConfig;
        private AutoReloadConfig _autoReloadConfig;
        private ShowOnLoadedConfig _showOnLoadedConfig;
        private PositionConfig _positionConfig;
        internal NativePlatformShowBuilder(AdmobNativePlatformController controller, string position, string layoutName, Action onShow, Action onClose, Action OnAdDismissedFullScreenContent)
        {
            _controller = controller;
            _position = position;
            _layoutName = layoutName;
            _onShow = onShow;
            _onClose = onClose;
            _onAdDismissedFullScreenContent = OnAdDismissedFullScreenContent;
            // Store parameters, wait for explicit Execute() call
        }

        /// <summary>
        /// Configure countdown behavior - store for explicit execution
        /// </summary>
        /// <param name="initialDelaySeconds">Initial delay before countdown starts</param>
        /// <param name="countdownDurationSeconds">Duration of countdown timer</param>
        /// <param name="closeButtonDelaySeconds">Delay before close button becomes clickable</param>
        public NativePlatformShowBuilder WithCountdown(float initialDelaySeconds, float countdownDurationSeconds, float closeButtonDelaySeconds)
        {
            if (initialDelaySeconds < 0 || countdownDurationSeconds <= 0 || closeButtonDelaySeconds < 0)
            {
                Debug.LogWarning("[NativePlatformShowBuilder] Invalid countdown timings. Configuration ignored.");
                return this;
            }

            Debug.Log($"[NativePlatformShowBuilder] Storing countdown config: {initialDelaySeconds}s initial, {countdownDurationSeconds}s countdown, {closeButtonDelaySeconds}s close delay");
            
            // Store configuration for explicit execution
            _countdownConfig = new CountdownConfig
            {
                InitialDelaySeconds = initialDelaySeconds,
                CountdownDurationSeconds = countdownDurationSeconds,
                CloseButtonDelaySeconds = closeButtonDelaySeconds
            };
            
            return this;
        }

        /// <summary>
        /// Configure auto-reload behavior - store for explicit execution
        /// Uses controller's own adUnitId automatically
        /// </summary>
        /// <param name="intervalSeconds">Reload interval in seconds</param>
        public NativePlatformShowBuilder WithAutoReload(float intervalSeconds)
        {
            if (intervalSeconds <= 0)
            {
                Debug.LogWarning("[NativePlatformShowBuilder] Invalid auto-reload config. Interval must be > 0.");
                return this;
            }

            Debug.Log($"[NativePlatformShowBuilder] Storing auto-reload config after {intervalSeconds}s");
            
            // Store configuration for explicit execution
            _autoReloadConfig = new AutoReloadConfig
            {
                IntervalSeconds = intervalSeconds
            };
            
            return this;
        }

        /// <summary>
        /// Configure show-on-loaded behavior - store for explicit execution
        /// </summary>
        /// <param name="enabled">Whether to automatically show ad when loaded</param>
        public NativePlatformShowBuilder WithShowOnLoaded(bool enabled)
        {
            Debug.Log($"[NativePlatformShowBuilder] Storing show-on-loaded config: {enabled}");
            
            // Store configuration for explicit execution
            _showOnLoadedConfig = new ShowOnLoadedConfig
            {
                Enabled = enabled
            };
            
            return this;
        }

        public NativePlatformShowBuilder WithPosition(AdsPos adsPos, Vector2Int offset)
        {
            _positionConfig = new PositionConfig
            {
                AdsPos = adsPos,
                Offset = offset
            };

            return this;
        }


        /// <summary>
        /// Execute all configurations: store configs for persistence, then show()
        /// </summary>
        private void ExecuteWithConfigurations()
        {
            if (_hasExecuted) return;
            
            Debug.Log($"[NativePlatformShowBuilder] Executing with configurations for position: {_position}, layout: {_layoutName}");
            
            _controller.StoreConfigs(_countdownConfig, _autoReloadConfig, _showOnLoadedConfig, _positionConfig);
            
            Debug.Log($"[NativePlatformShowBuilder] Executing show with stored configurations");
            _controller.ShowAds(_position, _layoutName, _onShow, _onClose, _onAdDismissedFullScreenContent);
            
            _hasExecuted = true;
        }

        /// <summary>
        /// Execute all stored configurations and show the ad
        /// Must be called to trigger actual ad display
        /// </summary>
        public void Execute()
        {
            ExecuteWithConfigurations();
        }

        #region Configuration Data Classes

        public class CountdownConfig
        {
            public float InitialDelaySeconds { get; set; }
            public float CountdownDurationSeconds { get; set; }
            public float CloseButtonDelaySeconds { get; set; }
            
            public CountdownConfig Clone() => new CountdownConfig
            {
                InitialDelaySeconds = this.InitialDelaySeconds,
                CountdownDurationSeconds = this.CountdownDurationSeconds,
                CloseButtonDelaySeconds = this.CloseButtonDelaySeconds
            };
            
            public override string ToString() => $"{InitialDelaySeconds}s,{CountdownDurationSeconds}s,{CloseButtonDelaySeconds}s";
        }

        public class AutoReloadConfig
        {
            public float IntervalSeconds { get; set; }
            
            public AutoReloadConfig Clone() => new AutoReloadConfig
            {
                IntervalSeconds = this.IntervalSeconds
            };
            
            public override string ToString() => $"{IntervalSeconds}s";
        }

        public class ShowOnLoadedConfig
        {
            public bool Enabled { get; set; }
            
            public ShowOnLoadedConfig Clone() => new ShowOnLoadedConfig
            {
                Enabled = this.Enabled
            };
            
            public override string ToString() => $"{Enabled}";
        }

        public class PositionConfig
        {
            public AdsPos AdsPos { get; set; }
            public Vector2Int Offset { get; set; }

            public PositionConfig Clone() => new PositionConfig
            {
                AdsPos = this.AdsPos,
                Offset = this.Offset
            };
            
            public override string ToString() => $"{Offset}";
        }

        #endregion
    }
}

#endif