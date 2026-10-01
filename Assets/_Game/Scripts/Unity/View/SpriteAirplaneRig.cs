using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The flat airplane. A top-down sprite cannot rise, so height is told the way 2D games
    /// always tell it: the shadow slides away from the plane and fades, and the plane grows.
    /// Bank narrows the wings, which is what a roll looks like from straight above.
    /// </summary>
    public sealed class SpriteAirplaneRig : AirplaneRig
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;

        [Tooltip("Wingspan in world units at scale 1. A board square is about 1.1 across.")]
        [SerializeField, Min(0.1f)] private float wingspan = 0.95f;

        [Tooltip("How far the shadow falls from the plane per unit of height, in world units.")]
        [SerializeField] private Vector2 shadowOffsetPerHeight = new Vector2(0.22f, -0.3f);

        [Tooltip("Shadow opacity on the ground and at the height below.")]
        [SerializeField, Range(0f, 1f)] private float groundShadowAlpha = 0.32f;

        [SerializeField, Range(0f, 1f)] private float highShadowAlpha = 0.12f;

        [SerializeField, Min(0.1f)] private float highHeight = 3f;

        [Tooltip("Narrowest the wings get at full bank, as a share of their width.")]
        [SerializeField, Range(0.3f, 1f)] private float minBankWidth = 0.7f;

        [Header("Pilot")]
        [Tooltip("Optional. The cat in the cockpit, kept upright while the plane turns so it always faces the player.")]
        [SerializeField] private SpriteRenderer pilotRenderer;

        [Tooltip("Cockpit centre ahead of the plane's centre, as a share of the plane sprite's size.")]
        [SerializeField] private float cockpitAhead = 0.158f;

        private Transform body;
        private Transform shadow;
        private Transform pilot;
        private Color shadowColor;

        private void Awake()
        {
            body = bodyRenderer.transform;
            shadow = shadowRenderer.transform;
            bodyRenderer.sortingOrder = BoardSortingOrder.Airplane;
            shadowRenderer.sortingOrder = BoardSortingOrder.AirplaneShadow;
            shadowColor = shadowRenderer.color;

            if (pilotRenderer != null)
            {
                pilot = pilotRenderer.transform;
                pilotRenderer.sortingOrder = BoardSortingOrder.AirplanePilot;
            }
        }

        public override void Pose(Vector3 position, float heading, float bank, float scale, float height)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, heading);
            float size = wingspan * scale;
            float width = Mathf.Lerp(minBankWidth, 1f, Mathf.Abs(Mathf.Cos(bank * Mathf.Deg2Rad)));
            var sized = new Vector3(size * width, size, 1f);

            body.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), rotation);
            body.localScale = sized;

            Vector2 offset = shadowOffsetPerHeight * height;
            shadow.SetPositionAndRotation(new Vector3(position.x + offset.x, position.y + offset.y, 0f), rotation);
            shadow.localScale = sized;

            float high = Mathf.Clamp01(height / highHeight);
            shadowColor.a = Mathf.Lerp(groundShadowAlpha, highShadowAlpha, high);
            shadowRenderer.color = shadowColor;

            if (pilot != null)
            {
                // Rides on the cockpit as the plane turns, but never turns itself.
                Vector3 cockpit = rotation * new Vector3(0f, cockpitAhead * size, 0f);
                pilot.SetPositionAndRotation(new Vector3(position.x + cockpit.x, position.y + cockpit.y, 0f), Quaternion.identity);
                pilot.localScale = new Vector3(size, size, 1f);
            }
        }

        public override void SetVisible(bool isVisible)
        {
            bodyRenderer.enabled = isVisible;
            shadowRenderer.enabled = isVisible;

            if (pilotRenderer != null)
            {
                pilotRenderer.enabled = isVisible;
            }
        }
    }
}
