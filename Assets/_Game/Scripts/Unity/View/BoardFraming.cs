using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Where the board camera ended up after framing a board.</summary>
    public readonly struct BoardFraming
    {
        public BoardFraming(Vector3 lookAt, Vector3 viewDirection, float distance, Vector2 boardSize)
        {
            LookAt = lookAt;
            ViewDirection = viewDirection;
            Distance = distance;
            BoardSize = boardSize;
        }

        /// <summary>The point on the board plane the camera looks at.</summary>
        public Vector3 LookAt { get; }

        /// <summary>The camera's forward, pointing down into the board.</summary>
        public Vector3 ViewDirection { get; }

        /// <summary>How far the camera sits from <see cref="LookAt"/>.</summary>
        public float Distance { get; }

        public Vector2 BoardSize { get; }

        /// <summary>Where the camera's line of sight meets a plane parallel to the board, <paramref name="depth"/> below it.</summary>
        public Vector3 LookAtOnPlane(float depth)
        {
            float along = depth / Mathf.Max(0.01f, ViewDirection.z);
            return LookAt + ViewDirection * along;
        }
    }
}
