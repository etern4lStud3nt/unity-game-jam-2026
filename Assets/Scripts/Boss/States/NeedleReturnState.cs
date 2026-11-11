using UnityEngine;

namespace Bosses
{
    // Flies back to its spot, turning point down on the way, then waits there.
    public class NeedleReturnState : NeedleState
    {
        public NeedleReturnState(Needle needle) : base(needle) { }

        protected override void OnTick()
        {
            if (Vector2.Distance(Body.position, Needle.Spot) < 0.05f)
                Fsm.SetState(Needle.Hover);
        }

        protected override void OnFixedTick()
        {
            Body.MovePosition(Vector2.MoveTowards(Body.position, Needle.Spot, Stats.ReturnSpeed * Time.fixedDeltaTime));
            Needle.TurnTowards(Vector2.down, TurnSpeed * Time.fixedDeltaTime);
        }
    }
}
