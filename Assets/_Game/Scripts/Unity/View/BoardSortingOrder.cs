namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The order the board's world renderers draw in.
    ///
    /// The connector runs *over* the squares. The reference game draws the path as a thick
    /// pipe in a lighter tint of the level's own colour, visibly crossing the middle of
    /// every square it has covered, not just the gaps between them.
    ///
    /// A cell is no longer one sprite either: it reuses the reference block art, whose own
    /// children sort from 0 to 10 among themselves, so anything meant to sit above a square
    /// has to clear that whole range.
    /// </summary>
    public static class BoardSortingOrder
    {
        /// <summary>The sea and the islands round the board.</summary>
        public const int Landscape = -100;

        /// <summary>Shadows the clouds cast on the sea.</summary>
        public const int CloudShadow = -95;

        /// <summary>Clouds drifting over the corners of the view.</summary>
        public const int Cloud = -60;

        /// <summary>The shallow water and surf round the foot of every block.</summary>
        public const int Shore = -10;

        /// <summary>The soft shadow under each flat tile, above the sky and clouds.</summary>
        public const int CellShadow = -20;

        /// <summary>The flat tile's fixed-colour art (lip, glint, outline) over its tinted face.</summary>
        public const int CellOverlay = 2;

        /// <summary>The soft shadow under a cat waiting on its square.</summary>
        public const int PassengerShadow = 3;

        /// <summary>A cat waiting on its square, above the runway so the route never hides it.</summary>
        public const int Passenger = 19;

        /// <summary>A cat mid-hop into the airplane, drawn over the plane it is jumping into.</summary>
        public const int PassengerBoarding = 37;

        public const int Cells = 0;

        /// <summary>Highest order used inside the cell prefab's own art.</summary>
        public const int CellArtCeiling = 10;

        /// <summary>A level rule's mark on its square (runway, wind): over the square's art, under the path.</summary>
        public const int RuleMarker = 11;

        /// <summary>
        /// First of the lines drawn under the path through the same points, such as the
        /// runway it is the centre line of. Each further underlay takes the next order, so
        /// there is room for two before the connector.
        /// </summary>
        public const int Underlay = 12;

        /// <summary>The pipe joining the squares, drawn across their faces.</summary>
        public const int Connector = 15;

        /// <summary>
        /// The white sheet a square washes over itself when the path lands on it. Above the
        /// pipe, because it is meant to read as light on the surface rather than under it.
        /// </summary>
        public const int ConnectFlash = 17;

        /// <summary>The hint's trail of puffs, over the squares it crosses.</summary>
        public const int HintTrail = 16;

        /// <summary>The hint's paper plane: over the board, under the plane it sets out from.</summary>
        public const int HintGuide = 34;

        /// <summary>
        /// The start and end dots. They sit above the pipe: the pipe runs through the
        /// middle of a square and would otherwise bury the very marks that say where the
        /// path begins and where it has stopped.
        /// </summary>
        public const int Marker = 18;

        /// <summary>The light riding the head of the path, above the pipe.</summary>
        public const int Spark = 20;

        /// <summary>The airplane's shadow, on the board surface above the path and its spark.</summary>
        public const int AirplaneShadow = 21;

        /// <summary>The dark of a night flight, over the board and under the planes.</summary>
        public const int NightShade = 22;

        /// <summary>What stays lit through a night flight: the runway lights, the gusts, the planes' glow.</summary>
        public const int NightLit = 23;

        /// <summary>
        /// The star on the square a level finishes on: lit through the night shade, under
        /// the planes taking off from that square.
        /// </summary>
        public const int GoalStar = 24;

        public const int Dust = 30;

        /// <summary>A night flight's lightning, over the dark but under the planes.</summary>
        public const int NightWeather = 33;

        /// <summary>The airplane flying the path, above the board but under the tutorial hand.</summary>
        public const int Airplane = 35;

        /// <summary>The cat in the cockpit, a separate sprite kept upright over the plane.</summary>
        public const int AirplanePilot = 36;

        /// <summary>The tutorial hand, above everything on the board it points at.</summary>
        public const int Guide = 40;

        /// <summary>The soft darkening round the edges of the view, over the whole world.</summary>
        public const int Vignette = 90;
    }
}
