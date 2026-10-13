using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What the rewards on top of a landing tell the screens: the extra lines a win card
    /// shows, and a finished daily flight. Kept apart from
    /// <see cref="GameplayEvents"/>, which speaks for the board alone.
    /// </summary>
    public static class FlightBonusEvents
    {
        /// <summary>One line for the win card about coins paid on top, already in the player's language.</summary>
        public static event Action<string> OnBonusLine;

        /// <summary>Raised when the daily flight is landed, with the days in a row it makes.</summary>
        public static event Action<int> OnDailyCompleted;

        public static void RaiseBonusLine(string line)
        {
            OnBonusLine?.Invoke(line);
        }

        public static void RaiseDailyCompleted(int dayStreak)
        {
            OnDailyCompleted?.Invoke(dayStreak);
        }

        /// <summary>Static events outlive a play session without a domain reload, so every run starts clean.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            OnBonusLine = null;
            OnDailyCompleted = null;
        }
    }
}
