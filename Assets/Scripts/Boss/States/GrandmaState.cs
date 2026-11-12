using Enemies;

namespace Bosses
{
    // Base for Grandma's states: gives each state the boss and her stats.
    public abstract class GrandmaState : TimedState
    {
        protected readonly GrandmaBoss Boss;

        protected GrandmaState(GrandmaBoss boss) => Boss = boss;

        protected GrandmaStats Stats => Boss.Stats;
        protected StateMachine Fsm => Boss.Fsm;
    }
}
