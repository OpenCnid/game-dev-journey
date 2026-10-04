using UnityEngine;

namespace Journey
{
    // Bounded, disposable presentation state. No damage or collision decisions live here.
    public sealed class CombatFeedback
    {
        public const int BarCapacity = 64, VisibleBarCapacity = 12, ImpactCapacity = 16;
        public const float BarRange = 40, HitLifetime = .18f, KillLifetime = .5f;
        public const float PlayerPulseLifetime = .35f, PlayerLabelLifetime = .65f;
        struct Impact
        {
            public ShotResult Shot;
            public Vector3 Anchor;
            public float Remaining;
        }
        public readonly struct HealthBar
        {
            public readonly int EnemyIndex, SpawnVersion, Health, Priority;
            public readonly Vector2 Viewport;
            public readonly float Distance;
            public HealthBar(int enemyIndex, int spawnVersion, int health, Vector2 viewport, float distance, int priority)
            { EnemyIndex = enemyIndex; SpawnVersion = spawnVersion; Health = health; Viewport = viewport; Distance = distance; Priority = priority; }
        }
        readonly Impact[] impacts = new Impact[ImpactCapacity];
        readonly HealthBar[] bars = new HealthBar[BarCapacity];
        int nextImpact, previousHealth = 100;
        float hitMarkerRemaining, killMarkerRemaining;
        readonly Rect[] barBounds = new Rect[VisibleBarCapacity];
        readonly int[] visibleBars = new int[VisibleBarCapacity];
        readonly int[] previousOwners = new int[VisibleBarCapacity], previousSpawns = new int[VisibleBarCapacity];
        readonly float[] previousOffsets = new float[VisibleBarCapacity];
        int previousBarCount;
        GUIStyle cueStyle;
        public int BarCount { get; private set; }
        public HealthBar Bar(int index) => bars[index];
        public int VisibleBarCount { get; private set; }
        public HealthBar VisibleBar(int index) => bars[visibleBars[index]];
        public Rect VisibleBarBounds(int index) => barBounds[index];
        public float PlayerPulseRemaining { get; private set; }
        public float PlayerLabelRemaining { get; private set; }
        public int PlayerDamage { get; private set; }
        public bool KillMarkerVisible => killMarkerRemaining > 0;
        public bool HitMarkerVisible => hitMarkerRemaining > 0;
        public int ImpactCount
        {
            get { int count = 0; for (int i = 0; i < impacts.Length; i++) if (impacts[i].Remaining > 0) count++; return count; }
        }

        public void RecordShot(CombatSimulation sim, ShotResult shot)
        {
            if (sim.Benchmark || !shot.Hit) return;
            // A wound follows that incarnation; a death burst stays at the captured hit point.
            impacts[nextImpact] = new Impact { Shot = shot, Anchor = shot.Killed ? shot.End : shot.End - sim.Position(shot.EnemyIndex),
                Remaining = shot.Killed ? KillLifetime : HitLifetime };
            nextImpact = (nextImpact + 1) % ImpactCapacity;
            hitMarkerRemaining = .12f;
            if (shot.Killed) killMarkerRemaining = KillLifetime;
        }

        public void ObservePlayerHealth(int health)
        {
            if (health < previousHealth)
            {
                PlayerDamage = previousHealth - health;
                PlayerPulseRemaining = PlayerPulseLifetime; PlayerLabelRemaining = PlayerLabelLifetime;
            }
            previousHealth = health;
        }

        public void Advance(CombatSimulation sim, float dt)
        {
            if (sim.Benchmark || sim.State == RunState.Ready) { Reset(sim.PlayerHealth); return; }
            if (sim.State == RunState.Paused) return;
            dt = Mathf.Max(0, dt);
            hitMarkerRemaining = Mathf.Max(0, hitMarkerRemaining - dt);
            killMarkerRemaining = Mathf.Max(0, killMarkerRemaining - dt);
            PlayerPulseRemaining = Mathf.Max(0, PlayerPulseRemaining - dt);
            PlayerLabelRemaining = Mathf.Max(0, PlayerLabelRemaining - dt);
            for (int i = 0; i < impacts.Length; i++)
            {
                Impact impact = impacts[i];
                if (impact.Remaining <= 0) continue;
                if (!impact.Shot.Killed && (impact.Shot.EnemyIndex >= sim.Count ||
                    impact.Shot.SpawnVersion != sim.EnemySpawnVersion(impact.Shot.EnemyIndex))) impact.Remaining = 0;
                else impact.Remaining = Mathf.Max(0, impact.Remaining - dt);
                impacts[i] = impact;
            }
        }

        public void CollectBars(CombatSimulation sim, Camera camera, float alpha)
        {
            BarCount = 0;
            if (sim.Benchmark || (sim.State != RunState.Running && sim.State != RunState.Paused)) return;
            Vector3 eye = camera.transform.position;
            int target = sim.AimTarget(new Ray(eye, camera.transform.forward), out _);
            for (int i = 0; i < sim.Count; i++)
            {
                Vector3 p = sim.RenderPosition(i, alpha);
                float distanceSquared = (p - eye).sqrMagnitude;
                if (distanceSquared > BarRange * BarRange) continue;
                Vector3 viewport = camera.WorldToViewportPoint(p + Vector3.up * 2.05f);
                // Leave room for the existing top/bottom HUD and the complete bar at each edge.
                if (viewport.z <= camera.nearClipPlane || viewport.x < .025f || viewport.x > .975f || viewport.y < .13f || viewport.y > .85f) continue;
                float distance = Mathf.Sqrt(distanceSquared);
                int health = sim.EnemyHealth(i);
                int priority = i == target ? 0 : health < CombatSimulation.EnemyMaxHealth ? 1 : 2;
                int insert = BarCount;
                while (insert > 0 && (bars[insert - 1].Priority > priority ||
                    (bars[insert - 1].Priority == priority && bars[insert - 1].Distance > distance))) insert--;
                if (insert >= BarCapacity) continue;
                for (int j = Mathf.Min(BarCount, BarCapacity - 1); j > insert; j--) bars[j] = bars[j - 1];
                bars[insert] = new HealthBar(i, sim.EnemySpawnVersion(i), health, viewport, distance, priority);
                BarCount = Mathf.Min(BarCount + 1, BarCapacity);
            }
        }

        public void LayoutBars(float width, float height)
        {
            VisibleBarCount = 0;
            for (int i = 0; i < BarCount && VisibleBarCount < VisibleBarCapacity; i++)
            {
                HealthBar bar = bars[i];
                float w = Mathf.Clamp(62 - bar.Distance * .65f, 36, 60) + 4;
                float x = bar.Viewport.x * width - w / 2, y = (1 - bar.Viewport.y) * height - 2;
                float preferred = 0;
                if (bar.Priority != 0)
                    for (int p = 0; p < previousBarCount; p++)
                        if (previousOwners[p] == bar.EnemyIndex && previousSpawns[p] == bar.SpawnVersion) preferred = previousOffsets[p];
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    float offset = attempt == 0 ? preferred : -(attempt - 1) * 18;
                    Rect rect = new Rect(x, y + offset, w, 11);
                    if (rect.yMin < 95 || rect.yMax > height - 85 || rect.xMin < 16 || rect.xMax > width - 16) continue;
                    bool blocked = false;
                    Rect padded = new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6);
                    for (int j = 0; j < VisibleBarCount; j++) if (padded.Overlaps(barBounds[j])) { blocked = true; break; }
                    if (blocked) continue;
                    visibleBars[VisibleBarCount] = i; barBounds[VisibleBarCount++] = rect;
                    break;
                }
            }
            previousBarCount = VisibleBarCount;
            for (int i = 0; i < VisibleBarCount; i++)
            {
                HealthBar bar = VisibleBar(i);
                previousOwners[i] = bar.EnemyIndex; previousSpawns[i] = bar.SpawnVersion;
                previousOffsets[i] = barBounds[i].y - ((1 - bar.Viewport.y) * height - 2);
            }
        }

        public void BuildSparks(CombatSimulation sim, float alpha, Matrix4x4[] hits, Matrix4x4[] kills, out int hitCount, out int killCount)
        {
            hitCount = killCount = 0;
            for (int i = 0; i < impacts.Length; i++)
            {
                Impact impact = impacts[i];
                if (impact.Remaining <= 0) continue;
                bool killed = impact.Shot.Killed;
                float progress = 1 - impact.Remaining / (killed ? KillLifetime : HitLifetime);
                Vector3 center = killed ? impact.Anchor : sim.RenderPosition(impact.Shot.EnemyIndex, alpha) + impact.Anchor;
                int count = killed ? 8 : 5;
                float size = Mathf.Lerp(killed ? .25f : .11f, killed ? .04f : .015f, progress);
                for (int n = 0; n < count; n++)
                {
                    float angle = n * Mathf.PI * 2 / count;
                    Vector3 spread = new Vector3(Mathf.Cos(angle), .3f + Mathf.Sin(angle), Mathf.Sin(angle * 2));
                    Vector3 p = center + spread * (progress * (killed ? 1.35f : .4f));
                    Matrix4x4 matrix = Matrix4x4.TRS(p, Quaternion.Euler(n * 31, progress * 180, n * 47), Vector3.one * size);
                    if (killed) kills[killCount++] = matrix; else hits[hitCount++] = matrix;
                }
            }
        }

        public void DrawHUD(float width, float height)
        {
            Color saved = GUI.color;
            if (cueStyle == null) cueStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            LayoutBars(width, height);
            for (int i = 0; i < VisibleBarCount; i++)
            {
                HealthBar bar = VisibleBar(i);
                Rect bounds = barBounds[i];
                Vector2 owner = new Vector2(bar.Viewport.x * width, (1 - bar.Viewport.y) * height + 6);
                Vector2 labelBottom = new Vector2(bounds.center.x, bounds.yMax);
                float opacity = bar.Priority == 2 ? .65f : 1;
                if (Vector2.Distance(owner, labelBottom) > 8) Line(owner, labelBottom, new Color(.65f, .8f, .85f, opacity * .75f));
                Paint(bounds, bar.Priority == 0 ? Color.white : new Color(.015f, .025f, .035f, .95f));
                float w = bounds.width - 4, x = bounds.x + 2, y = bounds.y + 2;
                Paint(new Rect(x, y, w, 7), new Color(.26f, .29f, .32f, opacity));
                Paint(new Rect(x, y, w * bar.Health / CombatSimulation.EnemyMaxHealth, 7),
                    bar.Health < CombatSimulation.EnemyMaxHealth ? new Color(1, .68f, .15f, opacity) : new Color(.35f, .95f, .72f, opacity));
                Paint(new Rect(x + w / 2 - 1, y, 2, 7), new Color(.015f, .025f, .035f));
            }
            if (PlayerPulseRemaining > 0)
            {
                float strength = PlayerPulseRemaining / PlayerPulseLifetime;
                for (int layer = 0; layer < 3; layer++)
                {
                    float inset = layer * 12;
                    Color red = new Color(1, .12f, .08f, strength * (.6f - layer * .18f));
                    Paint(new Rect(inset, inset, width - inset * 2, 12), red);
                    Paint(new Rect(inset, height - inset - 12, width - inset * 2, 12), red);
                    Paint(new Rect(inset, inset + 12, 12, height - inset * 2 - 24), red);
                    Paint(new Rect(width - inset - 12, inset + 12, 12, height - inset * 2 - 24), red);
                }
            }
            if (PlayerLabelRemaining > 0)
            {
                Paint(new Rect(width / 2 - 110, height - 124, 220, 32), new Color(.1f, .015f, .015f, .85f));
                GUI.color = new Color(1, .4f, .3f);
                GUI.Label(new Rect(width / 2 - 110, height - 124, 220, 32), $"DAMAGE  -{PlayerDamage} HP", cueStyle);
            }
            if (HitMarkerVisible) Marker(width, height, 9, Color.white);
            if (KillMarkerVisible)
            {
                Color gold = new Color(1, .75f, .15f);
                Marker(width, height, 22, gold);
                GUI.color = gold;
                GUI.Label(new Rect(width / 2 - 60, height / 2 + 35, 120, 26), "KILL", cueStyle);
            }
            GUI.color = saved;
        }
        static void Paint(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }
        static void Marker(float width, float height, float offset, Color color)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(45, new Vector2(width / 2, height / 2));
            Paint(new Rect(width / 2 - offset - 6, height / 2 - 1, 6, 2), color);
            Paint(new Rect(width / 2 + offset, height / 2 - 1, 6, 2), color);
            Paint(new Rect(width / 2 - 1, height / 2 - offset - 6, 2, 6), color);
            Paint(new Rect(width / 2 - 1, height / 2 + offset, 2, 6), color);
            GUI.matrix = saved;
        }
        static void Line(Vector2 from, Vector2 to, Color color)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg, from);
            Paint(new Rect(from.x, from.y, Vector2.Distance(from, to), 1), color);
            GUI.matrix = saved;
        }
        public void Reset(int playerHealth = 100)
        {
            System.Array.Clear(impacts, 0, impacts.Length);
            nextImpact = BarCount = VisibleBarCount = previousBarCount = PlayerDamage = 0; previousHealth = playerHealth;
            hitMarkerRemaining = killMarkerRemaining = PlayerPulseRemaining = PlayerLabelRemaining = 0;
        }
    }
}
