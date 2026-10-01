using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Repaints every face that samples <see cref="From"/> with <see cref="To"/>.</summary>
    [Serializable]
    public struct PaletteCellRemap
    {
        [SerializeField] private PaletteCell from;
        [SerializeField] private PaletteCell to;

        public PaletteCellRemap(PaletteCell from, PaletteCell to)
        {
            this.from = from;
            this.to = to;
        }

        public PaletteCell From => from;

        public PaletteCell To => to;
    }
}
