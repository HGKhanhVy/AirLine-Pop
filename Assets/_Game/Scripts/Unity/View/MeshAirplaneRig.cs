using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The modelled airplane. Height is real: the model sits above the board on -Z, and its
    /// shadow is the same mesh flattened onto the board by its shader.
    /// </summary>
    public sealed class MeshAirplaneRig : AirplaneRig
    {
        [SerializeField] private MeshRenderer bodyRenderer;
        [SerializeField] private MeshRenderer shadowRenderer;

        [Tooltip("Wingspan in world units. A board square is about 1.1 across.")]
        [SerializeField, Min(0.1f)] private float wingspan = 0.9f;

        private Transform body;
        private Transform shadow;

        private void Awake()
        {
            body = bodyRenderer.transform;
            shadow = shadowRenderer.transform;
            bodyRenderer.sortingOrder = BoardSortingOrder.Airplane;
            shadowRenderer.sortingOrder = BoardSortingOrder.AirplaneShadow;
        }

        public override void Pose(Vector3 position, float heading, float bank, float scale, float height)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, heading) * Quaternion.AngleAxis(bank, Vector3.up);
            var placed = new Vector3(position.x, position.y, -height);
            Vector3 size = Vector3.one * (wingspan * scale);

            body.SetPositionAndRotation(placed, rotation);
            body.localScale = size;
            shadow.SetPositionAndRotation(placed, rotation);
            shadow.localScale = size;
        }

        public override void SetVisible(bool isVisible)
        {
            bodyRenderer.enabled = isVisible;
            shadowRenderer.enabled = isVisible;
        }
    }
}
