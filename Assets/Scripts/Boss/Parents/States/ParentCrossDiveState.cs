using UnityEngine;

namespace Bosses
{
    // One half of the cross dive, for either parent. It appears up and to Side of Aim (Cross Dive Height over the
    // floor, Cross Dive Side Offset sideways; after Delay seconds, still away), hangs there shaking (Cross Dive Tell,
    // the warning; nothing moves any more), then dives diagonally through Aim down to the floor, through any
    // platform, and stays stuck there (Stuck Time, the opening) before it leaves. The director sends the two from
    // opposite sides at the same Aim, one a little after the other, so their paths cross where the player stood.
    public class ParentCrossDiveState : ParentState<ParentActor>
    {
        private enum Step { Wait, Hang, Dive, Stuck }

        // How far below the arena's top it stays.
        private const float CeilingClearance = 0.5f;

        private Step step;
        private Vector2 direction;

        public ParentCrossDiveState(ParentActor actor) : base(actor) { }

        // Set by ParentActor.CrossDive before the state is entered. Side: -1 = hangs to the left of Aim, 1 = right.
        public int Side { get; set; } = 1;
        public Vector2 Aim { get; set; }
        public float Delay { get; set; }

        protected override void OnEnter()
        {
            step = Step.Wait;
            Actor.IsAttacking = true;
        }

        protected override void OnTick()
        {
            switch (step)
            {
                case Step.Wait:
                    if (TimeInState < Delay)
                        return;
                    step = Step.Hang;
                    RestartTimer();
                    Vector2 from = HangPoint();
                    Vector2 to = Aim - from;
                    // Always downwards, even if the arena squeezed the hang point.
                    to.y = Mathf.Min(to.y, -0.1f);
                    direction = to.normalized;
                    Actor.Appear(from, -Side);
                    Actor.SetWarning(true);
                    return;

                case Step.Hang:
                    if (TimeInState < Stats.CrossDiveTell)
                        return;
                    step = Step.Dive;
                    RestartTimer();
                    Actor.SetWarning(false);
                    Actor.SetTrail(true);
                    return;

                case Step.Stuck:
                    if (TimeInState >= Stats.StuckTime)
                        Actor.Leave();
                    return;
            }
        }

        protected override void OnFixedTick()
        {
            if (step != Step.Dive)
                return;
            Vector2 velocity = direction * Stats.CrossDiveSpeed;
            float drop = Body.position.y - Actor.StandY;
            if (drop <= -velocity.y * Time.fixedDeltaTime)
                Land();
            else
                Actor.SetVelocity(velocity);
        }

        private Vector2 HangPoint()
        {
            float x = Boss.ClampToArena(Aim.x + Side * Stats.CrossDiveSideOffset);
            float y = Actor.StandY + Stats.CrossDiveHeight;
            if (Boss.Arena != null)
                y = Mathf.Min(y, Boss.Arena.bounds.max.y - Actor.HalfHeight - CeilingClearance);
            return new Vector2(x, y);
        }

        private void Land()
        {
            step = Step.Stuck;
            RestartTimer();
            Actor.SetVelocity(Vector2.zero);
            Body.position = new Vector2(Boss.ClampToArena(Body.position.x), Actor.StandY);
            Actor.SetTrail(false);
            Actor.Punch(Stats.ImpactPunch);
            Boss.Shake(Stats.ImpactShake);
            Actor.IsAttacking = false;
            Actor.IsRecovering = true;
        }
    }
}
