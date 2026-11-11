namespace Bosses
{
    // Stuck in the ground or a wall. With dives left in the attack it pulls out after ChainStuckTime for the next
    // one; after the last dive it stays StuckTime, then flies back. While Grandma sleeps it stays put.
    public class NeedleStuckState : NeedleState
    {
        public NeedleStuckState(Needle needle) : base(needle) { }

        protected override void OnTick()
        {
            if (Needle.ChainLeft > 0)
            {
                if (TimeInState >= Stats.ChainStuckTime)
                    Fsm.SetState(Needle.PullBack);
            }
            else if (TimeInState >= Stats.StuckTime && Boss.IsAwake)
            {
                Fsm.SetState(Needle.Return);
            }
        }
    }
}
