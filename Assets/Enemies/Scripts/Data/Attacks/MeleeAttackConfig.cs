using UnityEngine;

namespace RPGame.Enemies
{
    [CreateAssetMenu(
        fileName = "MeleeAttackConfig",
        menuName = "RPGame/Enemies/Attacks/Melee Attack Config")]
    public sealed class MeleeAttackConfig : AttackConfig
    {
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float windupDuration;

        public float AttackRange => attackRange;
        public float WindupDuration => windupDuration;

        protected override void OnValidate()
        {
            base.OnValidate();
            attackRange = Mathf.Max(0f, attackRange);
            windupDuration = Mathf.Max(0f, windupDuration);
        }
    }
}
