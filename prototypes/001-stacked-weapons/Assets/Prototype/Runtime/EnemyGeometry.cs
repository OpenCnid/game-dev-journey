using UnityEngine;

namespace Journey
{
    public enum HitRegion { None, Body, Head }

    public readonly struct AimResult
    {
        public readonly int EnemyIndex, SpawnVersion;
        public readonly HitRegion Region;
        public readonly float Distance;
        public bool Hit => Region != HitRegion.None;
        public AimResult(int enemyIndex, int spawnVersion, HitRegion region, float distance)
        { EnemyIndex = enemyIndex; SpawnVersion = spawnVersion; Region = region; Distance = distance; }
        public static AimResult Miss => new AimResult(-1, 0, HitRegion.None, CombatSimulation.RifleRange);
    }

    // Rendering and precision queries use the same transformed unit cubes.
    public static class EnemyGeometry
    {
        public static readonly Vector3 BodyOffset = Vector3.up * .8f, BodyScale = new Vector3(.85f, 1.4f, .85f);
        public static readonly Vector3 HeadOffset = Vector3.up * 1.6f, HeadScale = Vector3.one * .45f;
        public static Matrix4x4 Body(Vector3 p, bool wounded) => BurstInstances.CharacterMatrix(p, BodyOffset, BodyScale, wounded);
        public static Matrix4x4 Head(Vector3 p, bool wounded) => BurstInstances.CharacterMatrix(p, HeadOffset, HeadScale, wounded, core: true);

        public static bool ValidRay(Ray ray) => Finite(ray.origin) && Finite(ray.direction) && ray.direction.sqrMagnitude > 0;
        static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
            float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        public static bool RayCube(Ray ray, Matrix4x4 pose, out float distance)
        {
            distance = 0;
            if (!ValidRay(ray)) return false;
            Matrix4x4 inverse = pose.inverse;
            Vector3 origin = inverse.MultiplyPoint3x4(ray.origin);
            // Do not normalize after transforming: t must remain distance along the world ray.
            Vector3 direction = inverse.MultiplyVector(ray.direction);
            float near = 0, far = float.PositiveInfinity;
            for (int axis = 0; axis < 3; axis++)
            {
                if (direction[axis] == 0)
                { if (origin[axis] < -.5f || origin[axis] > .5f) return false; continue; }
                float a = (-.5f - origin[axis]) / direction[axis], b = (.5f - origin[axis]) / direction[axis];
                near = Mathf.Max(near, Mathf.Min(a, b)); far = Mathf.Min(far, Mathf.Max(a, b));
                if (near > far) return false;
            }
            distance = near;
            return true;
        }
    }
}
