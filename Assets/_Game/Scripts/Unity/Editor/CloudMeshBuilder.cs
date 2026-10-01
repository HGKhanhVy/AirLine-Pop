using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds soft cartoon clouds: a row of round cotton puffs, biggest in the middle, with
    /// a few more heaped on top. Smoothly shaded from bright white on top to a pale sky blue
    /// underneath, so they read as fluffy rather than faceted. About two units across;
    /// scale them per cloud.
    /// </summary>
    public static class CloudMeshBuilder
    {
        private const int Sides = 18;
        private const int Rings = 12;

        private static readonly Color Top = Color.white;
        private static readonly Color Underside = new Color(0.7f, 0.8f, 0.94f);

        public static Mesh Build(int variant)
        {
            var random = new System.Random(variant * 7919 + 13);
            var builder = new LowPolyBuilder();

            // The base row: puffs swelling towards the middle, their bottoms sitting level.
            int basePuffs = 4 + variant % 2;

            for (int i = 0; i < basePuffs; i++)
            {
                float along = Mathf.Lerp(-0.8f, 0.8f, i / (float)(basePuffs - 1));
                float radius = Mathf.Lerp(0.5f, 0.32f, Mathf.Abs(along)) * Lerp(random, 0.9f, 1.1f);
                var at = new Vector2(along, Lerp(random, -0.08f, 0.08f));
                AddPuff(builder, at, radius * 0.75f, radius);
            }

            // Rounder puffs heaped on top, set a little back, for the billowing crown.
            int topPuffs = 2 + variant % 2;

            for (int i = 0; i < topPuffs; i++)
            {
                float along = Mathf.Lerp(-0.35f, 0.35f, topPuffs == 1 ? 0.5f : i / (float)(topPuffs - 1));
                float radius = Lerp(random, 0.42f, 0.55f) * (1f - Mathf.Abs(along) * 0.4f);
                var at = new Vector2(along + Lerp(random, -0.08f, 0.08f), Lerp(random, 0.05f, 0.15f));
                AddPuff(builder, at, 0.3f + radius * 0.6f, radius);
            }

            return builder.ToMesh("Cloud " + variant);
        }

        private static void AddPuff(LowPolyBuilder builder, Vector2 at, float height, float radius)
        {
            builder.AddShadedBlob(at, height, radius, radius * 0.92f, Sides, Rings, Underside, Top);
        }

        private static float Lerp(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
