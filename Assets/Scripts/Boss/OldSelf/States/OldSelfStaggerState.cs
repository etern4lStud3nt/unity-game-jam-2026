namespace Bosses
{
    // Too many hits: whatever it was doing stops, it drops to the ground and stays open for Stagger Time (crouched
    // and shaking), then hops away and attacks.
    public class OldSelfStaggerState : OldSelfState
    {
        public OldSelfStaggerState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.StopAttackEffects();
            Boss.StopX();
            Boss.SetCrouch(true);
            Boss.SetWarning(true);
        }

        protected override void OnTick()
        {
            if (TimeInState >= Stats.StaggerTime && Boss.IsGrounded)
                Fsm.SetState(Boss.Hop);
        }

        protected override void OnFixedTick() => Boss.StopX();

        protected override void OnExit() => Boss.StopAttackEffects();
    }
}
