using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHandUndo : MonoBehaviour
{
    [Header("Movement Points")]
    [SerializeField] private RectTransform[] points;

    [Header("Block Controller")]
    [SerializeField] private TutorialUndoBlockController blockController;

    [Header("Wrong Effect")]
    [SerializeField] private TutorialWrongEffect wrongEffect;

    [Header("Hand")]
    [SerializeField] private Image handImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressSprite;

    [Header("Hand Appear")]
    [SerializeField] private float appearDuration = 0.25f;

    [Header("Press Animation")]
    [SerializeField] private float pressScale = 0.85f;
    [SerializeField] private float pressDownDuration = 0.12f;
    [SerializeField] private float pressDuration = 0.2f;
    [SerializeField] private float pressUpDuration = 0.15f;

    [Header("Path")]
    [SerializeField] private RectTransform pathContainer;
    [SerializeField] private Sprite pathSprite;
    [SerializeField] private Color pathColor = Color.white;
    [SerializeField] private float pathWidth = 30f;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float waitAtEndDuration = 0.8f;
    [SerializeField] private float restartDelay = 0.3f;

    private Coroutine tutorialCoroutine;

    private Vector3 normalScale;

    private UiPathPiecePool pathPieces;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        normalScale = transform.localScale;

        if (pathContainer != null)
        {
            pathPieces = new UiPathPiecePool(pathContainer, "TutorialPathPiece");
        }
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        StopTutorial();
    }


    // ==================================================
    // PLAY
    // ==================================================

    public void Play()
    {
        if (tutorialCoroutine != null)
            return;

        if (points == null || points.Length < 2)
        {
            Debug.LogWarning(
                "TutorialHandUndo: Chưa có đủ Points."
            );

            return;
        }

        gameObject.SetActive(true);

        tutorialCoroutine =
            StartCoroutine(TutorialRoutine());
    }


    // ==================================================
    // STOP
    // ==================================================

    public void StopTutorial()
    {
        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }

        ClearPath();

        // Reset block bằng Controller mới.
        // Không gọi TutorialBlockUndo.
        if (blockController != null)
        {
            blockController.ResetAll();
        }

        transform.localScale = normalScale;

        if (handImage != null)
        {
            handImage.sprite = normalSprite;
        }

        if (points != null && points.Length > 0)
        {
            transform.position =
                points[0].position;
        }
    }


    // ==================================================
    // MAIN TUTORIAL
    // ==================================================

    private IEnumerator TutorialRoutine()
    {
        // ------------------------------------------
        // RESET
        // ------------------------------------------

        ClearPath();

        if (blockController != null)
        {
            blockController.ResetAll();
        }

        // ------------------------------------------
        // Đưa Hand về A
        // ------------------------------------------

        transform.position =
            points[0].position;

        // ------------------------------------------
        // Hand xuất hiện
        // ------------------------------------------

        transform.localScale =
            Vector3.zero;

        if (handImage != null)
        {
            handImage.sprite =
                normalSprite;
        }

        yield return ScaleHand(
            Vector3.zero,
            normalScale,
            appearDuration
        );

        // ------------------------------------------
        // A -> B -> C -> ...
        // ------------------------------------------

        yield return MoveForward();

        // ------------------------------------------
        // Hiệu ứng sai ở cuối
        // ------------------------------------------

        PlayWrongEffect();

        yield return new WaitForSeconds(
            waitAtEndDuration
        );

        // ------------------------------------------
        // Cuối -> A
        // ------------------------------------------

        yield return MoveBackToStart();

        // ------------------------------------------
        // Ấn A
        // ------------------------------------------

        yield return PressPoint();


        // ==================================================
        // REPEAT
        // ==================================================

        while (true)
        {
            // ------------------------------------------
            // Reset Path + Block
            // ------------------------------------------

            ClearPath();

            if (blockController != null)
            {
                blockController.ResetAll();
            }

            yield return new WaitForSeconds(
                restartDelay
            );

            // ------------------------------------------
            // A -> B -> C -> ...
            // ------------------------------------------

            yield return MoveForward();

            // ------------------------------------------
            // Wrong Effect
            // ------------------------------------------

            PlayWrongEffect();

            yield return new WaitForSeconds(
                waitAtEndDuration
            );

            // ------------------------------------------
            // Cuối -> A
            // ------------------------------------------

            yield return MoveBackToStart();

            // ------------------------------------------
            // Ấn A
            // ------------------------------------------

            yield return PressPoint();
        }
    }


    // ==================================================
    // WRONG EFFECT
    // ==================================================

    private void PlayWrongEffect()
    {
        if (wrongEffect != null)
        {
            wrongEffect.PlayWrongEffect();
        }
    }


    // ==================================================
    // MOVE FORWARD
    // ==================================================

    private IEnumerator MoveForward()
    {
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 startPosition =
                points[i - 1].position;

            Vector3 targetPosition =
                points[i].position;

            Image pathSegment =
                CreatePathSegment();

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed / moveDuration
                    );

                t = Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

                Vector3 currentPosition =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        t
                    );

                transform.position =
                    currentPosition;

                UpdatePathSegment(
                    pathSegment,
                    startPosition,
                    currentPosition
                );

                yield return null;
            }

            // ------------------------------------------
            // Đảm bảo Hand tới đúng điểm
            // ------------------------------------------

            transform.position =
                targetPosition;

            UpdatePathSegment(
                pathSegment,
                startPosition,
                targetPosition
            );

            // ------------------------------------------
            // Nối Path
            // ------------------------------------------

            CreatePathJoint(
                targetPosition
            );

            // ------------------------------------------
            // Block đổi sprite
            // ------------------------------------------

            if (blockController != null)
            {
                blockController.FillBlock(i);
            }
        }
    }


    // ==================================================
    // MOVE BACK
    // ==================================================

    private IEnumerator MoveBackToStart()
    {
        Vector3 startPosition =
            transform.position;

        Vector3 targetPosition =
            points[0].position;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / moveDuration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            yield return null;
        }

        transform.position =
            targetPosition;
    }


    // ==================================================
    // PRESS A
    // ==================================================

    private IEnumerator PressPoint()
    {
        if (handImage != null)
        {
            handImage.sprite =
                pressSprite;
        }

        Vector3 startScale =
            normalScale;

        Vector3 pressedScale =
            normalScale * pressScale;

        // Nhấn xuống
        yield return ScaleHand(
            startScale,
            pressedScale,
            pressDownDuration
        );

        // Giữ
        yield return new WaitForSeconds(
            pressDuration
        );

        // Nhả
        yield return ScaleHand(
            pressedScale,
            startScale,
            pressUpDuration
        );

        if (handImage != null)
        {
            handImage.sprite =
                normalSprite;
        }
    }


    // ==================================================
    // SCALE HAND
    // ==================================================

    private IEnumerator ScaleHand(
        Vector3 startScale,
        Vector3 targetScale,
        float duration
    )
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            t = Mathf.SmoothStep(
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

        transform.localScale =
            targetScale;
    }


    // ==================================================
    // CREATE PATH
    // ==================================================

    private Image CreatePathSegment()
    {
        if (pathPieces == null)
            return null;

        Image image =
            pathPieces.Get();

        RoundPathImage.ApplySegment(
            image,
            pathSprite,
            pathWidth
        );

        image.color =
            pathColor;

        // Đưa Path xuống dưới Block
        image.transform.SetAsFirstSibling();

        return image;
    }


    // ==================================================
    // UPDATE PATH
    // ==================================================

    private void UpdatePathSegment(
        Image segment,
        Vector3 startWorld,
        Vector3 endWorld
    )
    {
        if (segment == null ||
            pathContainer == null)
            return;

        RectTransform rect =
            segment.rectTransform;

        Vector3 start =
            pathContainer.InverseTransformPoint(
                startWorld
            );

        Vector3 end =
            pathContainer.InverseTransformPoint(
                endWorld
            );

        Vector3 direction =
            end - start;

        float distance =
            direction.magnitude;

        rect.localPosition =
            (start + end) / 2f;

        rect.sizeDelta =
            new Vector2(
                distance + pathWidth,
                pathWidth
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }


    // ==================================================
    // PATH JOINT
    // ==================================================

    private void CreatePathJoint(
        Vector3 worldPosition
    )
    {
        if (pathPieces == null)
            return;

        Image image =
            pathPieces.Get();

        RoundPathImage.ApplyJoint(
            image,
            pathSprite
        );

        image.color =
            pathColor;

        // A pooled piece may come back from a rotated segment.
        RectTransform rect =
            image.rectTransform;

        rect.localPosition =
            pathContainer.InverseTransformPoint(
                worldPosition
            );

        rect.localRotation =
            Quaternion.identity;

        rect.sizeDelta =
            new Vector2(
                pathWidth,
                pathWidth
            );

        image.transform.SetAsFirstSibling();
    }


    // ==================================================
    // CLEAR PATH
    // ==================================================

    private void ClearPath()
    {
        pathPieces?.ReleaseAll();
    }


    // ==================================================
    // DESTROY
    // ==================================================

    private void OnDestroy()
    {
        pathPieces?.Dispose();
    }
}
