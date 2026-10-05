using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Where the top of the head and the neck are in every drawing of every cat, measured
    /// from the drawings by the editor, so a hat or a bow can ride the cat through a walk or
    /// a hop. Drawings where the cat is rolled over carry no anchor, and accessories hide for them.
    /// </summary>
    public sealed class CatHeadAnchorsSO : ScriptableObject
    {
        [SerializeField] private Sprite[] sprites = new Sprite[0];

        [Tooltip("The top of the head in each drawing, in the drawing's own units from its pivot.")]
        [SerializeField] private Vector2[] heads = new Vector2[0];

        [Tooltip("The neck, just under the chin, in each drawing, in the drawing's own units from its pivot.")]
        [SerializeField] private Vector2[] necks = new Vector2[0];

        [Tooltip("False for drawings where nothing can be worn, such as a roll.")]
        [SerializeField] private bool[] upright = new bool[0];

        private Dictionary<Sprite, int> index;

        /// <summary>True with the slot's position when this drawing has somewhere to wear an accessory.</summary>
        public bool TryGetAnchor(Sprite sprite, AccessorySlot slot, out Vector2 anchor)
        {
            index ??= BuildIndex();
            anchor = Vector2.zero;

            if (sprite == null || !index.TryGetValue(sprite, out int i) || !upright[i])
            {
                return false;
            }

            anchor = slot == AccessorySlot.Neck ? necks[i] : heads[i];
            return true;
        }

        private Dictionary<Sprite, int> BuildIndex()
        {
            var built = new Dictionary<Sprite, int>(sprites.Length);

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    built[sprites[i]] = i;
                }
            }

            return built;
        }

#if UNITY_EDITOR
        public void EditorSet(Sprite[] linkedSprites, Vector2[] linkedHeads, Vector2[] linkedNecks, bool[] linkedUpright)
        {
            sprites = linkedSprites;
            heads = linkedHeads;
            necks = linkedNecks;
            upright = linkedUpright;
            index = null;
        }
#endif
    }
}
