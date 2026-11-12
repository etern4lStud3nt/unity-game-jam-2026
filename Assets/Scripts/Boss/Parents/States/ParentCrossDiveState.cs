using UnityEngine;

namespace Bosses
{
    // One half of the cross dive, for either parent. It appears up and to Side of Aim (Cross Dive Height over the
    // floor, Cross Dive Side Offset sideways; after Delay seconds, still away), hangs there shaking (Cross Dive Tell,
    // the warning; nothing moves any more), then dives diagonally through Aim and on down to the floor, through any
    // platform, and stays stuck there (Stuck Time, the opening) before it leaves. The director sends the two from
    // opposite sides at the same Aim, one a little after the other, so their paths cross where the player stood.
    public class ParentCrossDiveState : ParentState<ParentActor>
    {
        private enum Step { Wait, Hang, Dive, Stuck }

        // How far below the arena's top it stays.
        private const float CeilingClearance = 0.5f;

        private Step step;
        private Vector2 direction;
        private Vector2 hangPoint;

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
                    // At the aim (the player, even in the air), but never flatter than Cross Dive Min Downward, so
                    // a high aim cannot turn it into a flat flight across the room.
                    Vector2 to = Aim - from;
                    direction = to.sqrMagnitude > 0.001f ? to.normalized : Vector2.down;
                    if (-direction.y < Stats.CrossDiveMinDownward)
                    {
                        float down = Mathf.Max(Stats.CrossDiveMinDownward, 0.1f);
                        float side = Mathf.Sqrt(Mathf.Max(0f, 1f - down * down));
                        direction = new Vector2(direction.x >= 0f ? side : -side, -down);
                    }
                    hangPoint = from;
                    Actor.Appear(from, -Side);
                    Actor.SetWarning(true);
                    return;

                case Step.Hang:
                    if (TimeInState < Stats.CrossDiveTell)
                        return;
                    step = Step.Dive;
                    RestartTimer();
                    Actor.HideDiveLine();
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
            if (step == Step.Hang)
            {
                // Draws back away from its dive while it shakes (eased), with the line showing its path.
                float k = Stats.CrossDiveTell > 0f ? Mathf.Clamp01(TimeInState / Stats.CrossDiveTell) : 1f;
                Vector2 at = hangPoint - direction * (Stats.HangPull * (1f - (1f - k) * (1f - k)));
                Body.MovePosition(at);
                float length = (at.y - Boss.FloorY) / Mathf.Max(0.1f, -direction.y);
                Actor.ShowDiveLine(at, direction, length, k);
                return;
            }
            if (step != Step.Dive)
                return;
            Vector2 velocity = direction * Stats.CrossDiveSpeed;
            float drop = Body.position.y - Actor.StandY;
            // Safety net: at the arena's edge it stops going sideways and drops straight down, so it never leaves
            // the room whatever the angle.
            float x = Body.position.x + velocity.x * Time.fixedDeltaTime;
            if (!Mathf.Approximately(Boss.ClampToArena(x), x))
            {
                direction = Vector2.down;
                velocity = direction * Stats.CrossDiveSpeed;
            }
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
            Actor.PlaySound(Stats.ImpactSound);
            Boss.Shake(Stats.ImpactShake);
            Actor.IsAttacking = false;
            Actor.IsRecovering = true;
        }
    }
}
