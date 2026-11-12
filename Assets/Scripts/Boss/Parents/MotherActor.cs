namespace Bosses
{
    // The Mother. Besides the dash both parents have, she throws waves of slippers from a side of the room
    // (MotherVolleyState).
    public class MotherActor : ParentActor
    {
        public MotherVolleyState Throwing { get; private set; }

        // Waves that have left her hand in the volley running now (the combo times the dive from the first).
        public int WavesLaunched { get; set; }

        protected override void BuildStates() => Throwing = new MotherVolleyState(this);

        // Appears at the side of the room (side: -1 left, 1 right), throws waves waves of slippers, then reloads.
        public void Volley(int side, int waves)
        {
            Throwing.Side = side >= 0 ? 1 : -1;
            Throwing.Waves = waves < 1 ? 1 : waves;
            WavesLaunched = 0;
            Fsm.SetState(Throwing, restart: true);
        }
    }
}
