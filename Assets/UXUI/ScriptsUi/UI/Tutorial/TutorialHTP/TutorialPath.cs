using System.Collections.Generic;
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


    private readonly List<GameObject> pathObjects =
        new List<GameObject>();


    private void Awake()
    {
        // Đưa Path ra phía sau Blocks
        //transform.SetAsFirstSibling();
    }


    // ==================================================
    // ADD PATH
    // ==================================================

    public void AddPath(
        Vector3 startWorld,
        Vector3 endWorld
    )
    {
        // ------------------------------------------
        // Chuyển World → Local
        // ------------------------------------------

        Vector3 startLocal =
            transform.InverseTransformPoint(
                startWorld
            );

        Vector3 endLocal =
            transform.InverseTransformPoint(
                endWorld
            );


        // ------------------------------------------
        // Tạo đoạn Path
        // ------------------------------------------

        CreateSegment(
            startLocal,
            endLocal
        );


        // ------------------------------------------
        // Tạo điểm nối
        // ------------------------------------------

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
        GameObject pathObject =
            new GameObject(
                "PathSegment",
                typeof(RectTransform),
                typeof(Image)
            );


        pathObject.transform.SetParent(
            transform,
            false
        );


        RectTransform rect =
            pathObject.GetComponent<RectTransform>();


        Image image =
            pathObject.GetComponent<Image>();


        image.sprite = pathSprite;
        image.color = pathColor;

        image.raycastTarget = false;


        Vector3 direction =
            end - start;


        float distance =
            direction.magnitude;


        // ------------------------------------------
        // Vị trí
        // ------------------------------------------

        rect.localPosition =
            (start + end) / 2f;


        // ------------------------------------------
        // Kích thước
        // ------------------------------------------

        rect.sizeDelta =
            new Vector2(
                distance + pathWidth * 0.5f,
                pathWidth
            );


        // ------------------------------------------
        // Xoay
        // ------------------------------------------

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


        pathObjects.Add(pathObject);
    }


    // ==================================================
    // CREATE JOINT
    // ==================================================

    private void CreateJoint(
        Vector3 position
    )
    {
        GameObject jointObject =
            new GameObject(
                "PathJoint",
                typeof(RectTransform),
                typeof(Image)
            );


        jointObject.transform.SetParent(
            transform,
            false
        );


        RectTransform rect =
            jointObject.GetComponent<RectTransform>();


        Image image =
            jointObject.GetComponent<Image>();


        image.sprite = pathSprite;
        image.color = pathColor;

        image.raycastTarget = false;


        rect.localPosition = position;


        rect.sizeDelta =
            new Vector2(
                pathWidth,
                pathWidth
            );


        pathObjects.Add(jointObject);
    }


    // ==================================================
    // CLEAR PATH
    // ==================================================

    public void ClearPath()
    {
        foreach (GameObject pathObject in pathObjects)
        {
            if (pathObject != null)
            {
                Destroy(pathObject);
            }
        }


        pathObjects.Clear();
    }
}