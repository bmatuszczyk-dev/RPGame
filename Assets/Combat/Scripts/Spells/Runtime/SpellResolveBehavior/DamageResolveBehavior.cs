using RPGame.Core.Damage;
using RPGame.Core.Spells;

namespace RPGame.Combat.Spells
{
    public sealed class DamageResolveBehavior : ISpellResolveBehavior
    {
        private readonly IDamageable damageable;
        private readonly CasterData casterData;

        public DamageResolveBehavior(IDamageable damageable, CasterData casterData)
        {
            this.damageable = damageable;
            this.casterData = casterData;
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;
        public DamageResult Result { get; private set; }

        public bool Resolve(SpellBehaviorContext context)
        {
            Result = damageable.ApplyDamage(new DamageData(
                DamageRangeRoller.Roll(casterData.DamageRanges),
                casterData.CasterObject));
            context.AddResolveResult(Result);
            return Result.WasApplied;
        }
    }
}
