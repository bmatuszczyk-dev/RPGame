using UnityEngine;

namespace RPGame.Enemies
{
    [CreateAssetMenu(
        fileName = "HealerEnemyBehaviourConfig",
        menuName = "RPGame/Enemies/Healer Enemy Behaviour Config")]
    public sealed class HealerEnemyBehaviourConfig : EnemyBehaviourConfigBase
    {
        [SerializeField] private float allySearchRange = 12f;
        [SerializeField] private float healRange = 6f;
        [SerializeField] private float healPerSecond = 10f;
        [SerializeField] private float manaCostPerSecond = 5f;
        [SerializeField] private float playerAvoidanceRange = 5f;

        public float AllySearchRange => allySearchRange;
        public float HealRange => healRange;
        public float HealPerSecond => healPerSecond;
        public float ManaCostPerSecond => manaCostPerSecond;
        public float PlayerAvoidanceRange => playerAvoidanceRange;

        private void OnValidate()
        {
            allySearchRange = Mathf.Max(0f, allySearchRange);
            healRange = Mathf.Max(0f, healRange);
            healPerSecond = Mathf.Max(0f, healPerSecond);
            manaCostPerSecond = Mathf.Max(0f, manaCostPerSecond);
            playerAvoidanceRange = Mathf.Max(0f, playerAvoidanceRange);
        }
    }
}
