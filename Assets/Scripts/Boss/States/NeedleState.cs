using Enemies;
using UnityEngine;

namespace Bosses
{
    // Base for a needle's states: gives each state the needle (and through it Grandma and her stats).
    public abstract class NeedleState : TimedState
    {
        // Degrees per second a needle turns when it is not snapping straight to a direction.
        protected const float TurnSpeed = 720f;

        protected readonly Needle Needle;

        protected NeedleState(Needle needle) => Needle = needle;

        protected GrandmaBoss Boss => Needle.Boss;
        protected GrandmaStats Stats => Needle.Stats;
        protected Rigidbody2D Body => Needle.Body;
        protected StateMachine Fsm => Needle.Fsm;
    }
}
