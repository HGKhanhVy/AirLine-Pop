using System.Collections.Generic;
using UnityEngine;

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
        private readonly Transform parent;

        /// <summary>
        /// One unlit material shared by every square. The project renders through URP 2D,
        /// where the default sprite material reacts to lights; the board is flat colour by
        /// design, so it opts out rather than depending on a light existing in the scene.
        /// </summary>
        private Material sharedMaterial;

        public int LiveCount => live.Count;

        public int IdleCount => idle.Count;

        public CellViewPool(Transform parent)
        {
            this.parent = parent;
        }

        public CellView Acquire()
        {
            CellView view = idle.Count > 0 ? idle.Pop() : Create();
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
                CellView view = Create();
                view.gameObject.SetActive(false);
                idle.Push(view);
            }
        }

        private CellView Create()
        {
            if (sharedMaterial == null)
            {
                sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            var go = new GameObject("Cell", typeof(SpriteRenderer), typeof(CellView));
            go.transform.SetParent(parent, worldPositionStays: false);

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sharedMaterial = sharedMaterial;

            var view = go.GetComponent<CellView>();
            view.Bind(renderer);
            return view;
        }
    }
}
