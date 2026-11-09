using System;

namespace Enemies
{
    // One mode of behaviour (patrol, chase, attack...). The owner calls Tick from Update and FixedTick from
    // FixedUpdate; physics movement belongs in FixedTick, decisions and timers in Tick.
    public interface IState
    {
        void Enter();
        void Tick();
        void FixedTick();
        void Exit();
    }

    // Holds the current state and swaps it, calling Exit on the old one and Enter on the new one.
    public class StateMachine
    {
        public IState Current { get; private set; }

        // (previous, next) after every switch; handy for debugging or animation hooks.
        public event Action<IState, IState> StateChanged;

        // restart = true re-enters the state even if it is already current (e.g. getting hit again while hurt).
        public void SetState(IState next, bool restart = false)
        {
            if (next == null || (next == Current && !restart))
                return;

            IState previous = Current;
            previous?.Exit();
            Current = next;
            Current.Enter();
            StateChanged?.Invoke(previous, next);
        }

        public void Tick() => Current?.Tick();

        public void FixedTick() => Current?.FixedTick();
    }
}
