using System.Collections.Generic;
using RPGame.Core.Effects;
using UnityEngine;

namespace RPGame.Progression
{
    [CreateAssetMenu(fileName = "PerkDefinition", menuName = "RPGame/Progression/Perk Definition")]
    public sealed class PerkDefinition : ScriptableObject
    {
        [SerializeField] private string perkId = "Perk";
        [SerializeField] private string displayName = "Perk";
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private List<string> connectedPerkIds = new();
        [SerializeField] private int cost = 1;
        [SerializeField] private bool isStartingPerk;
        [SerializeField] private Vector2 uiPosition;
        [SerializeField] private List<PassiveEffectDefinition> effects = new();

        public string PerkId => perkId;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<string> NextPerkIds => connectedPerkIds;
        public int Cost => cost;
        public bool IsStartingPerk => isStartingPerk;
        public Vector2 UIPosition => uiPosition;
        public IReadOnlyList<PassiveEffectDefinition> Effects => effects;

        private void OnValidate()
        {
            perkId = string.IsNullOrWhiteSpace(perkId) ? name : perkId.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? perkId : displayName.Trim();
            cost = Mathf.Max(1, cost);

            for (int i = 0; i < connectedPerkIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(connectedPerkIds[i]))
                {
                    connectedPerkIds[i] = connectedPerkIds[i].Trim();
                }
            }
        }
    }
}
