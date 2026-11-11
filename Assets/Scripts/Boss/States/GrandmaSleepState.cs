namespace Bosses
{
    // Asleep for SleepTime: the needles drop to the ground and stay there, then she wakes up and they fly back.
    public class GrandmaSleepState : GrandmaState
    {
        public GrandmaSleepState(GrandmaBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.ShowSleep(true);
            foreach (Needle needle in Boss.AllNeedles)
                needle.Drop();
        }

        protected override void OnTick()
        {
            if (TimeInState >= Stats.SleepTime)
                Fsm.SetState(Boss.Attacking);
        }

        protected override void OnExit() => Boss.ShowSleep(false);
    }
}
