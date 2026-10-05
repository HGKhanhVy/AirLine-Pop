using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>A bundle of snacks sold in the shop: bigger packs cost less per snack.</summary>
    [Serializable]
    public sealed class SnackPack
    {
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField, Min(0)] private int price = 40;
        [SerializeField] private Sprite artwork;

        public SnackPack(int quantity, int price)
        {
            this.quantity = quantity;
            this.price = price;
        }

        public int Quantity => quantity;

        public int Price => price;

        public Sprite Artwork => artwork;

#if UNITY_EDITOR
        public void EditorSetArtwork(Sprite linkedArtwork)
        {
            artwork = linkedArtwork;
        }
#endif
    }
}
