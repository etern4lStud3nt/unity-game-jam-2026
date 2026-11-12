namespace Bosses
{
    // Defeated: the needles disappear, she stops hurting and fades out, BossDefeated goes out and her reward
    // appears. Final: a respawn does not bring her back.
    public class GrandmaDeadState : GrandmaState
    {
        public GrandmaDeadState(GrandmaBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.ShowSleep(false);
            Boss.SetCollidersEnabled(false);
            foreach (Needle needle in Boss.AllNeedles)
                needle.PutAway();
            Boss.PublishDefeated();
        }

        protected override void OnTick() => Boss.FadeOut(TimeInState, Stats.DeathFadeTime);
    }
}
