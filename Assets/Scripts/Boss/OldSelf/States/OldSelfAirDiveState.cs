using UnityEngine;

namespace Bosses
{
    // Jumps straight up (Dive Jump Height), hangs in the air shaking while it aims at (or ahead of, Dive
    // Prediction) the player (Dive Hang Time, the aim locks for the last Dive Aim Lock Time), then dives in a straight line until it hits the ground. The
    // camera shakes on impact. It always dives at least a little downwards, so it cannot fly off upwards.
    public class OldSelfAirDiveState : OldSelfState
    {
        private enum Step { Rise, Hang, Dive }

        private const float MaxDiveTime = 2f;

        private Step step;
        private Vector2 direction;

        public OldSelfAirDiveState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            step = Step.Rise;
            Boss.FacePlayer();
            float up = Boss.JumpGravity(Stats.DiveJumpHeight, Stats.DiveRiseTime);
            Body.linearVelocity = new Vector2(0f, up);
        }

        protected override void OnTick()
        {
            switch (step)
            {
                case Step.Rise:
                    if (TimeInState < 0.05f || Body.linearVelocityY > 0f)
                        return;
                    step = Step.Hang;
                    RestartTimer();
                    Boss.SetGravity(false);
                    Body.linearVelocity = Vector2.zero;
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
                    bool landed = TimeInState > 0.05f && Boss.IsGrounded;
                    if (!landed && TimeInState < MaxDiveTime)
                        return;
                    Body.linearVelocity = Vector2.zero;
                    Boss.ResetGravity();
                    Boss.Shake(Stats.DiveShake);
                    Fsm.SetState(Boss.Recover);
                    return;
            }
        }

        protected override void OnFixedTick()
        {
            if (step == Step.Hang)
                Body.linearVelocity = Vector2.zero;
            else if (step == Step.Dive)
                Body.linearVelocity = direction * Stats.DiveSpeed;
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
            to.y = Mathf.Min(to.y, -0.3f * to.magnitude);
            direction = to.sqrMagnitude > 0.001f ? to.normalized : Vector2.down;
            Boss.Face(direction.x >= 0f ? 1 : -1);
        }

        protected override void OnExit()
        {
            Boss.StopAttackEffects();
        }
    }
}
