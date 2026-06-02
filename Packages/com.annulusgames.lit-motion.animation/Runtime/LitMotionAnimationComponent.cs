using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LitMotion.Animation
{
    [Serializable]
    public abstract class LitMotionAnimationComponent
    {
        public LitMotionAnimationComponent()
        {
#if UNITY_EDITOR
            var type = GetType();
            var attribute = type.GetCustomAttribute<LitMotionAnimationComponentMenuAttribute>();
            displayName = attribute != null
                ? attribute.MenuName.Split('/').Last()
                : type.Name;
#endif
        }

        [SerializeField] string displayName;
        [SerializeField] bool enabled = true;

        public bool Enabled => enabled;
        public string DisplayName => displayName;

        public abstract MotionHandle Play();

        public virtual void OnResume() { }
        public virtual void OnPause() { }
        public virtual void OnStop() { }

        public virtual void RecordState() { }
        public virtual void RestoreState() { }
        public virtual void ClearCache() { }
        public virtual bool OnInitialize() { return true; }

        [SerializeField] public UnityEngine.Events.UnityEvent onPlay;
        [SerializeField] public UnityEngine.Events.UnityEvent onComplete;

        public MotionHandle TrackedHandle { get; set; }
    }
}