namespace AirlinePop.SingleLine.Import
{
    /// <summary>The grid and the limits one batch of <see cref="BoardGenerator"/> boards is drawn within.</summary>
    public readonly struct BoardGeneratorSettings
    {
        public int Width { get; }

        public int Height { get; }

        public int MinCells { get; }

        public int MaxCells { get; }

        /// <summary>Share of the grid the route tries to cover before it stops on its own.</summary>
        public double FillTarget { get; }

        /// <summary>How strongly the route prefers squares with few free neighbours; 0 is a plain random walk.</summary>
        public double CornerBias { get; }

        public int MinBridges { get; }

        public int MaxBridges { get; }

        /// <summary>Chance per step of trying to fly over the route already drawn.</summary>
        public double BridgeChance { get; }

        public BoardGeneratorSettings(int width, int height, int minCells, int maxCells, double fillTarget = 1.0,
            double cornerBias = 1.5, int minBridges = 0, int maxBridges = 0, double bridgeChance = 0.25)
        {
            Width = width;
            Height = height;
            MinCells = minCells;
            MaxCells = maxCells;
            FillTarget = fillTarget;
            CornerBias = cornerBias;
            MinBridges = minBridges;
            MaxBridges = maxBridges;
            BridgeChance = bridgeChance;
        }
    }
}
