using System.Collections;
using UnityEngine;
using Watermelon.LevelSystem;

namespace Watermelon.SquadShooter
{
    public class BossMeleeBehavior : BaseEnemyBehavior
    {
        private static readonly int HIT_PARTICLE_HASH = "Enemy Melee Hit".GetHashCode();
        private readonly int ANIMATOR_ATTACK_HASH = Animator.StringToHash("Attack");

        [Header("Boss Melee - Charge Settings")]
        [SerializeField] float chargeSpeed = 15f;
        [SerializeField] float chargeDistance = 10f;
        [SerializeField] float aimDuration = 1f;
        public float AimDuration => aimDuration;
        [SerializeField] float chargeCooldown = 3f;
        
        [Space]
        [SerializeField] float hitRadius = 1.5f;
        [SerializeField] Transform hitParticlePosition;
        [SerializeField] LineRenderer aimLine;
        [SerializeField] GameObject auraParticle;

        [Header("Knockup Settings")]
        [SerializeField] float knockupForce = 2f;
        [SerializeField] float knockupDuration = 0.5f;

        private bool isCharging = false;
        private bool isAiming = false;
        private float lastChargeTime = 0f;
        
        private Vector3 chargeTargetPos;
        private Vector3 chargeDirection;

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
            lastChargeTime = Time.time;
            
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
        }

        public bool CanCharge()
        {
            return Time.time - lastChargeTime >= chargeCooldown && !isCharging && !isAiming;
        }

        public void StartAiming()
        {
            isAiming = true;
            IsWalking = false;
            navMeshAgent.isStopped = true;

            chargeDirection = (Target.position - transform.position).normalized;
            chargeDirection.y = 0;
            
            // Calculate max distance to avoid going through walls
            float actualChargeDistance = chargeDistance;
            if (Physics.Raycast(transform.position + Vector3.up * 1f, chargeDirection, out RaycastHit hit, chargeDistance, LayerMask.GetMask("Obstacle")))
            {
                actualChargeDistance = hit.distance - 1f; // Stop slightly before the wall
            }

            chargeTargetPos = transform.position + chargeDirection * actualChargeDistance;

            if (aimLine != null)
            {
                aimLine.gameObject.SetActive(true);
                aimLine.SetPosition(0, transform.position + Vector3.up * 0.1f);
                aimLine.SetPosition(1, chargeTargetPos + Vector3.up * 0.1f);
            }

            // Smoothly rotate towards target while aiming
            transform.rotation = Quaternion.LookRotation(chargeDirection);
        }

        public void StartCharge()
        {
            isAiming = false;
            isCharging = true;
            
            if (aimLine != null)
                aimLine.gameObject.SetActive(false);

            navMeshAgent.enabled = false; // Disable NavMesh to allow free movement during charge
            
            animatorRef.SetTrigger(ANIMATOR_ATTACK_HASH);

            StartCoroutine(ChargeRoutine());
        }

        private IEnumerator ChargeRoutine()
        {
            float distance = Vector3.Distance(transform.position, chargeTargetPos);
            float duration = distance / chargeSpeed;
            float elapsed = 0f;

            Vector3 startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, chargeTargetPos, elapsed / duration);

                // Check for collision with player during charge
                if (Vector3.Distance(transform.position, target.position) <= hitRadius)
                {
                    HitPlayer();
                    break; // Stop charge if hit player
                }

                yield return null;
            }

            // End charge
            isCharging = false;
            lastChargeTime = Time.time;
            
            navMeshAgent.enabled = true;
            if (!isDead)
            {
                navMeshAgent.Warp(transform.position); // Snap NavMeshAgent to current pos
                navMeshAgent.isStopped = false;
            }
            
            InvokeOnAttackFinished();
        }

        private void HitPlayer()
        {
            characterBehaviour.TakeDamage(GetCurrentDamage());
            
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
                
                // Ease Out Quad equivalent
                float easeT = 1f - (1f - t) * (1f - t);
                
                // Parabola for jump
                float yOffset = 4f * height * t * (1f - t);
                
                targetTransform.position = Vector3.Lerp(startPos, endPos, easeT) + Vector3.up * yOffset;
                yield return null;
            }
            
            targetTransform.position = endPos;
        }

        public override void Attack()
        {
            // Attack is handled by the state machine
        }

        public override void OnAnimatorCallback(EnemyCallbackType enemyCallbackType)
        {
            
        }
        
        public override void TakeDamage(float damage, Vector3 projectilePosition, Vector3 projectileDirection)
        {
            if (isDead) return;

            // Immunity to crowd control / knockback during charge!
            if (isCharging || isAiming)
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
