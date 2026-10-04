using UnityEngine;

namespace Journey
{
    // Bounded, disposable presentation state. No damage or collision decisions live here.
    public sealed class CombatFeedback
    {
        public const int BarCapacity = 64, VisibleBarCapacity = 3, ImpactCapacity = 16;
        public const float BarRange = 40, HitLifetime = .18f, KillLifetime = .5f;
        public const float PlayerPulseLifetime = .35f, PlayerLabelLifetime = .65f;
        const float OverheadHeight = .45f, MinimumHeadGap = 12;
        struct Impact
        {
            public ShotResult Shot;
            public Vector3 Anchor;
            public float Remaining;
        }
        public readonly struct HealthBar
        {
            public readonly int EnemyIndex, SpawnVersion, Health, Priority;
            public readonly Vector2 Viewport, OwnerViewport;
            public readonly float Distance, HeadTopViewport;
            public HealthBar(int enemyIndex, int spawnVersion, int health, Vector2 viewport, Vector2 ownerViewport, float distance, int priority, float headTopViewport)
            { EnemyIndex = enemyIndex; SpawnVersion = spawnVersion; Health = health; Viewport = viewport; OwnerViewport = ownerViewport; Distance = distance; Priority = priority; HeadTopViewport = headTopViewport; }
        }
        readonly Impact[] impacts = new Impact[ImpactCapacity];
        readonly HealthBar[] bars = new HealthBar[BarCapacity];
        int nextImpact, previousHealth = 100;
        float hitMarkerRemaining, killMarkerRemaining;
        readonly Rect[] barBounds = new Rect[VisibleBarCapacity];
        readonly int[] visibleBars = new int[VisibleBarCapacity];
        readonly int[] labelOwners = new int[VisibleBarCapacity], labelVersions = new int[VisibleBarCapacity];
        readonly float[] revealDelay = new float[VisibleBarCapacity];
        HealthBar focusBar;
        GUIStyle cueStyle;
        public CombatFeedback() { Reset(); }
        public int BarCount { get; private set; }
        public HealthBar Bar(int index) => bars[index];
        public int VisibleBarCount { get; private set; }
        public HealthBar VisibleBar(int index) => bars[visibleBars[index]];
        public Rect VisibleBarBounds(int index) => barBounds[index];
        public int LabelOwner(int slot) => labelOwners[slot];
        public int FocusEnemy { get; private set; } = -1;
        public int FocusHealth { get; private set; }
        public bool FocusAimed { get; private set; }
        public bool FocusVisible => FocusAimed && FocusHealth > 0;
        public bool TargetHealthBarVisible => FocusVisible && FocusHealth < CombatSimulation.EnemyMaxHealth;
        public Rect TargetBarBounds(float width, float height) => LabelBounds(focusBar, width, height);
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
            for (int i = 0; i < VisibleBarCapacity; i++) revealDelay[i] = Mathf.Max(0, revealDelay[i] - dt);
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
            FocusAimed = false; FocusEnemy = -1; FocusHealth = 0;
            if (sim.Benchmark || (sim.State != RunState.Running && sim.State != RunState.Paused))
                return;
            Vector3 eye = camera.transform.position;
            int target = sim.AimTarget(new Ray(eye, camera.transform.forward), out _);
            if (target >= 0)
            {
                FocusEnemy = target; FocusHealth = sim.EnemyHealth(target);
                FocusAimed = true;
                Vector3 p = sim.RenderPosition(target, alpha);
                focusBar = ProjectBar(camera, p, target, sim.EnemySpawnVersion(target), FocusHealth, Vector3.Distance(p, eye), 0);
            }
            for (int i = 0; i < sim.Count; i++)
            {
                Vector3 p = sim.RenderPosition(i, alpha);
                float distanceSquared = (p - eye).sqrMagnitude;
                if (distanceSquared > BarRange * BarRange) continue;
                int health = sim.EnemyHealth(i);
                float distance = Mathf.Sqrt(distanceSquared);
                int priority = health < CombatSimulation.EnemyMaxHealth ? 1 : 2;
                HealthBar bar = ProjectBar(camera, p, i, sim.EnemySpawnVersion(i), health, distance, priority);
                Vector2 viewport = bar.Viewport;
                // Leave room for the existing top/bottom HUD and the complete bar at each edge.
                if (bar.HeadTopViewport >= 1 || viewport.x < .025f || viewport.x > .975f || viewport.y < .13f || viewport.y > .85f) continue;
                int insert = BarCount;
                while (insert > 0 && (bars[insert - 1].Priority > priority ||
                    (bars[insert - 1].Priority == priority && bars[insert - 1].Distance > distance))) insert--;
                if (insert >= BarCapacity) continue;
                for (int j = Mathf.Min(BarCount, BarCapacity - 1); j > insert; j--) bars[j] = bars[j - 1];
                bars[insert] = bar;
                BarCount = Mathf.Min(BarCount + 1, BarCapacity);
            }
        }

        public void LayoutBars(float width, float height)
        {
            VisibleBarCount = 0;
            // Existing owners get first refusal; distance/aim changes cannot replace them.
            for (int slot = 0; slot < VisibleBarCapacity; slot++)
            {
                int candidate = -1;
                for (int i = 0; i < BarCount; i++)
                    if (bars[i].EnemyIndex == labelOwners[slot] && bars[i].SpawnVersion == labelVersions[slot] && bars[i].Health < CombatSimulation.EnemyMaxHealth)
                    { candidate = i; break; }
                if (candidate < 0 && labelOwners[slot] >= 0)
                { labelOwners[slot] = -1; revealDelay[slot] = .25f; }
                if (candidate < 0 && revealDelay[slot] <= 0)
                    for (int i = 0; i < BarCount; i++)
                    {
                        if (bars[i].Health >= CombatSimulation.EnemyMaxHealth || (TargetHealthBarVisible && bars[i].EnemyIndex == FocusEnemy)) continue;
                        bool held = false;
                        for (int j = 0; j < VisibleBarCapacity; j++) if (labelOwners[j] == bars[i].EnemyIndex) held = true;
                        Rect proposed = LabelBounds(bars[i], width, height);
                        if (held || !Fits(proposed, width, height) || Overlaps(proposed, width, height)) continue;
                        candidate = i; labelOwners[slot] = bars[i].EnemyIndex; labelVersions[slot] = bars[i].SpawnVersion; break;
                    }
                if (candidate < 0) continue;
                if (TargetHealthBarVisible && bars[candidate].EnemyIndex == FocusEnemy) continue;
                Rect rect = LabelBounds(bars[candidate], width, height);
                if (!Fits(rect, width, height) || Overlaps(rect, width, height)) { revealDelay[slot] = .25f; continue; }
                if (revealDelay[slot] > 0) continue;
                visibleBars[VisibleBarCount] = candidate; barBounds[VisibleBarCount++] = rect;
            }
        }
        static Rect LabelBounds(HealthBar bar, float width, float height)
        {
            float w = Mathf.Clamp(52 - bar.Distance * .5f, 30, 50) + 4;
            if (bar.Priority == 0) w += 8;
            float h = bar.Priority == 0 ? 11 : 9;
            float bottom = Mathf.Min((1 - bar.Viewport.y) * height, (1 - bar.HeadTopViewport) * height - MinimumHeadGap);
            return new Rect(bar.Viewport.x * width - w / 2, bottom - h, w, h);
        }
        static bool Fits(Rect rect, float width, float height) => rect.yMin >= 95 && rect.yMax <= height - 85 && rect.xMin >= 16 && rect.xMax <= width - 16;
        bool Overlaps(Rect rect, float width, float height)
        {
            Rect padded = new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8);
            Rect target = TargetBarBounds(width, height);
            if (TargetHealthBarVisible && Fits(target, width, height) && padded.Overlaps(target)) return true;
            for (int i = 0; i < VisibleBarCount; i++) if (padded.Overlaps(barBounds[i])) return true;
            return false;
        }
        static HealthBar ProjectBar(Camera camera, Vector3 p, int enemy, int version, int health, float distance, int priority)
        {
            Matrix4x4 core = BurstInstances.CharacterMatrix(p, Vector3.up * 1.6f, Vector3.one * .45f,
                health < CombatSimulation.EnemyMaxHealth, core: true);
            Vector3 owner = core.MultiplyPoint3x4(new Vector3(0, .55f, 0));
            Vector3 ownerViewport = camera.WorldToViewportPoint(owner);
            Vector3 viewport = camera.WorldToViewportPoint(owner + Vector3.up * OverheadHeight);
            float top = ownerViewport.y;
            // Clearance follows the highest visible head corner, including wounded shear and camera pitch.
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 v = camera.WorldToViewportPoint(core.MultiplyPoint3x4(new Vector3((corner & 1) == 0 ? -.5f : .5f,
                    (corner & 2) == 0 ? -.5f : .5f, (corner & 4) == 0 ? -.5f : .5f)));
                if (v.z <= camera.nearClipPlane || viewport.z <= camera.nearClipPlane) { top = 2; break; }
                top = Mathf.Max(top, v.y);
            }
            return new HealthBar(enemy, version, health, viewport, ownerViewport, distance, priority, top);
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
                DrawHealthBar(VisibleBar(i), barBounds[i], width, height);
            }
            if (TargetHealthBarVisible)
            {
                Rect targetBar = TargetBarBounds(width, height);
                if (Fits(targetBar, width, height)) DrawHealthBar(focusBar, targetBar, width, height);
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
        static void DrawHealthBar(HealthBar bar, Rect bounds, float width, float height)
        {
            Vector2 owner = new Vector2(bar.OwnerViewport.x * width, (1 - bar.OwnerViewport.y) * height);
            Vector2 bottom = new Vector2(bounds.center.x, bounds.yMax);
            if (Vector2.Distance(owner, bottom) > 8) Line(owner, bottom, new Color(.65f, .65f, .65f, .5f));
            Color border = new Color(.015f, .025f, .035f, .95f);
            Paint(bounds, border);
            float w = bounds.width - 4, h = bounds.height - 4, x = bounds.x + 2, y = bounds.y + 2;
            Paint(new Rect(x, y, w, h), new Color(.26f, .29f, .32f));
            Paint(new Rect(x, y, w * bar.Health / CombatSimulation.EnemyMaxHealth, h), new Color(1, .68f, .15f));
            Paint(new Rect(x + w / 2 - 1, y, 2, h), border);
        }
        static void Paint(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }
        static void Marker(float width, float height, float offset, Color color)
        {
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = RotateGUI(saved, new Vector2(width / 2, height / 2), 45);
            Paint(new Rect(width / 2 - offset - 6, height / 2 - 1, 6, 2), color);
            Paint(new Rect(width / 2 + offset, height / 2 - 1, 6, 2), color);
            Paint(new Rect(width / 2 - 1, height / 2 - offset - 6, 2, 6), color);
            Paint(new Rect(width / 2 - 1, height / 2 + offset, 2, 6), color);
            GUI.matrix = saved;
        }
        static void Line(Vector2 from, Vector2 to, Color color)
        {
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = RotateGUI(saved, from, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
            Paint(new Rect(from.x, from.y, Vector2.Distance(from, to), 1), color);
            GUI.matrix = saved;
        }
        public static Matrix4x4 RotateGUI(Matrix4x4 matrix, Vector2 pivot, float degrees)
        {
            Vector3 p = new Vector3(pivot.x, pivot.y, 0);
            return matrix * Matrix4x4.TRS(p, Quaternion.Euler(0, 0, degrees), Vector3.one) *
                Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
        }
        public void Reset(int playerHealth = 100)
        {
            System.Array.Clear(impacts, 0, impacts.Length);
            nextImpact = BarCount = VisibleBarCount = PlayerDamage = 0; previousHealth = playerHealth;
            FocusEnemy = -1; FocusHealth = 0; FocusAimed = false;
            for (int i = 0; i < VisibleBarCapacity; i++) { labelOwners[i] = -1; labelVersions[i] = 0; revealDelay[i] = 0; }
            hitMarkerRemaining = killMarkerRemaining = PlayerPulseRemaining = PlayerLabelRemaining = 0;
        }
    }
}
