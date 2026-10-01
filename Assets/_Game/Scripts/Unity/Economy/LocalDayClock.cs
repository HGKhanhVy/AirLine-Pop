using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Days since 2000-01-01 on the device's local calendar.</summary>
    public sealed class LocalDayClock : IDayClock
    {
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1);

        public int Today => (int)(DateTime.Now.Date - Epoch).TotalDays;
    }
}
