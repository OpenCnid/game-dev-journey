using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public enum ChaseHitRegion { Body, Head }

    public struct ChaseImpact
    {
        public ChaseEnemy Enemy;
        public Vector3 Point;
        public float Distance;
        public ChaseHitRegion Region;
    }

    public static class ChaseAim
    {
        public static bool Intersect(ChaseEnemy enemy, Vector3 origin, Vector3 direction,
            float reach, float payloadRadius, out ChaseImpact impact)
        {
            impact = default;
            direction.Normalize();
            if (direction.sqrMagnitude < .5f) return false;
            float head = Sphere(origin, direction, enemy.HeadPosition, enemy.HeadRadius + payloadRadius);
            float radius = enemy.BodyRadius + payloadRadius;
            Vector3 lower = enemy.Feet + Vector3.up * enemy.BodyRadius;
            Vector3 upper = enemy.Feet + Vector3.up * Mathf.Max(enemy.BodyRadius,
                enemy.HeadOffset.y * enemy.SizeScale - enemy.HeadRadius - enemy.BodyRadius);
            float body = Capsule(origin, direction, lower, upper, radius);
            float distance = Mathf.Min(head, body);
            if (distance < 0 || distance > reach || float.IsPositiveInfinity(distance)) return false;
            impact = new ChaseImpact { Enemy = enemy, Distance = distance,
                Point = origin + direction * distance,
                Region = head <= body ? ChaseHitRegion.Head : ChaseHitRegion.Body };
            return true;
        }

        static float Sphere(Vector3 origin, Vector3 direction, Vector3 center, float radius)
        {
            Vector3 offset = origin - center;
            float c = offset.sqrMagnitude - radius * radius;
            if (c <= 0) return 0;
            float b = Vector3.Dot(offset, direction);
            float discriminant = b * b - c;
            if (discriminant < 0) return float.PositiveInfinity;
            float distance = -b - Mathf.Sqrt(discriminant);
            return distance >= 0 ? distance : float.PositiveInfinity;
        }

        static float Capsule(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, float radius)
        {
            Vector3 segment = b - a, offset = origin - a;
            float length = segment.sqrMagnitude;
            if (length < .000001f) return Sphere(origin, direction, a, radius);
            Vector3 closest = a + segment * Mathf.Clamp01(Vector3.Dot(offset, segment) / length);
            if ((origin - closest).sqrMagnitude <= radius * radius) return 0;
            float sd = Vector3.Dot(segment, direction), so = Vector3.Dot(segment, offset);
            float od = Vector3.Dot(direction, offset);
            float quadratic = length - sd * sd;
            float linear = length * od - so * sd;
            float constant = length * offset.sqrMagnitude - so * so - radius * radius * length;
            float discriminant = linear * linear - quadratic * constant;
            float result = Mathf.Min(Sphere(origin, direction, a, radius), Sphere(origin, direction, b, radius));
            if (quadratic > .000001f && discriminant >= 0)
            {
                float distance = (-linear - Mathf.Sqrt(discriminant)) / quadratic;
                float along = so + distance * sd;
                if (distance >= 0 && along >= 0 && along <= length)
                    result = Mathf.Min(result, distance);
            }
            return result;
        }
    }
}
