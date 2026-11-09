namespace Enemies
{
    // Stunned after a hit, only used when the stats give a StunDuration above 0. Entering it cancels whatever
    // the enemy was doing, including a wind-up. The knockback itself is applied by the brain.
    public class HurtState : EnemyState
    {
        public HurtState(EnemyBrain brain) : base(brain) { }

        protected override void OnTick()
        {
            if (TimeInState >= Stats.StunDuration)
                Fsm.SetState(Senses.HasPlayer ? Brain.Chase : Brain.Idle);
        }

        protected override void OnFixedTick() => Motor.Stop();
    }
}
