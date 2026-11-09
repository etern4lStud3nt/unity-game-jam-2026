using UnityEngine;

namespace Enemies
{
    // A flyer's attack: one straight, fast line along the locked aim. Ends after DiveDistance, at the room's edge,
    // or when it hits ground or a wall; then Recover leaves it stuck there for a moment (the player's opening).
    // The body's Hurtbox does the damage; an Attack Hitbox, if assigned, is switched on for the dive as well.
    public class DiveState : EnemyState
    {
        private readonly FlyerBrain flyer;
        private Vector2 direction;
        private Vector2 start;

        public DiveState(FlyerBrain brain) : base(brain) => flyer = brain;

        protected override void OnEnter()
        {
            direction = flyer.DiveDirection.normalized;
            start = Motor.Body.position;
            Brain.SetAttackHitbox(true);
        }

        protected override void OnFixedTick()
        {
            FlyerStats stats = flyer.FlyerStats;
            float step = stats.DiveSpeed * Time.fixedDeltaTime;

            if (flyer.Flyer.Blocked(direction, step + 0.05f))
            {
                Fsm.SetState(Brain.Recover); // stuck in the ground or a wall
                return;
            }

            Vector2 next = Motor.Body.position + direction * step;
            if (Vector2.Distance(start, next) > stats.DiveDistance || !flyer.InTerritory(next))
            {
                Fsm.SetState(Brain.Recover);
                return;
            }

            flyer.Flyer.Fly(direction * stats.DiveSpeed);
        }

        protected override void OnExit()
        {
            Brain.SetAttackHitbox(false);
            Motor.Stop();
        }
    }
}
