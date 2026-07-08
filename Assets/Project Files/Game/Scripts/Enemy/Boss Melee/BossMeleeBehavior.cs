using System.Collections;
using UnityEngine;
using Watermelon.LevelSystem;

namespace Watermelon.SquadShooter
{
    public class BossMeleeBehavior : BaseEnemyBehavior
    {
        private static readonly int HIT_PARTICLE_HASH = "Enemy Melee Hit".GetHashCode();
        private readonly int ANIMATOR_ATTACK_HASH = Animator.StringToHash("Attack");
        private readonly int ANIMATOR_SMASH_HASH = Animator.StringToHash("Smash");

        [Header("Boss Melee - Normal Attack")]
        [SerializeField] float hitRadius = 1.5f;
        [SerializeField] DuoFloat slowDownDuration = new DuoFloat(1f, 1.5f);
        [SerializeField] float slowDownSpeedMult = 0.5f;
        [SerializeField] Transform hitParticlePosition;

        [Header("Boss Melee - Smash Settings (Ultimate)")]
        [SerializeField] float smashDelay = 0.4f;
        [SerializeField] float smashDistance = 10f;
        [SerializeField] float smashHitRadius = 2.0f;
        [SerializeField] float aimDuration = 1.2f;
        public float AimDuration => aimDuration;
        [SerializeField] float smashCooldown = 5f; // Tăng cooldown một chút vì giờ có thêm đánh thường
        
        [Space]
        [SerializeField] LineRenderer aimLine;
        [SerializeField] GameObject auraParticle;

        [Header("Knockup Settings (Ultimate)")]
        [SerializeField] float knockupForce = 2f;
        [SerializeField] float knockupDuration = 0.5f;

        // Flags for Ultimate
        private bool isSmashing = false;
        private bool isAiming = false;
        private float lastSmashTime = 0f;
        
        private Vector3 smashTargetPos;
        private Vector3 smashDirection;

        // Flags for Normal Attack
        private float slowRunningTimer;
        private bool isHitting;
        private bool isSlowRunning;

        protected override void Awake()
        {
            base.Awake();
            if (aimLine != null)
            {
                aimLine.gameObject.SetActive(false);
            }
        }

        public override void Init()
        {
            base.Init();
            if (auraParticle != null)
                auraParticle.SetActive(true);
            lastSmashTime = Time.time;
            
            // Ép Character nhận diện Boss này ngay khi vừa spawn!
            CharacterBehaviour.GetBehaviour().TryAddClosestEnemy(this);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isDead)
                return;

            if (!LevelController.IsGameplayActive)
                return;

            healthbarBehaviour.FollowUpdate();

            if (isSlowRunning)
            {
                slowRunningTimer -= Time.deltaTime;

                if (slowRunningTimer <= 0)
                    DisableSlowDown();
            }
        }

        #region Normal Attack Logic
        public override void Attack()
        {
            if (isHitting || isSmashing || isAiming)
                return;

            isHitting = true;
            ApplySlowDown();

            AudioController.PlaySound(AudioController.AudioClips.enemyMeleeHit, 0.5f);

            animatorRef.SetTrigger(ANIMATOR_ATTACK_HASH);
        }

        private void ApplySlowDown()
        {
            isSlowRunning = true;
            IsWalking = true;
            slowRunningTimer = slowDownDuration.Random();

            navMeshAgent.speed = Stats.MoveSpeed * slowDownSpeedMult;
        }

        private void DisableSlowDown()
        {
            isSlowRunning = false;
            IsWalking = false;

            navMeshAgent.speed = Stats.MoveSpeed;
        }

        public override void OnAnimatorCallback(EnemyCallbackType enemyCallbackType)
        {
            // Do not process normal attack hits if doing ultimate
            if (isSmashing || isAiming) return;

            if (enemyCallbackType == EnemyCallbackType.Hit)
            {
                if (Vector3.Distance(transform.position, target.position) <= hitRadius)
                {
                    characterBehaviour.TakeDamage(GetCurrentDamage());

                    if (hitParticlePosition != null)
                        ParticlesController.PlayParticle(HIT_PARTICLE_HASH).SetPosition(hitParticlePosition.position);
                }
            }
            else if (enemyCallbackType == EnemyCallbackType.HitFinish)
            {
                isHitting = false;
                InvokeOnAttackFinished();
            }
        }
        #endregion

        #region Ultimate Smash Logic
        public bool CanSmash()
        {
            return Time.time - lastSmashTime >= smashCooldown && !isSmashing && !isAiming && !isHitting;
        }

        public void StartAiming()
        {
            isAiming = true;
            IsWalking = false;
            navMeshAgent.isStopped = true;

            smashDirection = (Target.position - transform.position).normalized;
            smashDirection.y = 0;
            
            // Calculate max distance to avoid going through walls
            float actualSmashDistance = smashDistance;
            if (Physics.Raycast(transform.position + Vector3.up * 1f, smashDirection, out RaycastHit hit, smashDistance, LayerMask.GetMask("Obstacle")))
            {
                actualSmashDistance = hit.distance - 1f; // Stop slightly before the wall
            }

            smashTargetPos = transform.position + smashDirection * actualSmashDistance;

            if (aimLine != null)
            {
                aimLine.gameObject.SetActive(true);
                aimLine.SetPosition(0, transform.position + Vector3.up * 0.1f);
                aimLine.SetPosition(1, smashTargetPos + Vector3.up * 0.1f);
            }

            // Smoothly rotate towards target while aiming
            transform.rotation = Quaternion.LookRotation(smashDirection);
        }

        public void StartSmash()
        {
            isAiming = false;
            isSmashing = true;
            
            animatorRef.SetTrigger(ANIMATOR_SMASH_HASH);

            StartCoroutine(SmashRoutine());
        }

        private IEnumerator SmashRoutine()
        {
            // Wait for weapon to hit the ground (animation delay)
            yield return new WaitForSeconds(smashDelay);
            
            // Perform hit check
            if (aimLine != null)
                aimLine.gameObject.SetActive(false); // Hide line after smash
                
            CheckSmashHit();
            
            // Add a small delay for recovery before moving again
            yield return new WaitForSeconds(0.5f);

            // End smash
            isSmashing = false;
            lastSmashTime = Time.time;
            
            navMeshAgent.isStopped = false;
            
            InvokeOnAttackFinished();
        }
        
        private void CheckSmashHit()
        {
            Vector3 playerPos = characterBehaviour.transform.position;
            playerPos.y = 0;
            Vector3 startPos = transform.position;
            startPos.y = 0;
            Vector3 endPos = smashTargetPos;
            endPos.y = 0;
            
            float distanceToLine = DistancePointLine(playerPos, startPos, endPos);
            
            // Check if player is within the hit radius of the line
            if (distanceToLine <= smashHitRadius)
            {
                // Extra check to ensure player is not behind the boss
                Vector3 toPlayer = (playerPos - startPos).normalized;
                if (Vector3.Dot(smashDirection, toPlayer) > 0)
                {
                    HitPlayerKnockup();
                }
            }
            else
            {
                // Fallback for visual effects even if miss
                if (hitParticlePosition != null)
                    ParticlesController.PlayParticle(HIT_PARTICLE_HASH).SetPosition(hitParticlePosition.position);
                AudioController.PlaySound(AudioController.AudioClips.enemyMeleeHit, 0.8f);
            }
        }
        
        private float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();
            
            Vector3 pointVec = point - lineStart;
            float dotProduct = Vector3.Dot(pointVec, lineDirection);
            
            if (dotProduct <= 0) return Vector3.Distance(point, lineStart);
            if (dotProduct >= lineLength) return Vector3.Distance(point, lineEnd);
            
            Vector3 projection = lineStart + lineDirection * dotProduct;
            return Vector3.Distance(point, projection);
        }

        private void HitPlayerKnockup()
        {
            characterBehaviour.TakeDamage(GetCurrentDamage() * 1.5f); // Unti gây x1.5 sát thương
            
            if (hitParticlePosition != null)
                ParticlesController.PlayParticle(HIT_PARTICLE_HASH).SetPosition(hitParticlePosition.position);
                
            AudioController.PlaySound(AudioController.AudioClips.enemyMeleeHit, 0.8f);

            // Apply Knockup effect using Tween
            Vector3 pushDirection = (target.position - transform.position).normalized;
            pushDirection.y = 0;
            
            Vector3 knockupTarget = target.position + pushDirection * 2f;
            StartCoroutine(KnockupRoutine(target, knockupTarget, knockupForce, knockupDuration));
        }

        private IEnumerator KnockupRoutine(Transform targetTransform, Vector3 endPos, float height, float duration)
        {
            float elapsed = 0f;
            Vector3 startPos = targetTransform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float easeT = 1f - (1f - t) * (1f - t);
                float yOffset = 4f * height * t * (1f - t);
                
                targetTransform.position = Vector3.Lerp(startPos, endPos, easeT) + Vector3.up * yOffset;
                yield return null;
            }
            
            targetTransform.position = endPos;
        }
        #endregion

        public override void TakeDamage(float damage, Vector3 projectilePosition, Vector3 projectileDirection)
        {
            if (isDead) return;

            // Immunity to crowd control / knockback during smash!
            if (isSmashing || isAiming)
            {
                // Just take raw damage, no hit animation or knockback
                base.TakeDamage(damage, projectilePosition, Vector3.zero); // Pass zero direction to avoid push
            }
            else
            {
                base.TakeDamage(damage, projectilePosition, projectileDirection);
                
                if (hitAnimationTime < Time.time)
                    HitAnimation(Random.Range(0, 2));
            }
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            if (aimLine != null)
                aimLine.gameObject.SetActive(false);
            if (auraParticle != null)
                auraParticle.SetActive(false);
        }
    }
}
