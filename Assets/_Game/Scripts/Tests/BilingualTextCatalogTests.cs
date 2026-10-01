using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class BilingualTextCatalogTests
    {
        private static BilingualTextCatalog Catalog()
        {
            return new BilingualTextCatalog(new[]
            {
                new BilingualTextEntry { Key = "flight.number", English = "Flight {0}", Vietnamese = "Chuyến {0}" },
                new BilingualTextEntry { Key = "common.ok", English = "OK", Vietnamese = "" },
            });
        }

        [Test]
        public void ReturnsTheLineInTheLanguageAskedFor()
        {
            Assert.That(Catalog().Get("flight.number", Language.English), Is.EqualTo("Flight {0}"));
            Assert.That(Catalog().Get("flight.number", Language.Vietnamese), Is.EqualTo("Chuyến {0}"));
        }

        [Test]
        public void UntranslatedLineFallsBackToEnglish()
        {
            Assert.That(Catalog().Get("common.ok", Language.Vietnamese), Is.EqualTo("OK"));
        }

        [Test]
        public void UnknownKeyComesBackAsItself()
        {
            Assert.That(Catalog().Get("missing.key", Language.Vietnamese), Is.EqualTo("missing.key"));
            Assert.That(Catalog().Get(null, Language.English), Is.EqualTo(string.Empty));
        }
    }
}
