using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Player.Spells
{
    public sealed class SpellPlacementController : MonoBehaviour
    {
        [SerializeField] private Camera placementCamera;
        [SerializeField] private LayerMask placementLayers;

        private IIndicatorSpell activeSpell;
        private Transform caster;
        private GameObject indicator;
        private Vector3 placementPosition;

        public bool IsActive => activeSpell != null;
        public bool HasValidPlacement { get; private set; }

        public void Begin(IIndicatorSpell spell, Transform caster, float placementRadius)
        {
            Cancel();

            if (spell == null)
            {
                return;
            }

            activeSpell = spell;
            this.caster = caster;

            if (placementCamera == null)
            {
                placementCamera = Camera.main;
            }

            GameObject indicatorPrefab = spell.IndicatorPrefab;
            if (indicatorPrefab == null)
            {
                Debug.LogWarning($"{spell} cannot begin placement because indicator prefab is missing.", this);
                Cancel();
                return;
            }

            indicator = Instantiate(indicatorPrefab);
            Vector3 scale = indicator.transform.localScale;
            float diameter = Mathf.Max(0f, placementRadius) * 2f;
            indicator.transform.localScale = new Vector3(diameter, scale.y, diameter);
            indicator.SetActive(false);
        }

        public bool TryGetPlacement(out Vector3 position)
        {
            position = placementPosition;
            return IsActive && HasValidPlacement;
        }

        public void Cancel()
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }

            activeSpell = null;
            caster = null;
            indicator = null;
            placementPosition = default;
            HasValidPlacement = false;
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            UpdatePlacement();
        }

        private void UpdatePlacement()
        {
            HasValidPlacement = false;

            if (placementCamera == null || caster == null)
            {
                HideIndicator();
                return;
            }

            Ray ray = placementCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayers))
            {
                HideIndicator();
                return;
            }

            if (Vector3.Distance(caster.position, hit.point) > activeSpell.PlacementRange)
            {
                HideIndicator();
                return;
            }

            placementPosition = hit.point;
            HasValidPlacement = true;

            if (indicator != null)
            {
                indicator.transform.position = placementPosition;
                indicator.SetActive(true);
            }
        }

        private void HideIndicator()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }
    }
}
