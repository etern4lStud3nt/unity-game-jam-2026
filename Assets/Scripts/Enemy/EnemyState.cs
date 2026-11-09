using UnityEngine;

namespace Enemies
{
    // Base for enemy states: gives each state the brain (and through it the motor, senses and stats)
    // and tracks how long the state has been running.
    public abstract class EnemyState : IState
    {
        protected readonly EnemyBrain Brain;
        private float enteredAt;

        protected EnemyState(EnemyBrain brain) => Brain = brain;

        protected EnemyMotor Motor => Brain.Motor;
        protected EnemySenses Senses => Brain.Senses;
        protected EnemyStats Stats => Brain.Stats;
        protected StateMachine Fsm => Brain.Fsm;
        protected float TimeInState => Time.time - enteredAt;

        public void Enter()
        {
            enteredAt = Time.time;
            OnEnter();
        }

        public void Tick() => OnTick();
        public void FixedTick() => OnFixedTick();
        public void Exit() => OnExit();

        protected virtual void OnEnter() { }
        protected virtual void OnTick() { }
        protected virtual void OnFixedTick() { }
        protected virtual void OnExit() { }
    }
}
