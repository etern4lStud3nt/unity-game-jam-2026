using UnityEngine;

namespace Enemies
{
    // A guard: stands at its post instead of patrolling, chases the player only while they stay within the
    // leash, then walks back. Everything else (attack, getting hit, death) is the shared enemy behaviour.
    // Give it a GuardStats asset; a plain EnemyStats works too, it just never looks back.
    public class GuardBrain : EnemyBrain
    {
        public GuardStats GuardStats => Stats as GuardStats;

        protected override IState CreateIdleState() => new GuardState(this);
    }
}
