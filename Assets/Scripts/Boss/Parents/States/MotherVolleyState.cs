using System.Collections.Generic;
using UnityEngine;

namespace Bosses
{
    // The Mother's slipper volley. She appears on the floor at Side of the room, crouches and shakes (Throw Tell, the
    // warning), then throws Waves waves. A wave is a stack of slippers, one per lane (Lane Heights) except for Gap
    // Lanes lanes next to each other left empty: the gap the player escapes through (standing under it, jumping over
    // it or jumping through it). The slippers first hang in front of her (Wave Tell) so the gap can be read, then fly
    // flat across the arena. The gap moves every wave. After the last wave she bends down for new slippers (Reload
    // Time, the opening) and leaves.
    public class MotherVolleyState : ParentState<MotherActor>
    {
        private enum Step { Tell, Show, Wait, Reload }

        private readonly List<ParentSlipper> held = new();
        private Step step;
        private int wavesLeft;
        private int facing;
        private int lastGap = -1;

        public MotherVolleyState(MotherActor actor) : base(actor) { }

        // Set by MotherActor.Volley before the state is entered. Side: -1 = the left of the room, 1 = the right.
        public int Side { get; set; } = 1;
        public int Waves { get; set; } = 1;

        protected override void OnEnter()
        {
            step = Step.Tell;
            wavesLeft = Waves;
            facing = -Side;
            held.Clear();
            Actor.IsAttacking = true;
            Actor.Appear(new Vector2(Boss.SideX(Side), Actor.StandY), facing);
            Actor.SetCrouch(true);
            Actor.SetWarning(true);
        }

        protected override void OnTick()
        {
            switch (step)
            {
                case Step.Tell:
                    if (TimeInState < Stats.ThrowTell)
                        return;
                    Actor.SetCrouch(false);
                    Actor.SetWarning(false);
                    ShowWave();
                    return;

                case Step.Show:
                    if (TimeInState < Stats.WaveTell)
                        return;
                    Throw();
                    return;

                case Step.Wait:
                    if (TimeInState < Stats.WaveInterval)
                        return;
                    ShowWave();
                    return;

                case Step.Reload:
                    if (TimeInState >= Stats.ReloadTime)
                        Actor.Leave();
                    return;
            }
        }

        // Slippers left hanging (she was interrupted) never fly.
        protected override void OnExit()
        {
            foreach (ParentSlipper slipper in held)
                slipper.PutAway();
            held.Clear();
        }

        private void ShowWave()
        {
            step = Step.Show;
            RestartTimer();

            float[] lanes = Stats.LaneHeights;
            int gapSize = Mathf.Clamp(Stats.GapLanes, 1, Mathf.Max(1, lanes.Length - 1));
            int gap = PickGap(lanes.Length - gapSize + 1);
            float x = Body.position.x + facing * (Actor.HalfWidth + Stats.SlipperSpawnAhead);

            for (int lane = 0; lane < lanes.Length; lane++)
            {
                if (lane >= gap && lane < gap + gapSize)
                    continue;
                ParentSlipper slipper = Boss.TakeSlipper();
                if (slipper == null)
                    break;
                slipper.Hold(new Vector2(x, Boss.FloorY + lanes[lane]), facing);
                held.Add(slipper);
            }
        }

        // A gap position out of choices, never where the last wave's gap was (when there is a choice).
        private int PickGap(int choices)
        {
            int gap = Random.Range(0, choices);
            if (choices > 1 && gap == lastGap)
                gap = (gap + Random.Range(1, choices)) % choices;
            return lastGap = gap;
        }

        private void Throw()
        {
            foreach (ParentSlipper slipper in held)
                slipper.Launch();
            held.Clear();
            Actor.Punch(Stats.ThrowPunch);
            Actor.PlaySound(Stats.ThrowSound);
            Boss.Shake(Stats.ThrowShake);
            Actor.WavesLaunched++;
            wavesLeft--;
            RestartTimer();

            if (wavesLeft > 0)
            {
                step = Step.Wait;
                return;
            }
            step = Step.Reload;
            Actor.IsAttacking = false;
            Actor.IsRecovering = true;
            Actor.SetCrouch(true);
        }
    }
}
