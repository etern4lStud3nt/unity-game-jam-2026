using UnityEngine;

namespace Bosses
{
    // Shakes facing the player (Throw Windup), throws straight ahead at hand height and waits, standing, until the
    // thrown object comes back to its hand. The camera shakes a little on the catch.
    public class OldSelfThrowState : OldSelfState
    {
        private const float MaxWait = 5f;

        private bool thrown;

        public OldSelfThrowState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            thrown = false;
            Boss.StopX();
            Boss.FacePlayer();
            Boss.SetWarning(true);
        }

        protected override void OnTick()
        {
            if (!thrown)
            {
                if (TimeInState < Stats.ThrowWindup * Boss.TimeScale)
                    return;
                thrown = true;
                RestartTimer();
                Boss.SetWarning(false);
                Boss.Projectile.Launch(Boss.HandPoint, Vector2.right * Boss.Motor.Facing);
                Boss.Punch(new Vector2(1.2f, 0.85f));
                return;
            }

            if (Boss.Projectile.IsFlying && TimeInState < MaxWait)
                return;
            Boss.Shake(Stats.CatchShake);
            Boss.Punch(new Vector2(0.85f, 1.15f));
            Fsm.SetState(Boss.Recover);
        }

        protected override void OnFixedTick() => Boss.StopX();

        protected override void OnExit() => Boss.StopAttackEffects();
    }
}
