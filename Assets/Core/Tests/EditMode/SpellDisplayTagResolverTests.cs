using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Core.Tests
{
    public sealed class SpellDisplayTagResolverTests
    {
        [Test]
        public void GetDisplayTags_ReturnsTagsFromCapabilityAttributes()
        {
            TestAreaControlSpell spell = ScriptableObject.CreateInstance<TestAreaControlSpell>();
            try
            {
                IReadOnlyList<string> displayTags = SpellDisplayTagResolver.GetDisplayTags(spell);

                CollectionAssert.Contains(displayTags, "AoE");
                CollectionAssert.Contains(displayTags, "Control");
                Assert.AreEqual(2, displayTags.Count);
            }
            finally
            {
                Object.DestroyImmediate(spell);
            }
        }

        [Test]
        public void GetDisplayTags_CachesDisplayTagsPerSpellType()
        {
            TestAreaControlSpell firstSpell = ScriptableObject.CreateInstance<TestAreaControlSpell>();
            TestAreaControlSpell secondSpell = ScriptableObject.CreateInstance<TestAreaControlSpell>();
            try
            {
                IReadOnlyList<string> firstTags = SpellDisplayTagResolver.GetDisplayTags(firstSpell);
                IReadOnlyList<string> secondTags = SpellDisplayTagResolver.GetDisplayTags(secondSpell);

                Assert.AreSame(firstTags, secondTags);
            }
            finally
            {
                Object.DestroyImmediate(firstSpell);
                Object.DestroyImmediate(secondSpell);
            }
        }

        private sealed class TestAreaControlSpell : Spell, IAoECapability, IControlCapability
        {
            public float Radius => 3f;
            public float ControlPower => 2f;

            public override bool SupportsProperty(SpellProperty property)
            {
                return property is SpellProperty.Radius
                    or SpellProperty.Area
                    or SpellProperty.ControlPower;
            }

            public override void OnCast(CasterData casterData)
            {
            }
        }
    }
}
