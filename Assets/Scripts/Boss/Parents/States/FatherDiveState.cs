using UnityEngine;

namespace Bosses
{
    // The Father's dive, Count times in a row. For each: he appears above the player (Dive Height over the floor,
    // below the ceiling), hangs there shaking (Dive Tell, the warning; his position is locked), dives straight down
    // to the floor, through any platform, and stays stuck in it. The last Stuck is the opening (Stuck Time), then he
    // leaves; in a chain he is stuck only briefly (Chain Pause) and appears above the player again.
    public class FatherDiveState : ParentState<FatherActor>
    {
        private enum Step { Hang, Dive, Stuck }

        // How far below the arena's top he stays.
        private const float CeilingClearance = 0.5f;

        private Step step;
        private int divesLeft;

        public FatherDiveState(FatherActor actor) : base(actor) { }

        // Dives in a row; set by FatherActor.Dive before the state is entered.
        public int Count { get; set; } = 1;

        protected override void OnEnter()
        {
            divesLeft = Count;
            Actor.IsAttacking = true;
            StartHang();
        }

        protected override void OnTick()
        {
            switch (step)
            {
                case Step.Hang:
                    if (TimeInState < Stats.DiveTell)
                        return;
                    step = Step.Dive;
                    RestartTimer();
                    Actor.SetWarning(false);
                    Actor.SetTrail(true);
                    return;

                case Step.Stuck:
                    bool last = divesLeft <= 0;
                    if (TimeInState < (last ? Stats.StuckTime : Stats.ChainPause))
                        return;
                    if (last)
                        Actor.Leave();
                    else
                        StartHang();
                    return;
            }
        }

        protected override void OnFixedTick()
        {
            if (step != Step.Dive)
                return;
            float drop = Body.position.y - Actor.StandY;
            if (drop <= Stats.DiveSpeed * Time.fixedDeltaTime)
                Land();
            else
                Actor.SetVelocity(Vector2.down * Stats.DiveSpeed);
        }

        private void StartHang()
        {
            step = Step.Hang;
            RestartTimer();
            float x = Boss.ClampToArena(Boss.PlayerPoint.x);
            float y = Actor.StandY + Stats.DiveHeight;
            if (Boss.Arena != null)
                y = Mathf.Min(y, Boss.Arena.bounds.max.y - Actor.HalfHeight - CeilingClearance);
            Actor.Appear(new Vector2(x, y), Actor.PlayerSide);
            Actor.SetWarning(true);
        }

        private void Land()
        {
            divesLeft--;
            step = Step.Stuck;
            RestartTimer();
            Actor.SetVelocity(Vector2.zero);
            Body.position = new Vector2(Body.position.x, Actor.StandY);
            Actor.SetTrail(false);
            Actor.Punch(Stats.ImpactPunch);
            Boss.Shake(Stats.ImpactShake);
            if (divesLeft <= 0)
            {
                Actor.IsAttacking = false;
                Actor.IsRecovering = true;
            }
        }
    }
}
