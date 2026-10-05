using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Reads a picture board drawn as text, so a designer can sketch a special level in any
    /// editor:
    /// <code>
    /// # level: 90
    /// # name: plane
    /// ....S#....
    /// ..######..
    /// </code>
    /// <c>#</c> is a square to fly over, <c>S</c> the square the plane starts on and
    /// <c>.</c> open sky. Lines starting with <c>#</c> and a space are settings. The board
    /// carries no solution; the editor tool has the solver find one, which also proves the
    /// picture can be flown in one line.
    /// </summary>
    public static class ShapeLevelParser
    {
        /// <summary>Tag on every picture board, so the game can present it as a special flight.</summary>
        public const string SpecialTag = LevelTags.Special;

        public static SpecialLevel Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new FormatException("A picture board needs at least one row.");
            }

            int levelNumber = 0;
            string name = null;
            var rows = new List<string>();

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r', ' ', '\t');

                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    ReadSetting(line.Substring(2), ref levelNumber, ref name);
                }
                else if (line.Length > 0)
                {
                    rows.Add(line);
                }
            }

            if (levelNumber <= 0 || string.IsNullOrEmpty(name))
            {
                throw new FormatException("A picture board needs both '# level:' and '# name:'.");
            }

            return new SpecialLevel(levelNumber, Build(name, rows));
        }

        private static void ReadSetting(string setting, ref int levelNumber, ref string name)
        {
            int colon = setting.IndexOf(':');

            if (colon < 0)
            {
                return;
            }

            string key = setting.Substring(0, colon).Trim();
            string value = setting.Substring(colon + 1).Trim();

            if (key == "level")
            {
                if (!int.TryParse(value, out levelNumber))
                {
                    throw new FormatException("'# level:' must be a number, not '" + value + "'.");
                }
            }
            else if (key == "name")
            {
                name = value;
            }
        }

        private static LevelData Build(string name, List<string> rows)
        {
            if (rows.Count == 0)
            {
                throw new FormatException("Picture board '" + name + "' has no rows.");
            }

            int width = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                width = Math.Max(width, rows[i].Length);
            }

            var grid = new Grid(width, rows.Count);
            var cells = new List<int>();
            int start = LevelData.NoCell;

            for (int y = 0; y < rows.Count; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char mark = rows[y][x];

                    if (mark != '#' && mark != 'S')
                    {
                        continue;
                    }

                    int cell = y * width + x;
                    cells.Add(cell);

                    if (mark == 'S')
                    {
                        if (start != LevelData.NoCell)
                        {
                            throw new FormatException("Picture board '" + name + "' marks more than one start.");
                        }

                        start = cell;
                    }
                }
            }

            if (start == LevelData.NoCell)
            {
                throw new FormatException("Picture board '" + name + "' marks no start square 'S'.");
            }

            return new LevelData("special_" + name, 1, grid, cells.ToArray(), start, tags: new[] { SpecialTag, name });
        }
    }
}
