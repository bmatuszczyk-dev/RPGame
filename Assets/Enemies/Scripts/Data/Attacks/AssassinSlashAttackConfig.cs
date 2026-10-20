using UnityEngine;

namespace RPGame.Enemies
{
    [CreateAssetMenu(
        fileName = "AssassinSlashAttackConfig",
        menuName = "RPGame/Enemies/Attacks/Assassin Slash Attack Config")]
    public sealed class AssassinSlashAttackConfig : AttackConfig
    {
        [SerializeField, Min(0f)] private float windupDuration = 0.15f;
        [SerializeField, Min(0f)] private float slashDuration = 0.12f;
        [SerializeField, Range(0f, 1f)] private float hitTimeNormalized = 0.5f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.25f;
        [SerializeField] private LayerMask hitLayers;

        public float WindupDuration => windupDuration;
        public float SlashDuration => slashDuration;
        public float HitTimeNormalized => hitTimeNormalized;
        public float RecoveryDuration => recoveryDuration;
        public LayerMask HitLayers => hitLayers;

        protected override void OnValidate()
        {
            base.OnValidate();
            windupDuration = Mathf.Max(0f, windupDuration);
            slashDuration = Mathf.Max(0f, slashDuration);
            hitTimeNormalized = Mathf.Clamp01(hitTimeNormalized);
            recoveryDuration = Mathf.Max(0f, recoveryDuration);
        }
    }
}
