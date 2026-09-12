using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class TextCatalogTests
    {
        [Test]
        public void CatalogUsesProvidedTranslationAndPreservesNumericPlaceholder()
        {
            var catalog = new TextCatalog(new[]
            {
                new LocalizedTextEntry { Key = "gameplay.hint.undo", Text = "Undo {0} steps" }
            });
            Assert.That(catalog.Get("gameplay.hint.undo"), Is.EqualTo("Undo {0} steps"));
            Assert.That(catalog.Get("missing.key"), Is.EqualTo("missing.key"));
        }
    }
}
