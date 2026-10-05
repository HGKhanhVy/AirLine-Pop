using System;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Shape of the difficulty curve the campaign is built onto.
    ///
    /// The curve is deliberately not monotonic. Climbing for thirty levels without
    /// relief exhausts players and they stop, so each block of ten runs
    /// climb, climb, climb, rest, climb, climb, rest, climb, climb, peak.
    /// A chapter therefore holds six rests and three peaks.
    ///
    /// Chapters also overlap: a new chapter opens below the ceiling of the one before
    /// it, so arriving somewhere new feels like relief rather than a wall.
    /// </summary>
    public readonly struct ChapterLayout
    {
        /// <summary>
        /// Offsets applied on top of the rising trend, as fractions of one chapter's
        /// span. Index 3 and 6 are the rests, index 9 the peak that closes each block.
        /// </summary>
        public static readonly double[] DefaultRhythm =
        {
            0.00, 0.06, 0.12, -0.30, 0.10, 0.16, -0.24, 0.20, 0.26, 0.40
        };

        /// <summary>The MVP campaign of GDD 18.1: ten chapters of thirty.</summary>
        public static readonly ChapterLayout Default = new ChapterLayout(10, 30, 0.2, DefaultRhythm, 10, 3, 10);

        /// <summary>
        /// The whole route map: 41 cities of ten flights, stored as chapters of thirty, so
        /// the last chapter holds only twenty. A block of ten is one city, which puts the
        /// rhythm's peak on the flight that lands there.
        /// </summary>
        public static readonly ChapterLayout FullRoute = new ChapterLayout(14, 30, 0.2, DefaultRhythm, 10, 3, 10, 410, 1.6);

        private readonly double[] rhythm;
        private readonly double lowestTrend;
        private readonly double highestTrend;

        public int ChapterCount { get; }

        public int LevelsPerChapter { get; }

        /// <summary>
        /// How far a chapter reaches back below the previous chapter's ceiling, as a
        /// fraction of a chapter span. Zero would butt the chapters together and lose
        /// the moment of relief on arrival.
        /// </summary>
        public double Overlap { get; }

        /// <summary>Every slot of every chapter, unless a cap stops the last chapter short.</summary>
        public int TotalLevels { get; }
        public int OnboardingLevelCount { get; }
        public int OnboardingMinCells { get; }
        public int OnboardingMaxCells { get; }

        /// <summary>
        /// Bends the curve: above 1 it climbs gently through the opening chapters and
        /// steepens later, which suits a pool where small boards are scarce and difficulty
        /// is ranked by percentile.
        /// </summary>
        public double CurvePower { get; }

        public ChapterLayout(int chapterCount, int levelsPerChapter, double overlap, double[] rhythm,
            int onboardingLevelCount = 0, int onboardingMinCells = 3, int onboardingMaxCells = 10, int levelCap = 0,
            double curvePower = 1.0)
        {
            if (curvePower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(curvePower), curvePower, "The curve power must be positive.");
            }

            CurvePower = curvePower;
            if (chapterCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chapterCount), chapterCount, "Need at least one chapter.");
            }

            if (levelsPerChapter <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelsPerChapter), levelsPerChapter, "Need at least one level.");
            }

            if (rhythm == null || rhythm.Length == 0)
            {
                throw new ArgumentException("Rhythm must hold at least one offset.", nameof(rhythm));
            }

            ChapterCount = chapterCount;
            LevelsPerChapter = levelsPerChapter;
            int slots = chapterCount * levelsPerChapter;

            if (levelCap < 0 || levelCap > slots || (levelCap > 0 && levelCap <= slots - levelsPerChapter))
            {
                throw new ArgumentOutOfRangeException(nameof(levelCap), levelCap, "A cap may only shorten the last chapter.");
            }

            TotalLevels = levelCap > 0 ? levelCap : slots;
            if (onboardingLevelCount < 0 || onboardingLevelCount > chapterCount * levelsPerChapter ||
                onboardingMinCells < 1 || onboardingMaxCells < onboardingMinCells)
            {
                throw new ArgumentOutOfRangeException(nameof(onboardingLevelCount));
            }

            OnboardingLevelCount = onboardingLevelCount;
            OnboardingMinCells = onboardingMinCells;
            OnboardingMaxCells = onboardingMaxCells;
            Overlap = overlap;
            this.rhythm = rhythm;

            double lowestOffset = 0;
            double highestOffset = 0;

            for (int i = 0; i < rhythm.Length; i++)
            {
                if (rhythm[i] < lowestOffset)
                {
                    lowestOffset = rhythm[i];
                }

                if (rhythm[i] > highestOffset)
                {
                    highestOffset = rhythm[i];
                }
            }

            // The trend is inset far enough that the rhythm can swing to its extremes
            // without leaving the range. Clamping instead would make every late slot ask
            // for the very hardest level, they would all compete for the same handful of
            // candidates, and the curve would jitter exactly where it should be steadiest.
            double nominalSpan = 1.0 / chapterCount;
            lowestTrend = -lowestOffset * nominalSpan;
            highestTrend = 1.0 - highestOffset * nominalSpan;
        }

        /// <summary>
        /// Where on the 0 to 1 difficulty axis the level at <paramref name="slot"/> of
        /// <paramref name="chapterIndex"/> should sit. Both indices are zero based.
        /// </summary>
        public double GetTarget(int chapterIndex, int slot)
        {
            double span = (highestTrend - lowestTrend) / ChapterCount;
            double end = lowestTrend + (chapterIndex + 1) * span;
            double start = lowestTrend + chapterIndex * span - (chapterIndex == 0 ? 0 : Overlap * span);

            double progress = LevelsPerChapter == 1 ? 0 : slot / (double)(LevelsPerChapter - 1);
            double trend = start + (end - start) * progress;
            double offset = rhythm[slot % rhythm.Length] * span;
            double target = trend + offset;
            target = target < 0 ? 0 : target > 1 ? 1 : target;
            return CurvePower == 1.0 ? target : Math.Pow(target, CurvePower);
        }

        /// <summary>
        /// The largest board the onboarding level at <paramref name="levelIndex"/> may use:
        /// it grows from the minimum to the maximum, so the very first flight is the
        /// smallest and each one after it a step bigger.
        /// </summary>
        public int OnboardingCellLimit(int levelIndex)
        {
            if (OnboardingLevelCount <= 1)
            {
                return OnboardingMaxCells;
            }

            double step = (OnboardingMaxCells - OnboardingMinCells) / (double)(OnboardingLevelCount - 1);
            return OnboardingMinCells + (int)Math.Round(levelIndex * step);
        }

        /// <summary>True when this slot is one of the rests, which the tests lean on.</summary>
        public bool IsRest(int slot)
        {
            return rhythm[slot % rhythm.Length] < 0;
        }
    }
}
