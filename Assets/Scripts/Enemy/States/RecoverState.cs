namespace Enemies
{
    // Stands still after the lunge: the player's window to hit back.
    public class RecoverState : EnemyState
    {
        public RecoverState(EnemyBrain brain) : base(brain) { }

        protected override void OnEnter() => Motor.Stop();

        protected override void OnTick()
        {
            if (TimeInState >= Stats.RecoverTime)
                Fsm.SetState(Brain.Chase);
        }

        protected override void OnFixedTick() => Motor.Stop();
    }
}
