namespace Bosses
{
    // Before the fight (and after a lost attempt): she sits still, the needles are put away. The intro's Yarn
    // signal starts the fight; once the intro has run, so does the player being in the arena.
    public class GrandmaWaitState : GrandmaState
    {
        public GrandmaWaitState(GrandmaBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.ShowSleep(false);
            Boss.ResetFade();
        }

        protected override void OnTick() => Boss.StartOnSight();
    }
}
