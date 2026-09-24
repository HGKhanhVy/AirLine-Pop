using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paints a UI path piece with a round sprite, so segment ends and joints come out
/// rounded like the gameplay line instead of square.
/// </summary>
public static class RoundPathImage
{
    /// <summary>
    /// A segment: sliced, with the sprite's border scaled to half the line width so
    /// each end is a half circle whatever the segment length.
    /// </summary>
    public static void ApplySegment(Image image, Sprite sprite, float width)
    {
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = GetMultiplier(sprite, width);
    }

    /// <summary>A joint: the plain round sprite, sized to the line width by its rect.</summary>
    public static void ApplyJoint(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = Image.Type.Simple;
    }

    private static float GetMultiplier(Sprite sprite, float width)
    {
        if (sprite == null || width <= 0f || sprite.border.x <= 0f)
        {
            return 1f;
        }

        return sprite.border.x / (width * 0.5f);
    }
}
