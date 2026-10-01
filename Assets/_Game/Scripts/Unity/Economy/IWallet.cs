using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>The coin balance, read-only; spending goes through the services that sell things.</summary>
    public interface IWallet
    {
        long Coins { get; }

        event Action<long> OnCoinsChanged;
    }
}
