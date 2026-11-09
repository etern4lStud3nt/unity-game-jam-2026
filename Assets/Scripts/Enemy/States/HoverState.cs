using UnityEngine;

namespace Enemies
{
    // A flyer's calm state: bobs and drifts around home (flying back there first if it is away), staying inside
    // its room. Starts the approach when it sees the player inside the room.
    public class HoverState : EnemyState
    {
        private readonly FlyerBrain flyer;
        private readonly float phase;

        public HoverState(FlyerBrain brain) : base(brain)
        {
            flyer = brain;
            phase = Random.value * 10f; // so several flyers do not bob in sync
        }

        protected override void OnTick()
        {
            if (Senses.CanSeePlayer && Brain.PlayerInLeash)
                Fsm.SetState(Brain.Chase);
        }

        protected override void OnFixedTick()
        {
            FlyerStats stats = flyer.FlyerStats;
            float t = Time.time + phase;
            var wander = new Vector2(Mathf.Sin(t * 0.4f) * stats.HoverDrift, Mathf.Sin(t * 1.7f) * stats.HoverBob);
            Vector2 target = flyer.ClampToTerritory(Brain.Home + wander);
            flyer.Flyer.FlyTowards(target, Stats.PatrolSpeed, 1.5f);
        }
    }
}
