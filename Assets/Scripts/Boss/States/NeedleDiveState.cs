using UnityEngine;

namespace Bosses
{
    // One fast straight line along the locked aim, point first. Sticks where the point meets ground or a wall;
    // flies back (ending the attack) if it travels MaxDiveDistance without hitting anything.
    public class NeedleDiveState : NeedleState
    {
        private Vector2 direction;
        private float travelled;

        public NeedleDiveState(Needle needle) : base(needle) { }

        protected override void OnEnter()
        {
            direction = Needle.DiveDirection.normalized;
            travelled = 0f;
            Needle.SetPointing(direction);
        }

        protected override void OnFixedTick()
        {
            float step = Stats.DiveSpeed * Time.fixedDeltaTime;
            RaycastHit2D hit = Needle.PointHit(direction, step);
            if (hit.collider != null)
            {
                Needle.StickAt(hit, direction);
                Fsm.SetState(Needle.Stuck);
                return;
            }

            travelled += step;
            if (travelled > Stats.MaxDiveDistance)
            {
                Needle.ChainLeft = 0;
                Fsm.SetState(Needle.Return);
                return;
            }
            Body.MovePosition(Body.position + direction * step);
        }
    }
}
