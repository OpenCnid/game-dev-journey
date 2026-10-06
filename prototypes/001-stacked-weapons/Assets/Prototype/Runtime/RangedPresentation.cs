using System;
using UnityEngine;

namespace Journey
{
    // A bounded graybox silhouette and committed aim cue; reads authority snapshots only.
    public sealed class RangedPresentation : IDisposable
    {
        readonly GameObject root;
        readonly Transform[] crowns = new Transform[5], cannons = new Transform[5], charges = new Transform[5];
        readonly LineRenderer[] aims = new LineRenderer[5], tails = new LineRenderer[RangedThreats.Capacity];
        readonly Transform[] bolts = new Transform[RangedThreats.Capacity];
        readonly Material sentinel, warning, projectile;
        public RangedPresentation(Transform parent)
        {
            root = new GameObject("Ranged threat cues"); root.transform.SetParent(parent);
            sentinel = Material(new Color(1, .3f, .08f)); warning = Material(new Color(1, .55f, .12f)); projectile = Material(new Color(1, .88f, .45f));
            for (int i = 0; i < 5; i++)
            {
                crowns[i] = Shape("Sentinel crown", PrimitiveType.Cube, sentinel);
                cannons[i] = Shape("Sentinel cannon", PrimitiveType.Cube, sentinel);
                charges[i] = Shape("Charge", PrimitiveType.Sphere, warning);
                aims[i] = Line("Committed aim", warning, .028f);
            }
            for (int i = 0; i < bolts.Length; i++)
            { bolts[i] = Shape("Enemy bolt", PrimitiveType.Sphere, projectile); tails[i] = Line("Bolt trail", warning, .10f); }
        }
        static Material Material(Color color)
        { var m = new Material(Resources.Load<Material>("Trail")); m.color = color; return m; }
        Transform Shape(string name, PrimitiveType kind, Material material)
        {
            GameObject g = GameObject.CreatePrimitive(kind); g.name = name; g.transform.SetParent(root.transform);
            UnityEngine.Object.Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = material;
            g.SetActive(false); return g.transform;
        }
        LineRenderer Line(string name, Material material, float width)
        {
            var l = new GameObject(name).AddComponent<LineRenderer>(); l.transform.SetParent(root.transform);
            l.positionCount = 2; l.startWidth = l.endWidth = width; l.sharedMaterial = material; l.enabled = false; return l;
        }
        public void Draw(SharedSnapshot s)
        {
            bool active = s.Ranged != null && (s.State == SharedState.Running || s.State == SharedState.Choosing);
            for (int i = 0; i < crowns.Length; i++)
            {
                crowns[i].gameObject.SetActive(active); cannons[i].gameObject.SetActive(active);
                bool charging = active && s.Ranged.Attacks[i].Phase == RangedPhase.Windup;
                charges[i].gameObject.SetActive(charging); aims[i].enabled = charging;
                if (!active) continue;
                Vector3 position = s.Enemies[i].Position, direction = s.Ranged.Attacks[i].Direction;
                if (direction.sqrMagnitude < .5f) direction = Vector3.forward;
                Quaternion rotation = Quaternion.LookRotation(direction);
                crowns[i].SetPositionAndRotation(position + Vector3.up * 2.12f, rotation * Quaternion.Euler(0, 0, 45));
                crowns[i].localScale = new Vector3(1.05f, .25f, .25f);
                Vector3 muzzle = position + Vector3.up * 1.15f + direction * .65f;
                cannons[i].SetPositionAndRotation(muzzle, rotation); cannons[i].localScale = new Vector3(.28f, .28f, .8f);
                if (!charging) continue;
                float progress = Mathf.Clamp01(1 - (s.Ranged.Attacks[i].Until - s.Elapsed) / RangedThreats.Windup);
                charges[i].position = muzzle + direction * .4f; charges[i].localScale = Vector3.one * Mathf.Lerp(.18f, .7f, progress);
                aims[i].SetPosition(0, muzzle); aims[i].SetPosition(1, muzzle + direction * (RangedThreats.Speed * RangedThreats.Lifetime));
                aims[i].startWidth = aims[i].endWidth = Mathf.Lerp(.018f, .07f, progress);
            }
            for (int i = 0; i < bolts.Length; i++)
            {
                bool live = active && i < s.Ranged.Count;
                bolts[i].gameObject.SetActive(live); tails[i].enabled = live;
                if (!live) continue;
                EnemyBolt b = s.Ranged.Bolts[i]; Vector3 position = b.Position + Vector3.up * 1.15f;
                bolts[i].position = position; bolts[i].localScale = Vector3.one * (RangedThreats.Radius * 2);
                tails[i].SetPosition(0, position); tails[i].SetPosition(1, position - b.Direction * .9f);
            }
        }
        public void Dispose()
        { UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(sentinel); UnityEngine.Object.Destroy(warning); UnityEngine.Object.Destroy(projectile); }
    }
}
