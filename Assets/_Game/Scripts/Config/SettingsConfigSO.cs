using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Addresses the settings screen links out to. Kept as data so legal can change
    /// them without a code change.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Settings Config", fileName = "SettingsConfig")]
    public sealed class SettingsConfigSO : ScriptableObject
    {
        [SerializeField] private string privacyPolicyUrl;

        public string PrivacyPolicyUrl => privacyPolicyUrl;
    }
}
