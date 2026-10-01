using System;

using UnityEngine;
using UnityEngine.UI;

namespace Graphie
{
    public class AnimationRunner : MonoBehaviour
    {
        [Header("Animation Variables")]
        [Tooltip("Are you using image UI or Sprite Renderer?")]
        public bool isUsingImage;
        public AnimationData animationData;

        [Header("References")]
        public Animator animator;
        [Tooltip("Title, you may use TextMeshPro")]
        public Text animationTitleText;

        public void Awake()
        {
            SelectAndPlay(animationData.state, animationData.title);
        }

        public void SelectAndPlay(string state, string title)
        {
            PlayAnimation(state);

            if (isUsingImage)
            {
                if (animationTitleText != null)
                animationTitleText.text = title;
            }
        }

        public void SelectAndPlay(AnimationData data)
            => SelectAndPlay(data.state, data.title);

        void PlayAnimation(string state)
            => animator?.Play(state);


        void SetTitle(string title)
            => animationTitleText.text = title;

#if UNITY_EDITOR
        private void Reset()
        {
            animator = GetComponent<Animator>();
            animationTitleText = GetComponentInChildren<Text>();
        }
#endif
    }

    [Serializable]
    public struct AnimationData
    {
        public string state;
        [TextArea]
        public string title;
    }
}