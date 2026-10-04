using System;
using UnityEngine;
using Unity.Profiling;
using Unity.Collections;

namespace Journey
{
    // Snapshot before respawn: presentation must not infer a hit from tracer length.
    public readonly struct ShotResult
    {
        public readonly Vector3 End;
        public readonly int EnemyIndex, SpawnVersion, Damage, HealthAfter;
        public bool Hit => Damage > 0;
        public bool Killed => Hit && HealthAfter == 0;
        public ShotResult(Vector3 end, int enemyIndex, int spawnVersion, int damage, int healthAfter)
        { End = end; EnemyIndex = enemyIndex; SpawnVersion = spawnVersion; Damage = damage; HealthAfter = healthAfter; }
    }

    public enum RunState { Ready, Running, Paused, Won, Lost }

    // One owner, preallocated state, no per-enemy MonoBehaviour/rigidbody/Update.
    // Open arena only: local separation is not a substitute for obstacle pathfinding.
    public sealed class CombatSimulation : IDisposable
    {
        public const int Capacity = 20000;
        // Approximate soft separation: four rotating candidates per adjacent cell.
        // This bounds dense-cell work; it does NOT guarantee collision-free packing.
        public const int NeighborsPerCell = 4;
        public const float ArenaLimit = 29f;
        public const float Tick = 1f / 60f;
        public const float EnemyRadius = .62f;
        public const int EnemyMaxHealth = 40;
        const float CellSize = 2f;
        const int GridWidth = 32;
        readonly Vector3[] positions = new Vector3[Capacity];
        readonly Vector3[] previous = new Vector3[Capacity];
        readonly Vector3[] velocities = new Vector3[Capacity];
        readonly int[] health = new int[Capacity];
        readonly int[] spawnVersions = new int[Capacity];
        readonly int[] next = new int[Capacity];
        readonly int[] cells = new int[GridWidth * GridWidth];
        System.Random random;
        float fireRemaining, damageRemaining;
        RunState beforePause;
        int tick;
        static readonly ProfilerMarker CrowdMarker = new ProfilerMarker("Journey.Crowd");
        BurstCrowd burstCrowd;
        public bool UseBurst { get; set; }
        public bool UseNativeState { get; set; }
        public bool NativeState { get; private set; }
        internal NativeArray<Vector3> NativePositions, NativePrevious;
        public int BatchSize { get; set; } = 128;
        internal Vector3[] Positions => positions;
        internal Vector3[] PreviousPositions => previous;
        public readonly BossAbilities Abilities = new BossAbilities();
        public int NeighborChecks { get; private set; }

        public RunState State { get; private set; } = RunState.Ready;
        public Vector3 Player { get; private set; }
        public Vector3 PreviousPlayer { get; private set; }
        public int Count { get; private set; }
        public int Kills { get; private set; }
        public int PlayerHealth { get; private set; }
        public int Shots { get; private set; }
        public float Elapsed { get; private set; }
        public long StateVersion { get; private set; }
        public bool Benchmark { get; private set; }
        public float Duration { get; private set; }
        public Vector3 Position(int index) => NativeState ? NativePositions[index] : positions[index];
        public int EnemyHealth(int index) => health[index];
        public int EnemySpawnVersion(int index) => spawnVersions[index];
        public Vector3 RenderPosition(int index, float alpha) => NativeState ?
            Vector3.Lerp(NativePrevious[index], NativePositions[index], alpha) : Vector3.Lerp(previous[index], positions[index], alpha);
        public Vector3 RenderPlayer(float alpha) => Vector3.Lerp(PreviousPlayer, Player, alpha);

        public void Reset(int count, int seed, bool benchmark, float duration = 60f,
            bool bosses = false, bool synchronizedAbilities = false)
        {
            if (UseNativeState && !UseBurst) throw new InvalidOperationException("Native state requires Burst simulation.");
            NativeState = UseNativeState;
            if (NativeState && !NativePositions.IsCreated)
            {
                NativePositions = new NativeArray<Vector3>(Capacity, Allocator.Persistent);
                NativePrevious = new NativeArray<Vector3>(Capacity, Allocator.Persistent);
            }
            Count = Mathf.Clamp(count, 0, Capacity);
            random = new System.Random(seed);
            Benchmark = benchmark;
            Duration = duration;
            Elapsed = 0; StateVersion++;
            Kills = Shots = 0;
            PlayerHealth = 100;
            Player = PreviousPlayer = benchmark ? new Vector3(18, 0, 0) : Vector3.zero;
            fireRemaining = damageRemaining = 0;
            tick = NeighborChecks = 0;
            Abilities.Reset(Count, seed, bosses, synchronizedAbilities, NativeState);
            for (int i = 0; i < Count; i++) Respawn(i);
            State = RunState.Running;
        }

        public void TogglePause()
        {
            if (State == RunState.Paused) State = beforePause;
            else if (State == RunState.Running) { beforePause = State; State = RunState.Paused; }
        }

        public void Step(Vector3 movement, float dt)
        {
            if (State != RunState.Running || dt <= 0) return;
            StateVersion++;
            PreviousPlayer = Player;
            Elapsed += dt;
            fireRemaining -= dt;
            damageRemaining = Mathf.Max(0, damageRemaining - dt);
            if (Benchmark)
                Player = new Vector3(Mathf.Cos(Elapsed * .23f) * 18, 0, Mathf.Sin(Elapsed * .23f) * 18);
            else
            {
                Player += Vector3.ClampMagnitude(movement, 1) * (8f * dt);
                Player = new Vector3(Mathf.Clamp(Player.x, -ArenaLimit, ArenaLimit), 0,
                    Mathf.Clamp(Player.z, -ArenaLimit, ArenaLimit));
            }

            StageProfile.Ticks++;
            long crowdStart = StageProfile.Begin();
            using (CrowdMarker.Auto())
            {
                int start = Count == 0 ? 0 : (int)((long)tick++ * 977 % Count);
                if (UseBurst || NativeState)
                {
                    if (burstCrowd == null) burstCrowd = new BurstCrowd();
                    bool contact;
                    if (NativeState)
                        NeighborChecks = burstCrowd.StepNative(NativePositions, NativePrevious, Count, start, Player, dt, BatchSize, out contact);
                    else
                    {
                        Array.Copy(positions, previous, Count);
                        NeighborChecks = burstCrowd.Step(positions, Count, start, Player, dt, BatchSize, out contact);
                    }
                    if (contact && !Benchmark && damageRemaining <= 0)
                    { PlayerHealth = Mathf.Max(0, PlayerHealth - 10); damageRemaining = .4f; }
                }
                else
                {
                Array.Fill(cells, -1);
                NeighborChecks = 0;
                // Rotate insertion order so crowded cells do not always select the same IDs.
                for (int n = 0; n < Count; n++)
                {
                    int i = (n + start) % Count;
                    previous[i] = positions[i];
                    int cell = Cell(positions[i]);
                    next[i] = cells[cell]; cells[cell] = i;
                }
                // Read all positions before applying velocities: no order-dependent neighbor motion.
                for (int i = 0; i < Count; i++)
                {
                    Vector3 toward = Player - positions[i]; toward.y = 0;
                    Vector3 separation = Vector3.zero;
                    int cx = CellX(positions[i].x), cz = CellX(positions[i].z);
                    for (int z = Mathf.Max(0, cz - 1); z <= Mathf.Min(GridWidth - 1, cz + 1); z++)
                    for (int x = Mathf.Max(0, cx - 1); x <= Mathf.Min(GridWidth - 1, cx + 1); x++)
                    {
                    int visited = 0;
                    for (int j = cells[z * GridWidth + x]; j != -1 && visited < NeighborsPerCell; j = next[j], visited++)
                    {
                        NeighborChecks++;
                        if (j == i) continue;
                        Vector3 delta = positions[i] - positions[j];
                        float distance = delta.sqrMagnitude;
                        if (distance > .0001f && distance < 1.44f)
                            separation += delta * ((1.2f - Mathf.Sqrt(distance)) / Mathf.Sqrt(distance));
                    }
                    }
                    velocities[i] = Vector3.ClampMagnitude(toward.normalized * 3.1f + separation * 5, 5);
                }
                for (int i = 0; i < Count; i++)
                {
                    positions[i] += velocities[i] * dt;
                    positions[i].x = Mathf.Clamp(positions[i].x, -ArenaLimit, ArenaLimit);
                    positions[i].z = Mathf.Clamp(positions[i].z, -ArenaLimit, ArenaLimit);
                    Vector3 delta = positions[i] - Player; delta.y = 0;
                    if (!Benchmark && delta.sqrMagnitude < 1.25f && damageRemaining <= 0)
                    { PlayerHealth = Mathf.Max(0, PlayerHealth - 10); damageRemaining = .4f; }
                }
                }
            }
            StageProfile.CrowdMs += StageProfile.End(crowdStart);
            Abilities.Step(this, dt);
            if (PlayerHealth == 0) State = RunState.Lost;
            else if (!Benchmark && Elapsed >= Duration) State = RunState.Won;
        }

        public void Dispose()
        {
            burstCrowd?.Dispose(); burstCrowd = null; Abilities.Dispose();
            if (NativePositions.IsCreated) NativePositions.Dispose();
            if (NativePrevious.IsCreated) NativePrevious.Dispose();
        }

        internal void AbilityHit()
        {
            if (!Benchmark && damageRemaining <= 0)
            { PlayerHealth = Mathf.Max(0, PlayerHealth - 10); damageRemaining = .4f; }
        }

        public bool TryFire(Ray ray, out Vector3 end, out bool killed)
        {
            bool fired = TryFire(ray, out ShotResult shot);
            end = shot.End; killed = shot.Killed;
            return fired;
        }

        public bool TryFire(Ray ray, out ShotResult shot)
        {
            shot = new ShotResult(ray.origin + ray.direction * 65, -1, 0, 0, 0);
            if (State != RunState.Running || fireRemaining > 0) return false;
            // Simulation time drives cadence; render FPS does not change damage output.
            fireRemaining += .12f;
            Shots++;
            float nearest = 65;
            int target = -1;
            for (int i = 0; i < Count; i++)
            {
                if (RaySphere(ray, Position(i) + Vector3.up * 1.1f, EnemyRadius, out float hit) && hit < nearest)
                { nearest = hit; target = i; }
            }
            Vector3 end = ray.origin + ray.direction * nearest;
            if (target >= 0)
            {
                int damage = Mathf.Min(20, health[target]);
                health[target] -= damage;
                shot = new ShotResult(end, target, spawnVersions[target], damage, health[target]);
                if (health[target] <= 0)
                { Kills++; Respawn(target); }
            }
            return true;
        }

        public static bool RaySphere(Ray ray, Vector3 center, float radius, out float distance)
        {
            Vector3 relative = ray.origin - center;
            float a = Vector3.Dot(ray.direction, ray.direction);
            float b = Vector3.Dot(relative, ray.direction);
            float c = Vector3.Dot(relative, relative) - radius * radius;
            float discriminant = b * b - a * c;
            distance = 0;
            if (a < .00001f || discriminant < 0) return false;
            float root = Mathf.Sqrt(discriminant);
            float near = (-b - root) / a, far = (-b + root) / a;
            if (far < 0) return false;
            distance = Mathf.Max(0, near);
            return true;
        }

        void Respawn(int index)
        {
            // Spawn at the perimeter, preserving a safe radius from the player.
            Vector3 p = Vector3.zero;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                float offset = (float)random.NextDouble() * 56 - 28;
                int side = random.Next(4);
                p = side == 0 ? new Vector3(-28, 0, offset) : side == 1 ? new Vector3(28, 0, offset) :
                    side == 2 ? new Vector3(offset, 0, -28) : new Vector3(offset, 0, 28);
                if ((p - Player).sqrMagnitude > 100) break;
            }
            if (NativeState) NativePositions[index] = NativePrevious[index] = p;
            else positions[index] = previous[index] = p;
            health[index] = EnemyMaxHealth;
            spawnVersions[index]++;
            Abilities.CancelCast(index); StateVersion++;
        }
        static int CellX(float value) => Mathf.Clamp((int)((value + 32) / CellSize), 0, GridWidth - 1);
        static int Cell(Vector3 p) => CellX(p.z) * GridWidth + CellX(p.x);
    }
}
