using UnityEngine;

namespace Watermelon.SquadShooter
{
    public class RocketBulletBehavior : PlayerBulletBehavior
    {
        [Header("Explosion Settings")]
        [SerializeField] float explosionRadius = 3.5f;
        [SerializeField] string explosionParticleName = "Bomber Explosion";
        [SerializeField] string explosionDecalName = "Bomber Explosion Decal";

        [Header("Effects")]
        [SerializeField] ParticleSystem trailParticleSystem;

        private int explosionParticleHash;
        private int explosionDecalHash;

        private void Awake()
        {
            explosionParticleHash = explosionParticleName.GetHashCode();
            explosionDecalHash = explosionDecalName.GetHashCode();
        }

        public override void Init(float damage, float speed, BaseEnemyBehavior currentTarget, float autoDisableTime, bool autoDisableOnHit = true)
        {
            // Rocket disables on first hit, triggering explosion
            base.Init(damage, speed, currentTarget, autoDisableTime, autoDisableOnHit);

            if (trailParticleSystem != null)
            {
                trailParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                trailParticleSystem.Play();
            }
        }

        protected override void OnEnemyHitted(BaseEnemyBehavior baseEnemyBehavior)
        {
            Explode();
        }

        protected override void OnObstacleHitted()
        {
            Explode();
        }

        private void Explode()
        {
            // 1. Play particles
            ParticlesController.PlayParticle(explosionParticleHash).SetPosition(transform.position);
            ParticlesController.PlayParticle(explosionDecalHash).SetPosition(transform.position)
                .SetScale(Vector3.one * (explosionRadius * 0.8f))
                .SetRotation(Quaternion.Euler(-90f, 0f, 0f));

            // 2. Play sound
            AudioController.PlaySound(AudioController.AudioClips.explode);

            // 3. Shake camera
            VirtualCamera gameCameraCase = CameraController.GetCamera(CameraType.Gameplay);
            if (gameCameraCase != null)
            {
                gameCameraCase.Shake(0.08f, 0.08f, 0.4f, 1.0f);
            }

            // 4. AoE damage calculation
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            Vector3 travelDir = transform.forward;

            for (int i = 0; i < hitColliders.Length; i++)
            {
                if (hitColliders[i].gameObject.layer == PhysicsHelper.LAYER_ENEMY)
                {
                    BaseEnemyBehavior enemy = hitColliders[i].GetComponent<BaseEnemyBehavior>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        // Calculate damage multiplier based on distance from the explosion center
                        float distance = Vector3.Distance(transform.position, hitColliders[i].transform.position);
                        float damageMultiplier = 1.0f - Mathf.InverseLerp(0f, explosionRadius, distance);
                        
                        // Apply damage (at least 30% of base damage at the edge of explosion)
                        float finalDamage = damage * Mathf.Clamp(damageMultiplier, 0.3f, 1.0f);
                        
                        Vector3 pushDirection = (hitColliders[i].transform.position - transform.position).normalized;
                        enemy.TakeDamage(finalDamage, hitColliders[i].transform.position, pushDirection);
                    }
                }
            }

            // 5. Clean up
            if (trailParticleSystem != null)
            {
                trailParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            gameObject.SetActive(false);
        }
    }
}
