using UnityEditor;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>One menu entry that applies the whole flight look.</summary>
    public static class FlightThemeMenu
    {
        [MenuItem("Tools/AirLine Pop/Install Flight Theme")]
        public static void InstallFromMenu()
        {
            string report = BoardLightingInstaller.Install()
                + "\n\n" + FlightPathInstaller.Install()
                + "\n\n" + BlockTileInstaller.Install()
                + "\n\n" + IslandWorldInstaller.Install()
                + "\n\n" + HudStyleInstaller.Install();
            EditorUtility.DisplayDialog("AirLine Pop", report, "OK");
        }
    }
}
