using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Draws the cats waiting on the board.
    ///
    /// The seats are built into the prefab, one per possible passenger, and reused from
    /// level to level: nothing is instantiated or destroyed while playing. One Update
    /// ticks the seats that are showing.
    /// </summary>
    public sealed class PassengerBoardView : MonoBehaviour
    {
        [SerializeField] private BoardView board;
        [SerializeField] private PassengerLookSO looks;

        [Tooltip("One per passenger a level can have (PassengerPlanner.MaxPassengers).")]
        [SerializeField] private PassengerCatView[] seats = new PassengerCatView[0];

        [Tooltip("Wait before the first cat pops in, so the board has landed first.")]
        [SerializeField, Min(0f)] private float firstAppearDelay = 0.55f;

        [Tooltip("Gap between cats popping in on a new level.")]
        [SerializeField, Min(0f)] private float appearStagger = 0.12f;

        private int shown;

        public void Show(PassengerPlan plan)
        {
            shown = Mathf.Min(plan.Count, seats.Length);

            for (int i = 0; i < seats.Length; i++)
            {
                if (i < shown)
                {
                    Vector3 square = board.GetCellWorldPosition(plan.Cells[i]);
                    seats[i].Seat(square, looks.Get(plan.Seed + i), firstAppearDelay + i * appearStagger, i * 0.37f);
                }
                else
                {
                    seats[i].Hide();
                }
            }
        }

        public void Board(int passenger)
        {
            if (passenger < shown)
            {
                seats[passenger].Board();
            }
        }

        public void ReturnToSeat(int passenger)
        {
            if (passenger < shown)
            {
                seats[passenger].ReturnToSeat();
            }
        }

        public void SetExcited(int passenger, bool isExcited)
        {
            if (passenger < shown)
            {
                seats[passenger].SetExcited(isExcited);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < shown; i++)
            {
                if (seats[i].IsActive)
                {
                    seats[i].Tick(deltaTime);
                }
            }
        }

#if UNITY_EDITOR
        public void EditorLink(BoardView linkedBoard, PassengerLookSO linkedLooks, PassengerCatView[] linkedSeats)
        {
            board = linkedBoard;
            looks = linkedLooks;
            seats = linkedSeats;
        }
#endif
    }
}
