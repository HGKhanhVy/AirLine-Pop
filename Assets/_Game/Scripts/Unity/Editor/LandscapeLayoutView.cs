using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// The board camera at its reference distance, used to lay the world below out in
    /// screen terms: "an island peeking out at the top left" rather than in world units
    /// that would change with every tilt or field of view tweak.
    ///
    /// Results are in the landscape stage's own space, whose origin is where the camera's
    /// line of sight meets the ground and where the stage has a scale of one. Up, towards
    /// the board, is -Z.
    /// </summary>
    public readonly struct LandscapeLayoutView
    {
        private readonly Vector3 cameraPosition;
        private readonly Vector3 anchor;
        private readonly Quaternion rotation;
        private readonly float tanHalfHeight;
        private readonly float aspect;
        private readonly float groundDepth;

        public LandscapeLayoutView(float tilt, float fieldOfView, float aspect, float distance, float groundDepth)
        {
            rotation = Quaternion.Euler(-tilt, 0f, 0f);
            Vector3 forward = rotation * Vector3.forward;
            cameraPosition = -forward * distance;
            anchor = forward * (groundDepth / forward.z);
            tanHalfHeight = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            this.aspect = aspect;
            this.groundDepth = groundDepth;
        }

        public float GroundDepth => groundDepth;

        /// <summary>The spot on the ground seen at this viewport position.</summary>
        public Vector2 OnGround(float viewportX, float viewportY)
        {
            return OnPlane(viewportX, viewportY, groundDepth);
        }

        /// <summary>
        /// The point seen at this viewport position at <paramref name="depth"/> below the
        /// board surface. Its z is the height above the ground, as the stage stores it.
        /// </summary>
        public Vector3 OnPlane(float viewportX, float viewportY, float depth)
        {
            Vector3 ray = Ray(viewportX, viewportY);
            float along = (depth - cameraPosition.z) / Mathf.Max(1e-4f, ray.z);
            Vector3 hit = cameraPosition + ray * along;
            return new Vector3(hit.x - anchor.x, hit.y - anchor.y, depth - groundDepth);
        }

        /// <summary>How far the camera is from the ground seen at this viewport position.</summary>
        public float DistanceToGround(float viewportX, float viewportY)
        {
            Vector3 ray = Ray(viewportX, viewportY).normalized;
            return (groundDepth - cameraPosition.z) / Mathf.Max(1e-4f, ray.z);
        }

        private Vector3 Ray(float viewportX, float viewportY)
        {
            var local = new Vector3(
                (viewportX * 2f - 1f) * tanHalfHeight * aspect,
                (viewportY * 2f - 1f) * tanHalfHeight,
                1f);
            return rotation * local;
        }
    }
}
