using UnityEngine;

namespace Bosses
{
    // The warning: shakes in place while turning its point towards where the player will be (their running speed
    // times the time until the dive reaches them, scaled by Prediction). The aim freezes for the last AimLockTime,
    // then the dive starts along it.
    public class NeedleAimState : NeedleState
    {
        private Vector2 aim;

        public NeedleAimState(Needle needle) : base(needle) { }

        protected override void OnEnter()
        {
            if (Needle.Flash != null)
                Needle.Flash.SetWarning(true);
            aim = Needle.Forward;
            UpdateAim();
        }

        protected override void OnTick()
        {
            if (TimeInState < Stats.WindupTime - Stats.AimLockTime)
                UpdateAim();

            if (TimeInState >= Stats.WindupTime)
            {
                Needle.DiveDirection = aim;
                Fsm.SetState(Needle.Dive);
            }
        }

        protected override void OnFixedTick() => Needle.TurnTowards(aim, TurnSpeed * Time.fixedDeltaTime);

        protected override void OnExit()
        {
            if (Needle.Flash != null)
                Needle.Flash.SetWarning(false);
        }

        private void UpdateAim()
        {
            if (!Boss.HasPlayer)
                return;
            Vector2 from = Body.position;
            Vector2 target = Boss.PlayerPoint;

            // Only the horizontal run is predicted: jumps arc and land again, so leading them overshoots.
            float untilDive = Mathf.Max(0f, Stats.WindupTime - TimeInState);
            float travel = Vector2.Distance(from, target) / Stats.DiveSpeed;
            target.x += Boss.PlayerVelocity.x * (untilDive + travel) * Stats.Prediction;

            Vector2 direction = target - from;
            if (direction.sqrMagnitude > 0.0001f)
                aim = direction.normalized;
        }
    }
}
