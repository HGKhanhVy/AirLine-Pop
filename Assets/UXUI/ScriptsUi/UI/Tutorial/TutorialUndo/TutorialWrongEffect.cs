using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialWrongEffect : MonoBehaviour
{
    [Header("Shake Target")]
    [SerializeField] private RectTransform shakeTarget;

    [Header("Wrong Image")]
    [SerializeField] private Image wrongImage;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeStrength = 15f;

    [Header("Image Flash")]
    [SerializeField] private float fadeInDuration = 0.05f;
    [SerializeField] private float visibleDuration = 0.08f;
    [SerializeField] private float fadeOutDuration = 0.1f;

    private Vector2 originalPosition;

    private Coroutine effectCoroutine;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (shakeTarget != null)
        {
            originalPosition =
                shakeTarget.anchoredPosition;
        }

        // Không SetActive(false)
        // Image luôn Active.
        //
        // Chỉ đảm bảo Alpha = 0.
        HideWrongImage();
    }


    // ==================================================
    // PLAY EFFECT
    // ==================================================

    public void PlayWrongEffect()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine =
            StartCoroutine(
                WrongEffectRoutine()
            );
    }


    // ==================================================
    // MAIN EFFECT
    // ==================================================

    private IEnumerator WrongEffectRoutine()
    {
        // ------------------------------------------
        // Hiện Image
        // ------------------------------------------

        yield return FadeImage(
            0f,
            1f,
            fadeInDuration
        );


        // ------------------------------------------
        // Rung màn hình
        // ------------------------------------------

        yield return Shake();


        // ------------------------------------------
        // Giữ Image một chút
        // ------------------------------------------

        yield return new WaitForSeconds(
            visibleDuration
        );


        // ------------------------------------------
        // Tắt Image bằng Alpha
        // ------------------------------------------

        yield return FadeImage(
            1f,
            0f,
            fadeOutDuration
        );


        effectCoroutine = null;
    }


    // ==================================================
    // FADE IMAGE
    // ==================================================

    private IEnumerator FadeImage(
        float startAlpha,
        float targetAlpha,
        float duration
    )
    {
        if (wrongImage == null)
            yield break;


        Color color =
            wrongImage.color;


        color.a =
            startAlpha;


        wrongImage.color =
            color;


        if (duration <= 0f)
        {
            color.a =
                targetAlpha;

            wrongImage.color =
                color;

            yield break;
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            color.a =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );


            wrongImage.color =
                color;


            yield return null;
        }


        color.a =
            targetAlpha;


        wrongImage.color =
            color;
    }


    // ==================================================
    // SHAKE
    // ==================================================

    private IEnumerator Shake()
    {
        if (shakeTarget == null)
            yield break;


        float elapsed = 0f;


        while (elapsed < shakeDuration)
        {
            elapsed +=
                Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed / shakeDuration
                );


            // Càng về cuối càng nhẹ
            float strength =
                Mathf.Lerp(
                    shakeStrength,
                    0f,
                    progress
                );


            Vector2 randomOffset =
                Random.insideUnitCircle *
                strength;


            shakeTarget.anchoredPosition =
                originalPosition +
                randomOffset;


            yield return null;
        }


        // Trả về vị trí chính xác
        shakeTarget.anchoredPosition =
            originalPosition;
    }


    // ==================================================
    // HIDE IMAGE
    // ==================================================

    private void HideWrongImage()
    {
        if (wrongImage == null)
            return;


        Color color =
            wrongImage.color;


        color.a = 0f;


        wrongImage.color =
            color;


        // QUAN TRỌNG:
        // Không được SetActive(false)
        //
        // Image luôn Active.
    }
}