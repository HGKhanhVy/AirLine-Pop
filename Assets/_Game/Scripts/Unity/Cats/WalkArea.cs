using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Where cats may walk: a floor rectangle minus blocked rectangles for furniture. Kept
    /// as plain rectangles because the rooms are small, fixed layouts (GDD 4: V1 furniture
    /// does not move), which makes a navmesh unnecessary.
    /// </summary>
    public sealed class WalkArea : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(6f, 6f);
        [SerializeField] private Rect[] blocked = new Rect[0];

        private const int SampleAttempts = 12;

        public Vector3 RandomPoint()
        {
            for (int i = 0; i < SampleAttempts; i++)
            {
                var local = new Vector2(Random.Range(-size.x, size.x) * 0.5f, Random.Range(-size.y, size.y) * 0.5f);

                if (!IsBlocked(local))
                {
                    return ToWorld(local);
                }
            }

            return transform.position;
        }

        /// <summary>True when a world point is on the floor and clear of furniture.</summary>
        public bool IsWalkable(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            var flat = new Vector2(local.x, local.z);
            return Mathf.Abs(flat.x) <= size.x * 0.5f && Mathf.Abs(flat.y) <= size.y * 0.5f && !IsBlocked(flat);
        }

        private bool IsBlocked(Vector2 local)
        {
            for (int i = 0; i < blocked.Length; i++)
            {
                if (blocked[i].Contains(local))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 ToWorld(Vector2 local)
        {
            return transform.TransformPoint(new Vector3(local.x, 0f, local.y));
        }

#if UNITY_EDITOR
        public void EditorConfigure(Vector2 floorSize, Rect[] furniture)
        {
            size = floorSize;
            blocked = furniture;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.01f, size.y));
            Gizmos.color = Color.red;

            foreach (Rect r in blocked)
            {
                Gizmos.DrawWireCube(new Vector3(r.center.x, 0f, r.center.y), new Vector3(r.width, 0.01f, r.height));
            }
        }
#endif
    }
}
