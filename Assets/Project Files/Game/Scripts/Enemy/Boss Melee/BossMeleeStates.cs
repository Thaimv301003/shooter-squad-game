using UnityEngine;
using Watermelon.SquadShooter;

namespace Watermelon.Enemy.BossMelee
{

    public class BossMeleeFollowAttackState : StateBehavior<BossMeleeBehavior>
    {
        public BossMeleeFollowAttackState(BossMeleeBehavior enemy) : base(enemy) { }

        protected readonly int ANIMATOR_SPEED_HASH = Animator.StringToHash("Movement Speed");
        
        private Vector3 cachedTargetPos;
        private bool isSlowed = false;
        private bool isAttacking = false;

        public override void OnStart()
        {
            cachedTargetPos = Target.Target.position;

            isSlowed = Target.IsWalking;
            if (isSlowed)
            {
                Target.NavMeshAgent.speed = Target.Stats.PatrollingSpeed;
            }
            else
            {
                Target.NavMeshAgent.speed = Target.Stats.MoveSpeed;
            }

            Target.MoveToPoint(cachedTargetPos);
            isAttacking = false;
        }

        public override void OnUpdate()
        {
            if (Vector3.Distance(Target.Target.position, cachedTargetPos) > 0.1f)
            {
                cachedTargetPos = Target.Target.position;
                Target.MoveToPoint(cachedTargetPos);
            }

            if (isSlowed && !Target.IsWalking)
            {
                Target.NavMeshAgent.speed = Target.Stats.MoveSpeed;
            }
            else if (!isSlowed && Target.IsWalking)
            {
                Target.NavMeshAgent.speed = Target.Stats.PatrollingSpeed;
            }

            Target.Animator.SetFloat(ANIMATOR_SPEED_HASH, Target.NavMeshAgent.velocity.magnitude / Target.NavMeshAgent.speed * (isSlowed ? Target.Stats.PatrollingMutliplier : 1f));

            if (Target.IsTargetInAttackRange && !isAttacking && !CharacterBehaviour.IsDead)
            {
                isAttacking = true;
                Target.Attack();
                Target.OnAttackFinished += OnAttackFinished;
            }
        }

        private void OnAttackFinished()
        {
            Target.OnAttackFinished -= OnAttackFinished;
            isAttacking = false;
        }

        public override void OnEnd()
        {
            Target.OnAttackFinished -= OnAttackFinished;
            Target.StopMoving();
        }
    }

    public class BossMeleeAimingState : StateBehavior<BossMeleeBehavior>
    {
        public BossMeleeAimingState(BossMeleeBehavior enemy) : base(enemy) { }

        protected readonly int ANIMATOR_SPEED_HASH = Animator.StringToHash("Movement Speed");
        private float aimTimer = 0f;

        public override void OnStart()
        {
            Target.StartAiming();
            aimTimer = 0f;
            Target.Animator.SetFloat(ANIMATOR_SPEED_HASH, 0); // Stop moving animation
        }

        public override void OnUpdate()
        {
            aimTimer += Time.deltaTime;
            // The StartSmash transition is handled by the State Machine checking this timer
        }

        public bool IsAimingFinished()
        {
            return aimTimer >= Target.AimDuration;
        }

        public override void OnEnd()
        {
            // End is handled when switching to Smash
        }
    }

    public class BossMeleeSmashState : StateBehavior<BossMeleeBehavior>
    {
        public BossMeleeSmashState(BossMeleeBehavior enemy) : base(enemy) { }

        private bool isSmashFinished = false;

        public override void OnStart()
        {
            isSmashFinished = false;
            Target.StartSmash();
            Target.OnAttackFinished += OnSmashFinished;
        }

        public override void OnUpdate()
        {
            // Update logic is handled inside BossMeleeBehavior Coroutine
        }

        private void OnSmashFinished()
        {
            isSmashFinished = true;
        }

        public bool IsSmashFinished()
        {
            return isSmashFinished;
        }

        public override void OnEnd()
        {
            Target.OnAttackFinished -= OnSmashFinished;
        }
    }

    public enum BossMeleeState
    {
        Patrolling,
        FollowingAttack,
        AimingSmash,
        Smashing
    }
}
