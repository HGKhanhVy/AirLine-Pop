using UnityEngine;
using UnityEngine.UI;

public class TutorialPath : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Sprite pathSprite;

    [SerializeField] private float pathWidth = 30f;

    [SerializeField]
    private Color pathColor =
        new Color(0.0f, 0.55f, 0.45f, 1f);


    private UiPathPiecePool pieces;


    private void Awake()
    {
        pieces = new UiPathPiecePool((RectTransform)transform, "PathPiece");
    }


    // ==================================================
    // ADD PATH
    // ==================================================

    public void AddPath(
        Vector3 startWorld,
        Vector3 endWorld
    )
    {
        Vector3 startLocal =
            transform.InverseTransformPoint(
                startWorld
            );

        Vector3 endLocal =
            transform.InverseTransformPoint(
                endWorld
            );

        CreateSegment(startLocal, endLocal);
        CreateJoint(startLocal);
        CreateJoint(endLocal);
    }


    // ==================================================
    // CREATE SEGMENT
    // ==================================================

    private void CreateSegment(
        Vector3 start,
        Vector3 end
    )
    {
        Image image = pieces.Get();

        RoundPathImage.ApplySegment(image, pathSprite, pathWidth);
        image.color = pathColor;

        Vector3 direction = end - start;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        RectTransform rect = image.rectTransform;
        rect.localPosition = (start + end) / 2f;

        // A width past each centre, so the round ends land exactly on the joints.
        rect.sizeDelta = new Vector2(distance + pathWidth, pathWidth);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }


    // ==================================================
    // CREATE JOINT
    // ==================================================

    private void CreateJoint(
        Vector3 position
    )
    {
        Image image = pieces.Get();

        RoundPathImage.ApplyJoint(image, pathSprite);
        image.color = pathColor;

        // A pooled piece may come back from a rotated segment.
        RectTransform rect = image.rectTransform;
        rect.localPosition = position;
        rect.localRotation = Quaternion.identity;
        rect.sizeDelta = new Vector2(pathWidth, pathWidth);
    }


    // ==================================================
    // CLEAR PATH
    // ==================================================

    public void ClearPath()
    {
        pieces?.ReleaseAll();
    }


    private void OnDestroy()
    {
        pieces?.Dispose();
    }
}
