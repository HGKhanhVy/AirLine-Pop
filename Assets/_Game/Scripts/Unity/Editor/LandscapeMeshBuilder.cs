using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the painted tropical sea the board sits in: deep blue water with soft curved
    /// wave strokes, lightening to turquoise over the shallows; round every island a pale
    /// lagoon band with a wavy edge and a line of white surf at the foot of its cliffs.
    ///
    /// Islands are tall, rugged sandstone cliffs, faceted into columns, topped almost to the
    /// edge with grass and crowned with a few chunky pines (see <see cref="IslandProps"/>).
    /// Rock islets are a mossy crag with boulders at its foot, rising out of the surf.
    ///
    /// The water's colour is worked out per vertex from the distance to the nearest shore,
    /// so the shallows fade smoothly. Everything is placed in screen terms through
    /// <see cref="LandscapeLayoutView"/> and kept to the margins round the board.
    /// </summary>
    public static class LandscapeMeshBuilder
    {
        private const int Seed = 20260929;

        // Few sides, so the faceted cliff reads as a ring of rock columns.
        private const int OutlineSides = 20;

        // The sea grid: fine enough that the shallows hug the shores in a narrow band
        // instead of smearing across a whole cell.
        private const int SeaColumns = 130;
        private const int SeaRows = 200;

        // How far the shallows reach beyond the lagoon band, in world units at the
        // reference scale.
        private const float ShallowReach = 0.45f;

        // The water round a shore, as scalings of the island's outline: the surf starts
        // just inside the cliff foot, which hides its inner edge, and the pale lagoon band
        // runs on from it. Each outer edge ripples.
        private const float SandSurfInner = 0.97f;
        private const float SandSurfOuter = 1.06f;
        private const float SandLagoonOuter = 1.2f;
        private const float RockSurfInner = 0.7f;
        private const float RockSurfOuter = 0.9f;
        private const float RockLagoonOuter = 1.02f;
        private const float SurfHeight = 0.006f;
        private const float LagoonHeight = 0.004f;

        // An island is a tall cliff with a slightly rounded rim, carrying a thin plateau of
        // grass that leaves only a sliver of sand at the edge. Heights and bevels are shares
        // of the island's radius; the grass share is of its outline.
        private const float CliffHeight = 0.55f;
        private const float CliffBevel = 0.05f;
        private const float GrassShare = 0.87f;
        private const float GrassHeight = 0.05f;
        private const float GrassBevel = 0.035f;

        // The island size the prop sizes are chosen for.
        private const float ReferenceIslandRadius = 0.6f;

        private const int WaveStrokes = 110;
        private const float StrokeHeight = 0.003f;

        private static readonly Color DeepSea = new Color(0f, 0.33f, 0.66f);
        private static readonly Color OpenSea = new Color(0f, 0.54f, 0.88f);
        private static readonly Color Shallows = new Color(0.2f, 0.74f, 0.92f);
        private static readonly Color Lagoon = new Color(0.56f, 0.9f, 0.95f);
        private static readonly Color Surf = new Color(0.96f, 0.99f, 1f);
        private static readonly Color CliffTop = new Color(0.97f, 0.87f, 0.66f);
        private static readonly Color CliffWall = new Color(0.86f, 0.7f, 0.52f);
        private static readonly Color Grass = new Color(0.52f, 0.78f, 0.26f);
        private static readonly Color GrassWall = new Color(0.38f, 0.62f, 0.2f);

        private enum Feature
        {
            Woods,
            Grove,
            Rocks
        }

        // Where each island sits on screen, how big it is and what it is. The board takes
        // the middle of the view, so the islands gather in the bands above and below it,
        // whole and inside the frame, with only small rock islets at the sides.
        private static readonly (Vector2 viewport, float radius, Feature feature)[] Islands =
        {
            (new Vector2(0.37f, 0.875f), 0.36f, Feature.Rocks),
            (new Vector2(0.74f, 0.885f), 0.58f, Feature.Woods),
            (new Vector2(0.99f, 0.83f), 0.32f, Feature.Rocks),
            (new Vector2(0.95f, 0.65f), 0.4f, Feature.Grove),
            (new Vector2(0.04f, 0.42f), 0.4f, Feature.Rocks),
            (new Vector2(0.2f, 0.11f), 0.62f, Feature.Woods),
            (new Vector2(0.8f, 0.13f), 0.55f, Feature.Grove),
            (new Vector2(0.42f, 0.01f), 0.3f, Feature.Rocks)
        };

        private sealed class Island
        {
            public Island(Vector2 centre, float[] radii, float radius, Feature feature)
            {
                Centre = centre;
                Radii = radii;
                Radius = radius;
                Feature = feature;
            }

            public Vector2 Centre { get; }

            /// <summary>The shoreline, where the cliff foot meets the surf.</summary>
            public float[] Radii { get; }

            public float Radius { get; }

            public Feature Feature { get; }

            public bool IsRocky => Feature == Feature.Rocks;

            public float SurfInner => IsRocky ? RockSurfInner : SandSurfInner;

            public float SurfOuter => IsRocky ? RockSurfOuter : SandSurfOuter;

            public float LagoonOuter => IsRocky ? RockLagoonOuter : SandLagoonOuter;

            /// <summary>The shoreline's radius in the direction of <paramref name="point"/>.</summary>
            public float RadiusTowards(Vector2 point)
            {
                Vector2 offset = point - Centre;
                float angle = Mathf.Repeat(Mathf.Atan2(offset.y, offset.x), Mathf.PI * 2f);
                int side = Mathf.FloorToInt(angle / (Mathf.PI * 2f) * Radii.Length) % Radii.Length;
                return Radii[side];
            }
        }

        /// <summary>
        /// The sea and the land as separate meshes, so the water can take a material of its
        /// own, glossier than the land. The land is sheared back by <paramref name="shear"/>
        /// degrees, so the islands read as seen from lower down than the board is.
        /// </summary>
        public static (Mesh sea, Mesh land) Build(LandscapeLayoutView view, float shear)
        {
            var random = new System.Random(Seed);
            var islands = new List<Island>(Islands.Length);

            foreach (var (viewport, radius, feature) in Islands)
            {
                Vector2 centre = view.OnGround(viewport.x, viewport.y);
                islands.Add(new Island(centre, RuggedOutline(radius, random), radius, feature));
            }

            System.Func<Vector2, Color> waterAt = WaterColour(view, islands, random);
            var sea = new LowPolyBuilder();
            AddSea(sea, view, waterAt);
            AddWaveStrokes(sea, view, islands, waterAt, random);

            var land = new LowPolyBuilder();

            foreach (Island island in islands)
            {
                AddShoreWater(land, island, random);

                if (island.IsRocky)
                {
                    AddRockIslet(land, island, random);
                }
                else
                {
                    AddCliffIsland(land, island, random);
                }
            }

            land.ShearAway(shear);
            return (sea.ToMesh("Sea"), land.ToMesh("Landscape"));
        }

        /// <summary>
        /// Water coloured by how far it is from the nearest shore: turquoise over the
        /// shallows, open blue beyond, deepening towards the edges of the view.
        /// </summary>
        private static System.Func<Vector2, Color> WaterColour(LandscapeLayoutView view, List<Island> islands, System.Random random)
        {
            Vector2 middle = view.OnGround(0.5f, 0.5f);
            float reach = (view.OnGround(0.5f, 1f) - middle).magnitude;
            float phase = Range(random, 0f, 10f);

            return point =>
            {
                float edge = Mathf.Clamp01((point - middle).magnitude / reach);
                Color water = Color.Lerp(OpenSea, DeepSea, Smooth(0.2f, 1.1f, edge));

                // A slow, broad swell of lighter and darker water, so the open sea is not flat paint.
                float swell = Mathf.Sin(point.x * 0.45f + phase) * Mathf.Sin(point.y * 0.35f - phase);
                water = Color.Lerp(water, DeepSea, 0.12f + 0.12f * swell);

                float shallow = 1f - Smooth(0f, ShallowReach, DistanceToShore(point, islands));
                return Color.Lerp(water, Shallows, shallow * shallow);
            };
        }

        private static void AddSea(LowPolyBuilder builder, LandscapeLayoutView view, System.Func<Vector2, Color> waterAt)
        {
            // Wide enough for a tablet's view; the stage scales with the camera, so big
            // boards need no more than this.
            Vector2 nearLeft = view.OnGround(-0.5f, -0.3f);
            Vector2 farRight = view.OnGround(1.5f, 1.3f);
            var rect = Rect.MinMaxRect(nearLeft.x, nearLeft.y, farRight.x, farRight.y);
            builder.AddGroundGrid(rect, SeaColumns, SeaRows, 0f, waterAt);
        }

        /// <summary>
        /// Soft curved strokes on the open water, like brushed wave lines: most a shade
        /// lighter than the water under them, some a shade darker. Kept off the shallows.
        /// </summary>
        private static void AddWaveStrokes(LowPolyBuilder builder, LandscapeLayoutView view, List<Island> islands,
            System.Func<Vector2, Color> waterAt, System.Random random)
        {
            int placed = 0;

            for (int attempt = 0; attempt < WaveStrokes * 10 && placed < WaveStrokes; attempt++)
            {
                var viewport = new Vector2(Range(random, -0.1f, 1.1f), Range(random, -0.05f, 1.08f));
                Vector2 at = view.OnGround(viewport.x, viewport.y);

                if (DistanceToShore(at, islands) < 0.2f)
                {
                    continue;
                }

                // The arc bows up like a wave crest, give or take a little tilt.
                float radius = Range(random, 0.18f, 0.4f);
                float sweep = Range(random, 0.6f, 1.1f);
                float middle = Mathf.PI * 0.5f + Range(random, -0.25f, 0.25f);
                Vector2 centre = at - new Vector2(Mathf.Cos(middle), Mathf.Sin(middle)) * radius;

                Color water = waterAt(at);
                Color stroke = random.NextDouble() < 0.65
                    ? Color.Lerp(water, Color.white, 0.16f)
                    : Color.Lerp(water, DeepSea * 0.7f, 0.3f);

                builder.AddGroundArc(centre, radius, middle - sweep * 0.5f, sweep, Range(random, 0.018f, 0.032f),
                    8, StrokeHeight, stroke);
                placed++;
            }
        }

        private static float DistanceToShore(Vector2 point, List<Island> islands)
        {
            float nearest = float.MaxValue;

            foreach (Island island in islands)
            {
                float shore = island.RadiusTowards(point) * island.LagoonOuter;
                nearest = Mathf.Min(nearest, (point - island.Centre).magnitude - shore);
            }

            return Mathf.Max(0f, nearest);
        }

        /// <summary>
        /// The white surf at the foot of the island and the pale lagoon band beyond it, both
        /// with softly rippling outer edges.
        /// </summary>
        private static void AddShoreWater(LowPolyBuilder builder, Island island, System.Random random)
        {
            float[] surfInner = Scaled(island.Radii, island.SurfInner);
            float[] surfOuter = Rippled(island.Radii, island.SurfOuter, 0.025f, 7f, random);
            float[] lagoonOuter = Rippled(island.Radii, island.LagoonOuter, 0.035f, 5f, random);

            builder.AddBand(island.Centre, Scaled(surfOuter, 0.98f), lagoonOuter, LagoonHeight, Lagoon);
            builder.AddBand(island.Centre, surfInner, surfOuter, SurfHeight, Surf);
        }

        /// <summary>
        /// A tall faceted sandstone cliff with a grass plateau on it, crowned with a few
        /// chunky pines and a round tree or two.
        /// </summary>
        private static void AddCliffIsland(LowPolyBuilder builder, Island island, System.Random random)
        {
            float r = island.Radius;
            float cliffTop = CliffHeight * r;

            builder.IsFaceted = true;
            builder.AddRoundedPlateau(island.Centre, island.Radii, cliffTop, CliffBevel * r, CliffWall, CliffTop);

            // The grass is a second, thinner plateau on the cliff, leaving a sliver of sand.
            builder.Elevation = cliffTop;
            builder.AddRoundedPlateau(island.Centre, Scaled(island.Radii, GrassShare), GrassHeight * r, GrassBevel * r, GrassWall, Grass);
            builder.IsFaceted = false;

            builder.Elevation = cliffTop + GrassHeight * r;
            var taken = new List<(Vector2 at, float room)>();

            if (island.Feature == Feature.Grove)
            {
                PlaceGrove(builder, island, taken, 1, 0, random);
            }
            else
            {
                PlaceGrove(builder, island, taken, 3, 1, random);
            }

            builder.Elevation = 0f;
        }

        /// <summary>
        /// One big pine towards the back of the grass anchors the grove; smaller pines and
        /// round trees stand round it and a bush or two fills the edges.
        /// </summary>
        private static void PlaceGrove(LowPolyBuilder builder, Island island, List<(Vector2 at, float room)> taken,
            int pines, int roundTrees, System.Random random)
        {
            float r = island.Radius;
            float scale = Mathf.Sqrt(r / ReferenceIslandRadius);
            Vector2 anchor = island.Centre + new Vector2(-0.05f * r, 0.12f * r);
            float anchorSize = 0.85f * scale;

            IslandProps.AddPine(builder, anchor, anchorSize, random);
            taken.Add((anchor, 0.2f * anchorSize));

            Scatter(builder, island, taken, pines, 0.62f * scale, 0.8f, random, IslandProps.AddPine);
            Scatter(builder, island, taken, roundTrees, 0.55f * scale, 0.8f, random, IslandProps.AddRoundTree);
            Scatter(builder, island, taken, 2, 0.4f * scale, 0.9f, random, IslandProps.AddBush);
        }

        /// <summary>A tall mossy crag rising out of the surf, with smaller boulders at its foot.</summary>
        private static void AddRockIslet(LowPolyBuilder builder, Island island, System.Random random)
        {
            float r = island.Radius;
            IslandProps.AddCrag(builder, island.Centre, r * 3.6f, random);

            int count = 2 + random.Next(3);

            for (int i = 0; i < count; i++)
            {
                float angle = Range(random, 0f, Mathf.PI * 2f);
                Vector2 at = island.Centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r * Range(random, 0.45f, 0.65f);
                IslandProps.AddRock(builder, at, r * Range(random, 1.3f, 2f), random);
            }
        }

        /// <summary>
        /// Adds props at free spots on the grass. <paramref name="reach"/> is how far out
        /// from the middle they may stand, as a share of the grass outline. A spot too close
        /// to one already taken is skipped, so props never sink into each other.
        /// </summary>
        private static void Scatter(LowPolyBuilder builder, Island island, List<(Vector2 at, float room)> taken, int count,
            float size, float reach, System.Random random, System.Action<LowPolyBuilder, Vector2, float, System.Random> add)
        {
            int placed = 0;

            for (int attempt = 0; attempt < count * 12 && placed < count; attempt++)
            {
                float propSize = size * Range(random, 0.85f, 1.1f);

                // Crowns may overlap a little, as in a dense grove; only the trunks keep apart.
                float room = 0.18f * propSize;
                float angle = Range(random, 0f, Mathf.PI * 2f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float limit = island.RadiusTowards(island.Centre + direction) * GrassShare * reach - room;
                Vector2 at = island.Centre + direction * Mathf.Max(0f, limit) * Mathf.Sqrt((float)random.NextDouble());

                if (IsCrowded(at, room, taken))
                {
                    continue;
                }

                add(builder, at, propSize, random);
                taken.Add((at, room));
                placed++;
            }
        }

        private static bool IsCrowded(Vector2 at, float room, List<(Vector2 at, float room)> taken)
        {
            foreach (var (other, otherRoom) in taken)
            {
                if ((at - other).sqrMagnitude < (room + otherRoom) * (room + otherRoom))
                {
                    return true;
                }
            }

            return false;
        }

        private static float[] Scaled(float[] values, float scale)
        {
            var scaled = new float[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                scaled[i] = values[i] * scale;
            }

            return scaled;
        }

        /// <summary>An outline scaled by <paramref name="scale"/> whose edge ripples in and out.</summary>
        private static float[] Rippled(float[] radii, float scale, float depth, float waves, System.Random random)
        {
            var rippled = new float[radii.Length];
            float phase = Range(random, 0f, Mathf.PI * 2f);

            for (int i = 0; i < radii.Length; i++)
            {
                float angle = i * (Mathf.PI * 2f / radii.Length);
                rippled[i] = radii[i] * (scale + depth * Mathf.Sin(waves * angle + phase));
            }

            return rippled;
        }

        /// <summary>
        /// Radii round an outline that bulge in a few soft lobes, with every point nudged a
        /// little on its own, so the cliff reads as rugged rock rather than a smooth curve.
        /// </summary>
        private static float[] RuggedOutline(float radius, System.Random random)
        {
            var radii = new float[OutlineSides];
            float phaseA = Range(random, 0f, Mathf.PI * 2f);
            float phaseB = Range(random, 0f, Mathf.PI * 2f);

            for (int i = 0; i < OutlineSides; i++)
            {
                float angle = i * (Mathf.PI * 2f / OutlineSides);
                float wobble = 0.08f * Mathf.Sin(2f * angle + phaseA)
                    + 0.05f * Mathf.Sin(3f * angle + phaseB)
                    + Range(random, -0.035f, 0.035f);
                radii[i] = radius * (1f + wobble);
            }

            return radii;
        }

        /// <summary>
        /// The shader smoothstep: 0 below <paramref name="from"/>, 1 above <paramref name="to"/>.
        /// Mathf.SmoothStep instead eases between its first two arguments, a different thing.
        /// </summary>
        private static float Smooth(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
