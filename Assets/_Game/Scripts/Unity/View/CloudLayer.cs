using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Puffy clouds drifting beneath the floating board. They wrap round when they
    /// pass the edge of the layer, so the sky never empties.
    ///
    /// Works in the layer's own space, so the layer can be scaled with the rest of the
    /// scenery and the clouds keep their place on screen.
    /// </summary>
    public sealed class CloudLayer : MonoBehaviour
    {
        [SerializeField] private Transform[] clouds = System.Array.Empty<Transform>();

        [Tooltip("Drift speed in layer units per second. Each cloud varies it a little so they do not move as one.")]
        [SerializeField] private float speed = 0.35f;

        [Tooltip("Half the width of the layer. A cloud passing this far from the middle comes back in on the other side.")]
        [SerializeField, Min(1f)] private float halfSpan = 14f;

        private void LateUpdate()
        {
            float step = speed * Time.deltaTime;

            for (int i = 0; i < clouds.Length; i++)
            {
                Vector3 position = clouds[i].localPosition;
                position.x += step * (0.7f + 0.3f * (i % 3));

                if (position.x > halfSpan)
                {
                    position.x = -halfSpan;
                }

                clouds[i].localPosition = position;
            }
        }
    }
}
