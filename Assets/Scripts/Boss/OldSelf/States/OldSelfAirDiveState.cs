using UnityEngine;

namespace Bosses
{
    // Jumps straight up (Dive Jump Height), hangs in the air shaking while it aims at (or ahead of, Dive Prediction)
    // the player (Dive Hang Time; the aim locks for the last Dive Aim Lock Time), then dives in a straight line until
    // it hits the ground. The camera shakes on impact. It always dives at least Dive Min Downward steeply, so it
    // cannot fly off sideways or upwards.
    public class OldSelfAirDiveState : OldSelfState
    {
        private enum Step { Rise, Hang, Dive }

        // Extra units below the take-off height the dive may still need to fall (a lower floor), for the timeout.
        private const float ExtraFall = 4f;

        private Step step;
        private Vector2 direction;
        private int landingsBefore;

        public OldSelfAirDiveState(OldSelfBoss boss) : base(boss) { }

        // Safety stop: the slowest possible dive (steepness Dive Min Downward) falling the jump height plus a bit.
        private float MaxDiveTime =>
            (Stats.DiveJumpHeight + ExtraFall) / (Mathf.Max(Stats.DiveMinDownward, 0.1f) * Stats.DiveSpeed) + 0.5f;

        protected override void OnEnter()
        {
            step = Step.Rise;
            Boss.FacePlayer();
            landingsBefore = Motor.Landings;
            float up = Motor.JumpGravity(Stats.DiveJumpHeight, Stats.DiveRiseTime);
            Motor.SetVelocity(new Vector2(0f, up));
        }

        protected override void OnTick()
        {
            switch (step)
            {
                case Step.Rise:
                    // The take-off speed is set at once, so it rises until the top.
                    if (Body.linearVelocityY > 0f)
                        return;
                    step = Step.Hang;
                    RestartTimer();
                    Motor.SuspendGravity();
                    Motor.SetVelocity(Vector2.zero);
                    Boss.SetWarning(true);
                    Aim(Stats.DiveHangTime * Boss.TimeScale);
                    return;

                case Step.Hang:
                    float hang = Stats.DiveHangTime * Boss.TimeScale;
                    if (TimeInState < hang - Stats.DiveAimLockTime)
                        Aim(hang - TimeInState);
                    if (TimeInState < hang)
                        return;
                    step = Step.Dive;
                    RestartTimer();
                    Boss.SetWarning(false);
                    Boss.SetTrail(true);
                    return;

                case Step.Dive:
                    bool landed = Motor.Landings > landingsBefore;
                    if (!landed && TimeInState < MaxDiveTime)
                        return;
                    Motor.SetVelocity(Vector2.zero);
                    Boss.Shake(Stats.DiveShake);
                    Fsm.SetState(Boss.Recover);
                    return;
            }
        }

        protected override void OnFixedTick()
        {
            if (step == Step.Hang)
                Motor.SetVelocity(Vector2.zero);
            else if (step == Step.Dive)
                Motor.SetVelocity(direction * Stats.DiveSpeed);
        }

        // Aims ahead of the player: their running speed times the time until the dive reaches them (the rest of
        // the hang plus the flight), scaled by Dive Prediction.
        private void Aim(float untilDive)
        {
            Vector2 from = Body.worldCenterOfMass;
            Vector2 target = Boss.PlayerPoint;
            float flight = Vector2.Distance(from, target) / Stats.DiveSpeed;
            target.x += Boss.PlayerVelocity.x * (Mathf.Max(0f, untilDive) + flight) * Stats.DivePrediction;
            target.x = Boss.ClampToArena(target.x);

            Vector2 to = target - from;
            to.y = Mathf.Min(to.y, -Stats.DiveMinDownward * to.magnitude);
            direction = to.sqrMagnitude > 0.001f ? to.normalized : Vector2.down;
            Boss.Face(direction.x >= 0f ? 1 : -1);
        }
    }
}
