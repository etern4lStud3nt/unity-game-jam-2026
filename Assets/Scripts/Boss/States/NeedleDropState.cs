using UnityEngine;

namespace Bosses
{
    // Grandma fell asleep: drops straight down, point first, sticks in the ground and stays there until she wakes.
    public class NeedleDropState : NeedleState
    {
        // Stops falling after this many units (nothing below it) and waits in the air.
        private const float MaxFall = 50f;

        private bool landed;
        private float fallen;

        public NeedleDropState(Needle needle) : base(needle) { }

        protected override void OnEnter()
        {
            landed = false;
            fallen = 0f;
            Needle.SetPointing(Vector2.down);
        }

        protected override void OnTick()
        {
            if (landed && Boss.IsAwake)
                Fsm.SetState(Needle.Return);
        }

        protected override void OnFixedTick()
        {
            if (landed)
                return;
            float step = Stats.FallSpeed * Time.fixedDeltaTime;
            RaycastHit2D hit = Needle.PointHit(Vector2.down, step);
            if (hit.collider != null)
            {
                Needle.StickAt(hit, Vector2.down);
                landed = true;
                return;
            }
            fallen += step;
            landed = fallen > MaxFall;
            Body.MovePosition(Body.position + Vector2.down * step);
        }
    }
}
