using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialHand : MonoBehaviour
{
    [Header("Hand")]
    [SerializeField] private Image handImage;

    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressSprite;


    [Header("Movement Points")]
    [SerializeField] private RectTransform[] points;


    [Header("Blocks")]
    [SerializeField] private TutorialBlock[] blocks;


    [Header("Path")]
    [SerializeField] private TutorialPath tutorialPath;


    [Header("Timing")]
    [SerializeField] private float appearDuration = 0.25f;

    [SerializeField] private float moveToStartDuration = 0.6f;

    [SerializeField] private float pressDuration = 0.25f;

    [SerializeField] private float dragDurationPerPoint = 0.35f;

    [SerializeField] private float endWaitDuration = 0.8f;

    [SerializeField] private float restartDelay = 0.3f;


    // ==================================================
    // COROUTINE
    // ==================================================

    private Coroutine tutorialCoroutine;


    // ==================================================
    // UNITY
    // ==================================================

    private void Start()
    {
        Play();
    }


    // ==================================================
    // PLAY TUTORIAL
    // ==================================================

    public void Play()
    {
        // Nếu đang chạy thì dừng trước
        StopTutorial();

        // Chạy lại tutorial từ đầu
        tutorialCoroutine = StartCoroutine(PlayTutorial());
    }


    // ==================================================
    // STOP TUTORIAL
    // ==================================================

    public void StopTutorial()
    {
        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }

        ResetTutorial();
    }


    // ==================================================
    // MAIN TUTORIAL LOOP
    // ==================================================

    private IEnumerator PlayTutorial()
    {
        while (true)
        {
            yield return StartTutorial();

            yield return new WaitForSeconds(
                restartDelay
            );
        }
    }


    // ==================================================
    // START TUTORIAL
    // ==================================================

    private IEnumerator StartTutorial()
    {
        // ========================================
        // 1. RESET
        // ========================================

        ResetTutorial();


        // ========================================
        // 2. HAND XUẤT HIỆN
        // ========================================

        transform.localScale = Vector3.zero;

        yield return ScaleHand(
            Vector3.zero,
            Vector3.one,
            appearDuration
        );


        // ========================================
        // 3. ĐI TỚI BLOCK ĐẦU
        // ========================================

        if (points == null || points.Length == 0)
        {
            yield break;
        }

        yield return MoveHand(
            points[0].position,
            moveToStartDuration
        );


        // ========================================
        // 4. NHẤN
        // ========================================

        if (handImage != null)
        {
            handImage.sprite = pressSprite;
        }

        yield return new WaitForSeconds(
            pressDuration
        );


        // ========================================
        // 5. BLOCK ĐẦU TIÊN ĐỔI MÀU
        // ========================================

        FillBlock(0);


        // ========================================
        // 6. BẮT ĐẦU KÉO
        // ========================================

        for (int i = 1; i < points.Length; i++)
        {
            Vector3 startPosition =
                points[i - 1].position;

            Vector3 targetPosition =
                points[i].position;


            // ------------------------------------
            // Di chuyển ngón tay
            // ------------------------------------

            yield return MoveHand(
                targetPosition,
                dragDurationPerPoint
            );


            // ------------------------------------
            // Tạo Path phía sau ngón tay
            // ------------------------------------

            if (tutorialPath != null)
            {
                tutorialPath.AddPath(
                    startPosition,
                    targetPosition
                );
            }


            // ------------------------------------
            // Block đổi sang sprite đã đi qua
            // ------------------------------------

            FillBlock(i);
        }


        // ========================================
        // 7. THẢ NGÓN TAY
        // ========================================

        if (handImage != null)
        {
            handImage.sprite = normalSprite;
        }


        // ========================================
        // 8. CHỜ Ở CUỐI
        // ========================================

        yield return new WaitForSeconds(
            endWaitDuration
        );
    }


    // ==================================================
    // FILL BLOCK
    // ==================================================

    private void FillBlock(int index)
    {
        if (blocks == null)
            return;

        if (index < 0 || index >= blocks.Length)
            return;

        if (blocks[index] != null)
        {
            blocks[index].SetFilled(true);
        }
    }


    // ==================================================
    // RESET
    // ==================================================

    private void ResetTutorial()
    {
        // ----------------------------------------
        // Reset hand sprite
        // ----------------------------------------

        if (handImage != null)
        {
            handImage.sprite = normalSprite;
        }


        // ----------------------------------------
        // Reset vị trí
        // ----------------------------------------

        if (points != null && points.Length > 0)
        {
            transform.position =
                points[0].position;
        }


        // ----------------------------------------
        // Reset scale
        // ----------------------------------------

        transform.localScale =
            Vector3.zero;


        // ----------------------------------------
        // Xóa Path
        // ----------------------------------------

        if (tutorialPath != null)
        {
            tutorialPath.ClearPath();
        }


        // ----------------------------------------
        // Reset tất cả Block
        // ----------------------------------------

        if (blocks != null)
        {
            foreach (TutorialBlock block in blocks)
            {
                if (block != null)
                {
                    block.ResetBlock();
                }
            }
        }
    }


    // ==================================================
    // MOVE HAND
    // ==================================================

    private IEnumerator MoveHand(
        Vector3 target,
        float duration
    )
    {
        Vector3 start =
            transform.position;

        float time = 0f;


        while (time < duration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / duration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            transform.position =
                Vector3.Lerp(
                    start,
                    target,
                    t
                );


            yield return null;
        }


        transform.position = target;
    }


    // ==================================================
    // SCALE HAND
    // ==================================================

    private IEnumerator ScaleHand(
        Vector3 start,
        Vector3 target,
        float duration
    )
    {
        float time = 0f;


        while (time < duration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / duration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            transform.localScale =
                Vector3.Lerp(
                    start,
                    target,
                    t
                );


            yield return null;
        }


        transform.localScale = target;
    }
}