using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Lays the whole campaign out as one flight route climbing up the map.
    ///
    /// Each country is a chapter: inside it the route zigzags at a steady rhythm through
    /// smooth turns, and its levels sit at even distances along the curve, with more room
    /// around each city. Between two chapters the route makes a long international leg,
    /// sweeping from side to side with no level on it. Everything is computed once from
    /// the chapter sizes; the result is plain data the map view only reads.
    /// </summary>
    public sealed class RouteLayout
    {
        private const int StepsPerTurn = 24;
        private const int StepsPerCrossingTurn = 30;
        private const float MinimumGap = 0.85f;
        private const float NudgeStep = 6f;

        private readonly RouteLayoutSettings settings;
        private readonly int levelsPerCity;
        private readonly List<RouteVector> curve = new List<RouteVector>();
        private readonly List<float> curveDistance = new List<float>();
        private readonly List<RouteVector> levels = new List<RouteVector>();
        private readonly List<float> levelDistance = new List<float>();
        private readonly List<RouteVector> chapterMarks = new List<RouteVector>();
        private readonly List<float> hubDistance = new List<float>();
        private readonly List<RouteVector> hubs = new List<RouteVector>();

        /// <param name="levelsPerChapter">How many levels each country has, in flying order.</param>
        /// <param name="levelsPerCity">Levels per destination; the last level of each is the city itself.</param>
        public RouteLayout(RouteLayoutSettings settings, IReadOnlyList<int> levelsPerChapter, int levelsPerCity)
        {
            if (levelsPerChapter == null)
            {
                throw new ArgumentNullException(nameof(levelsPerChapter));
            }

            if (levelsPerCity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelsPerCity));
            }

            this.settings = settings;
            this.levelsPerCity = levelsPerCity;
            Build(levelsPerChapter);
        }

        public int LevelCount => levels.Count;

        /// <summary>The route as a polyline, from the first level upwards.</summary>
        public IReadOnlyList<RouteVector> Curve => curve;

        /// <summary>Distance along the route to each point of <see cref="Curve"/>.</summary>
        public IReadOnlyList<float> CurveDistance => curveDistance;

        /// <summary>
        /// Where each chapter's title sits: the first chapter's just below its first level,
        /// every later one beside the international leg that leads into it, clear of the route.
        /// </summary>
        public IReadOnlyList<RouteVector> ChapterMarks => chapterMarks;

        public float Height { get; private set; }

        /// <summary>One airport per city, on the route just after the city's last level.</summary>
        public int HubCount => hubs.Count;

        public RouteVector HubPosition(int city)
        {
            return hubs[city];
        }

        /// <summary>The first city whose airport is at or above a height; past the top, the hub count.</summary>
        public int FirstHubAtOrAbove(float y)
        {
            int low = 0;
            int high = hubs.Count;

            while (low < high)
            {
                int mid = (low + high) / 2;

                if (hubs[mid].Y < y)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        public bool IsCity(int levelNumber)
        {
            return levelNumber % levelsPerCity == 0;
        }

        /// <summary>Where a level sits; numbers outside the campaign clamp to its ends.</summary>
        public RouteVector LevelPosition(int levelNumber)
        {
            return levels[Clamp(levelNumber) - 1];
        }

        public float LevelDistance(int levelNumber)
        {
            return levelDistance[Clamp(levelNumber) - 1];
        }

        /// <summary>The point at a distance along the route.</summary>
        public RouteVector PointAt(float distance)
        {
            int index = IndexAt(distance);

            if (index >= curve.Count - 1)
            {
                return curve[curve.Count - 1];
            }

            float span = curveDistance[index + 1] - curveDistance[index];
            float t = span > 0f ? (distance - curveDistance[index]) / span : 0f;
            return RouteVector.Lerp(curve[index], curve[index + 1], t);
        }

        /// <summary>
        /// Fills <paramref name="path"/> with the route between two distances, ends included,
        /// for something that flies along it. Returns how many points were written.
        /// </summary>
        public int PathBetween(float from, float to, RouteVector[] path)
        {
            if (path == null || path.Length < 2)
            {
                return 0;
            }

            int count = 0;
            path[count++] = PointAt(from);

            for (int i = IndexAt(from) + 1; i < curve.Count && curveDistance[i] < to && count < path.Length - 1; i++)
            {
                path[count++] = curve[i];
            }

            path[count++] = PointAt(to);
            return count;
        }

        /// <summary>The first curve point at or below a height, for drawing only what is on screen.</summary>
        public int FirstCurveIndexAbove(float y)
        {
            // The route climbs overall but swings sideways, so search by height, not distance.
            int low = 0;
            int high = curve.Count - 1;

            while (low < high)
            {
                int mid = (low + high) / 2;

                if (curve[mid].Y < y)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return Math.Max(0, low - 1);
        }

        /// <summary>The lowest level whose position is at or above a height, 1-based; past the top, one past the last.</summary>
        public int FirstLevelAtOrAbove(float y)
        {
            int low = 0;
            int high = levels.Count;

            while (low < high)
            {
                int mid = (low + high) / 2;

                if (levels[mid].Y < y)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low + 1;
        }

        private int Clamp(int levelNumber)
        {
            return Math.Max(1, Math.Min(levelNumber, levels.Count));
        }

        private int IndexAt(float distance)
        {
            int low = 0;
            int high = curveDistance.Count - 1;

            while (low < high)
            {
                int mid = (low + high + 1) / 2;

                if (curveDistance[mid] <= distance)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return low;
        }

        private void Build(IReadOnlyList<int> levelsPerChapter)
        {
            var start = new RouteVector(settings.Width / 2f - settings.Swing * 0.5f, 0f);
            int firstLevel = 1;
            var points = new List<RouteVector>();

            for (int chapter = 0; chapter < levelsPerChapter.Count; chapter++)
            {
                int count = levelsPerChapter[chapter];

                if (chapter == 0)
                {
                    chapterMarks.Add(new RouteVector(settings.Width / 2f, start.Y - settings.LevelSpacing * 0.7f));
                }

                points.Clear();
                Sample(ChapterTurns(start, count), StepsPerTurn, points);
                int lastSegment = PlaceLevels(points, count, firstLevel);
                firstLevel += count;

                if (chapter == levelsPerChapter.Count - 1)
                {
                    // The last chapter keeps its curve on past the end, for the final airport.
                    Append(points, points.Count);
                    break;
                }

                // The chapter's curve ends on its last level, where the next leg takes off.
                Append(points, lastSegment + 1);
                AppendPoint(levels[levels.Count - 1]);

                points.Clear();
                RouteVector last = levels[levels.Count - 1];
                Sample(CrossingTurns(last, out RouteVector mark), StepsPerCrossingTurn, points);
                chapterMarks.Add(mark);

                // The leg's first point is the last level itself, already on the curve.
                for (int i = 1; i < points.Count; i++)
                {
                    AppendPoint(points[i]);
                }

                // The next country's first level sits where the leg ends, so the curve runs straight on into it.
                start = curve[curve.Count - 1];
            }

            for (int i = 0; i < hubDistance.Count; i++)
            {
                hubs.Add(PointAt(hubDistance[i]));
            }

            float top = 0f;

            for (int i = 0; i < curve.Count; i++)
            {
                top = Math.Max(top, curve[i].Y);
            }

            Height = top;
        }

        /// <summary>Turns inside a country: an even zigzag, each swing a little different.</summary>
        private List<RouteVector> ChapterTurns(RouteVector start, int count)
        {
            float centre = settings.Width / 2f;
            float side = start.X > centre ? -1f : 1f;
            int turns = (int)Math.Ceiling(count * settings.LevelSpacing / settings.RowHeight) + 3;
            var turnsList = new List<RouteVector>(turns + 1) { start };
            float y = start.Y;

            for (int i = 0; i < turns; i++)
            {
                y += settings.RowHeight;
                float jitter = (i * 53 % 50) / 50f * settings.SwingJitter;
                turnsList.Add(new RouteVector(centre + side * (settings.Swing + jitter), y));
                side = -side;
            }

            return turnsList;
        }

        /// <summary>
        /// An international leg: one long, wide arc out to the far side, up along it, and back
        /// in to the middle. The chapter title goes in the open sky on the near side, level
        /// with the arc's widest point, so the route never runs through it.
        /// </summary>
        private List<RouteVector> CrossingTurns(RouteVector from, out RouteVector mark)
        {
            float rise = settings.CrossingRise;
            float centre = settings.Width / 2f;
            bool outRight = from.X < centre;
            float edge = outRight ? settings.Width - settings.EdgeMargin : settings.EdgeMargin;
            float near = settings.Width - edge;
            mark = new RouteVector(near + (outRight ? settings.Swing : -settings.Swing), from.Y + rise * 2f);
            return new List<RouteVector>
            {
                from,
                new RouteVector(edge, from.Y + rise * 1.3f),
                new RouteVector(edge, from.Y + rise * 2.7f),
                new RouteVector(centre, from.Y + rise * 4f),
            };
        }

        /// <summary>Drops this chapter's levels along its curve; returns the segment the last one sits on.</summary>
        private int PlaceLevels(List<RouteVector> points, int count, int firstLevel)
        {
            float travelled = 0f;
            float target = 0f;
            int placed = 0;
            float baseDistance = curveDistance.Count > 0 ? curveDistance[curveDistance.Count - 1] : 0f;
            RouteVector previous = curve.Count > 0 ? curve[curve.Count - 1] : points[0];
            baseDistance += curve.Count > 0 ? RouteVector.Distance(previous, points[0]) : 0f;

            for (int i = 0; i < points.Count - 1 && placed < count; i++)
            {
                float segment = RouteVector.Distance(points[i], points[i + 1]);

                while (travelled + segment >= target && placed < count)
                {
                    float t = segment > 0f ? (target - travelled) / segment : 0f;
                    RouteVector position = RouteVector.Lerp(points[i], points[i + 1], t);

                    // On a tight turn two levels can be close as the crow flies although far
                    // along the route; slide the later one on until they are clear of each other.
                    if (placed > 0 && RouteVector.Distance(position, levels[levels.Count - 1]) < MinimumGap * settings.LevelSpacing)
                    {
                        target += NudgeStep;
                        continue;
                    }

                    levels.Add(position);
                    levelDistance.Add(baseDistance + target);
                    placed++;

                    if ((firstLevel - 1 + placed) % levelsPerCity == 0)
                    {
                        hubDistance.Add(baseDistance + target + settings.HubOffset);
                    }
                    target += GapAfter(firstLevel - 1 + placed);
                }

                if (placed == count)
                {
                    return i;
                }

                travelled += segment;
            }

            throw new InvalidOperationException("The chapter curve is too short for its levels.");
        }

        private float GapAfter(int levelNumber)
        {
            if (levelNumber % levelsPerCity == 0)
            {
                return settings.CitySpacing;
            }

            return levelNumber % levelsPerCity == levelsPerCity - 1 ? settings.CityApproach : settings.LevelSpacing;
        }

        private void Append(List<RouteVector> points, int count)
        {
            for (int i = 0; i < count; i++)
            {
                AppendPoint(points[i]);
            }
        }

        private void AppendPoint(RouteVector point)
        {
            float distance = curve.Count == 0
                ? 0f
                : curveDistance[curveDistance.Count - 1] + RouteVector.Distance(curve[curve.Count - 1], point);
            curve.Add(point);
            curveDistance.Add(distance);
        }

        /// <summary>A Catmull-Rom curve through the turns, with the ends held in place.</summary>
        private static void Sample(List<RouteVector> turns, int steps, List<RouteVector> into)
        {
            for (int i = 0; i < turns.Count - 1; i++)
            {
                RouteVector p0 = turns[Math.Max(0, i - 1)];
                RouteVector p1 = turns[i];
                RouteVector p2 = turns[i + 1];
                RouteVector p3 = turns[Math.Min(turns.Count - 1, i + 2)];

                for (int k = 0; k < steps; k++)
                {
                    into.Add(CatmullRom(p0, p1, p2, p3, (float)k / steps));
                }
            }

            into.Add(turns[turns.Count - 1]);
        }

        private static RouteVector CatmullRom(RouteVector p0, RouteVector p1, RouteVector p2, RouteVector p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return new RouteVector(
                Spline(p0.X, p1.X, p2.X, p3.X, t, t2, t3),
                Spline(p0.Y, p1.Y, p2.Y, p3.Y, t, t2, t3));
        }

        private static float Spline(float a, float b, float c, float d, float t, float t2, float t3)
        {
            return 0.5f * (2f * b + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (-a + 3f * b - 3f * c + d) * t3);
        }
    }
}
