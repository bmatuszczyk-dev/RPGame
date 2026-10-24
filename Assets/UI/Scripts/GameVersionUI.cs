using TMPro;
using UnityEngine;

namespace RPGame.UI
{
    public sealed class GameVersionUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text versionText;

        private void Awake()
        {
            versionText.text = $"Version {Application.version}";
        }
    }
}
