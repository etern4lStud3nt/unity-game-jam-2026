using UnityEngine;

namespace Bosses
{
    // Drifts and bobs around its spot, point down, for a random wait (shorter in the second half), then attacks if
    // Grandma still has attacks left before her nap.
    public class NeedleHoverState : NeedleState
    {
        private readonly float phase = Random.value * 10f; // so the needles do not move in sync
        private float wait;

        public NeedleHoverState(Needle needle) : base(needle) { }

        protected override void OnEnter()
        {
            wait = Random.Range(Stats.MinWait, Stats.MaxWait) * Boss.WaitScale;
        }

        protected override void OnTick()
        {
            if (TimeInState >= wait && Boss.TryStartAttack())
                Needle.Attack();
        }

        protected override void OnFixedTick()
        {
            float t = Time.time + phase;
            var wander = new Vector2(Mathf.Sin(t * 1.3f) * Stats.HoverDrift, Mathf.Sin(t * 2.7f) * Stats.HoverBob);
            Body.MovePosition(Vector2.MoveTowards(Body.position, Needle.Spot + wander, Stats.ReturnSpeed * Time.fixedDeltaTime));
            Needle.TurnTowards(Vector2.down, TurnSpeed * Time.fixedDeltaTime);
        }
    }
}
