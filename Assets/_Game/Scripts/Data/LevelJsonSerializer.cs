using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using Newtonsoft.Json;

namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Converts between the runtime level model and the exported chapter files.
    ///
    /// Engine free, so a chapter can be written by an editor tool, read by the game, and
    /// round tripped in a plain dotnet test without opening Unity.
    /// </summary>
    public static class LevelJsonSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string SerializeChapter(string chapterId, IReadOnlyList<LevelData> levels)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                throw new ArgumentException("Chapter id must not be empty.", nameof(chapterId));
            }

            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            var file = new ChapterFile
            {
                FormatVersion = ChapterFile.CurrentVersion,
                ChapterId = chapterId,
                Levels = new LevelDto[levels.Count]
            };

            for (int i = 0; i < levels.Count; i++)
            {
                file.Levels[i] = ToDto(levels[i]);
            }

            return JsonConvert.SerializeObject(file, Settings);
        }

        /// <summary>
        /// Reads a chapter file. Throws on a format version the build does not know,
        /// because silently reading half a file would surface later as a corrupt board.
        /// </summary>
        public static List<LevelData> DeserializeChapter(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException("Chapter json must not be empty.", nameof(json));
            }

            ChapterFile file = JsonConvert.DeserializeObject<ChapterFile>(json);

            if (file == null)
            {
                throw new FormatException("Chapter json did not contain an object.");
            }

            if (file.FormatVersion != ChapterFile.CurrentVersion)
            {
                throw new FormatException(
                    "Chapter " + file.ChapterId + " is format version " + file.FormatVersion +
                    " but this build reads version " + ChapterFile.CurrentVersion + ".");
            }

            var levels = new List<LevelData>(file.Levels == null ? 0 : file.Levels.Length);

            if (file.Levels == null)
            {
                return levels;
            }

            for (int i = 0; i < file.Levels.Length; i++)
            {
                levels.Add(FromDto(file.Levels[i]));
            }

            return levels;
        }

        public static LevelDto ToDto(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            return new LevelDto
            {
                Id = level.Id,
                Version = level.Version,
                Width = level.Grid.Width,
                Height = level.Grid.Height,
                ActiveCells = ToArray(level.DeclaredActiveCells),
                FixedStart = level.HasFixedStart ? level.FixedStart : (int?)null,
                FixedEnd = level.HasFixedEnd ? level.FixedEnd : (int?)null,
                Solution = level.HasSolution ? ToArray(level.Solution) : null,
                Difficulty = level.Difficulty,
                ThemeId = level.ThemeId,
                Tags = level.Tags.Count == 0 ? null : ToArray(level.Tags),
                Wind = level.HasWind ? FlattenWind(level.Wind) : null
            };
        }

        private static int[] FlattenWind(IReadOnlyList<WindCell> wind)
        {
            var pairs = new int[wind.Count * 2];

            for (int i = 0; i < wind.Count; i++)
            {
                pairs[i * 2] = wind[i].Cell;
                pairs[i * 2 + 1] = (int)wind[i].Direction;
            }

            return pairs;
        }

        private static WindCell[] ReadWind(int[] pairs)
        {
            if (pairs == null || pairs.Length < 2)
            {
                return null;
            }

            var wind = new WindCell[pairs.Length / 2];

            for (int i = 0; i < wind.Length; i++)
            {
                wind[i] = new WindCell(pairs[i * 2], (Direction)pairs[i * 2 + 1]);
            }

            return wind;
        }

        public static LevelData FromDto(LevelDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            return new LevelData(
                dto.Id,
                dto.Version,
                new Grid(dto.Width, dto.Height),
                dto.ActiveCells ?? new int[0],
                dto.FixedStart ?? LevelData.NoCell,
                dto.FixedEnd ?? LevelData.NoCell,
                dto.Solution,
                dto.Difficulty,
                dto.ThemeId,
                dto.Tags,
                ReadWind(dto.Wind));
        }

        private static T[] ToArray<T>(IReadOnlyList<T> source)
        {
            var copy = new T[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }
}
