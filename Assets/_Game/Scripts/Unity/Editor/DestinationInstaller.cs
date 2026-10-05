using System;
using System.Collections.Generic;
using System.IO;
using ASTeams.SingleLine.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Installs the route map: the destination catalog built from the postcards drawn by
    /// Tools/flat_art/generate_postcards.py and the passport pages drawn by
    /// generate_passport.py, and the postcard on the win card. The passport on Home is built
    /// by <see cref="StampAlbumUiBuilder"/>. Safe to rerun.
    /// </summary>
    public static class DestinationInstaller
    {
        private const string PostcardFolder = "Assets/_Game/Art/Postcards";
        private const string RoutesPath = PostcardFolder + "/routes.json";
        public const string PassportFolder = "Assets/_Game/Art/Passport";
        private const string PassportPath = PassportFolder + "/passport.json";
        public const string CatalogPath = "Assets/_Game/Config/DestinationCatalog.asset";
        private const string IconPath = "Assets/_Game/Art/UIKit/icon_postcard.png";
        private const string HudPrefabPath = "Assets/_Game/UI/AirlineHud.prefab";
        private const string PostcardName = "Postcard";
        private const int FlightsPerDestination = 10;

        [Serializable]
        private sealed class RouteList
        {
            public RouteEntry[] items;
        }

        [Serializable]
        private sealed class RouteEntry
        {
            public string id;
            public string name;
            public string name_vi;
            public string region;
            public string region_name;
            public string region_name_vi;
            public string postcard;
            public string postcard_vi;
            public string pin;
            public string code;
            public string stamp;
        }

        [Serializable]
        private sealed class PageList
        {
            public PageEntry[] items;
        }

        [Serializable]
        private sealed class PageEntry
        {
            public string region;
            public string map;
            public float[] entry;
            public CityEntry[] cities;
        }

        [Serializable]
        private sealed class CityEntry
        {
            public string id;
            public float[] uv;
        }

        [MenuItem("Tools/AirLine Pop/Install Destinations")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            DestinationCatalogSO catalog = BuildCatalog();

            if (catalog == null)
            {
                return "No " + RoutesPath + "; run generate_postcards.py first.";
            }

            UiKitInstaller.ImportKitSprite(IconPath);
            string hud = WireHud(catalog);
            AssetDatabase.SaveAssets();
            return "Destinations installed: " + catalog.Count + " cities.\n" + hud;
        }

        public static DestinationCatalogSO BuildCatalog()
        {
            if (!File.Exists(RoutesPath))
            {
                return null;
            }

            RouteEntry[] routes = ReadRoutes();
            PageEntry[] pageEntries = ReadPages();
            Dictionary<string, Vector2> mapPositions = MapPositions(pageEntries);
            var destinations = new Destination[routes.Length];

            for (int i = 0; i < destinations.Length; i++)
            {
                RouteEntry route = routes[i];
                Sprite postcard = SpriteImport.Import(PostcardFolder + "/" + route.postcard + ".png", 100f);
                Sprite postcardVietnamese = SpriteImport.Import(PostcardFolder + "/" + route.postcard_vi + ".png", 100f);
                Sprite pin = string.IsNullOrEmpty(route.pin) ? null : SpriteImport.Import(PassportFolder + "/" + route.pin + ".png", 100f);
                mapPositions.TryGetValue(route.id, out Vector2 mapPosition);
                Sprite stamp = string.IsNullOrEmpty(route.stamp) ? null : SpriteImport.Import(PostcardFolder + "/" + route.stamp + ".png", 100f);
                destinations[i] = new Destination(route.id, route.region, postcard, postcardVietnamese, pin, mapPosition, route.code, stamp);
            }

            var pages = new PassportPage[pageEntries.Length];

            for (int i = 0; i < pages.Length; i++)
            {
                PageEntry entry = pageEntries[i];
                Sprite map = SpriteImport.Import(PassportFolder + "/" + entry.map + ".png", 100f);
                pages[i] = new PassportPage(entry.region, map, new Vector2(entry.entry[0], entry.entry[1]));
            }

            var catalog = AssetDatabase.LoadAssetAtPath<DestinationCatalogSO>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DestinationCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSetDestinations(destinations, FlightsPerDestination, pages);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RouteEntry[] ReadRoutes()
        {
            return JsonUtility.FromJson<RouteList>("{\"items\":" + File.ReadAllText(RoutesPath) + "}").items;
        }

        private static PageEntry[] ReadPages()
        {
            if (!File.Exists(PassportPath))
            {
                return new PageEntry[0];
            }

            return JsonUtility.FromJson<PageList>("{\"items\":" + File.ReadAllText(PassportPath) + "}").items;
        }

        private static Dictionary<string, Vector2> MapPositions(PageEntry[] pages)
        {
            var positions = new Dictionary<string, Vector2>();

            foreach (PageEntry page in pages)
            {
                foreach (CityEntry city in page.cities)
                {
                    positions[city.id] = new Vector2(city.uv[0], city.uv[1]);
                }
            }

            return positions;
        }

        /// <summary>Each city's and country's name in both languages, for the localization table.</summary>
        public static IEnumerable<BilingualTextEntry> RouteTexts()
        {
            if (!File.Exists(RoutesPath))
            {
                yield break;
            }

            var regions = new HashSet<string>();

            foreach (RouteEntry route in ReadRoutes())
            {
                yield return new BilingualTextEntry { Key = Destination.NameKey(route.id), English = route.name, Vietnamese = route.name_vi };

                if (regions.Add(route.region))
                {
                    yield return new BilingualTextEntry
                    {
                        Key = Destination.RegionKey(route.region), English = route.region_name, Vietnamese = route.region_name_vi,
                    };
                }
            }
        }

        // ------------------------------------------------------------------ win card

        private static string WireHud(DestinationCatalogSO catalog)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(HudPrefabPath);

            try
            {
                var view = contents.GetComponentInChildren<WinPanelView>(true);
                var win = contents.GetComponentInChildren<WinSequenceController>(true);

                if (view == null || win == null)
                {
                    return "No win card in " + HudPrefabPath + "; HUD left unchanged.";
                }

                var portrait = (RawImage)new SerializedObject(view).FindProperty("catPortrait").objectReferenceValue;
                var card = (RectTransform)portrait.transform.parent;
                Transform old = card.Find(PostcardName);

                if (old != null)
                {
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }

                BuildWinPostcard(card, portrait.rectTransform, view);
                LinkWinRoutes(win, catalog);
                PrefabUtility.SaveAsPrefabAsset(contents, HudPrefabPath);
                return HudPrefabPath + ": postcard on the win card, route map linked.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>The postcard that takes the portrait's place, tilted a little like a card on a desk.</summary>
        public static void BuildWinPostcard(RectTransform card, RectTransform portrait, WinPanelView view)
        {
            Image postcard = UiBuilder.Image(PostcardName, card, "pill_cream", false);
            postcard.sprite = null;
            postcard.preserveAspect = true;
            postcard.enabled = false;

            RectTransform rect = postcard.rectTransform;
            rect.anchorMin = portrait.anchorMin;
            rect.anchorMax = portrait.anchorMax;
            rect.pivot = portrait.pivot;
            rect.anchoredPosition = portrait.anchoredPosition + new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(540f, 360f);
            rect.localRotation = Quaternion.Euler(0f, 0f, -4f);

            view.EditorLinkPostcard(postcard);
        }

        public static void LinkWinRoutes(WinSequenceController win, DestinationCatalogSO catalog)
        {
            var so = new SerializedObject(win);
            so.FindProperty("destinations").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
