using UnityEngine;
using UnityEngine.UI;

public class TutorialUndoBlockController : MonoBehaviour
{
    [System.Serializable]
    private class BlockData
    {
        public Image image;
        public Sprite normalSprite;
        public Sprite filledSprite;
    }

    [SerializeField] private BlockData[] blocks;


    // ==================================================
    // RESET TẤT CẢ BLOCK
    // ==================================================

    public void ResetAll()
    {
        if (blocks == null)
            return;

        foreach (BlockData block in blocks)
        {
            if (block == null || block.image == null)
                continue;

            block.image.sprite = block.normalSprite;
        }
    }


    // ==================================================
    // FILL BLOCK
    // ==================================================

    public void FillBlock(int index)
    {
        if (blocks == null)
            return;

        if (index < 0 || index >= blocks.Length)
            return;

        BlockData block = blocks[index];

        if (block == null || block.image == null)
            return;

        if (block.filledSprite != null)
        {
            block.image.sprite =
                block.filledSprite;
        }
    }
}