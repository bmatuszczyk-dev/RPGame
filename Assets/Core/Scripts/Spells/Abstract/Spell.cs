using UnityEngine;

namespace RPGame.Core.Spells
{
    public abstract class Spell : ScriptableObject, ICastable
    {
        [SerializeField] private string id;
        [SerializeField] private GameObject spellPrefab;
        [SerializeField] private float manaCost;

        public SpellId Id => new(id);
        public GameObject SpellPrefab => spellPrefab;
        public float ManaCost => manaCost;

        public virtual void OnDeactivation(CasterData casterData)
        {
        }

        public virtual ISpellActivationHandle OnActivation(CasterData casterData)
        {
            return null;
        }

        public abstract void OnCast(CasterData casterData);

        private void OnValidate()
        {
            manaCost = Mathf.Max(0f, manaCost);
        }
    }
}
