using UnityEngine;
using Watermelon.SquadShooter;

namespace Watermelon.Enemy.BossMelee
{


    public class BossMeleeFollowState : StateBehavior<BossMeleeBehavior>
    {
        public BossMeleeFollowState(BossMeleeBehavior enemy) : base(enemy) { }

        protected readonly int ANIMATOR_SPEED_HASH = Animator.StringToHash("Movement Speed");
        private Vector3 cachedTargetPos;

        public override void OnStart()
        {
            Target.IsWalking = false;
            Target.NavMeshAgent.speed = Target.Stats.MoveSpeed;
            cachedTargetPos = Target.Target.position;
            Target.MoveToPoint(cachedTargetPos);
        }

        public override void OnUpdate()
        {
            if (Vector3.Distance(Target.Target.position, cachedTargetPos) > 0.1f)
            {
                cachedTargetPos = Target.Target.position;
                Target.MoveToPoint(cachedTargetPos);
            }

            Target.Animator.SetFloat(ANIMATOR_SPEED_HASH, Target.NavMeshAgent.velocity.magnitude / Target.NavMeshAgent.speed);
        }

        public override void OnEnd()
        {
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
            // The StartCharge transition is handled by the State Machine checking this timer
        }

        public bool IsAimingFinished()
        {
            return aimTimer >= Target.AimDuration;
        }

        public override void OnEnd()
        {
            // End is handled when switching to Charge
        }
    }

    public class BossMeleeChargeState : StateBehavior<BossMeleeBehavior>
    {
        public BossMeleeChargeState(BossMeleeBehavior enemy) : base(enemy) { }

        private bool isChargeFinished = false;

        public override void OnStart()
        {
            isChargeFinished = false;
            Target.StartCharge();
            Target.OnAttackFinished += OnChargeFinished;
        }

        public override void OnUpdate()
        {
            // Update logic is handled inside BossMeleeBehavior Coroutine
        }

        private void OnChargeFinished()
        {
            isChargeFinished = true;
        }

        public bool IsChargeFinished()
        {
            return isChargeFinished;
        }

        public override void OnEnd()
        {
            Target.OnAttackFinished -= OnChargeFinished;
        }
    }

    public enum BossMeleeState
    {
        Patrolling,
        Following,
        AimingCharge,
        Charging
    }
}
