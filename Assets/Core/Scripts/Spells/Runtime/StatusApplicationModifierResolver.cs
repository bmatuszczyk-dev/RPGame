using System.Collections.Generic;
using RPGame.Core.Statuses;

namespace RPGame.Core.Spells
{
    public static class StatusApplicationModifierResolver
    {
        public static StatusContext Apply(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            StatusDefinition status,
            StatusContext context)
        {
            if (behaviors == null || status == null)
            {
                return context;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is IStatusApplicationModifier modifier)
                {
                    context = modifier.ModifyStatusApplication(status, context);
                }
            }

            return context;
        }
    }
}
