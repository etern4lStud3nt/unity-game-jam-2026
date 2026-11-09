using UnityEngine;

namespace Enemies
{
    // Runs at the player and follows them off ledges (only patrolling enemies stay on their platform).
    // Starts an attack when close enough and gives up after not seeing the player for a while.
    public class ChaseState : EnemyState
    {
        public ChaseState(EnemyBrain brain) : base(brain) { }

        protected override void OnTick()
        {
            // Gone, out of sight for too long, or too far from home (leash): back to the calm state.
            if (!Senses.HasPlayer || !Brain.PlayerInLeash || (!Senses.CanSeePlayer && Senses.TimeSinceSeen > Stats.LoseSightTime))
            {
                Fsm.SetState(Brain.Idle);
                return;
            }

            if (Stats.AttackType == EnemyAttackType.ContactOnly)
                return;

            bool inReach = Senses.HorizontalDistance <= Brain.AttackReach && Senses.VerticalDistance <= Stats.AttackHeight;
            if (inReach && Senses.CanSeePlayer && Time.time >= Brain.NextAttackTime && Motor.IsGrounded)
                Fsm.SetState(Brain.Windup);
        }

        protected override void OnFixedTick()
        {
            if (!Senses.HasPlayer)
                return;

            int dir = Senses.PlayerDirection;
            Motor.Face(dir);

            // Right under or over the player, or against a wall: wait instead of jittering.
            if (Senses.HorizontalDistance < 0.3f || Motor.WallAhead)
                Motor.Stop();
            else
                Motor.Move(dir, Stats.ChaseSpeed);
        }
    }
}
