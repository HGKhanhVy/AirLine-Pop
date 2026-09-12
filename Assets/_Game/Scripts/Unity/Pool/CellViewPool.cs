using System.Collections.Generic;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Recycles the squares a board is drawn from.
    ///
    /// Boards change on every level and range from three cells to ninety, so creating and
    /// destroying them per level would churn the heap for no reason. Nothing here is ever
    /// destroyed; released squares are hidden and handed out again.
    /// </summary>
    public sealed class CellViewPool
    {
        private readonly Stack<CellView> idle = new Stack<CellView>(64);
        private readonly List<CellView> live = new List<CellView>(64);
        private readonly ICellViewFactory factory;

        public int LiveCount => live.Count;

        /// <summary>Squares currently on the board, for effects that sweep over them.</summary>
        public IReadOnlyList<CellView> Live => live;

        public int IdleCount => idle.Count;

        public CellViewPool(ICellViewFactory factory)
        {
            this.factory = factory;
        }

        public CellView Acquire()
        {
            CellView view = idle.Count > 0 ? idle.Pop() : factory.Create();
            view.gameObject.SetActive(true);
            live.Add(view);
            return view;
        }

        /// <summary>Hands every live square back, which is what happens between levels.</summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < live.Count; i++)
            {
                CellView view = live[i];
                view.gameObject.SetActive(false);
                idle.Push(view);
            }

            live.Clear();
        }

        /// <summary>Grows the pool ahead of time so the first board of a session does not stutter.</summary>
        public void Prewarm(int count)
        {
            while (idle.Count < count)
            {
                CellView view = factory.Create();
                view.gameObject.SetActive(false);
                idle.Push(view);
            }
        }

    }
}
