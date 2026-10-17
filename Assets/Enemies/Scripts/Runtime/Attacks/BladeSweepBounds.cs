using UnityEngine;

namespace RPGame.Enemies
{
    internal static class BladeSweepBounds
    {
        private const int TrajectorySegments = 32;

        internal static bool TryCalculate(BladeSweepMotion motion, out Vector3 center, out Vector3 size)
        {
            Bounds bounds = default;
            bool hasBounds = false;
            AddBladeSweepBounds(motion, motion.LeftBladePivot, ref bounds, ref hasBounds);
            AddBladeSweepBounds(motion, motion.RightBladePivot, ref bounds, ref hasBounds);

            center = bounds.center;
            size = bounds.size;
            return hasBounds;
        }

        internal static void DrawGizmos(BladeSweepMotion motion)
        {
            DrawBladeTrajectory(motion, motion.LeftBladePivot, Color.cyan);
            DrawBladeTrajectory(motion, motion.RightBladePivot, Color.magenta);

            if (!TryCalculate(motion, out Vector3 center, out Vector3 size))
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.matrix = motion.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        private static void DrawBladeTrajectory(
            BladeSweepMotion motion,
            Transform pivot,
            Color color)
        {
            if (pivot == null || pivot.parent == null || !TryGetBladeTip(pivot, out Vector3 bladeTip))
            {
                return;
            }

            Quaternion neutralRotation = motion.GetNeutralRotation(pivot);
            Vector3 previousPoint = GetBladeTipWorldPosition(pivot, bladeTip, neutralRotation);
            Gizmos.color = color;

            for (int i = 1; i <= TrajectorySegments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(
                        motion.RotationAngle * i / TrajectorySegments,
                        Vector3.right)
                    * neutralRotation;
                Vector3 point = GetBladeTipWorldPosition(pivot, bladeTip, rotation);
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }

        private static void AddBladeSweepBounds(
            BladeSweepMotion motion,
            Transform pivot,
            ref Bounds bounds,
            ref bool hasBounds)
        {
            if (pivot == null || pivot.parent == null)
            {
                return;
            }

            MeshFilter[] meshFilters = pivot.GetComponentsInChildren<MeshFilter>(true);
            Quaternion neutralRotation = motion.GetNeutralRotation(pivot);
            Matrix4x4 parentToLocalMatrix = motion.transform.worldToLocalMatrix * pivot.parent.localToWorldMatrix;

            for (int i = 0; i <= TrajectorySegments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(
                        motion.RotationAngle * i / TrajectorySegments,
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

        private static Vector3 GetBladeTipWorldPosition(Transform pivot, Vector3 bladeTip, Quaternion pivotRotation)
        {
            Matrix4x4 pivotMatrix = pivot.parent.localToWorldMatrix
                * Matrix4x4.TRS(pivot.localPosition, pivotRotation, pivot.localScale);
            return pivotMatrix.MultiplyPoint3x4(bladeTip);
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
    }
}
