using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps the world far below the floating board looking the same on every level.
    ///
    /// The camera pulls back further for a big board than for a small one. Scenery fixed
    /// in the world would then drift round the screen from level to level. Instead the
    /// stage is anchored where the camera's line of sight meets the ground and scaled with
    /// the camera's distance to it, so every island, cloud and boat keeps its place on
    /// screen. It only moves on a re-frame.
    /// </summary>
    public sealed class LandscapeStage : MonoBehaviour
    {
        [SerializeField] private BoardCameraFramer framer;
        [SerializeField] private Transform stage;

        [Tooltip("Camera distance the scenery was laid out for; at this distance the stage has a scale of one.")]
        [SerializeField, Min(1f)] private float referenceDistance = 20f;

        [Tooltip("How far below the board the ground lies. Deep enough that the board reads as flying.")]
        [SerializeField, Min(0f)] private float groundDepth = 30f;

        private void OnEnable()
        {
            framer.OnFramed += HandleFramed;

            if (framer.HasFramed)
            {
                HandleFramed(framer.Framing);
            }
        }

        private void OnDisable()
        {
            framer.OnFramed -= HandleFramed;
        }

        private void HandleFramed(BoardFraming framing)
        {
            Vector3 anchor = framing.LookAtOnPlane(groundDepth);
            anchor.z = groundDepth;
            stage.position = anchor;

            // Scaling about the anchor keeps every prop on the same spot of the screen only
            // when it matches the camera's distance to the ground, not to the board.
            float toGround = groundDepth / Mathf.Max(0.01f, framing.ViewDirection.z);
            float scale = (framing.Distance + toGround) / (referenceDistance + toGround);
            stage.localScale = Vector3.one * scale;
        }
    }
}
