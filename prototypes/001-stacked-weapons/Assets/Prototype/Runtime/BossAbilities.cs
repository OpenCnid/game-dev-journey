using UnityEngine;
using Unity.Profiling;

namespace Journey
{
    // Synthetic boss workload: every enemy alternates a targeted area attack and
    // an eight-way projectile burst. All ability states/projectiles tick at 60 Hz.
    public sealed class BossAbilities : System.IDisposable
    {
        public const int ProjectileCapacity = 131072;
        public const float Cooldown = 6f, TelegraphSeconds = .75f, AreaRadius = 2.5f;
        public const float ProjectileLifetime = 3f, ProjectileSpeed = 8f;
        public const int BurstSize = 8;
        readonly float[] timers = new float[CombatSimulation.Capacity];
        readonly bool[] casting = new bool[CombatSimulation.Capacity];
        readonly bool[] burstNext = new bool[CombatSimulation.Capacity];
        readonly Vector3[] targets = new Vector3[CombatSimulation.Capacity];
        readonly Vector3[] projectilePositions = new Vector3[ProjectileCapacity];
        readonly Vector3[] projectilePrevious = new Vector3[ProjectileCapacity];
        readonly Vector3[] projectileVelocities = new Vector3[ProjectileCapacity];
        readonly float[] life = new float[ProjectileCapacity];
        readonly Vector3[] burstDirections = new Vector3[BurstSize];
        static readonly ProfilerMarker AbilityMarker = new ProfilerMarker("Journey.Abilities");
        static readonly ProfilerMarker ProjectileMarker = new ProfilerMarker("Journey.Projectiles");
        BurstProjectiles burstProjectiles;
        internal Vector3[] Positions => projectilePositions;
        internal Vector3[] PreviousPositions => projectilePrevious;
        public bool Enabled { get; private set; }
        public bool Synchronized { get; private set; }
        public int Projectiles { get; private set; }
        public int Telegraphs { get; private set; }
        public int Activations { get; private set; }
        public int Spawned { get; private set; }
        public int Dropped { get; private set; }
        public int PlayerHits { get; private set; }
        public int StateUpdates { get; private set; }
        public int ProjectileUpdates { get; private set; }
        public bool IsCasting(int i) => Enabled && casting[i];
        public Vector3 Target(int i) => targets[i];
        public Vector3 RenderProjectile(int i, float alpha) => Vector3.Lerp(projectilePrevious[i], projectilePositions[i], alpha);

        public void Reset(int count, int seed, bool enabled, bool synchronized)
        {
            Enabled = enabled; Synchronized = synchronized;
            Projectiles = Telegraphs = Activations = Spawned = Dropped = PlayerHits = 0;
            StateUpdates = ProjectileUpdates = 0;
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                timers[i] = synchronized ? 1f : (float)random.NextDouble() * Cooldown;
                casting[i] = false;
                burstNext[i] = i % 2 == 1;
            }
            for (int i = 0; i < BurstSize; i++)
            {
                float angle = i * Mathf.PI * 2 / BurstSize;
                burstDirections[i] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ProjectileSpeed;
            }
        }

        public void CancelCast(int i)
        {
            if (!Enabled) return;
            if (casting[i]) { casting[i] = false; Telegraphs--; }
            // Respawns must not bypass the ability cooldown.
            // Initial Reset has no cast; preserve its staggered phase.
        }

        public void Step(CombatSimulation sim, float dt)
        {
            if (!Enabled) return;
            long stageStart = StageProfile.Begin();
            using (AbilityMarker.Auto())
            {
                StateUpdates += sim.Count;
                for (int i = 0; i < sim.Count; i++)
                {
                    timers[i] -= dt;
                    if (timers[i] > 0) continue;
                    if (casting[i])
                    {
                        casting[i] = false; Telegraphs--; Activations++;
                        if ((targets[i] - sim.Player).sqrMagnitude <= AreaRadius * AreaRadius)
                        { PlayerHits++; sim.AbilityHit(); }
                        burstNext[i] = true; timers[i] += Cooldown;
                    }
                    else if (burstNext[i])
                    {
                        Activations++;
                        for (int j = 0; j < BurstSize; j++) Spawn(sim.Position(i), burstDirections[j]);
                        burstNext[i] = false; timers[i] += Cooldown;
                    }
                    else
                    {
                        casting[i] = true; Telegraphs++;
                        targets[i] = sim.Player; timers[i] += TelegraphSeconds;
                    }
                }
            }
            StageProfile.AbilityMs += StageProfile.End(stageStart);
            stageStart = StageProfile.Begin();
            using (ProjectileMarker.Auto())
            {
                ProjectileUpdates += Projectiles;
                if (sim.UseBurst)
                {
                    if (burstProjectiles == null) burstProjectiles = new BurstProjectiles();
                    Projectiles = burstProjectiles.Step(projectilePositions, projectilePrevious, projectileVelocities,
                        life, Projectiles, sim.Player, sim.PreviousPlayer, dt, sim.BatchSize, out int hits);
                    PlayerHits += hits;
                    if (hits > 0) sim.AbilityHit();
                }
                else
                {
                for (int i = Projectiles - 1; i >= 0; i--)
                {
                    Vector3 a = projectilePositions[i];
                    Vector3 b = a + projectileVelocities[i] * dt;
                    projectilePrevious[i] = a; projectilePositions[i] = b; life[i] -= dt;
                    // Swept collision against the player, including relative player motion.
                    bool hit = SegmentHitsPlayer(a - sim.PreviousPlayer, b - sim.Player);
                    if (hit) { PlayerHits++; sim.AbilityHit(); }
                    if (hit || life[i] <= 0) Remove(i);
                }
                }
            }
            StageProfile.ProjectileMs += StageProfile.End(stageStart);
        }

        public void Dispose() { burstProjectiles?.Dispose(); burstProjectiles = null; }

        public static bool SegmentHitsPlayer(Vector3 a, Vector3 b)
        {
            Vector3 delta = b - a;
            float t = delta.sqrMagnitude > .000001f ? Mathf.Clamp01(-Vector3.Dot(a, delta) / delta.sqrMagnitude) : 0;
            return (a + delta * t).sqrMagnitude <= .49f;
        }

        void Spawn(Vector3 position, Vector3 velocity)
        {
            if (Projectiles == ProjectileCapacity) { Dropped++; return; }
            int i = Projectiles++;
            projectilePositions[i] = projectilePrevious[i] = position;
            projectileVelocities[i] = velocity; life[i] = ProjectileLifetime; Spawned++;
        }
        void Remove(int i)
        {
            int last = --Projectiles;
            projectilePositions[i] = projectilePositions[last];
            projectilePrevious[i] = projectilePrevious[last];
            projectileVelocities[i] = projectileVelocities[last]; life[i] = life[last];
        }
    }
}
