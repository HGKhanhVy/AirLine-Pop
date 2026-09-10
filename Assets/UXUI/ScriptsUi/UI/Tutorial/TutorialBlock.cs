using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialBlock : MonoBehaviour
{
    [Header("Block Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite filledSprite;

    [Header("Hit Animation")]
    [SerializeField] private float scaleAmount = 1.08f;
    [SerializeField] private float scaleUpDuration = 0.08f;
    [SerializeField] private float scaleDownDuration = 0.12f;

    private Image blockImage;

    private Vector3 originalScale;

    private Coroutine scaleCoroutine;


    private void Awake()
    {
        blockImage = GetComponent<Image>();

        // Lưu kích thước ban đầu của Block
        originalScale = transform.localScale;
    }


    // ==================================================
    // SET FILLED
    // ==================================================

    public void SetFilled(bool filled)
    {
        if (blockImage == null)
        {
            blockImage = GetComponent<Image>();
        }

        if (filled)
        {
            // Đổi sang Sprite đã đi qua
            if (filledSprite != null)
            {
                blockImage.sprite = filledSprite;
            }

            // Chạy hiệu ứng scale
            PlayHitAnimation();
        }
        else
        {
            // Đổi lại Sprite bình thường
            if (normalSprite != null)
            {
                blockImage.sprite = normalSprite;
            }

            // Reset scale
            ResetScale();
        }
    }


    // ==================================================
    // HIT ANIMATION
    // ==================================================

    private void PlayHitAnimation()
    {
        // Nếu block đang scale thì dừng animation cũ
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }

        scaleCoroutine = StartCoroutine(ScaleEffect());
    }


    private IEnumerator ScaleEffect()
    {
        Vector3 startScale = originalScale;

        Vector3 bigScale =
            originalScale * scaleAmount;


        // ==========================================
        // SCALE TO
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
                    bigScale,
                    t
                );

            yield return null;
        }


        // ==========================================
        // SCALE BACK
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
                    bigScale,
                    startScale,
                    t
                );

            yield return null;
        }


        // Đảm bảo trở về đúng kích thước ban đầu
        transform.localScale =
            originalScale;

        scaleCoroutine = null;
    }


    // ==================================================
    // RESET SCALE
    // ==================================================

    private void ResetScale()
    {
        // Nếu đang chạy animation thì dừng
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);

            scaleCoroutine = null;
        }

        // Trả Block về kích thước ban đầu
        transform.localScale =
            originalScale;
    }


    // ==================================================
    // RESET BLOCK
    // ==================================================

    public void ResetBlock()
    {
        ResetScale();

        if (blockImage == null)
        {
            blockImage = GetComponent<Image>();
        }

        // Đổi lại Sprite ban đầu
        if (normalSprite != null)
        {
            blockImage.sprite =
                normalSprite;
        }
    }
}