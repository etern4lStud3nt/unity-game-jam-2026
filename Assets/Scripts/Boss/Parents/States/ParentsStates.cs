using Enemies;
using UnityEngine;

namespace Bosses
{
    // Base for the director's states: gives each state the boss, its stats and the two parents.
    public abstract class ParentsState : TimedState
    {
        protected readonly ParentsBoss Boss;

        protected ParentsState(ParentsBoss boss) => Boss = boss;

        protected ParentsStats Stats => Boss.Stats;
        protected FatherActor Father => Boss.Father;
        protected MotherActor Mother => Boss.Mother;
    }

    // Before the fight (and after a lost attempt): both stand where they were placed. The intro's Yarn signal starts
    // the fight; once the intro has run (or without a signal), so does the player being in the arena.
    public class ParentsWaitState : ParentsState
    {
        public ParentsWaitState(ParentsBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            Father.Hold();
            Mother.Hold();
        }

        protected override void OnTick() => Boss.StartOnSight();
    }

    // The fight: the director's loop. Both leave at once; from then on, whenever nobody is attacking any more, it
    // waits Idle Between, chooses the next move (the combo, the Mother's volley with the Father's dive in the middle,
    // with Combo Chance and never twice in a row; otherwise one of the solos by weight, never the same twice in a
    // row) and starts it as soon as the parents it needs are free. A parent still in its opening is not free, so
    // one may already appear while the other still stands open. Dashes and volleys start at the side of the room
    // that is farther from the player.
    public class ParentsFightState : ParentsState
    {
        private enum Move { None, Dash, Dive, Volley, CrossDive, Combo }

        private static readonly Move[] Solos = { Move.Dash, Move.Dive, Move.Volley, Move.CrossDive };

        private Move last;
        private Move next;
        private ParentActor dasher;
        private float quietSince;
        // The combo's dive: waiting for the Mother's first wave, then for diveAt.
        private bool comboDive;
        private float diveAt;

        public ParentsFightState(ParentsBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            last = Move.None;
            next = Move.None;
            comboDive = false;
            quietSince = Time.time;
            Father.Leave();
            Mother.Leave();
        }

        protected override void OnTick()
        {
            if (comboDive)
                TickComboDive();

            if (next == Move.None)
            {
                if (comboDive || Father.IsAttacking || Mother.IsAttacking)
                {
                    quietSince = Time.time;
                    return;
                }
                if (Time.time - quietSince < Boss.IdleBetween)
                    return;
                next = Choose();
                // The dash goes to whichever is free now (either, when both are).
                if (next == Move.Dash)
                    dasher = !Father.IsFree ? Mother : !Mother.IsFree ? Father : Random.value < 0.5f ? Father : (ParentActor)Mother;
            }

            if (!CanStart(next))
                return;
            Begin(next);
            last = next;
            next = Move.None;
            quietSince = Time.time;
        }

        private Move Choose()
        {
            if (last != Move.Combo && Random.value < Boss.ComboChance)
                return Move.Combo;
            float total = 0f;
            foreach (Move solo in Solos)
                total += Weight(solo);
            if (total <= 0f)
                return Move.Dash;
            float pick = Random.value * total;
            foreach (Move solo in Solos)
            {
                float weight = Weight(solo);
                pick -= weight;
                if (weight > 0f && pick <= 0f)
                    return solo;
            }
            return Move.Dash;
        }

        // A solo's weight; the one that just ran counts as 0.
        private float Weight(Move solo)
        {
            if (solo == last)
                return 0f;
            switch (solo)
            {
                case Move.Dash: return Stats.DashWeight;
                case Move.Dive: return Stats.DiveWeight;
                case Move.Volley: return Stats.VolleyWeight;
                default: return Boss.CrossDiveWeight;
            }
        }

        private bool CanStart(Move move)
        {
            switch (move)
            {
                case Move.Dash: return dasher.IsFree;
                case Move.Dive: return Father.IsFree;
                case Move.Volley: return Mother.IsFree;
                default: return Father.IsFree && Mother.IsFree;
            }
        }

        private void Begin(Move move)
        {
            int far = Boss.FarSide;
            switch (move)
            {
                case Move.Dash:
                    dasher.Dash(far, Boss.DashChain);
                    break;
                case Move.Dive:
                    Father.Dive(Boss.DiveChain);
                    break;
                case Move.Volley:
                    Mother.Volley(far, Boss.WaveCount);
                    break;
                case Move.CrossDive:
                    // Both aim at the same spot (CrossDiveAim), so their paths cross there; which one goes first,
                    // and from which side, is random.
                    bool fatherFirst = Random.value < 0.5f;
                    ParentActor first = fatherFirst ? Father : Mother;
                    ParentActor second = fatherFirst ? Mother : (ParentActor)Father;
                    int side = Random.value < 0.5f ? 1 : -1;
                    Vector2 aim = CrossDiveAim();
                    first.CrossDive(side, aim);
                    second.CrossDive(-side, aim, Stats.CrossDiveStagger);
                    break;
                case Move.Combo:
                    Mother.Volley(far, Boss.WaveCount);
                    comboDive = true;
                    diveAt = -1f;
                    break;
            }
        }

        // Where the cross dive aims: ahead of the player by their running speed times the time until the first dive
        // reaches them (the warning plus the flight from the hang point), scaled by Cross Dive Prediction. Only
        // sideways: a jump does not move the aim. Locked when the move begins.
        private Vector2 CrossDiveAim()
        {
            Vector2 aim = Boss.PlayerPoint;
            float rise = Stats.CrossDiveHeight - (aim.y - Boss.FloorY);
            float flight = new Vector2(Stats.CrossDiveSideOffset, Mathf.Max(0f, rise)).magnitude / Stats.CrossDiveSpeed;
            aim.x += Boss.PlayerVelocity.x * (Stats.CrossDiveTell + flight) * Stats.CrossDivePrediction;
            aim.x = Boss.ClampToArena(aim.x);
            return aim;
        }

        // The Father appears a random moment (Combo Dive Delay) after the Mother's first wave has left, so the two
        // never strike together. If she was stopped before throwing, he dives right away.
        private void TickComboDive()
        {
            if (diveAt < 0f)
            {
                bool thrown = Mother.WavesLaunched > 0;
                if (!thrown && Mother.IsAttacking)
                    return;
                diveAt = Time.time + (thrown ? Random.Range(Stats.ComboDiveDelay.x, Stats.ComboDiveDelay.y) : 0f);
            }
            if (Time.time < diveAt)
                return;
            comboDive = false;
            Father.Dive(Boss.DiveChain);
        }
    }

    // Defeated. Everything stops hurting at once and the slippers disappear; BossDefeated goes out. The one that
    // took the last hit drops to the floor and lies down, the other comes and walks over. Then their last words (the
    // Yarn node Last Words Node), the reward appears and both fade out. Final: a respawn does not bring them back.
    public class ParentsDeadState : ParentsState
    {
        private enum Step { Gather, Words, Fade, Done }

        private Step step;
        private ParentActor fallen;
        private ParentActor other;
        private int dialoguesBefore;

        public ParentsDeadState(ParentsBoss boss) : base(boss) { }

        protected override void OnEnter()
        {
            step = Step.Gather;
            Boss.PutAwaySlippers();
            Boss.PublishDefeated();

            fallen = Boss.LastHit != null ? Boss.LastHit : Father;
            other = fallen == Father ? Mother : Father;
            fallen.FallDown();
            other.WalkTo(fallen);
        }

        protected override void OnTick()
        {
            switch (step)
            {
                case Step.Gather:
                    bool gathered = fallen.HasFallen && other.HasArrived;
                    if (!gathered && TimeInState < Stats.EndingStepTimeout)
                        return;
                    RestartTimer();
                    dialoguesBefore = Boss.DialoguesEnded;
                    bool speaking = !string.IsNullOrEmpty(Stats.LastWordsNode) && DialogueBridge.Run(Stats.LastWordsNode);
                    if (speaking)
                        step = Step.Words;
                    else
                        StartFade();
                    return;

                case Step.Words:
                    if (Boss.DialoguesEnded > dialoguesBefore)
                        StartFade();
                    return;

                case Step.Fade:
                    float k = Stats.EndFadeTime > 0f ? Mathf.Clamp01(TimeInState / Stats.EndFadeTime) : 1f;
                    Father.SetFade(1f - k);
                    Mother.SetFade(1f - k);
                    if (k < 1f)
                        return;
                    step = Step.Done;
                    Father.gameObject.SetActive(false);
                    Mother.gameObject.SetActive(false);
                    return;
            }
        }

        private void StartFade()
        {
            step = Step.Fade;
            RestartTimer();
            Boss.SetCollidersEnabled(false);
            // The reward appears as they start to go.
            Boss.FadeOut(0f, 0f);
        }
    }
}
