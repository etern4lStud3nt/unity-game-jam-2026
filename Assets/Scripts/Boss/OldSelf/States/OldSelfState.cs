using Enemies;
using UnityEngine;

namespace Bosses
{
    // Base for the Old Self's states: gives each state the boss and tracks how long the state has been running.
    public abstract class OldSelfState : IState
    {
        protected readonly OldSelfBoss Boss;
        private float enteredAt;

        protected OldSelfState(OldSelfBoss boss) => Boss = boss;

        protected OldSelfStats Stats => Boss.Stats;
        protected StateMachine Fsm => Boss.Fsm;
        protected Rigidbody2D Body => Boss.Body;
        protected float TimeInState => Time.time - enteredAt;

        // Restarts TimeInState, for states made of several steps (warning, then the move).
        protected void RestartTimer() => enteredAt = Time.time;

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
