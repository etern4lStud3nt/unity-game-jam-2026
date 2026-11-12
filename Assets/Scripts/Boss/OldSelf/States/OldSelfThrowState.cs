using UnityEngine;

namespace Bosses
{
    // Shakes facing the player (Throw Windup), throws straight ahead at hand height and waits, standing, until the
    // thrown object comes back to its hand. The camera shakes a little on the catch.
    public class OldSelfThrowState : OldSelfState
    {
        private bool thrown;

        public OldSelfThrowState(OldSelfBoss boss) : base(boss) { }

        // Safety stop: the whole flight out and back (even from the far end of the arena), plus a second.
        private float MaxWait => Stats.ThrowOutTime + 2f * Stats.ThrowDistance / Stats.ThrowReturnSpeed + 1f;

        protected override void OnEnter()
        {
            thrown = false;
            Motor.Stop();
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
                Boss.Projectile.Launch(Boss.HandPoint, Vector2.right * Motor.Facing);
                Boss.Punch(Stats.ThrowPunch);
                return;
            }

            if (Boss.Projectile.IsFlying && TimeInState < MaxWait)
                return;
            Boss.Shake(Stats.CatchShake);
            Boss.Punch(Stats.CatchPunch);
            Fsm.SetState(Boss.Recover);
        }

        protected override void OnFixedTick() => Motor.Stop();
    }
}
