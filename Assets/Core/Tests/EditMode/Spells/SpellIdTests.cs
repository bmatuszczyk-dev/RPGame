using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Spells;

namespace RPGame.Core.Tests.Spells
{
    public sealed class SpellIdTests
    {
        [Test]
        public void Equals_WhenValuesMatch_ReturnsTrue()
        {
            Assert.AreEqual(new SpellId("wave"), new SpellId("wave"));
            Assert.IsTrue(new SpellId("wave") == new SpellId("wave"));
        }

        [Test]
        public void Equals_WhenValuesDiffer_ReturnsFalse()
        {
            Assert.AreNotEqual(new SpellId("wave"), new SpellId("orbit"));
            Assert.IsTrue(new SpellId("wave") != new SpellId("orbit"));
        }

        [Test]
        public void HashSet_WhenValuesMatch_KeepsSingleEntry()
        {
            HashSet<SpellId> spellIds = new()
            {
                new SpellId("wave"),
                new SpellId("wave")
            };

            Assert.AreEqual(1, spellIds.Count);
        }

        [Test]
        public void IsValid_WhenValueIsEmptyOrWhitespace_ReturnsFalse()
        {
            Assert.IsFalse(new SpellId(null).IsValid);
            Assert.IsFalse(new SpellId(string.Empty).IsValid);
            Assert.IsFalse(new SpellId("   ").IsValid);
        }
    }
}
