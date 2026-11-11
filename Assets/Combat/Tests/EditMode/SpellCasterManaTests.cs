using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using UnityEditor;
using UnityEngine;

namespace RPGame.Combat.Tests
{
    public sealed class SpellCasterManaTests
    {
        public sealed class TestSpell : Spell
        {
            public int CastCount { get; private set; }

            public override bool SupportsProperty(SpellProperty property) => false;

            public override void OnCast(CasterData casterData)
            {
                CastCount++;
            }
        }

        private GameObject gameObject;
        private StatisticsConfig config;
        private StatisticsController statisticsController;
        private SpellCaster spellCaster;
        private TestSpell spell;

        [SetUp]
        public void SetUp()
        {
            config = CreateConfig(maxMana: 50f);
            gameObject = new GameObject("SpellCasterManaTests");
            statisticsController = gameObject.AddComponent<StatisticsController>();
            SetControllerConfig(statisticsController, config);
            statisticsController.ResetToConfig();
            spellCaster = new SpellCaster();
            spell = ScriptableObject.CreateInstance<TestSpell>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(spell);
        }

        [Test]
        public void TryCast_WhenEnoughMana_SpendsManaAndCastsSpell()
        {
            SetManaCost(spell, 20f);

            bool wasCast = spellCaster.TryCast(spell, CreateCasterData());

            Assert.IsTrue(wasCast);
            Assert.AreEqual(1, spell.CastCount);
            Assert.AreEqual(30f, statisticsController.CurrentMana);
        }

        [Test]
        public void TryCast_WhenNotEnoughMana_DoesNotCastSpell()
        {
            SetManaCost(spell, 60f);

            bool wasCast = spellCaster.TryCast(spell, CreateCasterData());

            Assert.IsFalse(wasCast);
            Assert.AreEqual(0, spell.CastCount);
            Assert.AreEqual(50f, statisticsController.CurrentMana);
        }

        [Test]
        public void TryCast_WhenSpellHasNoManaCost_CastsSpell()
        {
            SetManaCost(spell, 0f);

            bool wasCast = spellCaster.TryCast(spell, CreateCasterData());

            Assert.IsTrue(wasCast);
            Assert.AreEqual(1, spell.CastCount);
            Assert.AreEqual(50f, statisticsController.CurrentMana);
        }

        [Test]
        public void TryCast_WhenManaCostRequiresStatisticsAndStatisticsAreMissing_DoesNotCastSpell()
        {
            SetManaCost(spell, 20f);

            bool wasCast = spellCaster.TryCast(spell, CreateCasterDataWithoutStatistics());

            Assert.IsFalse(wasCast);
            Assert.AreEqual(0, spell.CastCount);
            Assert.AreEqual(50f, statisticsController.CurrentMana);
        }

        [Test]
        public void TryCast_WhenSpellHasNoManaCostAndStatisticsAreMissing_CastsSpell()
        {
            SetManaCost(spell, 0f);

            bool wasCast = spellCaster.TryCast(spell, CreateCasterDataWithoutStatistics());

            Assert.IsTrue(wasCast);
            Assert.AreEqual(1, spell.CastCount);
            Assert.AreEqual(50f, statisticsController.CurrentMana);
        }

        private static StatisticsConfig CreateConfig(float maxMana)
        {
            StatisticsConfig statisticsConfig = ScriptableObject.CreateInstance<StatisticsConfig>();
            SerializedObject serializedConfig = new SerializedObject(statisticsConfig);
            serializedConfig.FindProperty("maxHealth").floatValue = 100f;
            serializedConfig.FindProperty("maxStamina").floatValue = 50f;
            serializedConfig.FindProperty("maxMana").floatValue = maxMana;
            serializedConfig.FindProperty("healthRegenerationPerSecond").floatValue = 0f;
            serializedConfig.FindProperty("staminaRegenerationPerSecond").floatValue = 0f;
            serializedConfig.FindProperty("staminaRegenerationDelay").floatValue = 0f;
            serializedConfig.FindProperty("manaRegenerationPerSecond").floatValue = 0f;
            serializedConfig.FindProperty("manaRegenerationDelay").floatValue = 0f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return statisticsConfig;
        }

        private static void SetControllerConfig(StatisticsController controller, StatisticsConfig statisticsConfig)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("config").objectReferenceValue = statisticsConfig;
            serializedController.FindProperty("initializeOnAwake").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetManaCost(Spell targetSpell, float manaCost)
        {
            SerializedObject serializedSpell = new SerializedObject(targetSpell);
            serializedSpell.FindProperty("manaCost").floatValue = manaCost;
            serializedSpell.ApplyModifiedPropertiesWithoutUndo();
        }

        private CasterData CreateCasterData()
        {
            return new CasterDataBuilder(gameObject, gameObject.transform, null)
                .WithStatistics(statisticsController)
                .Build();
        }

        private CasterData CreateCasterDataWithoutStatistics()
        {
            return new CasterDataBuilder(gameObject, gameObject.transform, null)
                .Build();
        }
    }
}
