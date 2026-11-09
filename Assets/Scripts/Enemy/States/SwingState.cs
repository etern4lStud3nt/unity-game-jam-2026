namespace Enemies
{
    // A melee swing: switches the brain's Attack Hitbox on for SwingActiveTime while taking a small step forward,
    // then recovers. Exit always switches the hitbox off, so a hit or death mid-swing never leaves it active.
    public class SwingState : EnemyState
    {
        private int direction;

        public SwingState(EnemyBrain brain) : base(brain) { }

        protected override void OnEnter()
        {
            direction = Motor.Facing;
            Brain.SetAttackHitbox(true);
        }

        protected override void OnTick()
        {
            if (TimeInState >= Stats.SwingActiveTime)
                Fsm.SetState(Brain.Recover);
        }

        protected override void OnFixedTick()
        {
            if (Stats.SwingStep <= 0f || Motor.Blocked())
                Motor.Stop();
            else
                Motor.Move(direction, Stats.SwingStep / Stats.SwingActiveTime * Stats.Burst(TimeInState / Stats.SwingActiveTime));
        }

        protected override void OnExit()
        {
            Brain.SetAttackHitbox(false);
            Motor.Stop();
        }
    }
}
