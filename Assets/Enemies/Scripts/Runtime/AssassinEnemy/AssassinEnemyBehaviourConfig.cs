using UnityEngine;

namespace RPGame.Enemies
{
    [CreateAssetMenu(
        fileName = "AssassinEnemyBehaviourConfig",
        menuName = "RPGame/Enemies/Assassin Enemy Behaviour Config")]
    public sealed class AssassinEnemyBehaviourConfig : EnemyBehaviourConfigBase
    {
        [SerializeField, Min(0f)] private float combatLeapMinDistance = 3f;
        [SerializeField, Min(0f)] private float combatLeapMaxDistance = 9f;
        [SerializeField, Min(0f)] private float combatLeapSpeed = 15f;
        [SerializeField, Min(0f)] private float combatLeapArcHeight = 2.25f;
        [SerializeField, Min(0f)] private float combatLeapFallSpeed = 8f;
        [SerializeField, Min(0f)] private float combatLeapCooldown = 5f;
        [SerializeField, Range(0f, 1f)] private float combatLeapChance = 1f;
        [SerializeField, Min(0.01f)] private float combatLeapTriggerCheckInterval = 0.5f;
        [SerializeField, Min(0f)] private float preferredDistanceFromPlayer = 5.5f;
        [SerializeField, Min(0f)] private float strafeLookAheadDistance = 2f;

        public float CombatLeapMinDistance => combatLeapMinDistance;
        public float CombatLeapMaxDistance => combatLeapMaxDistance;
        public float CombatLeapSpeed => combatLeapSpeed;
        public float CombatLeapArcHeight => combatLeapArcHeight;
        public float CombatLeapFallSpeed => combatLeapFallSpeed;
        public float CombatLeapCooldown => combatLeapCooldown;
        public float CombatLeapChance => combatLeapChance;
        public float CombatLeapTriggerCheckInterval => combatLeapTriggerCheckInterval;
        public float PreferredDistanceFromPlayer => preferredDistanceFromPlayer;
        public float StrafeLookAheadDistance => strafeLookAheadDistance;

        private void OnValidate()
        {
            combatLeapMinDistance = Mathf.Max(0f, combatLeapMinDistance);
            combatLeapMaxDistance = Mathf.Max(combatLeapMinDistance, combatLeapMaxDistance);
            combatLeapSpeed = Mathf.Max(0f, combatLeapSpeed);
            combatLeapArcHeight = Mathf.Max(0f, combatLeapArcHeight);
            combatLeapFallSpeed = Mathf.Max(0f, combatLeapFallSpeed);
            combatLeapCooldown = Mathf.Max(0f, combatLeapCooldown);
            combatLeapChance = Mathf.Clamp01(combatLeapChance);
            combatLeapTriggerCheckInterval = Mathf.Max(0.01f, combatLeapTriggerCheckInterval);
            preferredDistanceFromPlayer = Mathf.Max(0f, preferredDistanceFromPlayer);
            strafeLookAheadDistance = Mathf.Max(0f, strafeLookAheadDistance);
        }
    }
}
