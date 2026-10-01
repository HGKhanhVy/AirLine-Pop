using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>The looks waiting cats are dressed in. Adding a breed here adds it to every flight.</summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Passenger Looks", fileName = "PassengerLooks")]
    public sealed class PassengerLookSO : ScriptableObject
    {
        [SerializeField] private PassengerLook[] looks = new PassengerLook[0];

        public int Count => looks.Length;

        /// <summary>A look picked by any whole number, wrapped into range; null when there are none.</summary>
        public PassengerLook Get(int index)
        {
            if (looks.Length == 0)
            {
                return null;
            }

            int wrapped = index % looks.Length;
            return looks[wrapped < 0 ? wrapped + looks.Length : wrapped];
        }

#if UNITY_EDITOR
        public void EditorSetLooks(PassengerLook[] newLooks)
        {
            looks = newLooks;
        }
#endif
    }
}
