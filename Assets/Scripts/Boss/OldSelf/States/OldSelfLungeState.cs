using UnityEngine;

namespace Bosses
{
    // Crouches and shakes facing the player (Lunge Windup), then dashes along the ground until it has gone Lunge
    // Distance or meets a wall or a ledge. The camera shakes as the dash ends. Its Hurtbox does the damage.
    public class OldSelfLungeState : OldSelfState
    {
        private bool dashing;
        private float startX;

        public OldSelfLungeState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            dashing = false;
            Boss.StopX();
            Boss.FacePlayer();
            Boss.SetCrouch(true);
            Boss.SetWarning(true);
        }

        protected override void OnTick()
        {
            if (!dashing)
            {
                if (TimeInState < Stats.LungeWindup * Boss.TimeScale)
                    return;
                dashing = true;
                startX = Body.position.x;
                Boss.SetCrouch(false);
                Boss.SetWarning(false);
                return;
            }

            if (Mathf.Abs(Body.position.x - startX) >= Stats.LungeDistance || Boss.WallAhead
                || Boss.Motor.LedgeAhead(Stats.LungeSpeed))
            {
                Boss.StopX();
                Boss.Shake(Stats.LungeShake);
                Fsm.SetState(Boss.Recover);
            }
        }

        protected override void OnFixedTick()
        {
            if (dashing)
                Body.linearVelocityX = Boss.Motor.Facing * Stats.LungeSpeed;
            else
                Boss.StopX();
        }

        protected override void OnExit()
        {
            Boss.StopAttackEffects();
            Boss.StopX();
        }
    }
}
