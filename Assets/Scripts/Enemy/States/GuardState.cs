using UnityEngine;

namespace Enemies
{
    // A guard's calm state: walks back to its post if it is away, then stands facing the way it was placed,
    // glancing behind every now and then. Starts the chase when it sees the player inside its leash, and just
    // watches them when they are visible but outside it.
    public class GuardState : EnemyState
    {
        private const float HomeTolerance = 0.1f;
        // Keeps watching this long after losing sight, so it does not flicker away the moment the player ducks.
        private const float WatchGrace = 0.5f;

        private readonly GuardBrain guard;
        private bool atPost;
        private float atPostSince;

        public GuardState(GuardBrain brain) : base(brain) => guard = brain;

        protected override void OnEnter()
        {
            atPost = false;
        }

        protected override void OnTick()
        {
            if (Senses.CanSeePlayer && Brain.PlayerInLeash)
                Fsm.SetState(Brain.Chase);
        }

        protected override void OnFixedTick()
        {
            float toHome = Brain.Home.x - Brain.transform.position.x;
            if (Mathf.Abs(toHome) > HomeTolerance)
            {
                atPost = false;
                int dir = toHome > 0f ? 1 : -1;
                Motor.Face(dir);
                if (Motor.Blocked(Stats.PatrolSpeed))
                    Motor.Stop();
                else
                    Motor.Move(dir, Stats.PatrolSpeed);
                return;
            }

            Motor.Stop();
            if (!atPost)
            {
                atPost = true;
                atPostSince = Time.time;
            }

            // Player in sight but outside the leash: keep staring at them instead of glancing away.
            // The look-back cycle restarts once they are gone.
            if (Senses.HasPlayer && Senses.TimeSinceSeen < WatchGrace)
            {
                Motor.Face(Senses.PlayerDirection);
                atPostSince = Time.time;
                return;
            }

            Motor.Face(LookingBack() ? -Brain.HomeFacing : Brain.HomeFacing);
        }

        // Cycles: LookBackInterval facing the post, then LookBackTime facing the other way.
        private bool LookingBack()
        {
            GuardStats stats = guard.GuardStats;
            if (stats == null || stats.LookBackInterval <= 0f || stats.LookBackTime <= 0f)
                return false;
            float cycle = stats.LookBackInterval + stats.LookBackTime;
            return (Time.time - atPostSince) % cycle >= stats.LookBackInterval;
        }
    }
}
