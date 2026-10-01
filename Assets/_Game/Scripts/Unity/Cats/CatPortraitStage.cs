using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A tiny off-screen stage that films one cat into a texture, so a UI panel can show
    /// the companion cat reacting (GDD 3). The stage sits far from the board, so its own
    /// camera needs no layer set-up to see only the cat, and it renders only while shown.
    /// </summary>
    public sealed class CatPortraitStage : MonoBehaviour
    {
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform catAnchor;
        [SerializeField, Min(64)] private int textureSize = 512;
        [SerializeField, Min(0.1f)] private float catScale = 1f;

        private RenderTexture texture;
        private CatView cat;

        public RenderTexture Texture => texture;

        public bool HasCat => cat != null;

        public void Initialize(CatView prefab)
        {
            texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32) { name = "CatPortrait" };
            stageCamera.targetTexture = texture;
            stageCamera.enabled = false;

            if (prefab != null)
            {
                cat = Instantiate(prefab, catAnchor.position, catAnchor.rotation, catAnchor);
                cat.transform.localScale = Vector3.one * catScale;
                cat.TapCollider.enabled = false;
                cat.SetViewer(stageCamera.transform);

                // The win card shows the cat sitting and facing the player, not walking.
                cat.SetPose(CatPose.Wait);
            }
        }

        private void OnDestroy()
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
        }

        public void SetRendering(bool isRendering)
        {
            stageCamera.enabled = isRendering && texture != null;
        }

        public void Play(int trigger)
        {
            if (cat != null)
            {
                cat.Trigger(trigger);
            }
        }

#if UNITY_EDITOR
        public void EditorLink(Camera linkedCamera, Transform linkedAnchor, float scale)
        {
            stageCamera = linkedCamera;
            catAnchor = linkedAnchor;
            catScale = scale;
        }
#endif
    }
}
