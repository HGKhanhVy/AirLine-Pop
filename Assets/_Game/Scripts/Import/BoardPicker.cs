using System;
using System.Collections.Generic;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// Holds the pool of boards a build draws from, each measured once by a
    /// <see cref="ThoughtfulPlayer"/>, and hands out the board that comes closest to a target
    /// once its conditions are laid on. Measuring after the conditions matters: a cloud or a
    /// gust can turn a plain board into a hard one, or settle a hard one. A board handed out
    /// is used up, so no flight repeats another.
    /// </summary>
    public sealed class BoardPicker
    {
        private readonly IReadOnlyList<ILevelVariant> variants;
        private readonly ThoughtfulPlayer player;
        private readonly int shortlistSize;
        private readonly List<Candidate> candidates = new List<Candidate>();

        public BoardPicker(IReadOnlyList<ILevelVariant> variants, ThoughtfulPlayer player, int shortlistSize = 16)
        {
            this.variants = variants ?? throw new ArgumentNullException(nameof(variants));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.shortlistSize = Math.Max(1, shortlistSize);
        }

        /// <summary>Formation first, since it rebuilds the board the others are laid onto; night last, since it only marks it.</summary>
        public static IReadOnlyList<ILevelVariant> DefaultVariants => new ILevelVariant[]
        {
            new FormationVariant(), new MarkVariant(CellKind.Straight), new MarkVariant(CellKind.Beacon), new WindVariant(),
            new RunwayVariant(), new NightVariant()
        };

        public int UnusedCount
        {
            get
            {
                int count = 0;

                foreach (Candidate candidate in candidates)
                {
                    if (!candidate.IsUsed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Add(IReadOnlyList<LevelData> pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            foreach (LevelData level in pool)
            {
                candidates.Add(new Candidate(level, player.MeasureTrials(level)));
            }

            // A stable order keeps the build deterministic whatever order the pool arrived in.
            candidates.Sort((a, b) => string.CompareOrdinal(a.Level.Id, b.Level.Id));
        }

        /// <summary>
        /// The biggest unused plain board within <paramref name="maxCells"/>, the smoothest of
        /// those on a tie, for flights that grow the board one step at a time. A pool with no
        /// board of that size moves on to the next size up.
        /// </summary>
        public bool TryPickOnboarding(int minCells, int maxCells, out PickedBoard picked)
        {
            for (int cap = maxCells; cap <= maxCells + 20; cap++)
            {
                Candidate best = null;

                foreach (Candidate candidate in candidates)
                {
                    int cells = candidate.Level.PathLength;

                    if (candidate.IsUsed || candidate.HasBridge || cells < minCells || cells > cap)
                    {
                        continue;
                    }

                    if (best == null || cells > best.Level.PathLength ||
                        (cells == best.Level.PathLength && candidate.Trials < best.Trials))
                    {
                        best = candidate;
                    }
                }

                if (best != null)
                {
                    best.IsUsed = true;
                    picked = new PickedBoard(best.Level, best.Trials);
                    return true;
                }
            }

            picked = default;
            return false;
        }

        /// <summary>
        /// Walks out from the unused board closest to the target before conditions, laying the
        /// first mix it takes in full onto each, and keeps the one closest after. Stops once
        /// enough boards have taken a mix, since night and formation flights turn down every
        /// board above a certain size. False when no board takes any mix.
        /// </summary>
        public bool TryPick(FlightTarget target, IReadOnlyList<LevelRule> mixes, bool needsBridge, string avoidSize,
            out PickedBoard picked)
        {
            List<Candidate> ranked = Rank(target, needsBridge, avoidSize);

            Candidate chosen = null;
            LevelData chosenLevel = null;
            double chosenTrials = 0;
            double chosenDistance = double.MaxValue;
            int tried = 0;

            foreach (Candidate candidate in ranked)
            {
                if (tried >= shortlistSize)
                {
                    break;
                }

                LevelData decorated = DecorateWithFirst(candidate.Level, mixes);

                if (decorated == null)
                {
                    continue;
                }

                tried++;
                double trials = ReferenceEquals(decorated, candidate.Level) ? candidate.Trials : player.MeasureTrials(decorated);
                double distance = target.DistanceTo(decorated.PathLength, trials);

                if (distance < chosenDistance)
                {
                    chosen = candidate;
                    chosenLevel = decorated;
                    chosenTrials = trials;
                    chosenDistance = distance;
                }
            }

            if (chosen == null)
            {
                picked = default;
                return false;
            }

            chosen.IsUsed = true;
            picked = new PickedBoard(chosenLevel, chosenTrials);
            return true;
        }

        /// <summary>A board whose own solution no longer flies would fail the build here rather than ship.</summary>
        public static void Verify(LevelData level)
        {
            if (!PathSequenceValidator.IsValid(level, level.Solution, new byte[level.Grid.CellCount], true))
            {
                throw new InvalidOperationException("The conditions broke the solution of " + level.Id + ".");
            }
        }

        private List<Candidate> Rank(FlightTarget target, bool needsBridge, string avoidSize)
        {
            var ranked = new List<Candidate>(candidates.Count);

            foreach (Candidate candidate in candidates)
            {
                if (candidate.IsUsed || candidate.HasBridge != needsBridge ||
                    (avoidSize != null && candidate.Level.Grid.ToString() == avoidSize))
                {
                    continue;
                }

                candidate.Distance = target.DistanceTo(candidate.Level.PathLength, candidate.Trials);
                ranked.Add(candidate);
            }

            // Ties fall back on the id, so the order never depends on the sort's stability.
            ranked.Sort((a, b) =>
            {
                int byDistance = a.Distance.CompareTo(b.Distance);
                return byDistance != 0 ? byDistance : string.CompareOrdinal(a.Level.Id, b.Level.Id);
            });

            return ranked;
        }

        /// <summary>The board with the first mix it takes in full, or null when it takes none.</summary>
        private LevelData DecorateWithFirst(LevelData level, IReadOnlyList<LevelRule> mixes)
        {
            foreach (LevelRule mix in mixes)
            {
                LevelData result = level;

                for (int i = 0; i < variants.Count; i++)
                {
                    if ((mix & variants[i].Rule) != 0)
                    {
                        result = variants[i].Apply(result);
                    }
                }

                if ((result.Rules & mix) == mix)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>A pool board with its measured wrong turns, before any condition is laid on.</summary>
        private sealed class Candidate
        {
            public LevelData Level { get; }

            public double Trials { get; }

            public bool HasBridge { get; }

            public bool IsUsed { get; set; }

            /// <summary>Scratch: distance to the flight being placed.</summary>
            public double Distance { get; set; }

            public Candidate(LevelData level, double trials)
            {
                Level = level;
                Trials = trials;
                HasBridge = level.BridgeCount > 0;
            }
        }
    }
}
