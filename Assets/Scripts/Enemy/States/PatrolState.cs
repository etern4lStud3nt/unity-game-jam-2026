namespace Enemies
{
    // Walks back and forth, pausing and turning at walls, ledges and the patrol limit, until it sees the player.
    public class PatrolState : EnemyState
    {
        private bool pausing;
        private float pauseEnd;

        public PatrolState(EnemyBrain brain) : base(brain) { }

        protected override void OnEnter()
        {
            pausing = false;
        }

        protected override void OnTick()
        {
            if (Senses.CanSeePlayer && Brain.PlayerInLeash)
                Fsm.SetState(Brain.Chase);
        }

        protected override void OnFixedTick()
        {
            if (pausing)
            {
                Motor.Stop();
                if (UnityEngine.Time.time < pauseEnd)
                    return;
                pausing = false;
                Motor.TurnAround();
                return;
            }

            if (Motor.Blocked(Stats.PatrolSpeed) || PastPatrolLimit())
            {
                pausing = true;
                pauseEnd = UnityEngine.Time.time + Stats.TurnPause;
                Motor.Stop();
                return;
            }

            Motor.Move(Motor.Facing, Stats.PatrolSpeed);
        }

        // Walked PatrolDistance away from home in the facing direction. After a chase that ended far away,
        // this also turns it back towards home.
        private bool PastPatrolLimit()
        {
            if (Stats.PatrolDistance <= 0f)
                return false;
            float offset = Brain.transform.position.x - Brain.Home.x;
            return offset * Motor.Facing >= Stats.PatrolDistance;
        }
    }
}
