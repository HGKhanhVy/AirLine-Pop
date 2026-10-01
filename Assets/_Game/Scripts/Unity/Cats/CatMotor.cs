using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Moves one cat across the floor (GDD 4, 11): turn on the spot until roughly facing
    /// the target, then walk with the walk cycle matched to ground speed, ease into idle on
    /// arrival, keep clear of other cats, and give up on a target it cannot reach in time
    /// rather than pushing through furniture or teleporting.
    /// </summary>
    public sealed class CatMotor
    {
        private const float ArriveDistance = 0.06f;
        private const float SlowDownDistance = 0.35f;
        private static readonly float[] SidestepAngles = { 0f, 50f, -50f, 85f, -85f };

        private readonly CatView view;
        private readonly CatBehaviourConfigSO config;
        private readonly WalkArea area;

        private Vector3 target;
        private float travelTime;
        private float travelBudget;
        private float currentSpeed;

        public CatMotor(CatView view, CatBehaviourConfigSO config, WalkArea area)
        {
            this.view = view;
            this.config = config;
            this.area = area;
        }

        public bool IsMoving { get; private set; }

        /// <summary>False when the last move ended by giving up rather than reaching its target.</summary>
        public bool HasArrived { get; private set; }

        public Vector3 Position => view.Root.position;

        public void MoveTo(Vector3 point)
        {
            target = point;
            target.y = view.Root.position.y;
            travelTime = 0f;

            // The give-up timer is slack on top of the walk itself, so a long walk across
            // the room is not abandoned halfway just for being long.
            travelBudget = config.GiveUpAfter + Vector3.Distance(target, view.Root.position) / config.WalkSpeed;
            HasArrived = false;
            IsMoving = true;
        }

        public void Stop()
        {
            IsMoving = false;
            currentSpeed = 0f;
            view.SetSpeed(0f);
            view.SetPlaybackRate(1f);
        }

        /// <summary>Turns towards a point without walking; true once facing it.</summary>
        public bool Face(Vector3 point, float deltaTime)
        {
            Vector3 flat = point - view.Root.position;
            flat.y = 0f;

            if (flat.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            Quaternion goal = Quaternion.LookRotation(flat);
            view.Root.rotation = Quaternion.RotateTowards(view.Root.rotation, goal, config.TurnSpeed * deltaTime);
            return Quaternion.Angle(view.Root.rotation, goal) <= config.FacingTolerance;
        }

        /// <summary>Advances one frame; returns true when the move ended, reached or abandoned.</summary>
        public bool Tick(float deltaTime, IReadOnlyList<CatMotor> others)
        {
            if (!IsMoving)
            {
                return true;
            }

            travelTime += deltaTime;
            Vector3 toTarget = target - view.Root.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance <= ArriveDistance || travelTime >= travelBudget)
            {
                HasArrived = distance <= ArriveDistance;
                Stop();
                return true;
            }

            if (!Face(target, deltaTime))
            {
                SetSpeed(0f, deltaTime);
                return false;
            }

            float desired = config.WalkSpeed * Mathf.Clamp01(distance / SlowDownDistance + 0.3f);
            Vector3 step = view.Root.forward * (desired * deltaTime);

            if (!TryFindStep(step, others, out Vector3 next))
            {
                // Wait rather than walk through; the give-up timer ends a stuck move.
                SetSpeed(0f, deltaTime);
                return false;
            }

            view.Root.position = next;
            SetSpeed(desired, deltaTime);
            return false;
        }

        /// <summary>
        /// Straight ahead if clear, else veer around whatever is in the way (a resting cat,
        /// the corner of the seats) so a cat does not stand stuck behind it.
        /// </summary>
        private bool TryFindStep(Vector3 step, IReadOnlyList<CatMotor> others, out Vector3 next)
        {
            Vector3 origin = view.Root.position;

            for (int i = 0; i < SidestepAngles.Length; i++)
            {
                next = origin + Quaternion.Euler(0f, SidestepAngles[i], 0f) * step;

                if (!IsCrowded(next, others) && (area == null || area.IsWalkable(next)))
                {
                    return true;
                }
            }

            next = origin;
            return false;
        }

        private void SetSpeed(float metresPerSecond, float deltaTime)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, metresPerSecond, config.WalkSpeed * 4f * deltaTime);
            float blend = currentSpeed / config.WalkSpeed * CatAnimatorParams.WalkSpeed;
            view.SetSpeed(blend);
            view.SetPlaybackRate(currentSpeed > 0.01f ? Mathf.Max(0.6f, currentSpeed / config.WalkClipSpeed) : 1f);
        }

        private bool IsCrowded(Vector3 next, IReadOnlyList<CatMotor> others)
        {
            float space = config.PersonalSpace * config.PersonalSpace;

            for (int i = 0; i < others.Count; i++)
            {
                CatMotor other = others[i];

                if (other == this)
                {
                    continue;
                }

                Vector3 now = other.Position - view.Root.position;
                Vector3 then = other.Position - next;
                now.y = 0f;
                then.y = 0f;

                // Only block steps that close the gap, so two cats that touch can separate.
                if (then.sqrMagnitude < space && then.sqrMagnitude < now.sqrMagnitude)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
