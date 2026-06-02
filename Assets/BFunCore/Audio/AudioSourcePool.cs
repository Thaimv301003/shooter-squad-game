using UnityEngine;
namespace BFunCoreKit
{
    public class AudioSourcePool : PoolBase<AudioSource>
    {
        protected override AudioSource CreateFunc()
        {
            var obj = base.CreateFunc();
            obj.playOnAwake = false;
            return obj;
        }

        protected override void OnReleaseToPool(AudioSource source)
        {
            base.OnReleaseToPool(source);
            source.Stop();
            source.clip = null;
        }
    }
}