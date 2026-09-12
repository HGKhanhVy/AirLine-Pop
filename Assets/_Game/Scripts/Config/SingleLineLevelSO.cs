using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One entry of the campaign as a designer sees it: which board this slot plays and
    /// how hard it was rated.
    ///
    /// The board itself stays in the exported chapter files, because three hundred boards
    /// inlined into assets would be three hundred merge conflicts waiting to happen and
    /// would drop the repository, validator and solver out of the pipeline. What lives
    /// here is the decision, not the data: swap the id and the slot plays another board.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Level", fileName = "Level")]
    public sealed class SingleLineLevelSO : ScriptableObject
    {
        [Tooltip("Id inside the exported chapter file, such as ch01_001.")]
        [SerializeField] private string levelId;

        [Tooltip("Where this level sits in the campaign. 1 is the first level a player sees.")]
        [SerializeField, Min(1)] private int levelNumber = 1;

        [Tooltip("Chapter file this level is read from, such as ch01.")]
        [SerializeField] private string chapterId;

        [Tooltip("Rating the importer gave this board, 1 to 10. Display only.")]
        [SerializeField, Range(1, 10)] private int difficulty = 1;

        public string LevelId => levelId;

        public int LevelNumber => levelNumber;

        public string ChapterId => chapterId;

        public int Difficulty => difficulty;

        public override string ToString()
        {
            return levelNumber + ". " + levelId;
        }
    }
}
