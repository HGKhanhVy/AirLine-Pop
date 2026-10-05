using System;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Runs the pickup flight: seats cats on each new level and boards them as the route
    /// reaches their squares.
    ///
    /// A cat is aboard exactly when its square is on the path, so this reads the path after
    /// every change instead of keeping a second record: undo, restart and the rewind put
    /// cats back on their seats with no extra rules. It only reads gameplay and tells the
    /// view and the rest of the game what changed.
    /// </summary>
    public sealed class PassengerController : MonoBehaviour
    {
        [SerializeField] private GameplayController gameplay;
        [SerializeField] private PassengerBoardView view;

        private readonly bool[] aboard = new bool[PassengerPlanner.MaxPassengers];
        private LevelData plannedLevel;
        private PassengerPlan plan = PassengerPlan.Empty;
        private int aboardCount;

        private void OnEnable()
        {
            gameplay.OnPathChanged += HandlePathChanged;
            GameplayEvents.OnSnapshotRequested += Announce;
        }

        private void OnDisable()
        {
            gameplay.OnPathChanged -= HandlePathChanged;
            GameplayEvents.OnSnapshotRequested -= Announce;
        }

        private void HandlePathChanged()
        {
            LevelData level = gameplay.Level;

            if (level == null)
            {
                return;
            }

            if (!ReferenceEquals(level, plannedLevel))
            {
                Replan(level);
            }

            int head = gameplay.Head;
            bool changed = false;

            for (int i = 0; i < plan.Count; i++)
            {
                int cell = plan.Cells[i];
                bool isAboard = gameplay.IsVisited(cell);

                if (isAboard != aboard[i])
                {
                    aboard[i] = isAboard;
                    aboardCount += isAboard ? 1 : -1;
                    changed = true;

                    if (isAboard)
                    {
                        view.Board(i);
                    }
                    else
                    {
                        view.ReturnToSeat(i);
                    }
                }

                // A waiting cat hops when the plane pulls up beside it: it says "me next".
                bool isNext = !isAboard && head != LevelData.NoCell &&
                    (level.Grid.AreAdjacent(head, cell) || (level.IsFormation && level.Grid.AreAdjacent(level.MirrorOf(head), cell)));
                view.SetExcited(i, isNext);
            }

            if (changed)
            {
                Announce();
            }
        }

        private void Replan(LevelData level)
        {
            plannedLevel = level;
            plan = PassengerPlanner.Plan(level);
            Array.Clear(aboard, 0, aboard.Length);
            aboardCount = 0;
            view.Show(plan);
            Announce();
        }

        private void Announce()
        {
            GameplayEvents.RaisePassengersChanged(aboardCount, plan.Count);
        }

#if UNITY_EDITOR
        public void EditorLink(GameplayController linkedGameplay, PassengerBoardView linkedView)
        {
            gameplay = linkedGameplay;
            view = linkedView;
        }
#endif
    }
}
