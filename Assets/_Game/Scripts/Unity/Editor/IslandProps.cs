using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// The kit the islands and the grass squares are dressed with, in the painted low poly
    /// look: pines of two plump teardrop tiers on a stout brown trunk, round-crowned trees
    /// and bushes, all in big soft facets shaded from a dark foot to a sunlit top, plus
    /// faceted boulders and mossy crags. Every prop is tipped away from the camera about
    /// its foot, so the camera looking down from high up still sees it from the side.
    ///
    /// Sizes are in world units at the stage's reference scale, standing on whatever
    /// elevation the builder is set to.
    /// </summary>
    public static class IslandProps
    {
        /// <summary>How far props lean away from the camera, making up for its steep view.</summary>
        public const float Lean = 6f;

        // Few sides and flat shading: big soft facets, the painted low poly look.
        private const int RoundSides = 9;
        private const int TierSteps = 10;
        private const int CrownRings = 6;

        private static readonly Color Trunk = new Color(0.47f, 0.29f, 0.17f);

        // Each tier runs from its shadowed foot to a sunlit tip; the pairs vary the green a
        // little from tree to tree.
        private static readonly (Color dark, Color light)[] Pines =
        {
            (new Color(0.19f, 0.45f, 0.16f), new Color(0.56f, 0.8f, 0.3f)),
            (new Color(0.16f, 0.42f, 0.18f), new Color(0.5f, 0.77f, 0.32f)),
            (new Color(0.21f, 0.48f, 0.14f), new Color(0.6f, 0.82f, 0.28f))
        };

        private static readonly (Color dark, Color light)[] Crowns =
        {
            (new Color(0.2f, 0.47f, 0.15f), new Color(0.58f, 0.82f, 0.3f)),
            (new Color(0.17f, 0.44f, 0.18f), new Color(0.5f, 0.78f, 0.32f))
        };

        private static readonly (Color dark, Color light) Bush = (new Color(0.2f, 0.46f, 0.17f), new Color(0.52f, 0.78f, 0.3f));

        private static readonly Color[] Rocks =
        {
            new Color(0.62f, 0.62f, 0.66f),
            new Color(0.7f, 0.68f, 0.66f),
            new Color(0.54f, 0.56f, 0.62f)
        };

        private static readonly Color Moss = new Color(0.42f, 0.68f, 0.26f);

        /// <summary>
        /// A storybook pine: a stout brown trunk under two plump teardrop tiers, the lower
        /// one round-bellied, the upper one smaller and pointier, tucked into it.
        /// </summary>
        public static void AddPine(LowPolyBuilder builder, Vector2 at, float size, System.Random random)
        {
            var (dark, light) = Pines[random.Next(Pines.Length)];

            Begin(builder, at);
            builder.AddFrustum(at, 0f, 0.26f * size, 0.075f * size, 0.06f * size, 7, Trunk, Trunk);
            builder.AddLathe(at, TierProfile(0.18f * size, 0.36f * size, 0.54f * size, 0.3f, 0.35f), RoundSides, dark, light);

            // The upper tier starts lighter: it catches more of the sun.
            builder.AddLathe(at, TierProfile(0.5f * size, 0.27f * size, 0.6f * size, 0.18f, 0.75f), RoundSides,
                Color.Lerp(dark, light, 0.3f), light);
            End(builder);
        }

        /// <summary>A broadleaf tree: a stout brown trunk under one full, round crown.</summary>
        public static void AddRoundTree(LowPolyBuilder builder, Vector2 at, float size, System.Random random)
        {
            var (dark, light) = Crowns[random.Next(Crowns.Length)];
            float trunk = 0.2f * size;
            float radius = 0.27f * size;

            Begin(builder, at);
            builder.AddFrustum(at, 0f, trunk + 0.1f * size, 0.065f * size, 0.055f * size, 7, Trunk, Trunk);
            builder.AddShadedBlob(at, trunk + radius * 0.85f, radius, radius * 0.95f, RoundSides, CrownRings, dark, light);
            End(builder);
        }

        /// <summary>A low bush of two or three soft domes.</summary>
        public static void AddBush(LowPolyBuilder builder, Vector2 at, float size, System.Random random)
        {
            int count = 2 + random.Next(2);

            Begin(builder, at);

            for (int i = 0; i < count; i++)
            {
                var offset = new Vector2(Range(random, -0.14f, 0.14f), Range(random, -0.1f, 0.1f)) * size;
                float radius = Range(random, 0.12f, 0.17f) * size;
                builder.AddShadedBlob(at + offset, radius * 0.25f, radius, radius * 0.8f, 8, CrownRings, Bush.dark, Bush.light);
            }

            End(builder);
        }

        /// <summary>A faceted boulder, squat and a little lopsided.</summary>
        public static void AddRock(LowPolyBuilder builder, Vector2 at, float size, System.Random random)
        {
            Color color = Rocks[random.Next(Rocks.Length)];
            float radius = 0.2f * size;
            var radii = new Vector3(radius * Range(random, 0.9f, 1.2f), radius * Range(random, 0.8f, 1f), radius * Range(random, 0.6f, 0.85f));

            Begin(builder, at);
            builder.AddBlob(at, radii.z * 0.2f, radii, 6, 4, color);
            End(builder);
        }

        /// <summary>A tall faceted crag capped with a tuft of moss, the heart of a rock islet.</summary>
        public static void AddCrag(LowPolyBuilder builder, Vector2 at, float size, System.Random random)
        {
            Color color = Rocks[random.Next(Rocks.Length)];
            float radius = 0.2f * size;
            float height = radius * Range(random, 1.1f, 1.35f);

            Begin(builder, at);
            builder.AddBlob(at, 0f, new Vector3(radius, radius * 0.9f, height), 7, 6, color, isDome: true);
            builder.AddBlob(at + new Vector2(-0.1f, 0.12f) * radius, height * 0.85f,
                new Vector3(radius * 0.5f, radius * 0.45f, radius * 0.18f), 6, 4, Moss);
            End(builder);
        }

        /// <summary>
        /// A pine tier shaped like a teardrop: it swells from a tucked-in underside to its
        /// widest at <paramref name="belly"/> (a share of its height), then narrows to the
        /// tip. <paramref name="coneness"/> runs from 0, a round dome over the belly, to 1, a
        /// straight cone. Shade runs from 0 underneath to 1 at the tip.
        /// </summary>
        private static List<Vector3> TierProfile(float bottom, float radius, float height, float belly, float coneness)
        {
            var profile = new List<Vector3>(TierSteps + 1);

            for (int i = 0; i <= TierSteps; i++)
            {
                float t = i / (float)TierSteps;
                float across;

                if (t <= belly)
                {
                    // A quarter ellipse from a small underside out to the belly: a rounded bottom.
                    float swell = t / belly;
                    across = radius * (0.15f + 0.85f * Mathf.Sqrt(1f - (1f - swell) * (1f - swell)));
                }
                else
                {
                    float up = (t - belly) / (1f - belly);
                    across = radius * Mathf.Lerp(Mathf.Cos(up * Mathf.PI * 0.5f), 1f - up, coneness);
                }

                profile.Add(new Vector3(across, bottom + height * t, Mathf.Pow(t, 0.8f)));
            }

            return profile;
        }

        private static void Begin(LowPolyBuilder builder, Vector2 foot)
        {
            builder.IsFaceted = true;
            builder.BeginLean(foot, Lean);
        }

        private static void End(LowPolyBuilder builder)
        {
            builder.EndLean();
            builder.IsFaceted = false;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
