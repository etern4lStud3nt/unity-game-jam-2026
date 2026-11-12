using UnityEngine;

namespace Bosses
{
    // A short pause on the ground facing the player, then a hop to a new spot (Hop Chance) or straight into an
    // attack. (A hop goes straight into an attack when it lands.)
    public class OldSelfIdleState : OldSelfState
    {
        private float pause;

        public OldSelfIdleState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Motor.Stop();
            Boss.FacePlayer();
            pause = Random.Range(Stats.MinIdleTime, Stats.MaxIdleTime) * Boss.TimeScale;
        }

        protected override void OnTick()
        {
            if (TimeInState < pause || !Boss.IsGrounded)
                return;
            if (Random.value < Stats.HopChance)
                Fsm.SetState(Boss.Hop);
            else
                Boss.StartAttack();
        }

        protected override void OnFixedTick() => Motor.Stop();
    }
}
