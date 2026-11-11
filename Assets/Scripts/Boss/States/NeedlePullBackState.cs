using UnityEngine;

namespace Bosses
{
    // Between two dives of an attack: pulls back out of the ground the way it came, PullBackDistance units, then
    // aims for the next dive.
    public class NeedlePullBackState : NeedleState
    {
        private Vector2 direction;
        private float moved;

        public NeedlePullBackState(Needle needle) : base(needle) { }

        protected override void OnEnter()
        {
            direction = -Needle.DiveDirection.normalized;
            moved = 0f;
        }

        protected override void OnFixedTick()
        {
            float step = Mathf.Min(Stats.PullBackSpeed * Time.fixedDeltaTime, Stats.PullBackDistance - moved);
            Body.MovePosition(Body.position + direction * step);
            moved += step;
            if (moved >= Stats.PullBackDistance - 0.001f)
            {
                Needle.ChainLeft--;
                Fsm.SetState(Needle.Aim);
            }
        }
    }
}
