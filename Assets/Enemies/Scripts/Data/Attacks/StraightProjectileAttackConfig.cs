using RPGame.Core.Buildup;
using UnityEngine;

namespace RPGame.Enemies
{
    [CreateAssetMenu(
        fileName = "StraightProjectileAttackConfig",
        menuName = "RPGame/Enemies/Attacks/Straight Projectile Attack Config")]
    public sealed class StraightProjectileAttackConfig : RangedAttackConfig
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private BuildupDefinition buildupDefinition;
        [SerializeField, Min(0f)] private float buildupAmount;

        public GameObject ProjectilePrefab => projectilePrefab;
        public BuildupDefinition BuildupDefinition => buildupDefinition;
        public float BuildupAmount => buildupAmount;

        protected override void OnValidate()
        {
            base.OnValidate();
            buildupAmount = Mathf.Max(0f, buildupAmount);
        }
    }
}
