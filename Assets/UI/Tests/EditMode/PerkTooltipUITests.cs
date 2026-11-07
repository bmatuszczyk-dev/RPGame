using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Progression;
using RPGame.UI.Jobs;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RPGame.UI.Tests
{
    public sealed class PerkTooltipUITests
    {
        private readonly List<Object> objectsToDestroy = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
            {
                if (objectsToDestroy[i] != null)
                {
                    Object.DestroyImmediate(objectsToDestroy[i]);
                }
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Hide_SetsCanvasGroupInvisibleAndNotInteractable()
        {
            PerkTooltipUI tooltip = CreateTooltip(out CanvasGroup canvasGroup);

            tooltip.Hide();

            Assert.AreEqual(0f, canvasGroup.alpha);
            Assert.IsFalse(canvasGroup.interactable);
        }

        [Test]
        public void Show_SetsNameDescriptionAndBonuses()
        {
            StatEffectDefinition effect = CreateStatEffect(EffectStat.ManaRegeneration, EffectModifierType.Percent, 0.05f);
            PerkDefinition perk = CreatePerk("spark", "Spark", "Small burst", effect);
            PerkTooltipUI tooltip = CreateTooltip(
                out CanvasGroup canvasGroup,
                out TextMeshProUGUI nameText,
                out TextMeshProUGUI descriptionText,
                out TextMeshProUGUI bonusesText);

            tooltip.Show(perk, Vector2.zero);

            Assert.AreEqual(1f, canvasGroup.alpha);
            Assert.IsTrue(canvasGroup.interactable);
            Assert.AreEqual("Spark", nameText.text);
            Assert.AreEqual("Small burst", descriptionText.text);
            Assert.AreEqual("+5% Mana Regeneration", bonusesText.text);
        }

        [Test]
        public void Show_WhenPerkHasNoEffects_SetsNoBonusesText()
        {
            PerkDefinition perk = CreatePerk("spark", "Spark", "Small burst");
            PerkTooltipUI tooltip = CreateTooltip(
                out _,
                out _,
                out _,
                out TextMeshProUGUI bonusesText);

            tooltip.Show(perk, Vector2.zero);

            Assert.AreEqual("No bonuses", bonusesText.text);
        }

        [Test]
        public void Show_NearBottomOfScreen_ShowsTooltipAboveCursor()
        {
            PerkDefinition perk = CreatePerk("spark", "Spark", "Small burst");
            PerkTooltipUI tooltip = CreateTooltip(out _);
            RectTransform tooltipTransform = (RectTransform)tooltip.transform;
            tooltipTransform.sizeDelta = new Vector2(200f, 100f);

            Vector2 cursorPosition = new Vector2(Screen.width * 0.5f, 80f);
            tooltip.Show(perk, cursorPosition);

            float tooltipBottom = tooltipTransform.position.y - tooltipTransform.rect.height * tooltipTransform.pivot.y;
            Assert.Greater(tooltipBottom, cursorPosition.y);
        }

        private PerkTooltipUI CreateTooltip(out CanvasGroup canvasGroup)
        {
            return CreateTooltip(out canvasGroup, out _, out _, out _);
        }

        private PerkTooltipUI CreateTooltip(
            out CanvasGroup canvasGroup,
            out TextMeshProUGUI nameText,
            out TextMeshProUGUI descriptionText,
            out TextMeshProUGUI bonusesText)
        {
            GameObject gameObject = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(PerkTooltipUI));
            objectsToDestroy.Add(gameObject);

            nameText = CreateText("Name", gameObject.transform);
            descriptionText = CreateText("Description", gameObject.transform);
            bonusesText = CreateText("Bonuses", gameObject.transform);

            SerializedObject serializedTooltip = new SerializedObject(gameObject.GetComponent<PerkTooltipUI>());
            serializedTooltip.FindProperty("canvasGroup").objectReferenceValue = gameObject.GetComponent<CanvasGroup>();
            serializedTooltip.FindProperty("nameText").objectReferenceValue = nameText;
            serializedTooltip.FindProperty("descriptionText").objectReferenceValue = descriptionText;
            serializedTooltip.FindProperty("bonusesText").objectReferenceValue = bonusesText;
            serializedTooltip.ApplyModifiedPropertiesWithoutUndo();

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            return gameObject.GetComponent<PerkTooltipUI>();
        }

        private TextMeshProUGUI CreateText(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent);
            return gameObject.GetComponent<TextMeshProUGUI>();
        }

        private PerkDefinition CreatePerk(
            string perkId,
            string displayName,
            string description,
            params EffectDefinition[] effects)
        {
            PerkDefinition perk = ScriptableObject.CreateInstance<PerkDefinition>();
            objectsToDestroy.Add(perk);

            SerializedObject serializedPerk = new SerializedObject(perk);
            serializedPerk.FindProperty("perkId").stringValue = perkId;
            serializedPerk.FindProperty("displayName").stringValue = displayName;
            serializedPerk.FindProperty("description").stringValue = description;
            serializedPerk.FindProperty("cost").intValue = 1;

            SerializedProperty perkEffects = serializedPerk.FindProperty("effects");
            perkEffects.arraySize = effects.Length;
            for (int i = 0; i < effects.Length; i++)
            {
                perkEffects.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];
            }

            serializedPerk.ApplyModifiedPropertiesWithoutUndo();
            return perk;
        }

        private StatEffectDefinition CreateStatEffect(
            EffectStat stat,
            EffectModifierType modifierType,
            float value)
        {
            StatEffectDefinition definition = ScriptableObject.CreateInstance<StatEffectDefinition>();
            objectsToDestroy.Add(definition);

            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stat").enumValueIndex = (int)stat;
            serializedDefinition.FindProperty("modifierType").enumValueIndex = (int)modifierType;
            serializedDefinition.FindProperty("value").floatValue = value;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}
