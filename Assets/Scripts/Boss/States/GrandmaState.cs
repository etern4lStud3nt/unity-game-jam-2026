using Enemies;
using UnityEngine;

namespace Bosses
{
    // Base for Grandma's states: gives each state the boss and tracks how long the state has been running.
    public abstract class GrandmaState : IState
    {
        protected readonly GrandmaBoss Boss;
        private float enteredAt;

        protected GrandmaState(GrandmaBoss boss) => Boss = boss;

        protected GrandmaStats Stats => Boss.Stats;
        protected StateMachine Fsm => Boss.Fsm;
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
