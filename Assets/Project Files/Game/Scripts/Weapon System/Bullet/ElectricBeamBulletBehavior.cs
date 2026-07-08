using UnityEngine;

namespace Watermelon.SquadShooter
{
    [RequireComponent(typeof(LineRenderer))]
    public class ElectricBeamBulletBehavior : PlayerBulletBehavior
    {
        private static readonly int PARTICLE_HIT_HASH = "Tesla Hit".GetHashCode();
        private static readonly int PARTICLE_WALL_HIT_HASH = "Minigun Wall Hit".GetHashCode();

        [SerializeField] LineRenderer lineRenderer;
        [SerializeField] float beamWidth = 0.3f;
        [SerializeField] float maxBeamLength = 40f;
        [SerializeField] float beamDuration = 0.3f; // Duration in seconds the beam stays visible
        [SerializeField] LayerMask collisionLayers; // Target layers (Enemies and Obstacles)

        private float fadeDuration;
        private float fadeTimer;
        private Color initialStartColor;
        private Color initialEndColor;

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null)
            {
                initialStartColor = lineRenderer.startColor;
                initialEndColor = lineRenderer.endColor;
            }
        }

        public override void Init(float damage, float speed, BaseEnemyBehavior currentTarget, float autoDisableTime, bool autoDisableOnHit = false)
        {
            // Set speed to 0 because the beam is instant. Use custom beamDuration instead of autoDisableTime.
            base.Init(damage, 0f, currentTarget, beamDuration, false);

            fadeDuration = beamDuration;
            fadeTimer = beamDuration;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.startWidth = beamWidth;
                lineRenderer.endWidth = beamWidth;
                lineRenderer.startColor = initialStartColor;
                lineRenderer.endColor = initialEndColor;
                lineRenderer.SetPosition(0, transform.position);
            }

            FireBeam();
        }

        private void FireBeam()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            float actualLength = maxBeamLength;

            // 1. Raycast to find if we hit a wall/obstacle to block the beam
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxBeamLength, collisionLayers);
            
            // Find the closest obstacle to block the beam
            float closestObstacleDistance = maxBeamLength;
            bool hitObstacle = false;
            Vector3 obstacleHitPoint = origin + direction * maxBeamLength;

            foreach (var hit in hits)
            {
                // If it's not on the enemy layer, it's an obstacle/wall
                if (hit.collider.gameObject.layer != PhysicsHelper.LAYER_ENEMY)
                {
                    if (hit.distance < closestObstacleDistance)
                    {
                        closestObstacleDistance = hit.distance;
                        obstacleHitPoint = hit.point;
                        hitObstacle = true;
                    }
                }
            }

            actualLength = closestObstacleDistance;
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(1, obstacleHitPoint);
            }

            if (hitObstacle)
            {
                ParticlesController.PlayParticle(PARTICLE_WALL_HIT_HASH).SetPosition(obstacleHitPoint);
            }

            // 2. Perform SphereCastAll to find all enemies along the beam's width
            RaycastHit[] enemyHits = Physics.SphereCastAll(origin, beamWidth * 0.5f, direction, actualLength, collisionLayers);

            foreach (var hit in enemyHits)
            {
                if (hit.collider.gameObject.layer == PhysicsHelper.LAYER_ENEMY)
                {
                    BaseEnemyBehavior enemy = hit.collider.GetComponent<BaseEnemyBehavior>();
                    if (enemy == null) enemy = hit.collider.GetComponentInParent<BaseEnemyBehavior>();

                    if (enemy != null && !enemy.IsDead)
                    {
                        // Deal damage to the enemy
                        enemy.TakeDamage(damage, hit.point, direction);
                        
                        // Play hit particle at the enemy position
                        ParticlesController.PlayParticle(PARTICLE_HIT_HASH).SetPosition(hit.point);
                    }
                }
            }
        }

        private void Update()
        {
            if (fadeTimer > 0)
            {
                fadeTimer -= Time.deltaTime;
                float alpha = fadeTimer / fadeDuration;

                if (lineRenderer != null)
                {
                    Color startCol = initialStartColor;
                    Color endCol = initialEndColor;
                    startCol.a *= alpha;
                    endCol.a *= alpha;
                    lineRenderer.startColor = startCol;
                    lineRenderer.endColor = endCol;
                    lineRenderer.startWidth = beamWidth * alpha;
                    lineRenderer.endWidth = beamWidth * alpha;
                }
            }
        }

        protected override void OnEnemyHitted(BaseEnemyBehavior baseEnemyBehavior)
        {
            // Unused as hit registration is instant in Init()
        }

        protected override void OnObstacleHitted()
        {
            // Unused as hit registration is instant in Init()
        }
    }
}
