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

        // The VIP guest, who takes the companion's place on the card after a VIP flight.
        private CatView guest;

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

        /// <summary>Seats the VIP guest on the stage too, out of sight until a VIP flight is won.</summary>
        public void InitializeGuest(CatView prefab)
        {
            if (prefab == null)
            {
                return;
            }

            guest = Instantiate(prefab, catAnchor.position, catAnchor.rotation, catAnchor);
            guest.transform.localScale = Vector3.one * catScale;
            guest.TapCollider.enabled = false;
            guest.SetViewer(stageCamera.transform);
            guest.SetPose(CatPose.Wait);
            guest.gameObject.SetActive(false);
        }

        /// <summary>Puts the VIP guest in front of the camera instead of the companion, or back again.</summary>
        public void ShowGuest(bool isShown)
        {
            bool hasGuest = isShown && guest != null;

            if (guest != null)
            {
                guest.gameObject.SetActive(hasGuest);
            }

            if (cat != null)
            {
                cat.gameObject.SetActive(!hasGuest);
            }
        }

        public void SetRendering(bool isRendering)
        {
            stageCamera.enabled = isRendering && texture != null;
        }

        public void Play(int trigger)
        {
            CatView shown = guest != null && guest.gameObject.activeSelf ? guest : cat;

            if (shown != null)
            {
                shown.Trigger(trigger);
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
