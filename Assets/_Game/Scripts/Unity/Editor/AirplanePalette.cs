using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    public readonly struct AirplanePalette
    {
        public AirplanePalette(Color body, Color wing, Color accent, Color glass, Color engine)
        {
            Body = body;
            Wing = wing;
            Accent = accent;
            Glass = glass;
            Engine = engine;
        }

        public Color Body { get; }

        public Color Wing { get; }

        /// <summary>The wing tips and the top of the fin.</summary>
        public Color Accent { get; }

        /// <summary>The windscreen band round the nose.</summary>
        public Color Glass { get; }

        public Color Engine { get; }
    }
}
