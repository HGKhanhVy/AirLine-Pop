using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialBlockUndo : MonoBehaviour
{
    [Header("Filled Sprite")]
    [SerializeField] private Sprite filledSprite;

    [Header("Hit Animation")]
    [SerializeField] private float scaleAmount = 1.08f;
    [SerializeField] private float scaleUpDuration = 0.08f;
    [SerializeField] private float scaleDownDuration = 0.12f;

    private Image blockImage;

    private Sprite originalSprite;

    private Vector3 originalScale;

    private Coroutine animationCoroutine;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        blockImage =
            GetComponent<Image>();

        // Lưu sprite hiện tại của Block
        // làm sprite gốc.
        if (blockImage != null)
        {
            originalSprite =
                blockImage.sprite;
        }
    
    
        transform.SetAsLastSibling();
    
    originalScale =
            transform.localScale;
    }



    // ==================================================
    // SET FILLED
    // ==================================================

    public void SetFilled()
    {
        if (blockImage == null)
        {
            blockImage =
                GetComponent<Image>();
        }


        // Đổi sang sprite đã đi qua
        if (filledSprite != null)
        {
            blockImage.sprite =
                filledSprite;
        }


        // Animation
        PlayHitAnimation();
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetVisual()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine = null;
        }


        // Reset scale
        transform.localScale =
            originalScale;


        if (blockImage == null)
        {
            blockImage =
                GetComponent<Image>();
        }


        // Trả lại sprite ban đầu
        if (blockImage != null)
        {
            blockImage.sprite =
                originalSprite;
        }
    }


    // ==================================================
    // HIT ANIMATION
    // ==================================================

    private void PlayHitAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );
        }


        animationCoroutine =
            StartCoroutine(
                HitAnimation()
            );
    }


    private IEnumerator HitAnimation()
    {
        Vector3 startScale =
            originalScale;

        Vector3 targetScale =
            originalScale * scaleAmount;


        // ==========================================
        // SCALE UP
        // ==========================================

        float time = 0f;


        while (time < scaleUpDuration)
        {
            time += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    time / scaleUpDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    t
                );


            yield return null;
        }


        // ==========================================
        // SCALE DOWN
        // ==========================================

        time = 0f;


        while (time < scaleDownDuration)
        {
            time += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    time / scaleDownDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            transform.localScale =
                Vector3.Lerp(
                    targetScale,
                    startScale,
                    t
                );


            yield return null;
        }


        // Đảm bảo scale chính xác
        transform.localScale =
            originalScale;


        animationCoroutine =
            null;
    }
}