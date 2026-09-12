using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Plays a board effect at a place on the board. Callers name the moment and the
    /// spot; everything about which asset that is, how many may run at once and how they
    /// are recycled belongs to the implementation.
    /// </summary>
    public interface IEffectPlayer
    {
        void Play(GameplayEffect effect, Vector3 worldPosition, Color tint);

        /// <summary>Stops anything still running, for a restart or a new level.</summary>
        void StopAll();
    }
}
