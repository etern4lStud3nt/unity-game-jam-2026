using UnityEngine;

namespace Bosses
{
    // Defeated: the thrown object and traps disappear, it stops hurting, falls to the ground and fades out,
    // BossDefeated goes out and its reward appears. Final: a respawn does not bring it back.
    public class OldSelfDeadState : OldSelfState
    {
        // Seconds it may take to land before it stops anyway (it died over a pit).
        private const float SettleTimeout = 2f;

        private bool settled;

        public OldSelfDeadState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.PutAwayThrownAndTraps();
            Motor.SetVelocity(Vector2.zero);
            Boss.PublishDefeated();
        }

        protected override void OnTick()
        {
            // Turn the colliders off only once it lies on the ground, or it would fall through the floor.
            if (!settled && (Boss.IsGrounded || TimeInState > SettleTimeout))
            {
                settled = true;
                Motor.SetVelocity(Vector2.zero);
                Body.bodyType = RigidbodyType2D.Kinematic;
                Boss.SetCollidersEnabled(false);
            }
            // The reward appears only once it has landed.
            if (settled)
                Boss.FadeOut(TimeInState, Stats.DeathFadeTime);
        }

        protected override void OnFixedTick() => Motor.Stop();
    }
}
