using UnityEngine;

namespace Bosses
{
    // A short pause on the ground facing the player, then a hop to a new spot (Hop Chance) or straight into an
    // attack. After a hop it always attacks.
    public class OldSelfIdleState : OldSelfState
    {
        private float pause;

        public OldSelfIdleState(OldSelfBoss boss) : base(boss) { }

        // Set by the hop: skip the pause and the hop roll, attack now.
        public bool AttackNext { get; set; }

        protected override void OnEnter()
        {
            Boss.StopX();
            Boss.FacePlayer();
            pause = AttackNext ? 0f : Random.Range(Stats.MinIdleTime, Stats.MaxIdleTime) * Boss.TimeScale;
        }

        protected override void OnTick()
        {
            if (TimeInState < pause || !Boss.IsGrounded)
                return;

            if (!AttackNext && Random.value < Stats.HopChance)
            {
                Fsm.SetState(Boss.Hop);
                return;
            }
            AttackNext = false;
            Boss.StartAttack();
        }

        protected override void OnFixedTick() => Boss.StopX();
    }
}
