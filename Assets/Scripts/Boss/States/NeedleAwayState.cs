namespace Bosses
{
    // Put away: hidden and inactive until Grandma starts the fight.
    public class NeedleAwayState : NeedleState
    {
        public NeedleAwayState(Needle needle) : base(needle) { }

        protected override void OnEnter() => Needle.gameObject.SetActive(false);
    }
}
