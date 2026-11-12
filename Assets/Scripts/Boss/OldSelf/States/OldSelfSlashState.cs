namespace Bosses
{
    // Up close: shakes facing the player (Slash Windup), then steps forward with the slash hitbox on for Slash Time.
    public class OldSelfSlashState : OldSelfState
    {
        private bool slashing;

        public OldSelfSlashState(OldSelfBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            slashing = false;
            Boss.StopX();
            Boss.FacePlayer();
            Boss.SetWarning(true);
        }

        protected override void OnTick()
        {
            if (!slashing)
            {
                if (TimeInState < Stats.SlashWindup * Boss.TimeScale)
                    return;
                slashing = true;
                RestartTimer();
                Boss.SetWarning(false);
                Boss.SetSlashHitbox(true);
                Boss.Shake(Stats.SlashShake);
                Boss.Punch(new UnityEngine.Vector2(1.3f, 0.8f));
                return;
            }

            if (TimeInState >= Stats.SlashTime)
                Fsm.SetState(Boss.Recover);
        }

        protected override void OnFixedTick()
        {
            if (slashing && !Boss.WallAhead)
                Body.linearVelocityX = Boss.Motor.Facing * Stats.SlashStep;
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
