using UnityEngine;

namespace Enemies
{
    // A flyer's wind-up: shakes and draws back slightly while aiming at where the player will be (their running
    // speed times the time until the dive lands, scaled by Prediction). The aim freezes for the last AimLockTime,
    // then the dive starts along it.
    public class AimState : EnemyState
    {
        private readonly FlyerBrain flyer;
        private Vector2 aim;

        public AimState(FlyerBrain brain) : base(brain) => flyer = brain;

        protected override void OnEnter()
        {
            FlyerStats stats = flyer.FlyerStats;
            Motor.Stop();
            if (Brain.Flash != null)
                Brain.Flash.SetWarning(true);

            float diveTime = stats.DiveSpeed > 0f ? stats.DiveDistance / stats.DiveSpeed : 0f;
            Brain.NextAttackTime = Time.time + Stats.WindupTime + diveTime + Stats.RecoverTime + Stats.AttackCooldown;
            UpdateAim();
        }

        protected override void OnTick()
        {
            if (TimeInState < Stats.WindupTime - flyer.FlyerStats.AimLockTime)
                UpdateAim();

            if (TimeInState >= Stats.WindupTime)
            {
                flyer.DiveDirection = aim;
                Fsm.SetState(flyer.Dive);
            }
        }

        // Pulls back a little against the aim, like drawing a spear.
        protected override void OnFixedTick()
        {
            flyer.Flyer.Steer(-aim);
            Motor.Face(aim.x >= 0f ? 1 : -1);
        }

        protected override void OnExit()
        {
            if (Brain.Flash != null)
                Brain.Flash.SetWarning(false);
        }

        private void UpdateAim()
        {
            if (!Senses.HasPlayer)
                return;
            FlyerStats stats = flyer.FlyerStats;
            Vector2 from = Motor.Body.position;
            Vector2 target = Senses.PlayerPoint;

            // Only the horizontal run is predicted: jumps arc and land again, so leading them overshoots.
            float untilDive = Mathf.Max(0f, Stats.WindupTime - TimeInState);
            float travel = stats.DiveSpeed > 0f ? Vector2.Distance(from, target) / stats.DiveSpeed : 0f;
            target.x += Senses.PlayerVelocity.x * (untilDive + travel) * stats.Prediction;

            Vector2 direction = target - from;
            aim = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
        }
    }
}
