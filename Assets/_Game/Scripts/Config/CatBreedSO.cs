using UnityEngine;
using UnityEngine.Serialization;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One regular passenger of the airline. Regulars are not bought: each one starts
    /// waiting in the lounge once the player has flown enough flights, because a cat that
    /// likes the airline keeps coming back. The editor bakes <see cref="Prefab"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Cat Breed", fileName = "CatBreed")]
    public sealed class CatBreedSO : ScriptableObject
    {
        [Tooltip("Saved with the player's profile, so it must not change once it has shipped.")]
        [SerializeField] private string id = "cat";

        [SerializeField] private string displayName = "Cat";

        [Tooltip("The cat becomes a regular once this many flights have been flown. 0 for the starter.")]
        [FormerlySerializedAs("price")]
        [SerializeField, Min(0)] private int arrivesAfterFlight;

        [Tooltip("On for the free starter cat a new player already has.")]
        [SerializeField] private bool isOwnedByDefault;

        [Header("Look (baked by Tools/AirLine Pop/Build Cube Cats)")]
        [SerializeField] private PaletteCellRemap[] remaps = new PaletteCellRemap[0];

        [Tooltip("Base model parts the remaps must not touch, such as the eyes.")]
        [SerializeField] private string[] preservedParts = { "MAT_1" };

        [Tooltip("Base model parts a cat does not have, such as the fox's hair tuft.")]
        [SerializeField] private string[] hiddenParts = { "TOC_1" };

        [SerializeField] private CatView prefab;

        public string Id => id;

        /// <summary>The cat's name in the chosen language; the asset's own name if the table has none.</summary>
        public string DisplayName
        {
            get
            {
                string key = NameKey(id);
                string text = Localization.Get(key);
                return text == key ? displayName : text;
            }
        }

        public static string NameKey(string breedId)
        {
            return "cat." + breedId;
        }

        public int ArrivesAfterFlight => arrivesAfterFlight;

        public bool IsOwnedByDefault => isOwnedByDefault;

        public PaletteCellRemap[] Remaps => remaps;

        public string[] PreservedParts => preservedParts;

        public string[] HiddenParts => hiddenParts;

        public CatView Prefab => prefab;

#if UNITY_EDITOR
        /// <summary>Editor only: the builder links the prefab it baked.</summary>
        public void EditorSetPrefab(CatView baked)
        {
            prefab = baked;
        }

        /// <summary>Editor only: seeds a breed the first time the builder runs.</summary>
        public void EditorConfigure(string breedId, string name, int arrivalFlight, bool isStarter, PaletteCellRemap[] swatches)
        {
            id = breedId;
            displayName = name;
            arrivesAfterFlight = arrivalFlight;
            isOwnedByDefault = isStarter;
            remaps = swatches;
        }

        /// <summary>Editor only: sets when the cat starts coming to the lounge.</summary>
        public void EditorSetArrival(int afterFlight)
        {
            arrivesAfterFlight = afterFlight;
        }
#endif
    }
}
