namespace Enemies
{
    // Dashes forward in the direction locked at the end of the wind-up. The Hurtbox's contact damage does the
    // hitting. Stops early at a wall; at a ledge it flies off, like the chase.
    public class LungeState : EnemyState
    {
        private int direction;

        public LungeState(EnemyBrain brain) : base(brain) { }

        protected override void OnEnter()
        {
            direction = Motor.Facing;
        }

        protected override void OnTick()
        {
            if (TimeInState >= Stats.LungeTime)
                Fsm.SetState(Brain.Recover);
        }

        protected override void OnFixedTick()
        {
            if (Motor.WallAhead)
            {
                Fsm.SetState(Brain.Recover);
                return;
            }
            Motor.Move(direction, Stats.LungeSpeed * Stats.Burst(TimeInState / Stats.LungeTime));
        }

        protected override void OnExit() => Motor.Stop();
    }
}
