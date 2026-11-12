using Enemies;
using UnityEngine;

namespace Bosses
{
    // Base for the states of the Mother and the Father: gives each state its parent, the boss, the stats and the
    // body. Leaving any state puts every move effect back to rest (ParentActor.StopEffects), so states never clean
    // up after themselves.
    public abstract class ParentState<T> : TimedState where T : ParentActor
    {
        protected readonly T Actor;

        protected ParentState(T actor) => Actor = actor;

        protected ParentsBoss Boss => Actor.Boss;
        protected ParentsStats Stats => Actor.Stats;
        protected StateMachine Fsm => Actor.Fsm;
        protected Rigidbody2D Body => Actor.Body;

        protected override void AfterExit() => Actor.StopEffects();
    }

    // Stands still where it is: before the fight and after a lost attempt.
    public class ParentHoldState : ParentState<ParentActor>
    {
        public ParentHoldState(ParentActor actor) : base(actor) { }
    }

    // Away: hidden and untouchable, free for the director's next move.
    public class ParentAwayState : ParentState<ParentActor>
    {
        public ParentAwayState(ParentActor actor) : base(actor) { }

        protected override void OnEnter() => Actor.SetPresent(false);
    }

    // Leaving: fades away over Vanish Time (it no longer hurts, and is not free yet), then is away.
    public class ParentVanishState : ParentState<ParentActor>
    {
        public ParentVanishState(ParentActor actor) : base(actor) { }

        protected override void OnEnter()
        {
            Actor.IsRecovering = true;
            Actor.SetHurting(false);
        }

        protected override void OnTick()
        {
            float k = Stats.VanishTime > 0f ? Mathf.Clamp01(TimeInState / Stats.VanishTime) : 1f;
            Actor.SetFade(1f - k);
            if (k >= 1f)
                Fsm.SetState(Actor.Away);
        }
    }

    // The dash, for either parent, Count times in a row. It appears on the floor at Side of the room (after Delay
    // seconds, still away), crouches and shakes (Dash Tell, the warning), then dashes across the whole floor to the
    // other side. After the last dash it stands open there (Dash Recover) and leaves; in a chain it only pauses
    // (Chain Pause) and dashes back. The player jumps over it.
    public class ParentDashState : ParentState<ParentActor>
    {
        private enum Step { Wait, Tell, Dash, Rest }

        private Step step;
        private int dashesLeft;
        private int direction;
        private float endX;

        public ParentDashState(ParentActor actor) : base(actor) { }

        // Set by ParentActor.Dash before the state is entered. Side: -1 = starts at the left, 1 = at the right.
        public int Side { get; set; } = 1;
        public int Count { get; set; } = 1;
        public float Delay { get; set; }

        protected override void OnEnter()
        {
            step = Step.Wait;
            dashesLeft = Count;
            direction = -Side;
            Actor.IsAttacking = true;
        }

        protected override void OnTick()
        {
            switch (step)
            {
                case Step.Wait:
                    if (TimeInState < Delay)
                        return;
                    Actor.Appear(new Vector2(Boss.SideX(-direction), Actor.StandY), direction);
                    StartTell();
                    return;

                case Step.Tell:
                    if (TimeInState < Stats.DashTell)
                        return;
                    step = Step.Dash;
                    RestartTimer();
                    endX = Boss.SideX(direction);
                    Actor.SetCrouch(false);
                    Actor.SetWarning(false);
                    Actor.SetTrail(true);
                    return;

                case Step.Rest:
                    bool last = dashesLeft <= 0;
                    if (TimeInState < (last ? Stats.DashRecover : Stats.ChainPause))
                        return;
                    if (last)
                    {
                        Actor.Leave();
                        return;
                    }
                    direction = -direction;
                    Actor.Motor.Face(direction);
                    StartTell();
                    return;
            }
        }

        protected override void OnFixedTick()
        {
            if (step != Step.Dash)
                return;
            float left = (endX - Body.position.x) * direction;
            if (left <= Stats.DashSpeed * Time.fixedDeltaTime)
                EndDash();
            else
                Actor.SetVelocity(Vector2.right * (direction * Stats.DashSpeed));
        }

        private void StartTell()
        {
            step = Step.Tell;
            RestartTimer();
            Actor.SetCrouch(true);
            Actor.SetWarning(true);
        }

        private void EndDash()
        {
            dashesLeft--;
            step = Step.Rest;
            RestartTimer();
            Actor.SetVelocity(Vector2.zero);
            Body.position = new Vector2(endX, Body.position.y);
            Actor.SetTrail(false);
            Boss.Shake(Stats.DashShake);
            if (dashesLeft <= 0)
            {
                Actor.IsAttacking = false;
                Actor.IsRecovering = true;
            }
        }
    }

    // The ending, for the one that took the last hit: drops to the floor and lies there.
    public class ParentFallState : ParentState<ParentActor>
    {
        public ParentFallState(ParentActor actor) : base(actor) { }

        protected override void OnEnter()
        {
            Actor.HasFallen = false;
            Actor.SetPresent(true);
            Actor.SetHurting(false);
        }

        protected override void OnFixedTick()
        {
            if (Actor.HasFallen)
                return;
            float drop = Body.position.y - Actor.StandY;
            if (drop > Stats.EndFallSpeed * Time.fixedDeltaTime)
            {
                Actor.SetVelocity(Vector2.down * Stats.EndFallSpeed);
                return;
            }
            Actor.SetVelocity(Vector2.zero);
            Body.position = new Vector2(Body.position.x, Actor.StandY);
            Actor.HasFallen = true;
            Actor.LieDown();
        }
    }

    // The ending, for the other one: if it was away it appears Walk In Distance from Target; it comes down to the
    // floor, walks to Target and stops Kneel Offset from it.
    public class ParentWalkToState : ParentState<ParentActor>
    {
        public ParentWalkToState(ParentActor actor) : base(actor) { }

        public ParentActor Target { get; set; }

        protected override void OnEnter()
        {
            Actor.HasArrived = false;
            if (!Actor.IsPresent && Target != null)
            {
                float targetX = Target.Body.position.x;
                int side = Boss.Arena != null && targetX > Boss.Arena.bounds.center.x ? -1 : 1;
                float x = Boss.ClampToArena(targetX + side * Stats.WalkInDistance);
                Actor.Appear(new Vector2(x, Actor.StandY), -side);
            }
            Actor.SetPresent(true);
            Actor.SetHurting(false);
        }

        protected override void OnFixedTick()
        {
            if (Actor.HasArrived || Target == null)
            {
                Actor.SetVelocity(Vector2.zero);
                return;
            }

            float drop = Body.position.y - Actor.StandY;
            if (drop > Stats.EndFallSpeed * Time.fixedDeltaTime)
            {
                Actor.SetVelocity(Vector2.down * Stats.EndFallSpeed);
                return;
            }
            Body.position = new Vector2(Body.position.x, Actor.StandY);

            float offset = Target.Body.position.x - Body.position.x;
            int direction = offset >= 0f ? 1 : -1;
            Actor.Motor.Face(direction);
            if (Mathf.Abs(offset) <= Stats.KneelOffset)
            {
                Actor.SetVelocity(Vector2.zero);
                Actor.HasArrived = true;
                return;
            }
            Actor.SetVelocity(Vector2.right * (direction * Stats.WalkSpeed));
        }
    }
}
