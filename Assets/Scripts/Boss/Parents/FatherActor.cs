using UnityEngine;

namespace Bosses
{
    // The Father. Besides the dash both parents have, he dives: appears above the player, hangs and dives straight
    // down (FatherDiveState).
    public class FatherActor : ParentActor
    {
        public FatherDiveState Diving { get; private set; }

        protected override void BuildStates() => Diving = new FatherDiveState(this);

        protected override SoundData Voice => Stats.FatherVoice;

        // Dives count times in a row, appearing above the player before each.
        public void Dive(int count)
        {
            Diving.Count = Mathf.Max(1, count);
            Fsm.SetState(Diving, restart: true);
        }
    }
}
