using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Which extra conditions each level of the campaign gets. Rules unlock along the
    /// route: the runway first, then night flights, wind, both runway and wind, and
    /// formation flying. Within each city of ten flights three climbing flights carry the
    /// runway or wind, about a third of the run; every other city one of its rests is a
    /// night flight or a formation flight, a change of pace rather than a harder climb. In
    /// the last stretch those rests pair up: night flights land on a runway lit in the dark,
    /// formation flights fly at night. The city's closing flight and the picture boards stay plain.
    ///
    /// Each chapter closes on a VIP flight that mixes the conditions learnt so far.
    /// </summary>
    public sealed class RulePlan
    {
        public static readonly RulePlan Default = new RulePlan(21, 101, 211, 61, 141, 281);

        // Slots within a city of ten, counted from zero.
        private const int FormationSlot = 3;
        private const int NightSlot = 6;

        private static readonly LevelRule[] VipRules = { LevelRule.Runway, LevelRule.Night, LevelRule.Wind, LevelRule.Formation };

        private readonly int runwayFrom;
        private readonly int windFrom;
        private readonly int bothFrom;
        private readonly int nightFrom;
        private readonly int formationFrom;
        private readonly int pairsFrom;

        public RulePlan(int runwayFrom, int windFrom, int bothFrom, int nightFrom = int.MaxValue, int formationFrom = int.MaxValue,
            int pairsFrom = int.MaxValue)
        {
            this.pairsFrom = pairsFrom;
            this.runwayFrom = runwayFrom;
            this.windFrom = windFrom;
            this.bothFrom = bothFrom;
            this.nightFrom = nightFrom;
            this.formationFrom = formationFrom;
        }

        /// <summary>The first level of the campaign that carries <paramref name="rule"/>.</summary>
        public int FirstLevelWith(LevelRule rule)
        {
            for (int level = 1; level <= 10000; level++)
            {
                if ((RulesFor(level, false) & rule) != 0)
                {
                    return level;
                }
            }

            return -1;
        }

        public LevelRule RulesFor(int levelNumber, bool isSpecial)
        {
            if (isSpecial || levelNumber < runwayFrom)
            {
                return LevelRule.None;
            }

            int slot = (levelNumber - 1) % 10;
            bool isOddCity = (levelNumber - 1) / 10 % 2 == 1;

            bool isPaired = levelNumber >= pairsFrom;

            if (slot == FormationSlot)
            {
                LevelRule formation = isPaired ? LevelRule.Formation | LevelRule.Night : LevelRule.Formation;
                return isOddCity && levelNumber >= formationFrom ? formation : LevelRule.None;
            }

            if (slot == NightSlot)
            {
                LevelRule night = isPaired ? LevelRule.Night | LevelRule.Runway : LevelRule.Night;
                return !isOddCity && levelNumber >= nightFrom ? night : LevelRule.None;
            }

            if (slot != 1 && slot != 4 && slot != 7)
            {
                return LevelRule.None;
            }

            if (levelNumber < windFrom)
            {
                return LevelRule.Runway;
            }

            if (levelNumber < bothFrom)
            {
                return slot == 4 ? LevelRule.Runway : LevelRule.Wind;
            }

            return slot == 1 ? LevelRule.Runway : slot == 4 ? LevelRule.Wind : LevelRule.Runway | LevelRule.Wind;
        }

        /// <summary>
        /// The mixes a chapter's VIP flight may take, best first: pairs of the conditions the
        /// player has already met, starting at a different pair each chapter so the VIP
        /// flights vary, then single conditions for a board that cannot take any pair.
        /// Formation flying pairs only with night, since the wingman would have to obey a
        /// runway or a gust meant for the player's half.
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
            return !hasFormation || (mix & (LevelRule.Runway | LevelRule.Wind)) == 0;
        }
    }
}
