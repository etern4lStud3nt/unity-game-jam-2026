using UnityEngine;

namespace Bosses
{
    // One half of the cross dive, for either parent, with its own aim. After Delay seconds (still away) it appears up
    // and to Side of where the player is then (Cross Dive Height over the floor, Cross Dive Side Offset sideways) and
    // hangs there shaking (Cross Dive Tell, the warning). While it hangs it keeps aiming at the player, ahead of them
    // by Cross Dive Prediction, until Cross Dive Aim Lock seconds before it dives; then it dives diagonally through
    // that spot and on down to the floor, through any platform, and stays stuck there (Stuck Time, the opening)
    // before it leaves. The director sends the two from opposite sides, one a little after the other: dodging the
    // first does not dodge the second, which aims at where the player went.
    public class ParentCrossDiveState : ParentState<ParentActor>
    {
        private enum Step { Wait, Hang, Dive, Stuck }

        // How far below the arena's top it stays.
        private const float CeilingClearance = 0.5f;

        private Step step;
        private Vector2 direction;
        private Vector2 hangPoint;

        public ParentCrossDiveState(ParentActor actor) : base(actor) { }

        // Set by ParentActor.CrossDive before the state is entered. Side: -1 = hangs to the left of the player,
        // 1 = to the right.
        public int Side { get; set; } = 1;
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
                    hangPoint = HangPoint();
                    Actor.Appear(hangPoint, -Side);
                    Actor.SetWarning(true);
                    AimAt(hangPoint, Stats.CrossDiveTell);
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
                // Keeps aiming until the lock, then holds that line.
                float untilDive = Stats.CrossDiveTell - TimeInState;
                if (untilDive > Stats.CrossDiveAimLock)
                    AimAt(hangPoint, untilDive);

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

        // Aims from from at the player, ahead of them by their running speed times the time until the dive reaches
        // them (untilDive, the rest of the warning, plus the flight), scaled by Cross Dive Prediction. Never flatter
        // than Cross Dive Min Downward, so a player high in the air cannot turn it into a flat flight across the room.
        private void AimAt(Vector2 from, float untilDive)
        {
            Vector2 target = Boss.PlayerPoint;
            float flight = Vector2.Distance(from, target) / Stats.CrossDiveSpeed;
            target.x += Boss.PlayerVelocity.x * (Mathf.Max(0f, untilDive) + flight) * Stats.CrossDivePrediction;
            target.x = Boss.ClampToArena(target.x);

            Vector2 to = target - from;
            direction = to.sqrMagnitude > 0.001f ? to.normalized : Vector2.down;
            if (-direction.y < Stats.CrossDiveMinDownward)
            {
                float down = Mathf.Max(Stats.CrossDiveMinDownward, 0.1f);
                float side = Mathf.Sqrt(Mathf.Max(0f, 1f - down * down));
                direction = new Vector2(direction.x >= 0f ? side : -side, -down);
            }
        }

        // Up and to Side of where the player is right now.
        private Vector2 HangPoint()
        {
            float x = Boss.ClampToArena(Boss.PlayerPoint.x + Side * Stats.CrossDiveSideOffset);
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
