using Enemies;
using UnityEngine;

namespace Bosses
{
    // Base for a needle's states: gives each state the needle (and through it Grandma and her stats) and tracks
    // how long the state has been running.
    public abstract class NeedleState : IState
    {
        // Degrees per second a needle turns when it is not snapping straight to a direction.
        protected const float TurnSpeed = 720f;

        protected readonly Needle Needle;
        private float enteredAt;

        protected NeedleState(Needle needle) => Needle = needle;

        protected GrandmaBoss Boss => Needle.Boss;
        protected GrandmaStats Stats => Needle.Stats;
        protected Rigidbody2D Body => Needle.Body;
        protected StateMachine Fsm => Needle.Fsm;
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
