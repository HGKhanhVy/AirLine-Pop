using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Decides what one cat does next, like a real cat would: wander about, stand and look
    /// around, or pick one of its routines (groom, stretch, yawn, nap, play with the yarn,
    /// rub against the seats). A routine that belongs somewhere sends the cat there first
    /// and has it face the thing before it starts.
    ///
    /// Plain C# ticked by <see cref="CatRoster"/>; the pose itself is the animator's job.
    /// </summary>
    public sealed class CatBrain
    {
        private readonly CatView view;
        private readonly CatMotor motor;
        private readonly CatBehaviourConfigSO config;
        private readonly WalkArea area;
        private readonly IReadOnlyList<CatSpot> spots;

        private float timer;
        private Vector3 lookTarget;
        private CatRoutine routine;
        private CatSpot spot;
        private float pokeTimer;

        public CatBrain(CatView view, CatMotor motor, CatBehaviourConfigSO config, WalkArea area, IReadOnlyList<CatSpot> spots)
        {
            this.view = view;
            this.motor = motor;
            this.config = config;
            this.area = area;
            this.spots = spots ?? System.Array.Empty<CatSpot>();

            // Stagger the first decision so cats spawned together do not start together.
            timer = Random.Range(0.2f, 2f);
            Activity = CatActivity.Idle;
        }

        public CatActivity Activity { get; private set; }

        public CatView View => view;

        public CatMotor Motor => motor;

        public void Tick(float deltaTime, IReadOnlyList<CatMotor> crowd)
        {
            switch (Activity)
            {
                case CatActivity.Selected:
                    motor.Face(lookTarget, deltaTime);
                    return;

                case CatActivity.Busy:
                    motor.Tick(deltaTime, crowd);
                    return;

                case CatActivity.Wander:
                    if (motor.Tick(deltaTime, crowd))
                    {
                        BeginIdle();
                    }

                    return;

                case CatActivity.Approach:
                    TickApproach(deltaTime, crowd);
                    return;

                case CatActivity.Routine:
                    TickRoutine(deltaTime);
                    return;
            }

            timer -= deltaTime;

            if (timer <= 0f)
            {
                ChooseNext();
            }
        }

        public void Select(Vector3 viewer)
        {
            motor.Stop();
            LeaveSpot();
            lookTarget = viewer;
            view.SetPose(CatPose.Wait);
            Activity = CatActivity.Selected;
        }

        public void Deselect()
        {
            if (Activity == CatActivity.Selected)
            {
                BeginIdle();
            }
        }

        public void Occupy()
        {
            Activity = CatActivity.Busy;
        }

        public void Release(bool isStillSelected)
        {
            if (isStillSelected)
            {
                Activity = CatActivity.Selected;
                return;
            }

            BeginIdle();
        }

        private void TickApproach(float deltaTime, IReadOnlyList<CatMotor> crowd)
        {
            if (!motor.Tick(deltaTime, crowd))
            {
                return;
            }

            // A cat blocked on the way does not bat at yarn that is metres away.
            if (!motor.HasArrived)
            {
                BeginIdle();
                return;
            }

            if (motor.Face(spot.Position, deltaTime))
            {
                StartRoutine();
            }
        }

        private void TickRoutine(float deltaTime)
        {
            timer -= deltaTime;

            if (routine.PokeEvery > 0f && spot != null)
            {
                pokeTimer -= deltaTime;

                if (pokeTimer <= 0f)
                {
                    pokeTimer = routine.PokeEvery;
                    spot.Poke(motor.Position);
                }
            }

            if (timer <= 0f)
            {
                BeginIdle();
            }
        }

        private void ChooseNext()
        {
            IReadOnlyList<CatRoutine> routines = config.Routines;
            float total = config.WanderWeight + config.IdleWeight;

            for (int i = 0; i < routines.Count; i++)
            {
                if (IsAvailable(routines[i]))
                {
                    total += routines[i].Weight;
                }
            }

            float roll = Random.value * total;

            if ((roll -= config.WanderWeight) < 0f && area != null)
            {
                Activity = CatActivity.Wander;
                motor.MoveTo(area.RandomPoint());
                return;
            }

            if ((roll -= config.IdleWeight) < 0f)
            {
                BeginIdle();
                return;
            }

            for (int i = 0; i < routines.Count; i++)
            {
                if (IsAvailable(routines[i]) && (roll -= routines[i].Weight) < 0f)
                {
                    Begin(routines[i]);
                    return;
                }
            }

            BeginIdle();
        }

        private void Begin(CatRoutine next)
        {
            routine = next;
            spot = next.Spot == CatSpotKind.None ? null : PickSpot(next.Spot);

            if (spot == null)
            {
                StartRoutine();
                return;
            }

            spot.Claim(this);

            Activity = CatActivity.Approach;
            view.SetPose(CatPose.None);
            motor.MoveTo(spot.StandingPoint(motor.Position));
        }

        private void StartRoutine()
        {
            motor.Stop();
            view.SetPose(routine.Pose);
            timer = Random.Range(routine.Duration.x, routine.Duration.y);
            pokeTimer = routine.PokeEvery * 0.5f;
            Activity = CatActivity.Routine;
        }

        private void BeginIdle()
        {
            motor.Stop();
            view.SetPose(CatPose.None);
            LeaveSpot();
            routine = null;
            Activity = CatActivity.Idle;
            timer = Random.Range(config.IdleDuration.x, config.IdleDuration.y);
        }

        private void LeaveSpot()
        {
            if (spot != null)
            {
                spot.Unclaim(this);
                spot = null;
            }
        }

        private static bool IsUsable(CatSpot candidate, CatSpotKind kind)
        {
            return candidate != null && candidate.Kind == kind && candidate.IsFree;
        }

        /// <summary>A routine can run here if it needs no spot, or this room has a free one of its kind.</summary>
        private bool IsAvailable(CatRoutine candidate)
        {
            if (candidate.Weight <= 0f)
            {
                return false;
            }

            if (candidate.Spot == CatSpotKind.None)
            {
                return true;
            }

            for (int i = 0; i < spots.Count; i++)
            {
                if (IsUsable(spots[i], candidate.Spot))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A random spot of the kind; counted first so nothing is allocated.</summary>
        private CatSpot PickSpot(CatSpotKind kind)
        {
            int count = 0;

            for (int i = 0; i < spots.Count; i++)
            {
                if (IsUsable(spots[i], kind))
                {
                    count++;
                }
            }

            int pick = Random.Range(0, count);

            for (int i = 0; i < spots.Count; i++)
            {
                if (IsUsable(spots[i], kind) && pick-- == 0)
                {
                    return spots[i];
                }
            }

            return null;
        }
    }
}
