using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Every line of player-facing text, in English and Vietnamese, in one place. The
    /// builders take labels from here by key; LocalizationInstaller writes it into the
    /// runtime table. Add a line here rather than typing text into a builder.
    /// </summary>
    public static class LocalizationTable
    {
        private static readonly (string key, string english, string vietnamese)[] Lines =
        {
            // Shared
            ("flight.number", "Flight {0}", "Chuyến {0}"),
            ("flight.withDestination", "Flight {0} · {1}", "Chuyến {0} · {1}"),
            ("common.restart", "Restart", "Chơi lại"),
            ("common.backToAirport", "Back to airport", "Về sân bay"),
            ("common.continue", "Continue", "Tiếp tục"),
            ("common.tapToClose", "Tap to close", "Chạm để đóng"),

            // Settings
            ("settings.title", "Settings", "Cài đặt"),
            ("settings.music", "Music", "Nhạc nền"),
            ("settings.sound", "Sound", "Âm thanh"),
            ("settings.vibration", "Vibration", "Rung"),
            ("settings.language", "Language", "Ngôn ngữ"),

            // Home
            ("home.fly", "FLY", "BAY"),
            ("nav.airport", "Airport", "Sân bay"),
            ("nav.lounge", "Lounge", "Phòng chờ"),
            ("nav.shop", "Shop", "Cửa hàng"),
            ("lounge.title", "Lounge", "Phòng chờ"),
            ("lounge.hint", "Tap a cat passenger", "Chạm vào khách mèo"),
            ("shop.title", "Shop", "Cửa hàng"),
            ("shop.soon", "Snacks · Planes · Airport · Lounge", "Đồ ăn · Máy bay · Sân bay · Phòng chờ"),
            ("toast.arrived", "{0} is now one of our regulars!", "{0} vừa thành khách quen của hãng!"),
            ("departures.boarding", "BOARDING", "LÊN MÁY BAY"),
            ("departures.onTime", "ON TIME", "ĐÚNG GIỜ"),

            // Postcards
            ("album.title", "Postcard Album", "Sổ bưu thiếp"),
            ("album.summary", "{0}/{1} postcards", "{0}/{1} bưu thiếp"),
            ("album.stamps", "{0}/{1} stamps", "{0}/{1} tem"),

            // Lounge care
            ("care.greet", "Say hi", "Chào"),
            ("care.play", "Play", "Chơi"),
            ("care.feed", "Feed · {0} left", "Mời ăn · còn {0}"),
            ("care.buy", "Buy snack · {0} coins", "Mua đồ ăn · {0} xu"),
            ("care.bond", "+{0} bond", "+{0} thân thiết"),
            ("care.tierUp", "Up to {0}!", "Lên {0}!"),
            ("care.alreadyGreeted", "Already said hi today, still happy!", "Hôm nay đã chào rồi, bé vẫn vui lắm!"),
            ("care.alreadyPlayed", "Already played today, still having fun!", "Hôm nay đã chơi rồi, bé vẫn thích lắm!"),
            ("care.full", "{0} is full, come back tomorrow", "{0} no rồi, mai mời tiếp nhé"),
            ("care.noCoins", "Not enough coins for a snack", "Chưa đủ xu để mua đồ ăn"),
            ("care.bought", "Bought a snack", "Đã mua 1 phần đồ ăn"),
            ("tier.0", "New guest", "Khách mới"),
            ("tier.1", "Bronze", "Hạng Đồng"),
            ("tier.2", "Silver", "Hạng Bạc"),
            ("tier.3", "Gold", "Hạng Vàng"),
            ("tier.4", "Diamond", "Kim Cương"),

            // The regulars
            ("cat.bo", "Butter", "Bơ"),
            ("cat.kem", "Cream", "Kem"),
            ("cat.mun", "Ebony", "Mun"),
            ("cat.muop", "Tabby", "Mướp"),
            ("cat.tro", "Ash", "Tro"),

            // Gameplay
            ("gameplay.level", "Flight {0}", "Chuyến {0}"),
            ("gameplay.progress", "{0} / {1}", "{0} / {1}"),
            ("gameplay.hint.restart", "Try a Restart", "Thử Chơi lại nhé"),
            ("gameplay.hint.undo", "Drag back {0} steps", "Kéo lùi {0} bước"),
            ("hud.hint", "Hint", "Gợi ý"),
            ("hud.stuck", "No way forward · Drag back or Restart", "Hết đường rồi · Kéo lùi hoặc Chơi lại"),
            ("pause.title", "Paused", "Tạm dừng"),
            ("pause.resume", "Resume", "Tiếp tục"),
            ("win.title", "Flight {0} is off!", "Chuyến {0} cất cánh!"),
            ("win.titleTo", "Flight {0} to {1}!", "Chuyến {0} tới {1}!"),
            ("win.detail", "{0} cat passengers · Stamp {1}/{2}", "{0} khách mèo · Tem {1}/{2}"),
            ("win.postcard", "Postcard from {0} added to your album!", "Bưu thiếp {0} đã về sổ!"),
            ("win.replayNote", "Ticket already earned for this flight", "Đã nhận tiền vé chuyến này rồi"),
        };

        private static Dictionary<string, string> english;

        /// <summary>The English line, for builders laying out a label before the game first runs.</summary>
        public static string English(string key)
        {
            if (english == null)
            {
                english = new Dictionary<string, string>();

                foreach ((string k, string en, string _) in Lines)
                {
                    english[k] = en;
                }
            }

            return english.TryGetValue(key, out string text) ? text : key;
        }

        public static BilingualTextEntry[] Entries(IEnumerable<BilingualTextEntry> extra)
        {
            var entries = new List<BilingualTextEntry>(Lines.Length + 64);

            foreach ((string key, string en, string vi) in Lines)
            {
                entries.Add(new BilingualTextEntry { Key = key, English = en, Vietnamese = vi });
            }

            entries.AddRange(extra);
            return entries.ToArray();
        }
    }
}
