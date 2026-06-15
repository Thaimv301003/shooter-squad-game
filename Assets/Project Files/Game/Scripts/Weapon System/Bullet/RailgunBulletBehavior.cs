using UnityEngine;

namespace Watermelon.SquadShooter
{
    public class RailgunBulletBehavior : PlayerBulletBehavior
    {
        private static readonly int PARTICLE_HIT_HASH = "Tesla Hit".GetHashCode();
        private static readonly int PARTICLE_WALL_HIT_HASH = "Minigun Wall Hit".GetHashCode();

        [SerializeField] TrailRenderer trailRenderer;

        public override void Init(float damage, float speed, BaseEnemyBehavior currentTarget, float autoDisableTime, bool autoDisableOnHit = false)
        {
            // For Railgun, autoDisableOnHit is false by default to allow piercing
            base.Init(damage, speed, currentTarget, autoDisableTime, autoDisableOnHit);

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }

        protected override void OnEnemyHitted(BaseEnemyBehavior baseEnemyBehavior)
        {
            ParticlesController.PlayParticle(PARTICLE_HIT_HASH).SetPosition(transform.position);

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }

        protected override void OnObstacleHitted()
        {
            base.OnObstacleHitted();

            ParticlesController.PlayParticle(PARTICLE_WALL_HIT_HASH).SetPosition(transform.position);
            
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }
    }
}
