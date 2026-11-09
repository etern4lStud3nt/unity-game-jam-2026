using UnityEngine;

namespace Enemies
{
    // The tell before the lunge or swing: stops, turns to the player once and shakes. The direction is locked
    // from the first frame, so jumping over or behind the enemy during the wind-up makes it strike thin air.
    public class WindupState : EnemyState
    {
        public WindupState(EnemyBrain brain) : base(brain) { }

        protected override void OnEnter()
        {
            Motor.Stop();
            Motor.Face(Senses.PlayerDirection);
            if (Brain.Flash != null)
                Brain.Flash.SetWarning(true);

            // The cooldown counts from the end of this attack, even if a hit cancels it.
            Brain.NextAttackTime = Time.time + Stats.WindupTime + Stats.AttackActiveTime + Stats.RecoverTime + Stats.AttackCooldown;
        }

        protected override void OnTick()
        {
            if (TimeInState >= Stats.WindupTime)
                Fsm.SetState(Stats.AttackType == EnemyAttackType.WindupSwing ? Brain.Swing : (IState)Brain.Lunge);
        }

        protected override void OnFixedTick() => Motor.Stop();

        protected override void OnExit()
        {
            if (Brain.Flash != null)
                Brain.Flash.SetWarning(false);
        }
    }
}
