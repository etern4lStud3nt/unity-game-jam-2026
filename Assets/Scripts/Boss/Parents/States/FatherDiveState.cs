using UnityEngine;

namespace Bosses
{
    // The Father's dive, Count times in a row. For each: he appears Dive Height above the player,
    // follows their height while shaking (Dive Tell, the warning; horizontal position stays locked), then dives down
    // to the floor, through any platform, and stays stuck in it. The last Stuck is the opening (Stuck Time), then he
    // leaves; in a chain he is stuck only briefly (Chain Pause) and appears above the player again.
    public class FatherDiveState : ParentState<FatherActor>
    {
        private enum Step { Hang, Dive, Stuck }

        private Step step;
        private int divesLeft;
        private float hangX;

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
                    Actor.HideDiveLine();
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
            if (step == Step.Hang)
            {
                // Tracks only the player's height during the warning, leaving room to dodge sideways.
                float k = Stats.DiveTell > 0f ? Mathf.Clamp01(TimeInState / Stats.DiveTell) : 1f;
                Vector2 at = HangPoint() + Vector2.up * (Stats.HangPull * (1f - (1f - k) * (1f - k)));
                Body.MovePosition(at);
                Actor.ShowDiveLine(at, Vector2.down, at.y - Boss.FloorY, k);
                return;
            }
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
            hangX = Boss.ClampToArena(Boss.PlayerPoint.x);
            Actor.Appear(HangPoint(), Actor.PlayerSide);
            Actor.SetWarning(true);
        }

        private Vector2 HangPoint()
        {
            // Keep the same gap even near the arena's top; a ceiling clamp could put him on the player.
            float y = Boss.PlayerPoint.y + Stats.DiveHeight;
            return new Vector2(hangX, y);
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
            Actor.PlaySound(Stats.ImpactSound);
            Boss.Shake(Stats.ImpactShake);
            if (divesLeft <= 0)
            {
                Actor.IsAttacking = false;
                Actor.IsRecovering = true;
            }
        }
    }
}
