using UnityEngine;

namespace Bosses
{
    // Defeated: the thrown object and traps disappear, it stops hurting, falls to the ground and fades out,
    // BossDefeated goes out and its reward appears. Final: a respawn does not bring it back.
    public class OldSelfDeadState : OldSelfState
    {
        private bool rewarded;
        private bool settled;

        public OldSelfDeadState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.StopAttackEffects();
            Boss.PutAwayThrownAndTraps();
            Body.linearVelocity = Vector2.zero;
            EventBus.Publish(new BossDefeated { RoomId = Boss.RoomId, Position = Boss.transform.position });
        }

        protected override void OnTick()
        {
            // Turn the colliders off only once it lies on the ground, or it would fall through the floor.
            if (!settled && (Boss.IsGrounded || TimeInState > 2f))
            {
                settled = true;
                Body.linearVelocity = Vector2.zero;
                Body.bodyType = RigidbodyType2D.Kinematic;
                Boss.SetCollidersEnabled(false);
            }

            float k = Stats.DeathFadeTime > 0f ? Mathf.Clamp01(TimeInState / Stats.DeathFadeTime) : 1f;
            if (Boss.Flash != null)
                Boss.Flash.Fade = 1f - k;
            if (k >= 1f && settled && !rewarded)
            {
                rewarded = true;
                Boss.ShowReward();
            }
        }

        protected override void OnFixedTick() => Boss.StopX();
    }
}
