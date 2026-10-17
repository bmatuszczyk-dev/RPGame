using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class AssasinSlashView : MonoBehaviour
    {
        private const int TrajectorySegments = 32;

        [SerializeField] private Transform leftBladePivot;
        [SerializeField] private Transform rightBladePivot;
        [SerializeField, Min(0.01f)] private float rotationDuration = 0.52f;
        [SerializeField, Min(0f)] private float rotationAngle = 360f;

        private Quaternion leftNeutralRotation;
        private Quaternion rightNeutralRotation;
        private float elapsedRotationTime;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        public bool TryGetHitbox(out Vector3 center, out Vector3 size)
        {
            Bounds bounds = default;
            bool hasBounds = false;
            AddBladeSweepBounds(leftBladePivot, ref bounds, ref hasBounds);
            AddBladeSweepBounds(rightBladePivot, ref bounds, ref hasBounds);

            center = bounds.center;
            size = bounds.size;
            return hasBounds;
        }

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
            float normalizedTime = elapsedRotationTime / rotationDuration;
            SetRotation(normalizedTime);

            if (elapsedRotationTime >= rotationDuration)
            {
                isPlaying = false;
                SetPivotsActive(false);
            }
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
            DrawBladeTrajectory(leftBladePivot, Color.cyan);
            DrawBladeTrajectory(rightBladePivot, Color.magenta);
            DrawHitbox();
        }

        private void DrawBladeTrajectory(
            Transform pivot,
            Color color)
        {
            if (pivot == null || pivot.parent == null || !TryGetBladeTip(pivot, out Vector3 bladeTip))
            {
                return;
            }

            Quaternion neutralRotation = GetNeutralRotation(pivot);

            Gizmos.color = color;
            DrawTrajectory(pivot, bladeTip, neutralRotation);
        }

        private Quaternion GetNeutralRotation(Transform pivot)
        {
            if (!Application.isPlaying)
            {
                return pivot.localRotation;
            }

            return pivot == leftBladePivot
                ? leftNeutralRotation
                : rightNeutralRotation;
        }

        private void DrawTrajectory(
            Transform pivot,
            Vector3 bladeTip,
            Quaternion neutralRotation)
        {
            Vector3 previousPoint = GetBladeTipWorldPosition(pivot, bladeTip, neutralRotation);
            for (int i = 1; i <= TrajectorySegments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(
                        rotationAngle * i / TrajectorySegments,
                        Vector3.right)
                    * neutralRotation;
                Vector3 point = GetBladeTipWorldPosition(pivot, bladeTip, rotation);
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }

        private static Vector3 GetBladeTipWorldPosition(Transform pivot, Vector3 bladeTip, Quaternion pivotRotation)
        {
            Matrix4x4 pivotMatrix = pivot.parent.localToWorldMatrix
                * Matrix4x4.TRS(pivot.localPosition, pivotRotation, pivot.localScale);
            return pivotMatrix.MultiplyPoint3x4(bladeTip);
        }

        private static bool TryGetBladeTip(Transform pivot, out Vector3 bladeTip)
        {
            MeshFilter[] meshFilters = pivot.GetComponentsInChildren<MeshFilter>(true);
            float farthestDistanceSquared = 0f;
            bladeTip = default;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                Mesh mesh = meshFilters[i].sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Bounds bounds = mesh.bounds;
                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        for (int z = 0; z <= 1; z++)
                        {
                            Vector3 point = new(
                                x == 0 ? bounds.min.x : bounds.max.x,
                                y == 0 ? bounds.min.y : bounds.max.y,
                                z == 0 ? bounds.min.z : bounds.max.z);
                            Vector3 pointInPivotSpace = pivot.InverseTransformPoint(
                                meshFilters[i].transform.TransformPoint(point));
                            float distanceSquared = pointInPivotSpace.sqrMagnitude;
                            if (distanceSquared <= farthestDistanceSquared)
                            {
                                continue;
                            }

                            farthestDistanceSquared = distanceSquared;
                            bladeTip = pointInPivotSpace;
                        }
                    }
                }
            }

            return farthestDistanceSquared > 0f;
        }

        private void AddBladeSweepBounds(Transform pivot, ref Bounds bounds, ref bool hasBounds)
        {
            if (pivot == null || pivot.parent == null)
            {
                return;
            }

            MeshFilter[] meshFilters = pivot.GetComponentsInChildren<MeshFilter>(true);
            Quaternion neutralRotation = GetNeutralRotation(pivot);
            Matrix4x4 parentToLocalMatrix = transform.worldToLocalMatrix * pivot.parent.localToWorldMatrix;

            for (int i = 0; i <= TrajectorySegments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(
                        rotationAngle * i / TrajectorySegments,
                        Vector3.right)
                    * neutralRotation;
                Matrix4x4 pivotToLocalMatrix = parentToLocalMatrix
                    * Matrix4x4.TRS(pivot.localPosition, rotation, pivot.localScale);

                for (int filterIndex = 0; filterIndex < meshFilters.Length; filterIndex++)
                {
                    Mesh mesh = meshFilters[filterIndex].sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    AddMeshBounds(
                        pivot,
                        meshFilters[filterIndex],
                        mesh.bounds,
                        pivotToLocalMatrix,
                        ref bounds,
                        ref hasBounds);
                }
            }
        }

        private static void AddMeshBounds(
            Transform pivot,
            MeshFilter meshFilter,
            Bounds meshBounds,
            Matrix4x4 pivotToLocalMatrix,
            ref Bounds bounds,
            ref bool hasBounds)
        {
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 meshPoint = new(
                            x == 0 ? meshBounds.min.x : meshBounds.max.x,
                            y == 0 ? meshBounds.min.y : meshBounds.max.y,
                            z == 0 ? meshBounds.min.z : meshBounds.max.z);
                        Vector3 pointInPivotSpace = pivot.InverseTransformPoint(
                            meshFilter.transform.TransformPoint(meshPoint));
                        Vector3 pointInLocalSpace = pivotToLocalMatrix.MultiplyPoint3x4(pointInPivotSpace);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(pointInLocalSpace, Vector3.zero);
                            hasBounds = true;
                            continue;
                        }

                        bounds.Encapsulate(pointInLocalSpace);
                    }
                }
            }
        }

        private void DrawHitbox()
        {
            if (!TryGetHitbox(out Vector3 center, out Vector3 size))
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
