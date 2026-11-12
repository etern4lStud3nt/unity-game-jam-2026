namespace Bosses
{
    // Before the fight (and after a lost attempt): stands still. The intro's Yarn signal starts the fight; once the
    // intro has run (or without a signal), so does the player being in the arena.
    public class OldSelfWaitState : OldSelfState
    {
        public OldSelfWaitState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Motor.Stop();
            Boss.ResetFade();
        }

        protected override void OnTick() => Boss.StartOnSight();
    }
}
