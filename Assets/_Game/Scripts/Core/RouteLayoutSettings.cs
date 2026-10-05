namespace ASTeams.SingleLine.Core
{
    /// <summary>The numbers that shape the route map; every distance is in map units.</summary>
    public readonly struct RouteLayoutSettings
    {
        public RouteLayoutSettings(float width, float levelSpacing, float cityApproach, float citySpacing, float hubOffset,
            float rowHeight, float swing, float swingJitter, float crossingRise, float edgeMargin)
        {
            HubOffset = hubOffset;
            Width = width;
            LevelSpacing = levelSpacing;
            CityApproach = cityApproach;
            CitySpacing = citySpacing;
            RowHeight = rowHeight;
            Swing = swing;
            SwingJitter = swingJitter;
            CrossingRise = crossingRise;
            EdgeMargin = edgeMargin;
        }

        public float Width { get; }

        /// <summary>Distance along the route between two ordinary levels.</summary>
        public float LevelSpacing { get; }

        /// <summary>Distance into a city's last level, the one that lands at its airport.</summary>
        public float CityApproach { get; }

        /// <summary>Distance out of a city to the next level: the hop between two destinations, its airport on the way.</summary>
        public float CitySpacing { get; }

        /// <summary>Distance from a city's last level to its airport card, on the hop out of the city.</summary>
        public float HubOffset { get; }

        /// <summary>How far the route climbs between two turns inside a country.</summary>
        public float RowHeight { get; }

        /// <summary>How far each turn swings out from the middle.</summary>
        public float Swing { get; }

        public float SwingJitter { get; }

        /// <summary>How far the route climbs on each pass of an international leg.</summary>
        public float CrossingRise { get; }

        /// <summary>How close an international leg comes to the sides.</summary>
        public float EdgeMargin { get; }
    }
}
