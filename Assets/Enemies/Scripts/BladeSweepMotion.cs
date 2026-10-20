using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class BladeSweepMotion : MonoBehaviour
    {
        [SerializeField] private Transform leftBladePivot;
        [SerializeField] private Transform rightBladePivot;
        [SerializeField, Min(0.01f)] private float rotationDuration = 0.52f;
        [SerializeField, Min(0f)] private float rotationAngle = 360f;

        private Quaternion leftNeutralRotation;
        private Quaternion rightNeutralRotation;
        private float elapsedRotationTime;
        private bool isPlaying;
        private bool hasNeutralRotations;

        public bool IsPlaying => isPlaying;
        internal Transform LeftBladePivot => leftBladePivot;
        internal Transform RightBladePivot => rightBladePivot;
        internal float RotationAngle => rotationAngle;

        private void Awake()
        {
            if (leftBladePivot != null)
            {
                leftNeutralRotation = leftBladePivot.localRotation;
            }

            if (rightBladePivot != null)
            {
                rightNeutralRotation = rightBladePivot.localRotation;
            }

            hasNeutralRotations = true;
            SetPivotsActive(false);
        }

        public void Play()
        {
            elapsedRotationTime = 0f;
            isPlaying = true;
            SetPivotsActive(true);
            SetRotation(0f);
        }

        public void Stop()
        {
            isPlaying = false;
            elapsedRotationTime = 0f;
            SetRotation(0f);
            SetPivotsActive(false);
        }

        public void Tick(float deltaTime)
        {
            if (!isPlaying)
            {
                return;
            }

            elapsedRotationTime = Mathf.Min(rotationDuration, elapsedRotationTime + deltaTime);
            SetRotation(elapsedRotationTime / rotationDuration);

            if (elapsedRotationTime >= rotationDuration)
            {
                isPlaying = false;
                SetPivotsActive(false);
            }
        }

        internal Quaternion GetNeutralRotation(Transform pivot)
        {
            if (!Application.isPlaying || !hasNeutralRotations)
            {
                return pivot.localRotation;
            }

            return pivot == leftBladePivot
                ? leftNeutralRotation
                : rightNeutralRotation;
        }

        private void OnValidate()
        {
            rotationDuration = Mathf.Max(0.01f, rotationDuration);
            rotationAngle = Mathf.Max(0f, rotationAngle);
        }

        private void SetRotation(float normalizedTime)
        {
            if (leftBladePivot != null)
            {
                leftBladePivot.localRotation = Quaternion.AngleAxis(
                        rotationAngle * normalizedTime,
                        Vector3.right)
                    * leftNeutralRotation;
            }

            if (rightBladePivot != null)
            {
                rightBladePivot.localRotation = Quaternion.AngleAxis(
                        rotationAngle * normalizedTime,
                        Vector3.right)
                    * rightNeutralRotation;
            }
        }

        private void SetPivotsActive(bool isActive)
        {
            if (leftBladePivot != null)
            {
                leftBladePivot.gameObject.SetActive(isActive);
            }

            if (rightBladePivot != null)
            {
                rightBladePivot.gameObject.SetActive(isActive);
            }
        }

        private void OnDrawGizmosSelected()
        {
            BladeSweepBounds.DrawGizmos(this);
        }
    }
}
