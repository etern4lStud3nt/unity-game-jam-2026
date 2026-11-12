namespace Bosses
{
    // Before the fight (and after a lost attempt): stands still. The intro's Yarn signal starts the fight; once the
    // intro has run (or without a signal), so does the player being in the arena.
    public class OldSelfWaitState : OldSelfState
    {
        public OldSelfWaitState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.StopX();
            if (Boss.Flash != null)
                Boss.Flash.Fade = 1f;
        }

        protected override void OnTick()
        {
            if (Boss.ShouldStartOnSight && Boss.PlayerInArena)
                Boss.StartFight();
        }
    }
}
