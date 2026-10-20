using System;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Player.Targeting
{
    public sealed class TargetingController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float maxTargetDistance = 25f;
        [SerializeField, Range(0f, 1f)] private float targetingRadius = 0.2f;
        [SerializeField, Range(0f, 1f)] private float targetSwitchThreshold = 0.03f;
        [SerializeField] private float targetRetentionTime = 0.2f;

        private float targetRetentionTimer;

        public event Action<ITargetable> TargetChanged;

        public ITargetable CurrentTarget { get; private set; }

        private void LateUpdate()
        {
            UpdateCurrentTarget(Time.deltaTime);
        }

        private void UpdateCurrentTarget(float deltaTime)
        {
            if (CurrentTarget == null)
            {
                SetCurrentTarget(SelectBestTarget());
                return;
            }

            if (!TryEvaluateCurrentTarget(out float currentScore))
            {
                targetRetentionTimer = 0f;
                SetCurrentTarget(SelectBestTarget());
                return;
            }

            if (currentScore <= targetingRadius)
            {
                targetRetentionTimer = 0f;
                TrySwitchToBetterTarget(currentScore);
                return;
            }

            targetRetentionTimer += Mathf.Max(0f, deltaTime);
            if (targetRetentionTimer >= targetRetentionTime)
            {
                targetRetentionTimer = 0f;
                SetCurrentTarget(SelectBestTarget());
            }
        }

        private ITargetable SelectBestTarget()
        {
            ITargetable selectedTarget = TargetSelector.SelectBest(
                TargetRegistry.EnemyTargets,
                playerCamera,
                transform.position,
                maxTargetDistance,
                targetingRadius);

            return selectedTarget;
        }

        private void TrySwitchToBetterTarget(float currentScore)
        {
            TargetSelector.Candidate candidate = TargetSelector.SelectBestCandidate(
                TargetRegistry.EnemyTargets,
                playerCamera,
                transform.position,
                maxTargetDistance,
                targetingRadius);

            if (!candidate.HasTarget
                || candidate.Target == CurrentTarget
                || candidate.Score > currentScore - targetSwitchThreshold)
            {
                return;
            }

            SetCurrentTarget(candidate.Target);
        }

        private bool TryEvaluateCurrentTarget(out float currentScore)
        {
            currentScore = 0f;

            if (!ContainsTarget(TargetRegistry.EnemyTargets, CurrentTarget))
            {
                return false;
            }

            return TargetSelector.TryEvaluate(CurrentTarget, playerCamera, transform.position, maxTargetDistance, out currentScore);
        }

        private void SetCurrentTarget(ITargetable target)
        {
            if (target == CurrentTarget)
            {
                return;
            }

            CurrentTarget = target;
            Debug.Log($"[Targeting] Current target: {FormatTarget(CurrentTarget)}", this);
            TargetChanged?.Invoke(CurrentTarget);
        }

        private void OnValidate()
        {
            maxTargetDistance = Mathf.Max(0f, maxTargetDistance);
            targetingRadius = Mathf.Clamp01(targetingRadius);
            targetSwitchThreshold = Mathf.Clamp01(targetSwitchThreshold);
            targetRetentionTime = Mathf.Max(0f, targetRetentionTime);
        }

        private static bool ContainsTarget(System.Collections.Generic.IReadOnlyList<ITargetable> targets, ITargetable target)
        {
            if (targets == null)
            {
                return false;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatTarget(ITargetable target)
        {
            if (target == null)
            {
                return "None";
            }

            if (target is UnityEngine.Object unityObject)
            {
                return unityObject.name;
            }

            return target.TargetPoint != null ? target.TargetPoint.name : target.ToString();
        }
    }
}
