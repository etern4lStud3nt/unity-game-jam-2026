using UnityEngine;

namespace Bosses
{
    // Awake: she walks away from the player, or jumps away (after a crouch) when they get close, while the needles
    // attack on their own timers, each attack (a chain of dives) using up one of a random number allowed. When none
    // are left, no needle is still attacking and she is on the ground, she falls asleep. The second-half needles
    // join when she wakes up low enough (see GrandmaBoss.CheckPhase2).
    public class GrandmaAttackState : GrandmaState
    {
        public GrandmaAttackState(GrandmaBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.ShowSleep(false);
            Boss.AttacksLeft = Random.Range(Stats.MinAttacksBeforeSleep, Stats.MaxAttacksBeforeSleep + 1);
            Boss.CheckPhase2();
        }

        protected override void OnTick()
        {
            if (Boss.AttacksLeft <= 0 && !Boss.AnyNeedleAttacking() && !Boss.IsJumping)
                Fsm.SetState(Boss.Sleeping);
        }

        protected override void OnFixedTick()
        {
            if (Boss.IsJumping)
                Boss.UpdateJump();
            else if (Boss.ShouldJump)
                Boss.StartJump();
            else
                Boss.WalkAwayFromPlayer();
        }

        protected override void OnExit()
        {
            Boss.CancelJump();
            Boss.StopWalking();
        }
    }
}
