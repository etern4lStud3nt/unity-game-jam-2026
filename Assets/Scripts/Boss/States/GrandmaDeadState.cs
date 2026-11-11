using UnityEngine;

namespace Bosses
{
    // Defeated: the needles disappear, she stops hurting and fades out, BossDefeated goes out and her reward
    // appears. Final: a respawn does not bring her back.
    public class GrandmaDeadState : GrandmaState
    {
        private bool rewarded;

        public GrandmaDeadState(GrandmaBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Boss.ShowSleep(false);
            Boss.SetCollidersEnabled(false);
            foreach (Needle needle in Boss.AllNeedles)
                needle.PutAway();
            EventBus.Publish(new BossDefeated { RoomId = Boss.RoomId, Position = Boss.transform.position });
        }

        protected override void OnTick()
        {
            float k = Stats.DeathFadeTime > 0f ? Mathf.Clamp01(TimeInState / Stats.DeathFadeTime) : 1f;
            if (Boss.Flash != null)
                Boss.Flash.Fade = 1f - k;
            if (k >= 1f && !rewarded)
            {
                rewarded = true;
                Boss.ShowReward();
            }
        }
    }
}
