using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

/// <summary>
/// Reuses the images a tutorial draws its path with. The demo loops forever, so every
/// lap takes its pieces back from here instead of creating and destroying new ones.
/// </summary>
public sealed class UiPathPiecePool
{
    private readonly RectTransform container;
    private readonly string pieceName;
    private readonly ObjectPool<Image> pool;
    private readonly List<Image> active = new List<Image>();

    public UiPathPiecePool(RectTransform container, string pieceName)
    {
        this.container = container;
        this.pieceName = pieceName;
        pool = new ObjectPool<Image>(Create, OnGet, OnRelease, OnDestroyPiece);
    }

    /// <summary>An active piece under the container; the caller sets sprite, colour and rect.</summary>
    public Image Get()
    {
        Image piece = pool.Get();
        active.Add(piece);
        return piece;
    }

    /// <summary>Hides every piece handed out since the last call and keeps them for reuse.</summary>
    public void ReleaseAll()
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] != null)
            {
                pool.Release(active[i]);
            }
        }

        active.Clear();
    }

    /// <summary>End of life: frees the pooled pieces along with the owner.</summary>
    public void Dispose()
    {
        ReleaseAll();
        pool.Clear();
    }

    private Image Create()
    {
        var pieceObject = new GameObject(pieceName, typeof(RectTransform));
        pieceObject.transform.SetParent(container, false);

        Image image = pieceObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void OnGet(Image piece)
    {
        piece.gameObject.SetActive(true);
    }

    private static void OnRelease(Image piece)
    {
        piece.gameObject.SetActive(false);
    }

    private static void OnDestroyPiece(Image piece)
    {
        if (piece != null)
        {
            Object.Destroy(piece.gameObject);
        }
    }
}
