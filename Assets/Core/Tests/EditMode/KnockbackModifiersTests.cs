using NUnit.Framework;
using RPGame.Core.Movement;

namespace RPGame.Core.Tests
{
    public sealed class KnockbackModifiersTests
    {
        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(0.25f, 0.75f)]
        public void AddResistance_UpdatesMultiplier(float resistance, float expectedMultiplier)
        {
            KnockbackModifiers modifiers = new();

            modifiers.AddResistance(resistance);

            Assert.That(modifiers.Multiplier, Is.EqualTo(expectedMultiplier));
        }

        [Test]
        public void AddResistance_MultipliesModifiers()
        {
            KnockbackModifiers modifiers = new();
            modifiers.AddResistance(0.8f);
            modifiers.AddResistance(0.5f);

            Assert.That(modifiers.Multiplier, Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void Remove_RestoresPreviousMultiplier()
        {
            KnockbackModifiers modifiers = new();
            modifiers.AddResistance(0.8f);
            int modifierId = modifiers.AddResistance(0.5f);

            modifiers.Remove(modifierId);

            Assert.That(modifiers.Multiplier, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void Clear_RestoresMultiplierToOne()
        {
            KnockbackModifiers modifiers = new();
            modifiers.AddResistance(0.8f);

            modifiers.Clear();

            Assert.That(modifiers.Multiplier, Is.EqualTo(1f));
        }
    }
}
