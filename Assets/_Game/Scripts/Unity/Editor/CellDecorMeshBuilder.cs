using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the looks a grass square can be dressed with: two or three trees, pines and
    /// broadleaf trees in turn, and a rock or two, from the same kit as the islands round
    /// the board (<see cref="IslandProps"/>). A square is only dressed while the route has
    /// not crossed it, so the whole top is free to use. In the size root's units, where a
    /// block is about 0.97 across.
    /// </summary>
    public static class CellDecorMeshBuilder
    {
        public const int VariantCount = 5;

        private const float TreeSize = 0.5f;
        private const float RockSize = 0.6f;

        // Tree spots and rock spots per look, laid out so the trees never crowd one another.
        private static readonly (Vector2[] trees, Vector2[] rocks)[] Layouts =
        {
            (new[] { new Vector2(-0.18f, 0.16f), new Vector2(0.17f, -0.12f) }, new[] { new Vector2(0.22f, 0.24f) }),
            (new[] { new Vector2(-0.2f, -0.14f), new Vector2(0.02f, 0.2f), new Vector2(0.22f, -0.16f) }, new Vector2[0]),
            (new[] { new Vector2(0.14f, 0.1f) }, new[] { new Vector2(-0.2f, -0.18f), new Vector2(-0.22f, 0.2f) }),
            (new[] { new Vector2(-0.15f, 0.18f), new Vector2(0.18f, 0.16f), new Vector2(-0.02f, -0.18f) }, new[] { new Vector2(0.24f, -0.24f) }),
            (new[] { new Vector2(-0.16f, -0.06f), new Vector2(0.2f, 0.16f) }, new Vector2[0])
        };

        public static Mesh Build(int variant)
        {
            var random = new System.Random(variant * 977 + 5);
            var builder = new LowPolyBuilder();
            var (trees, rocks) = Layouts[variant % Layouts.Length];

            for (int i = 0; i < trees.Length; i++)
            {
                float size = TreeSize * (0.85f + 0.3f * (float)random.NextDouble());

                // Pines and round trees take turns, so no square is a row of one kind.
                if ((i + variant) % 2 == 0)
                {
                    IslandProps.AddPine(builder, trees[i], size, random);
                }
                else
                {
                    IslandProps.AddRoundTree(builder, trees[i], size, random);
                }
            }

            foreach (Vector2 at in rocks)
            {
                IslandProps.AddRock(builder, at, RockSize, random);
            }

            return builder.ToMesh("Cell Decor " + variant);
        }
    }
}
