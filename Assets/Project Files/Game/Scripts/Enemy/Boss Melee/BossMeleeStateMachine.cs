using System.Collections.Generic;
using UnityEngine;
using Watermelon.SquadShooter;

namespace Watermelon.Enemy.BossMelee
{
    [RequireComponent(typeof(BossMeleeBehavior))]
    public class BossMeleeStateMachine : AbstractStateMachine<BossMeleeState>
    {
        private BossMeleeBehavior enemy;

        private void Awake()
        {
            enemy = GetComponent<BossMeleeBehavior>();

            // 1. Patrolling State
            var patrollingStateCase = new StateCase();
            patrollingStateCase.state = new Watermelon.Enemy.PatrollingState(enemy);
            patrollingStateCase.transitions = new List<StateTransition<BossMeleeState>> {
                new StateTransition<BossMeleeState>(PatrollingStateTransition)
            };

            // 2. Following & Attacking State (Chasing target and using Normal Attack)
            var followingAttackStateCase = new StateCase();
            followingAttackStateCase.state = new BossMeleeFollowAttackState(enemy);
            followingAttackStateCase.transitions = new List<StateTransition<BossMeleeState>>
            {
                new StateTransition<BossMeleeState>(FollowingAttackStateTransition)
            };

            // 3. Aiming Smash State (Ultimate charge up)
            var aimingStateCase = new StateCase();
            aimingStateCase.state = new BossMeleeAimingState(enemy);
            aimingStateCase.transitions = new List<StateTransition<BossMeleeState>>
            {
                new StateTransition<BossMeleeState>(AimingStateTransition)
            };

            // 4. Smashing State (Ultimate execution)
            var smashingStateCase = new StateCase();
            smashingStateCase.state = new BossMeleeSmashState(enemy);
            smashingStateCase.transitions = new List<StateTransition<BossMeleeState>>
            {
                new StateTransition<BossMeleeState>(SmashingStateTransition)
            };

            states.Add(BossMeleeState.Patrolling, patrollingStateCase);
            states.Add(BossMeleeState.FollowingAttack, followingAttackStateCase);
            states.Add(BossMeleeState.AimingSmash, aimingStateCase);
            states.Add(BossMeleeState.Smashing, smashingStateCase);
        }

        private bool PatrollingStateTransition(out BossMeleeState nextState)
        {
            bool isTargetSpotted = enemy.IsTargetInVisionRange || enemy.HasTakenDamage;

            if (!isTargetSpotted)
            {
                nextState = BossMeleeState.Patrolling;
                return false;
            }

            if (enemy.CanSmash())
            {
                nextState = BossMeleeState.AimingSmash;
            }
            else
            {
                nextState = BossMeleeState.FollowingAttack;
            }
            return true;
        }

        private bool FollowingAttackStateTransition(out BossMeleeState nextState)
        {
            if (enemy.CanSmash())
            {
                nextState = BossMeleeState.AimingSmash;
                return true;
            }

            nextState = BossMeleeState.FollowingAttack;
            return false;
        }

        private bool AimingStateTransition(out BossMeleeState nextState)
        {
            var aimingState = (BossMeleeAimingState)states[BossMeleeState.AimingSmash].state;

            if (aimingState.IsAimingFinished())
            {
                nextState = BossMeleeState.Smashing;
                return true;
            }

            nextState = BossMeleeState.AimingSmash;
            return false;
        }

        private bool SmashingStateTransition(out BossMeleeState nextState)
        {
            var smashingState = (BossMeleeSmashState)states[BossMeleeState.Smashing].state;

            if (smashingState.IsSmashFinished())
            {
                nextState = BossMeleeState.FollowingAttack;
                return true;
            }

            nextState = BossMeleeState.Smashing;
            return false;
        }
    }
}
