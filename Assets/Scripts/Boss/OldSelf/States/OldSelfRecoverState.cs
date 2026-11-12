using UnityEngine;

namespace Bosses
{
    // The opening after an attack: stands still for Recover Time (shorter in the second half), then goes back to
    // Idle. In the second half an attack may instead be followed straight away by another (Phase 2 Chain Chance),
    // but never more than two in a row.
    public class OldSelfRecoverState : OldSelfState
    {
        public OldSelfRecoverState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Motor.Stop();
            if (Boss.InPhase2 && !Boss.Chained && Boss.IsGrounded && Random.value < Stats.Phase2ChainChance)
            {
                Boss.Chained = true;
                Boss.StartAttack();
                return;
            }
            Boss.Chained = false;
        }

        protected override void OnTick()
        {
            if (TimeInState >= Stats.RecoverTime * Boss.TimeScale && Boss.IsGrounded)
                Fsm.SetState(Boss.Idle);
        }

        protected override void OnFixedTick() => Motor.Stop();
    }
}
