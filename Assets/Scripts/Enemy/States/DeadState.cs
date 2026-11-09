using UnityEngine;

namespace Enemies
{
    // Final state: stops all physics (so the body no longer hurts or blocks), flashes, shrinks and fades,
    // then destroys the enemy.
    public class DeadState : EnemyState
    {
        private Vector3 startScale;

        public DeadState(EnemyBrain brain) : base(brain) { }

        protected override void OnEnter()
        {
            Motor.Body.linearVelocity = Vector2.zero;
            Motor.Body.simulated = false;
            startScale = Brain.transform.localScale;
            if (Brain.Flash != null)
                Brain.Flash.Hit();
            Brain.RaiseDied();
        }

        protected override void OnTick()
        {
            float t = Mathf.Clamp01(TimeInState / Stats.DeathTime);
            Brain.transform.localScale = startScale * (1f - 0.5f * t);
            if (Brain.Flash != null)
                Brain.Flash.Fade = 1f - t;

            if (t >= 1f)
                Object.Destroy(Brain.gameObject);
        }
    }
}
