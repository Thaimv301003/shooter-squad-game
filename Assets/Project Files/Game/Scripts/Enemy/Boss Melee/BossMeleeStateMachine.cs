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

            // 2. Following State (when target spotted but waiting for cooldown)
            var followingStateCase = new StateCase();
            followingStateCase.state = new BossMeleeFollowState(enemy);
            followingStateCase.transitions = new List<StateTransition<BossMeleeState>>
            {
                new StateTransition<BossMeleeState>(FollowingStateTransition)
            };

            // 3. Aiming State
            var aimingStateCase = new StateCase();
            aimingStateCase.state = new BossMeleeAimingState(enemy);
            aimingStateCase.transitions = new List<StateTransition<BossMeleeState>>
            {
                new StateTransition<BossMeleeState>(AimingStateTransition)
            };

            // 4. Charging State
            var chargingStateCase = new StateCase();
            chargingStateCase.state = new BossMeleeChargeState(enemy);
            chargingStateCase.transitions = new List<StateTransition<BossMeleeState>>
            {
                new StateTransition<BossMeleeState>(ChargingStateTransition)
            };

            states.Add(BossMeleeState.Patrolling, patrollingStateCase);
            states.Add(BossMeleeState.Following, followingStateCase);
            states.Add(BossMeleeState.AimingCharge, aimingStateCase);
            states.Add(BossMeleeState.Charging, chargingStateCase);
        }

        private bool PatrollingStateTransition(out BossMeleeState nextState)
        {
            bool isTargetSpotted = enemy.IsTargetInVisionRange || enemy.HasTakenDamage;

            if (!isTargetSpotted)
            {
                nextState = BossMeleeState.Patrolling;
                return false;
            }

            if (enemy.CanCharge())
            {
                nextState = BossMeleeState.AimingCharge;
            }
            else
            {
                nextState = BossMeleeState.Following;
            }
            return true;
        }

        private bool FollowingStateTransition(out BossMeleeState nextState)
        {
            if (enemy.CanCharge())
            {
                nextState = BossMeleeState.AimingCharge;
                return true;
            }

            nextState = BossMeleeState.Following;
            return false;
        }

        private bool AimingStateTransition(out BossMeleeState nextState)
        {
            var aimingState = (BossMeleeAimingState)states[BossMeleeState.AimingCharge].state;

            if (aimingState.IsAimingFinished())
            {
                nextState = BossMeleeState.Charging;
                return true;
            }

            nextState = BossMeleeState.AimingCharge;
            return false;
        }

        private bool ChargingStateTransition(out BossMeleeState nextState)
        {
            var chargingState = (BossMeleeChargeState)states[BossMeleeState.Charging].state;

            if (chargingState.IsChargeFinished())
            {
                nextState = BossMeleeState.Following;
                return true;
            }

            nextState = BossMeleeState.Charging;
            return false;
        }
    }
}
