using UnityEngine;

namespace Bosses
{
    // Jumps to a new spot in the arena (Hop Distance away, inside Edge Margin, with ground under it). Away from the
    // player when they are close, otherwise either way. Lands and attacks straight away.
    public class OldSelfHopState : OldSelfState
    {
        private bool landed;

        public OldSelfHopState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            landed = false;
            float x = Body.position.x;
            float target = PickTarget();

            Boss.Face(target >= x ? 1 : -1);
            float rise = Stats.HopTime * 0.5f;
            float up = Boss.JumpGravity(Stats.HopHeight, rise);
            Body.linearVelocity = new Vector2((target - x) / Stats.HopTime, up);
        }

        // Tries the chosen way at full and half distance, then the other way, and keeps the first spot that is
        // far enough to be worth it and has ground to land on (no hopping into a pit). Nothing fits: hops in place.
        private float PickTarget()
        {
            float x = Body.position.x;
            float y = Body.position.y;
            int direction = Boss.PlayerDistanceX < Stats.SlashRange ? -Boss.PlayerSide : (Random.value < 0.5f ? -1 : 1);
            float distance = Random.Range(Stats.HopDistance.x, Stats.HopDistance.y);
            float maxDrop = Stats.HopHeight + 3f;

            foreach (int side in new[] { direction, -direction })
                foreach (float share in new[] { 1f, 0.5f })
                {
                    float target = Boss.ClampToArena(x + side * distance * share);
                    if (Mathf.Abs(target - x) >= Stats.HopDistance.x * 0.5f && Boss.HasFloorAt(target, y, maxDrop))
                        return target;
                }
            return x;
        }

        protected override void OnTick()
        {
            // Give it a moment to leave the ground before checking for the landing.
            if (landed || TimeInState < 0.1f || Body.linearVelocityY > 0.01f || !Boss.IsGrounded)
                return;
            landed = true;
            Boss.Idle.AttackNext = true;
            Fsm.SetState(Boss.Idle);
        }

        protected override void OnExit()
        {
            Boss.ResetGravity();
            Boss.StopX();
        }
    }
}
