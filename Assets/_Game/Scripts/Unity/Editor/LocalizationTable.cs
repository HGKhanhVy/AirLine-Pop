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

            // Loading screen: Captain Bơ talking while the cats chase the yarn round him.
            ("loading.tip.wings", "Let me check the wings first...", "Để Bơ kiểm tra cánh máy bay đã nha..."),
            ("loading.tip.bags", "Are all the bags on board?", "Hành lý lên khoang hết chưa nè?"),
            ("loading.tip.belts", "Fasten your seatbelts, kitties!", "Các bé thắt dây an toàn nhé!"),
            ("loading.tip.snacks", "Did we pack enough fish snacks?", "Cá khô cho chuyến bay xếp đủ chưa?"),
            ("loading.tip.tower", "Control tower, come in... meow?", "Alô đài kiểm soát... meo meo?"),
            ("loading.tip.nap", "Someone wake up the co-pilot!", "Ai đánh thức cơ phó dậy giùm Bơ với!"),
            ("loading.tip.yarn", "Hey, come back here, yarn!", "Ơ kìa, cuộn len lăn đi đâu rồi!"),
            ("loading.tip.window", "No fighting over the window seat!", "Đừng giành ghế cửa sổ nữa mấy đứa!"),

            // Home
            ("home.fly", "FLY", "BAY"),
            ("nav.airport", "Airport", "Sân bay"),
            ("nav.lounge", "Lounge", "Phòng chờ"),
            ("nav.shop", "Shop", "Cửa hàng"),
            ("nav.map", "Map", "Bản đồ"),
            ("map.chapter", "Chapter {0}", "Chương {0}"),
            ("map.international", "International flight", "Chuyến bay quốc tế"),
            ("map.title", "Flight routes", "Đường bay của hãng"),
            ("map.airport", "AIRPORT", "SÂN BAY"),
            ("map.hubProgress", "{0}/{1} flights", "{0}/{1} chuyến"),
            ("lounge.title", "Lounge", "Phòng chờ"),
            ("lounge.hint", "Tap a cat passenger", "Chạm vào khách mèo"),
            ("shop.title", "Shop", "Cửa hàng"),
            ("shop.snacks", "Snacks", "Đồ ăn"),
            ("shop.planes", "Planes", "Máy bay"),
            ("shop.themes", "Themes", "Giao diện"),
            ("shop.themesHint", "Dress your board and route map", "Đổi giao diện màn chơi và bản đồ"),
            ("shop.boughtTheme", "Theme {0} is on!", "Đã dùng giao diện {0}!"),
            ("theme.default", "Blue Sky", "Trời xanh"),
            ("theme.ocean", "Mint", "Bạc hà"),
            ("theme.sunset", "Sunset", "Hoàng hôn"),
            ("theme.starter", "Sakura", "Anh đào"),
            ("theme.neon", "Starry Night", "Đêm sao"),
            ("shop.use", "Use", "Dùng"),
            ("shop.inUse", "In use", "Đang dùng"),
            ("shop.snackPack", "{0} snacks", "{0} phần ăn"),
            ("shop.snackPackOne", "1 snack", "1 phần ăn"),
            ("shop.snackStock", "You have {0} snacks · feed your regulars in the lounge", "Bạn có {0} phần ăn · mời khách quen ở phòng chờ"),
            ("shop.planesHint", "Pick a plane for your airline", "Chọn máy bay cho hãng của bạn"),
            ("shop.boughtSnacks", "Bought {0} snacks!", "Đã mua {0} phần ăn!"),
            ("shop.boughtLivery", "Your plane is now {0}!", "Máy bay đã đổi sang màu {0}!"),
            ("shop.noCoins", "Not enough coins", "Chưa đủ xu"),
            ("livery.coral", "Classic Pop", "Pop cổ điển"),
            ("livery.sunny", "Propeller", "Cánh quạt"),
            ("livery.mint", "Seaplane", "Thủy phi cơ"),
            ("livery.sky", "Jumbo", "Jumbo"),
            ("livery.lavender", "Supersonic", "Siêu thanh"),
            ("livery.whale", "Sky Whale", "Cá voi bay"),
            ("toast.arrived", "{0} is now one of our regulars!", "{0} vừa thành khách quen của hãng!"),
            ("departures.boarding", "BOARDING", "LÊN MÁY BAY"),
            ("departures.onTime", "ON TIME", "ĐÚNG GIỜ"),

            // Postcards
            ("album.title", "Stamp Collection", "Sổ tem"),
            ("album.summary", "{0}/{1} stamps", "{0}/{1} con tem"),
            ("album.stamps", "{0}/{1}", "{0}/{1}"),
            ("album.country", "{0} · {1}/{2}", "{0} · {1}/{2}"),
            ("album.page", "Page {0}/{1}", "Trang {0}/{1}"),

            // Lounge care
            ("care.greet", "Say hi", "Chào"),
            ("care.play", "Play", "Chơi"),
            ("care.feed", "Feed · {0} left", "Mời ăn · còn {0}"),
            ("care.buy", "Buy snack · {0} coins", "Mua đồ ăn · {0} xu"),
            ("care.feedShort", "Snack", "Mời ăn"),
            ("care.wardrobe", "Dress up", "Thay đồ"),
            ("accessory.none", "None", "Bỏ"),
            ("accessory.bow", "Bow", "Nơ hồng"),
            ("accessory.pilot_cap", "Pilot cap", "Mũ phi công"),
            ("accessory.flower_crown", "Flower crown", "Vòng hoa"),
            ("accessory.crown", "Crown", "Vương miện"),
            ("care.buyShort", "Buy · {0}", "Mua · {0}"),
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
            ("gameplay.specialLevel", "Flight {0} · {1}", "Chuyến {0} · {1}"),
            ("rule.runway.title", "Runway", "Đường băng"),
            ("rule.runway.body", "Land on the runway: the flight has to end on this square, so plan your route to finish here.",
                "Hạ cánh ở đường băng: chuyến bay phải kết thúc tại ô này, hãy tính đường để về đích ở đây."),
            ("rule.night.title", "Night flight", "Bay đêm"),
            ("rule.night.body", "Watch the route once, then fly it in the dark. Only your plane's lights show the way.",
                "Nhớ kỹ đường bay được chỉ một lần, rồi bay trong đêm. Chỉ có đèn máy bay soi đường."),
            ("rule.formation.title", "Formation", "Bay đội hình"),
            ("rule.formation.body", "Two planes fly as mirror images. Cover your half and your wingman covers the other.",
                "Hai máy bay bay đối xứng như soi gương. Phủ kín nửa của bạn, máy bay đồng đội sẽ phủ nửa còn lại."),
            ("rule.vip.title", "VIP flight", "Chuyến bay VIP"),
            ("rule.vip.body", "A chapter's last flight mixes the rules you know. Land it for a gold seal on the stamp and a VIP guest in the lounge!",
                "Chuyến cuối chương kết hợp các luật đã học. Hoàn thành để nhận dấu vàng trên tem và một vị khách VIP ghé phòng chờ!"),
            ("special.vip", "VIP", "VIP"),
            ("rule.wind.title", "Wind", "Luồng gió"),
            ("rule.wind.body", "Fly onto a windy square and the gust pushes you on the way its arrow points.",
                "Bay vào ô có gió, máy bay sẽ bị đẩy đi tiếp theo đúng hướng mũi tên."),
            ("rule.gotIt", "Got it!", "Đã hiểu!"),
            ("special.heart", "Heart", "Trái tim"),
            ("special.plane", "Plane", "Máy bay"),
            ("special.fish", "Fish", "Con cá"),
            ("special.suitcase", "Suitcase", "Vali"),
            ("special.lotus", "Lotus", "Hoa sen"),
            ("special.fuji", "Mount Fuji", "Núi Phú Sĩ"),
            ("special.kimchi", "Kimchi jar", "Hũ kim chi"),
            ("special.eiffel", "Eiffel Tower", "Tháp Eiffel"),
            ("special.pyramid", "Pyramid", "Kim tự tháp"),
            ("special.cat", "Cat face", "Mặt mèo"),
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
            ("win.postcard", "Stamp and postcard from {0} added to your collection!", "Tem và bưu thiếp {0} đã về sổ tem!"),
            ("win.vipTitle", "His Majesty is delighted with flight {0}!", "Hoàng thượng rất hài lòng với chuyến {0}!"),
            ("win.vipDetail", "He'll drop by your lounge · Gold seal on your stamp", "Ngài sẽ ghé phòng chờ · Tem được đóng dấu vàng"),
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
