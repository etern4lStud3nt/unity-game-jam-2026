using Enemies;
using UnityEngine;

namespace Bosses
{
    // Base for the Old Self's states: gives each state the boss, its stats and its motor. Leaving any state puts
    // every move effect back to rest (Boss.StopAttackEffects), so states never clean up after themselves.
    public abstract class OldSelfState : TimedState
    {
        protected readonly OldSelfBoss Boss;

        protected OldSelfState(OldSelfBoss boss) => Boss = boss;

        protected OldSelfStats Stats => Boss.Stats;
        protected StateMachine Fsm => Boss.Fsm;
        protected EnemyMotor Motor => Boss.Motor;
        protected Rigidbody2D Body => Boss.Body;

        protected override void AfterExit() => Boss.StopAttackEffects();
    }
}
