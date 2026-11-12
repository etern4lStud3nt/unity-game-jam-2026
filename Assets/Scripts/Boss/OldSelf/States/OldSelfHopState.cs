using UnityEngine;

namespace Bosses
{
    // Jumps to a new spot in the arena (Hop Distance away, inside Edge Margin, with ground under it). Away from the
    // player when they are close, otherwise either way. Lands and attacks straight away.
    public class OldSelfHopState : OldSelfState
    {
        // A spot closer than this share of the shortest hop is not worth hopping to.
        private const float WorthItShare = 0.5f;

        private int landingsBefore;

        public OldSelfHopState(OldSelfBoss boss) : base(boss) { }

        // Safety stop if it never leaves the ground (a low ceiling): twice the hop time.
        private float MaxHopTime => Stats.HopTime * 2f;

        protected override void OnEnter()
        {
            float x = Body.position.x;
            float target = PickTarget();

            Boss.Face(target >= x ? 1 : -1);
            landingsBefore = Motor.Landings;
            float up = Motor.JumpGravity(Stats.HopHeight, Stats.HopTime * 0.5f);
            Motor.SetVelocity(new Vector2((target - x) / Stats.HopTime, up));
        }

        // Tries the chosen way at full and half distance, then the other way, and keeps the first spot that is
        // far enough to be worth it and has ground to land on (no hopping into a pit). Nothing fits: hops in place.
        private float PickTarget()
        {
            float x = Body.position.x;
            float y = Body.position.y;
            int direction = Boss.PlayerDistanceX < Stats.SlashRange ? -Boss.PlayerSide : (Random.value < 0.5f ? -1 : 1);
            float distance = Random.Range(Stats.HopDistance.x, Stats.HopDistance.y);

            for (int tries = 0; tries < 4; tries++)
            {
                int side = tries < 2 ? direction : -direction;
                float share = tries % 2 == 0 ? 1f : 0.5f;
                float target = Boss.ClampToArena(x + side * distance * share);
                if (Mathf.Abs(target - x) >= Stats.HopDistance.x * WorthItShare
                    && Boss.HasFloorAt(target, y, Stats.HopMaxDrop))
                    return target;
            }
            return x;
        }

        protected override void OnTick()
        {
            bool landed = Motor.Landings > landingsBefore;
            if (landed || (TimeInState > MaxHopTime && Boss.IsGrounded))
                Boss.StartAttack();
        }
    }
}
