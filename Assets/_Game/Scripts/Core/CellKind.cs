namespace AirlinePop.SingleLine.Core
{
    /// <summary>What a marked square asks of the plane passing over it.</summary>
    public enum CellKind : byte
    {
        Plain = 0,

        /// <summary>Turbulence cloud: the plane must fly straight through, no turning on it.</summary>
        Straight = 1,

        /// <summary>Beacon: flown in from its tail and out along its arrow, a one-way passage.</summary>
        Beacon = 2,

        /// <summary>Overpass: flown over twice, once each way across, straight through both times.</summary>
        Bridge = 3
    }
}
