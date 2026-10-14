using System;
using System.Collections.Generic;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// Which extra conditions each flight of the campaign gets, and the part it plays in its
    /// city. A new condition opens about every ten flights early on, so a player meets
    /// something new long before plain boards wear thin. The city where a moving condition
    /// opens practises it on every climb after its first flight; later cities rotate through
    /// everything learnt so far, with plain boards in between.
    ///
    /// Each city of ten runs climb, climb, climb, rest, climb, climb, rest, climb, storm,
    /// landing. The rest slots take the night and formation flights, a change of pace (formation
    /// flights are aimed as hard as a climb, see CampaignPlanner); the storm is the
    /// city's hardest flight and, once enough conditions are known, mixes two of them.
    /// </summary>
    public sealed class RulePlan
    {
        public static readonly RulePlan Default = new RulePlan(new[]
        {
            new RuleOpening(LevelRule.Runway, 12),
            new RuleOpening(LevelRule.Wind, 22),
            new RuleOpening(LevelRule.Night, 37),
            new RuleOpening(LevelRule.Turbulence, 42),
            new RuleOpening(LevelRule.Formation, 54),
            new RuleOpening(LevelRule.Beacon, 62),
            new RuleOpening(LevelRule.Bridge, 72)
        }, 10, 81);

        public const int FlightsPerCity = 10;

        // Slots within a city of ten, counted from zero.
        private const int FormationSlot = 3;
        private const int NightSlot = 6;
        private const int StormSlot = 8;
        private const int LandingSlot = 9;

        /// <summary>Conditions that change how the plane may move, as opposed to night and formation.</summary>
        private static readonly LevelRule[] MoveRules =
            { LevelRule.Runway, LevelRule.Wind, LevelRule.Turbulence, LevelRule.Beacon, LevelRule.Bridge };

        /// <summary>Overpasses come with the board they are drawn on, so a VIP flight cannot add them.</summary>
        private static readonly LevelRule[] VipRules =
            { LevelRule.Runway, LevelRule.Night, LevelRule.Wind, LevelRule.Formation, LevelRule.Turbulence, LevelRule.Beacon };

        private readonly RuleOpening[] openings;
        private readonly int onboardingCount;
        private readonly int pairsFrom;

        /// <summary>The first flights, plain and growing a step at a time.</summary>
        public int OnboardingCount => onboardingCount;

        public RulePlan(RuleOpening[] openings, int onboardingCount, int pairsFrom)
        {
            this.openings = openings ?? throw new ArgumentNullException(nameof(openings));
            this.onboardingCount = onboardingCount;
            this.pairsFrom = pairsFrom;
        }

        public FlightRole RoleOf(int levelNumber)
        {
            if (levelNumber <= onboardingCount)
            {
                return FlightRole.Onboarding;
            }

            switch ((levelNumber - 1) % FlightsPerCity)
            {
                case FormationSlot:
                case NightSlot:
                    return FlightRole.Rest;
                case StormSlot:
                    return FlightRole.Storm;
                case LandingSlot:
                    return FlightRole.Landing;
                default:
                    return FlightRole.Climb;
            }
        }

        /// <summary>The first flight of the campaign that carries <paramref name="rule"/>, or -1.</summary>
        public int FirstLevelWith(LevelRule rule)
        {
            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i].Rule == rule)
                {
                    return openings[i].LevelNumber;
                }
            }

            return -1;
        }

        public LevelRule RulesFor(int levelNumber, bool isSpecial)
        {
            if (isSpecial || levelNumber <= onboardingCount)
            {
                return LevelRule.None;
            }

            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i].LevelNumber == levelNumber)
                {
                    return openings[i].Rule;
                }
            }

            int city = (levelNumber - 1) / FlightsPerCity;
            int slot = (levelNumber - 1) % FlightsPerCity;

            switch (RoleOf(levelNumber))
            {
                case FlightRole.Rest:
                    return RestRules(levelNumber, city, slot);
                case FlightRole.Landing:
                    return LevelRule.None;
                case FlightRole.Storm:
                    return StormRules(levelNumber, city);
                default:
                    return ClimbRules(levelNumber, city, slot);
            }
        }

        /// <summary>Formation flights rest in odd cities, night flights in even ones; late on, each brings a partner.</summary>
        private LevelRule RestRules(int levelNumber, int city, int slot)
        {
            bool isOddCity = city % 2 == 1;
            bool isPaired = levelNumber >= pairsFrom;

            if (slot == FormationSlot)
            {
                if (!isOddCity || !IsKnown(LevelRule.Formation, levelNumber))
                {
                    return LevelRule.None;
                }

                return isPaired ? LevelRule.Formation | LevelRule.Night : LevelRule.Formation;
            }

            if (isOddCity || !IsKnown(LevelRule.Night, levelNumber))
            {
                return LevelRule.None;
            }

            return isPaired ? LevelRule.Night | LevelRule.Runway : LevelRule.Night;
        }

        /// <summary>
        /// The city where a moving condition opens practises it on every climb after its first
        /// flight; elsewhere the climbs rotate through what is known, a plain board among them.
        /// </summary>
        private LevelRule ClimbRules(int levelNumber, int city, int slot)
        {
            LevelRule opening = OpenedInCityBefore(levelNumber, city);

            if (opening != LevelRule.None)
            {
                return opening;
            }

            List<LevelRule> known = KnownMoves(levelNumber);

            if (known.Count == 0)
            {
                return LevelRule.None;
            }

            int climbIndex = slot < FormationSlot ? slot : slot < NightSlot ? slot - 1 : slot - 2;
            int turn = (city * 5 + climbIndex) % (known.Count + 1);
            return turn == known.Count ? LevelRule.None : known[turn];
        }

        private LevelRule StormRules(int levelNumber, int city)
        {
            List<LevelRule> known = KnownMoves(levelNumber);

            if (known.Count == 0)
            {
                return LevelRule.None;
            }

            LevelRule opening = OpenedInCityBefore(levelNumber, city);

            if (opening != LevelRule.None)
            {
                return opening;
            }

            if (levelNumber < pairsFrom || known.Count < 2)
            {
                return known[known.Count - 1];
            }

            // Every pair of known moving conditions in turn, a different one each city.
            var pairs = new List<LevelRule>();

            for (int i = 0; i < known.Count; i++)
            {
                for (int j = i + 1; j < known.Count; j++)
                {
                    pairs.Add(known[i] | known[j]);
                }
            }

            return pairs[city % pairs.Count];
        }

        /// <summary>The moving condition that opened earlier in this same city, or none.</summary>
        private LevelRule OpenedInCityBefore(int levelNumber, int city)
        {
            for (int i = 0; i < openings.Length; i++)
            {
                int opened = openings[i].LevelNumber;

                if ((opened - 1) / FlightsPerCity == city && opened < levelNumber && IsMoveRule(openings[i].Rule))
                {
                    return openings[i].Rule;
                }
            }

            return LevelRule.None;
        }

        private List<LevelRule> KnownMoves(int levelNumber)
        {
            var known = new List<LevelRule>(MoveRules.Length);

            for (int i = 0; i < MoveRules.Length; i++)
            {
                if (IsKnown(MoveRules[i], levelNumber))
                {
                    known.Add(MoveRules[i]);
                }
            }

            return known;
        }

        private bool IsKnown(LevelRule rule, int levelNumber)
        {
            int first = FirstLevelWith(rule);
            return first > 0 && first <= levelNumber;
        }

        private static bool IsMoveRule(LevelRule rule)
        {
            return Array.IndexOf(MoveRules, rule) >= 0;
        }

        /// <summary>
        /// The mixes a chapter's VIP flight may take, best first: pairs of the conditions the
        /// player has already met, starting at a different pair each chapter so the VIP
        /// flights vary, then single conditions for a board that cannot take any pair.
        /// Formation flying pairs only with night, since the wingman would have to obey a
        /// condition meant for the player's half.
        /// </summary>
        public IReadOnlyList<LevelRule> VipMixes(int levelNumber, int chapterNumber)
        {
            var known = new List<LevelRule>(VipRules.Length);

            foreach (LevelRule rule in VipRules)
            {
                int first = FirstLevelWith(rule);

                if (first > 0 && first < levelNumber)
                {
                    known.Add(rule);
                }
            }

            var pairs = new List<LevelRule>();

            for (int i = 0; i < known.Count; i++)
            {
                for (int j = i + 1; j < known.Count; j++)
                {
                    LevelRule pair = known[i] | known[j];

                    if (CanFly(pair))
                    {
                        pairs.Add(pair);
                    }
                }
            }

            var mixes = new List<LevelRule>(pairs.Count + known.Count);

            for (int i = 0; i < pairs.Count; i++)
            {
                mixes.Add(pairs[(i + chapterNumber) % pairs.Count]);
            }

            // Newest first among the singles: the condition just learnt is the one worth showing off.
            for (int i = known.Count - 1; i >= 0; i--)
            {
                mixes.Add(known[i]);
            }

            return mixes;
        }

        private static bool CanFly(LevelRule mix)
        {
            bool hasFormation = (mix & LevelRule.Formation) != 0;
            return !hasFormation || (mix & ~(LevelRule.Formation | LevelRule.Night)) == 0;
        }
    }
}
