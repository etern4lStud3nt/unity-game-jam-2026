using UnityEngine;

namespace Enemies
{
    // A flyer's chase: flies to a spot above and to the side of the player (on the side it is already on), so the
    // dive comes in at an angle, and starts aiming once it is lined up. Gives up when the player leaves the room
    // or stays out of sight.
    public class ApproachState : EnemyState
    {
        // If the line-up spot cannot be reached (low ceiling, room edge), attack from wherever it is after this long.
        private const float LineUpTimeout = 2.5f;

        private readonly FlyerBrain flyer;

        public ApproachState(FlyerBrain brain) : base(brain) => flyer = brain;

        protected override void OnTick()
        {
            if (!Senses.HasPlayer || !Brain.PlayerInLeash || (!Senses.CanSeePlayer && Senses.TimeSinceSeen > Stats.LoseSightTime))
            {
                Fsm.SetState(Brain.Idle);
                return;
            }

            bool linedUp = Vector2.Distance(Motor.Body.position, LineUpSpot()) <= flyer.FlyerStats.LineUpTolerance;
            if ((linedUp || TimeInState > LineUpTimeout) && Senses.CanSeePlayer && Time.time >= Brain.NextAttackTime)
                Fsm.SetState(flyer.Aim);
        }

        protected override void OnFixedTick()
        {
            if (!Senses.HasPlayer)
                return;
            flyer.Flyer.FlyTowards(LineUpSpot(), Stats.ChaseSpeed, 1.5f);
            Motor.Face(Senses.PlayerDirection);
        }

        private Vector2 LineUpSpot()
        {
            FlyerStats stats = flyer.FlyerStats;
            Vector2 player = Senses.PlayerPoint;
            float side = Motor.Body.position.x >= player.x ? 1f : -1f;
            return flyer.ClampToTerritory(player + new Vector2(side * stats.DiveSide, stats.DiveHeight));
        }
    }
}
