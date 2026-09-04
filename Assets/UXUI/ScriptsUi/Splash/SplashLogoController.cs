using System.Collections;
using UnityEngine;

public class SplashLogoController : MonoBehaviour
{
    [Header("BLOCK - từng chữ rơi xuống")]
    [SerializeField] private float blockStartHeight = 500f;
    [SerializeField] private float letterDuration = 0.45f;
    [SerializeField] private float letterDelay = 0.10f;

    [Header("FILL - đi từ bên phải vào")]
    [SerializeField] private float fillStartDistance = 700f;
    [SerializeField] private float fillDuration = 0.65f;
    [SerializeField] private float fillDelay = 0.15f;

    [Header("PUZZLE - đi từ dưới lên")]
    [SerializeField] private float puzzleStartDistance = 500f;
    [SerializeField] private float puzzleDuration = 0.70f;
    [SerializeField] private float puzzleDelay = 0.30f;


    // ============================
    // Các chữ BLOCK
    // ============================

    private RectTransform letterB;
    private RectTransform letterL;
    private RectTransform letterO;
    private RectTransform letterC;
    private RectTransform letterK;

    // Các object khác
    private RectTransform fill;
    private RectTransform puzzle;


    // ============================
    // Vị trí cuối
    // ============================

    private Vector2 targetB;
    private Vector2 targetL;
    private Vector2 targetO;
    private Vector2 targetC;
    private Vector2 targetK;

    private Vector2 targetFill;
    private Vector2 targetPuzzle;


    private void Awake()
    {
        // Tìm các object con bên trong LOGO

        letterB = FindChild("B");
        letterL = FindChild("L");
        letterO = FindChild("O");
        letterC = FindChild("C");
        letterK = FindChild("K");

        fill = FindChild("Fill");
        puzzle = FindChild("PUZZLE");
    }


    private void Start()
    {
        // Nếu thiếu object thì không chạy
        if (!CheckObjects())
        {
            return;
        }


        // ============================
        // Lưu vị trí cuối
        // ============================

        targetB = letterB.anchoredPosition;
        targetL = letterL.anchoredPosition;
        targetO = letterO.anchoredPosition;
        targetC = letterC.anchoredPosition;
        targetK = letterK.anchoredPosition;

        targetFill = fill.anchoredPosition;
        targetPuzzle = puzzle.anchoredPosition;


        // ============================
        // Đưa BLOCK lên phía trên
        // ============================

        letterB.anchoredPosition =
            targetB + Vector2.up * blockStartHeight;

        letterL.anchoredPosition =
            targetL + Vector2.up * blockStartHeight;

        letterO.anchoredPosition =
            targetO + Vector2.up * blockStartHeight;

        letterC.anchoredPosition =
            targetC + Vector2.up * blockStartHeight;

        letterK.anchoredPosition =
            targetK + Vector2.up * blockStartHeight;


        // ============================
        // Đưa FILL sang bên phải
        // ============================

        fill.anchoredPosition =
            targetFill + Vector2.right * fillStartDistance;


        // ============================
        // Đưa PUZZLE xuống dưới
        // ============================

        puzzle.anchoredPosition =
            targetPuzzle + Vector2.down * puzzleStartDistance;


        // ============================
        // Bắt đầu animation
        // ============================

        StartCoroutine(PlaySplashAnimation());
    }


    private IEnumerator PlaySplashAnimation()
    {
        // ==========================================
        // BLOCK
        // B → L → O → C → K
        // ==========================================

        StartCoroutine(
            MoveUI(
                letterB,
                targetB,
                letterDuration
            )
        );

        yield return new WaitForSeconds(letterDelay);


        StartCoroutine(
            MoveUI(
                letterL,
                targetL,
                letterDuration
            )
        );

        yield return new WaitForSeconds(letterDelay);


        StartCoroutine(
            MoveUI(
                letterO,
                targetO,
                letterDuration
            )
        );

        yield return new WaitForSeconds(letterDelay);


        StartCoroutine(
            MoveUI(
                letterC,
                targetC,
                letterDuration
            )
        );

        yield return new WaitForSeconds(letterDelay);


        StartCoroutine(
            MoveUI(
                letterK,
                targetK,
                letterDuration
            )
        );


        // ==========================================
        // FILL
        // ==========================================

        yield return new WaitForSeconds(fillDelay);

        StartCoroutine(
            MoveUI(
                fill,
                targetFill,
                fillDuration
            )
        );


        // ==========================================
        // PUZZLE
        // ==========================================

        yield return new WaitForSeconds(puzzleDelay);

        StartCoroutine(
            MoveUI(
                puzzle,
                targetPuzzle,
                puzzleDuration
            )
        );
    }


    // ==========================================
    // Di chuyển UI
    // ==========================================

    private IEnumerator MoveUI(
        RectTransform target,
        Vector2 destination,
        float duration
    )
    {
        if (target == null)
        {
            yield break;
        }


        Vector2 startPosition =
            target.anchoredPosition;

        float time = 0f;


        while (time < duration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(time / duration);


            // Chuyển động mượt
            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            target.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    destination,
                    t
                );


            yield return null;
        }


        // Đảm bảo vị trí cuối chính xác
        target.anchoredPosition =
            destination;
    }


    // ==========================================
    // Tìm object con
    // ==========================================

    private RectTransform FindChild(
        string childName
    )
    {
        Transform child =
            transform.Find(childName);


        if (child == null)
        {
            Debug.LogError(
                "Không tìm thấy object '" +
                childName +
                "' bên trong '" +
                gameObject.name +
                "'!"
            );

            return null;
        }


        RectTransform rect =
            child.GetComponent<RectTransform>();


        if (rect == null)
        {
            Debug.LogError(
                "Object '" +
                childName +
                "' không có RectTransform!"
            );

            return null;
        }


        return rect;
    }


    // ==========================================
    // Kiểm tra object
    // ==========================================

    private bool CheckObjects()
    {
        bool valid = true;


        if (letterB == null)
        {
            Debug.LogError("Thiếu object B!");
            valid = false;
        }

        if (letterL == null)
        {
            Debug.LogError("Thiếu object L!");
            valid = false;
        }

        if (letterO == null)
        {
            Debug.LogError("Thiếu object O!");
            valid = false;
        }

        if (letterC == null)
        {
            Debug.LogError("Thiếu object C!");
            valid = false;
        }

        if (letterK == null)
        {
            Debug.LogError("Thiếu object K!");
            valid = false;
        }

        if (fill == null)
        {
            Debug.LogError("Thiếu object Fill!");
            valid = false;
        }

        if (puzzle == null)
        {
            Debug.LogError("Thiếu object PUZZLE!");
            valid = false;
        }


        return valid;
    }
}