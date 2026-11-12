using UnityEngine;

namespace Enemies
{
    // Base for the states of every state machine (enemies, bosses, needles): forwards Enter / Tick / FixedTick /
    // Exit to overridable hooks and tracks how long the state has been running. Each owner's base state derives
    // from it and adds typed access to its owner.
    public abstract class TimedState : IState
    {
        private float enteredAt;

        protected float TimeInState => Time.time - enteredAt;

        // Restarts TimeInState, for states made of several steps (a warning, then the move).
        protected void RestartTimer() => enteredAt = Time.time;

        public void Enter()
        {
            enteredAt = Time.time;
            OnEnter();
        }

        public void Tick() => OnTick();
        public void FixedTick() => OnFixedTick();

        public void Exit()
        {
            OnExit();
            AfterExit();
        }

        protected virtual void OnEnter() { }
        protected virtual void OnTick() { }
        protected virtual void OnFixedTick() { }
        protected virtual void OnExit() { }

        // Runs after every state's own exit, for clean-up an owner wants after all of its states.
        protected virtual void AfterExit() { }
    }
}
