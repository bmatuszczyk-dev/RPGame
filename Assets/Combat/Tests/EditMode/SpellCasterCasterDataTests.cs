using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Spells;
using RPGame.Core.Statistics.Attributes;
using UnityEditor;
using UnityEngine;

namespace RPGame.Combat.Tests
{
    public sealed class SpellCasterCasterDataTests
    {
        private GameObject gameObject;
        private CharacterAttributesConfig config;
        private CharacterAttributes attributes;
        private SpellCaster spellCaster;
        private CaptureCasterDataSpell spell;

        [SetUp]
        public void SetUp()
        {
            config = CreateConfig(power: 6);
            gameObject = new GameObject("SpellCasterCasterDataTests");
            attributes = gameObject.AddComponent<CharacterAttributes>();
            SetAttributesConfig(attributes, config);
            spellCaster = new SpellCaster();
            spell = ScriptableObject.CreateInstance<CaptureCasterDataSpell>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(spell);
        }

        [Test]
        public void SpellCaster_CanBeCreatedWithoutGameObject()
        {
            SpellCaster caster = new SpellCaster();

            Assert.IsNotNull(caster);
        }

        [Test]
        public void TryCast_PassesCurrentCharacterAttributes()
        {
            CasterData casterData = new CasterDataBuilder(gameObject, gameObject.transform, null)
                .WithAttributes(attributes)
                .Build();

            bool wasCast = spellCaster.TryCast(spell, casterData);

            Assert.IsTrue(wasCast);
            Assert.IsNotNull(spell.LastCasterData.Attributes);
            Assert.AreEqual(6, spell.LastCasterData.Attributes.Power);
        }

        [Test]
        public void TryCast_UsesProvidedCasterData()
        {
            GameObject targetObject = new("Target");
            try
            {
                CasterData casterData = new CasterData(gameObject, gameObject.transform, targetObject.transform);

                bool wasCast = spellCaster.TryCast(spell, casterData);

                Assert.IsTrue(wasCast);
                Assert.AreSame(gameObject, spell.LastCasterData.CasterObject);
                Assert.AreSame(gameObject.transform, spell.LastCasterData.CastOrigin);
                Assert.AreSame(targetObject.transform, spell.LastCasterData.Target);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void TryCast_DoesNotKeepTargetBetweenCasts()
        {
            GameObject targetObject = new("Target");
            CaptureCasterDataSpell secondSpell = ScriptableObject.CreateInstance<CaptureCasterDataSpell>();
            try
            {
                CasterData targetedCastData = new CasterData(gameObject, gameObject.transform, targetObject.transform);
                CasterData untargetedCastData = new CasterData(gameObject, gameObject.transform, null);

                Assert.IsTrue(spellCaster.TryCast(spell, targetedCastData));
                Assert.IsTrue(spellCaster.TryCast(secondSpell, untargetedCastData));

                Assert.AreSame(targetObject.transform, spell.LastCasterData.Target);
                Assert.IsNull(secondSpell.LastCasterData.Target);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(secondSpell);
            }
        }

        [Test]
        public void SpellCaster_DoesNotStorePlayerSpecificReferences()
        {
            System.Reflection.FieldInfo[] fields = typeof(SpellCaster).GetFields(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                Assert.AreNotEqual("RPGame.Player.Targeting.Targeting", fields[i].FieldType.FullName);
                Assert.AreNotEqual("RPGame.Player.Spells.CastController", fields[i].FieldType.FullName);
            }
        }

        private static CharacterAttributesConfig CreateConfig(int power)
        {
            CharacterAttributesConfig attributesConfig = ScriptableObject.CreateInstance<CharacterAttributesConfig>();
            SerializedObject serializedConfig = new(attributesConfig);
            serializedConfig.FindProperty("strength").intValue = 0;
            serializedConfig.FindProperty("dexterity").intValue = 0;
            serializedConfig.FindProperty("endurance").intValue = 0;
            serializedConfig.FindProperty("vitality").intValue = 0;
            serializedConfig.FindProperty("intelligence").intValue = 0;
            serializedConfig.FindProperty("power").intValue = power;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return attributesConfig;
        }

        private static void SetAttributesConfig(
            CharacterAttributes characterAttributes,
            CharacterAttributesConfig attributesConfig)
        {
            SerializedObject serializedAttributes = new(characterAttributes);
            serializedAttributes.FindProperty("config").objectReferenceValue = attributesConfig;
            serializedAttributes.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class CaptureCasterDataSpell : Spell
        {
            public CasterData LastCasterData { get; private set; }

            public override bool SupportsProperty(SpellProperty property) => false;

            public override void OnCast(CasterData casterData)
            {
                LastCasterData = casterData;
            }
        }
    }
}
