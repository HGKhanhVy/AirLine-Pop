using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A tiny off-screen stage that films one cat into a texture, so a UI panel can show a
    /// cat reacting (GDD 3): the companion as a rule, His Majesty the VIP guest after a VIP
    /// flight, or a new regular on the flight that brings it. The stage sits far from the
    /// board, so its own camera needs no layer set-up to see only the cat, and it renders
    /// only while shown. Each cat is seated once and then only shown or hidden.
    /// </summary>
    public sealed class CatPortraitStage : MonoBehaviour
    {
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform catAnchor;
        [SerializeField, Min(64)] private int textureSize = 512;
        [SerializeField, Min(0.1f)] private float catScale = 1f;

        private readonly Dictionary<CatView, CatView> arrivals = new Dictionary<CatView, CatView>();
        private RenderTexture texture;
        private CatView cat;
        private CatView guest;
        private CatView featured;

        public RenderTexture Texture => texture;

        public bool HasCat => cat != null;

        public void Initialize(CatView prefab)
        {
            texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32) { name = "CatPortrait" };
            stageCamera.targetTexture = texture;
            stageCamera.enabled = false;
            cat = Seat(prefab);
            Feature(cat);
        }

        /// <summary>Seats the VIP guest too, out of sight until a VIP flight is won.</summary>
        public void InitializeGuest(CatView prefab)
        {
            guest = Seat(prefab);
            Feature(featured);
        }

        public void FeatureCompanion()
        {
            Feature(cat);
        }

        /// <summary>Puts His Majesty in front of the camera, or the companion when there is no guest.</summary>
        public void FeatureGuest()
        {
            Feature(guest != null ? guest : cat);
        }

        /// <summary>Puts the regular who has just joined in front of the camera.</summary>
        public void FeatureArrival(CatView prefab)
        {
            if (prefab == null)
            {
                FeatureCompanion();
                return;
            }

            if (!arrivals.TryGetValue(prefab, out CatView arrival))
            {
                arrival = Seat(prefab);
                arrivals.Add(prefab, arrival);
            }

            Feature(arrival);
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
            if (featured != null)
            {
                featured.Trigger(trigger);
            }
        }

        private CatView Seat(CatView prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            CatView seated = Instantiate(prefab, catAnchor.position, catAnchor.rotation, catAnchor);
            seated.transform.localScale = Vector3.one * catScale;
            seated.TapCollider.enabled = false;
            seated.SetViewer(stageCamera.transform);

            // The win card shows the cat sitting and facing the player, not walking.
            seated.SetPose(CatPose.Wait);
            seated.gameObject.SetActive(false);
            return seated;
        }

        /// <summary>Shows one cat on the stage and hides the rest.</summary>
        private void Feature(CatView shown)
        {
            featured = shown;
            SetShown(cat, shown);
            SetShown(guest, shown);

            foreach (CatView arrival in arrivals.Values)
            {
                SetShown(arrival, shown);
            }

            // An animator starts over when its cat is switched back on, so the pose is set again.
            if (shown != null)
            {
                shown.SetPose(CatPose.Wait);
            }
        }

        private static void SetShown(CatView candidate, CatView shown)
        {
            if (candidate != null)
            {
                candidate.gameObject.SetActive(candidate == shown);
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
