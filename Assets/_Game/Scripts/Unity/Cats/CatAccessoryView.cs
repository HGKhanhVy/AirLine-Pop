using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The accessory a cat wears, on the top of its head or under its chin. The body's drawing
    /// changes every few frames as the cat walks or hops; each time it does, the accessory
    /// moves to that drawing's head or neck, and hides through drawings with nowhere to wear
    /// it, such as a roll.
    /// </summary>
    public sealed class CatAccessoryView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer accessory;
        [SerializeField] private CatHeadAnchorsSO anchors;

        [Tooltip("How far a hat sinks into the head, in the drawing's units, so it sits rather than floats.")]
        [SerializeField] private float sink = 0.03f;

        [Tooltip("Optional. Worn from the start, for a cat who always dresses the same, such as His Majesty's robe.")]
        [SerializeField] private AccessorySO wornFromStart;

        private Sprite lastBody;
        private bool isWearing;
        private AccessorySlot slot;

        private void Awake()
        {
            accessory.enabled = false;

            if (wornFromStart != null)
            {
                Wear(wornFromStart);
            }
        }

        /// <summary>Puts an accessory on, or takes it off with null.</summary>
        public void Wear(AccessorySO item)
        {
            isWearing = item != null && item.Worn != null;
            accessory.sprite = isWearing ? item.Worn : null;
            slot = isWearing ? item.Slot : AccessorySlot.Head;
            accessory.transform.localScale = Vector3.one * (isWearing ? item.Scale : 1f);
            lastBody = null;
            Follow();
        }

        private void LateUpdate()
        {
            if (isWearing && body.sprite != lastBody)
            {
                Follow();
            }
        }

        private void Follow()
        {
            lastBody = body.sprite;

            if (!isWearing || !anchors.TryGetAnchor(lastBody, slot, out Vector2 anchor))
            {
                accessory.enabled = false;
                return;
            }

            float drop = slot == AccessorySlot.Head ? sink : 0f;
            accessory.transform.localPosition = new Vector3(anchor.x, anchor.y - drop, 0f);
            accessory.enabled = true;
        }

#if UNITY_EDITOR
        public void EditorLink(SpriteRenderer linkedBody, SpriteRenderer linkedAccessory, CatHeadAnchorsSO linkedAnchors)
        {
            body = linkedBody;
            accessory = linkedAccessory;
            anchors = linkedAnchors;
        }

        public void EditorLinkWornFromStart(AccessorySO linkedItem)
        {
            wornFromStart = linkedItem;
        }
#endif
    }
}
