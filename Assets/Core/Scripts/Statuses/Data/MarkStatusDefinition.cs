using UnityEngine;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "MarkStatus", menuName = "RPGame/Statuses/Mark")]
    public sealed class MarkStatusDefinition : StatusDefinition
    {
        public override string ToString()
        {
            return "Mark";
        }
    }
}
