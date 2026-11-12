namespace Enemies
{
    // Base for enemy states: gives each state the brain (and through it the motor, senses and stats).
    public abstract class EnemyState : TimedState
    {
        protected readonly EnemyBrain Brain;

        protected EnemyState(EnemyBrain brain) => Brain = brain;

        protected EnemyMotor Motor => Brain.Motor;
        protected EnemySenses Senses => Brain.Senses;
        protected EnemyStats Stats => Brain.Stats;
        protected StateMachine Fsm => Brain.Fsm;
    }
}
