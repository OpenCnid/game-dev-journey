using System;
using System.Collections.Generic;
using UnityEngine;

namespace Journey
{
    public enum SharedState { Waiting, Running, Complete, Defeat, Aborted }
    public enum ShotDecision { Damage, Miss, StaleRun, StaleLife, Duplicate, Invalid, StalePose, StaleEnemy, Terminal }

    [Serializable] public struct SharedPlayer
    {
        public Vector3 Position;
        public float Yaw, Pitch, ReturnAt, ImmuneUntil, ContactUntil;
        public int Health, Life, Shots, Hits, Damage, Kills;
        public int Weapon, BodyDamage;
        public float RifleInterval;
        public bool Ready;
    }
    [Serializable] public struct SharedEnemy
    {
        public Vector3 Position;
        public int Health, Incarnation;
    }
    public sealed class SharedSnapshot
    {
        public int Run, Tick;
        public float Elapsed;
        public SharedState State;
        public readonly SharedPlayer[] Players = new SharedPlayer[2];
        public readonly SharedEnemy[] Enemies = new SharedEnemy[SharedEncounter.EnemyCount];
        public void CopyFrom(SharedSnapshot other)
        {
            Run = other.Run; Tick = other.Tick; Elapsed = other.Elapsed; State = other.State;
            Array.Copy(other.Players, Players, 2); Array.Copy(other.Enemies, Enemies, Enemies.Length);
        }
        public AimResult Query(Ray ray)
        {
            AimResult result = AimResult.Miss;
            if (!EnemyGeometry.ValidRay(ray)) return result;
            for (int i = 0; i < Enemies.Length; i++)
            {
                SharedEnemy e = Enemies[i];
                bool wounded = e.Health < SharedEncounter.EnemyHealth;
                bool body = EnemyGeometry.RayCube(ray, EnemyGeometry.Body(e.Position, wounded), out float b);
                bool head = EnemyGeometry.RayCube(ray, EnemyGeometry.Head(e.Position, wounded), out float h);
                if (!body && !head) continue;
                bool critical = head && (!body || h <= b);
                float distance = critical ? h : b;
                if (distance <= CombatSimulation.RifleRange && (!result.Hit || distance < result.Distance))
                    result = new AimResult(i, e.Incarnation, critical ? HitRegion.Head : HitRegion.Body, distance);
            }
            return result;
        }
    }
    public struct SharedShot
    {
        public int Run, Player, Life, Weapon, Event, PoseTick;
        public Ray Ray;
    }
    // Immutable attribution survives slot reuse and future progression changes.
    public readonly struct SharedHit
    {
        public readonly SharedShot Source;
        public readonly ShotDecision Decision;
        public readonly ShotResult Shot;
        public readonly Vector3 DeathPosition;
        public SharedHit(SharedShot source, ShotDecision decision, ShotResult shot, Vector3 deathPosition)
        { Source = source; Decision = decision; Shot = shot; DeathPosition = deathPosition; }
    }

    // Host-only rules. Presentation receives snapshots; it cannot mutate this encounter.
    public sealed class SharedEncounter
    {
        public const int EnemyCount = 12, EnemyHealth = 60, HistoryTicks = 30, QueueCapacity = 32;
        public const float Duration = 60, ReturnDelay = 5, ReturnImmunity = 1;
        public readonly SharedSnapshot Current = new SharedSnapshot();
        readonly SharedSnapshot[] history = new SharedSnapshot[HistoryTicks + 1];
        readonly List<SharedShot> pending = new List<SharedShot>(QueueCapacity);
        readonly double[] nextShot = new double[2];
        readonly int[] lastEvent = new int[2], lastInput = new int[2];
        readonly float[] inputTime = new float[2];
        readonly Vector3[] velocities = new Vector3[EnemyCount];
        System.Random random;
        public event Action<SharedHit> Resolved;
        public int Rejected { get; private set; }
        public string AbortReason { get; private set; } = "";
        public SharedEncounter()
        {
            for (int i = 0; i < history.Length; i++) history[i] = new SharedSnapshot { Tick = -1 };
        }
        public void Start()
        {
            Current.Run++; Current.Tick = 0; Current.Elapsed = 0; Current.State = SharedState.Running;
            random = new System.Random(137); pending.Clear(); Rejected = 0; AbortReason = "";
            for (int p = 0; p < 2; p++)
            {
                Current.Players[p] = new SharedPlayer { Position = Spawn(p), Health = 100, Life = 1,
                    Weapon = p + 1, BodyDamage = 20, RifleInterval = .12f };
                nextShot[p] = 0; lastEvent[p] = lastInput[p] = 0; inputTime[p] = 0;
            }
            for (int i = 0; i < EnemyCount; i++) { Current.Enemies[i] = default; Respawn(i); }
            foreach (SharedSnapshot pose in history) { pose.Run = 0; pose.Tick = -1; }
            Remember();
        }
        public static Vector3 Spawn(int p) => new Vector3(p == 0 ? -2 : 2, 0, 0);
        public static Vector3 Move(Vector3 position, Vector3 movement, float dt)
        {
            position += Vector3.ClampMagnitude(movement, 1) * (8 * dt);
            return new Vector3(Mathf.Clamp(position.x, -CombatSimulation.ArenaLimit, CombatSimulation.ArenaLimit), 0,
                Mathf.Clamp(position.z, -CombatSimulation.ArenaLimit, CombatSimulation.ArenaLimit));
        }
        // Trusted cooperative movement ownership, bounded by time, speed and arena.
        // Life/run and increasing input time prevent old movement from undoing a return/reset.
        public bool Input(int run, int p, int life, int sequence, float time, Vector3 position, float yaw, float pitch)
        {
            if (run != Current.Run || p < 0 || p > 1 || Current.State != SharedState.Running) return false;
            SharedPlayer player = Current.Players[p];
            if (player.Health == 0 || life != player.Life || sequence <= lastInput[p] || !Finite(time) ||
                !Finite(yaw) || !Finite(pitch) || time < inputTime[p] || !EnemyGeometry.ValidRay(new Ray(position, Vector3.forward))) return false;
            float dt = time - inputTime[p];
            if (dt > .25f || position.y != 0 || Mathf.Abs(position.x) > CombatSimulation.ArenaLimit ||
                Mathf.Abs(position.z) > CombatSimulation.ArenaLimit || (position - player.Position).magnitude > 8 * dt + .005f) return false;
            player.Position = position; player.Yaw = yaw; player.Pitch = Mathf.Clamp(pitch, -80, 80);
            Current.Players[p] = player; lastInput[p] = sequence; inputTime[p] = time; return true;
        }
        public void Submit(SharedShot shot)
        {
            ShotDecision? rejection = null;
            if (shot.Run != Current.Run) rejection = ShotDecision.StaleRun;
            else if (Current.State != SharedState.Running) rejection = ShotDecision.Terminal;
            else if (shot.Player < 0 || shot.Player > 1) rejection = ShotDecision.Invalid;
            else if (shot.Life != Current.Players[shot.Player].Life || Current.Players[shot.Player].Health == 0) rejection = ShotDecision.StaleLife;
            else if (shot.Event <= lastEvent[shot.Player]) rejection = ShotDecision.Duplicate;
            else
            {
                lastEvent[shot.Player] = shot.Event;
                if (shot.Weapon != Current.Players[shot.Player].Weapon || !EnemyGeometry.ValidRay(shot.Ray) ||
                    Mathf.Abs(shot.Ray.direction.sqrMagnitude - 1) > .001f) rejection = ShotDecision.Invalid;
                else if (pending.Count == QueueCapacity) { Abort("Shot queue capacity exceeded"); rejection = ShotDecision.Terminal; }
            }
            if (rejection.HasValue) { Reject(shot, rejection.Value); return; }
            pending.Add(shot);
        }
        public void Step()
        {
            if (Current.State != SharedState.Running) return;
            Current.Tick++; Current.Elapsed = Current.Tick * CombatSimulation.Tick;
            pending.Sort((a, b) => a.Player != b.Player ? a.Player.CompareTo(b.Player) : a.Event.CompareTo(b.Event));
            for (int i = 0; i < pending.Count;)
            {
                SharedShot shot = pending[i];
                if (Current.Elapsed + .00001 < nextShot[shot.Player]) { i++; continue; }
                pending.RemoveAt(i); Resolve(shot);
            }
            // Read all positions before applying velocities, matching the local steering rule.
            for (int i = 0; i < EnemyCount; i++)
            {
                Vector3 position = Current.Enemies[i].Position, separation = Vector3.zero;
                int target = NearestLiving(position);
                for (int j = 0; j < EnemyCount; j++)
                {
                    Vector3 delta = position - Current.Enemies[j].Position; float distance = delta.magnitude;
                    if (j != i && distance > .01f && distance < 1.2f) separation += delta * ((1.2f - distance) / distance);
                }
                velocities[i] = target < 0 ? Vector3.zero : Vector3.ClampMagnitude(
                    (Current.Players[target].Position - position).normalized * 3.1f + separation * 5, 5);
            }
            for (int i = 0; i < EnemyCount; i++)
            {
                SharedEnemy e = Current.Enemies[i]; e.Position += velocities[i] * CombatSimulation.Tick;
                e.Position.x = Mathf.Clamp(e.Position.x, -29, 29); e.Position.z = Mathf.Clamp(e.Position.z, -29, 29); Current.Enemies[i] = e;
                for (int p = 0; p < 2; p++) if ((e.Position - Current.Players[p].Position).sqrMagnitude < 1.25f) Contact(p);
            }
            // Wipe wins over both completion and an expiring return.
            if (Current.Players[0].Health == 0 && Current.Players[1].Health == 0) Current.State = SharedState.Defeat;
            else if (Current.Tick >= 3600) Current.State = SharedState.Complete;
            else for (int p = 0; p < 2; p++)
            {
                SharedPlayer player = Current.Players[p];
                if (player.Health == 0 && Current.Elapsed + .00001f >= player.ReturnAt)
                {
                    player.Health = 100; player.Life++; player.Position = Spawn(p); player.Yaw = player.Pitch = 0;
                    player.ReturnAt = player.ContactUntil = 0; player.ImmuneUntil = Current.Elapsed + ReturnImmunity;
                    Current.Players[p] = player; nextShot[p] = Current.Elapsed;
                    lastInput[p] = 0; inputTime[p] = 0;
                }
            }
            Remember();
        }
        public int NearestLiving(Vector3 position)
        {
            int result = -1; float distance = float.PositiveInfinity;
            for (int p = 0; p < 2; p++) if (Current.Players[p].Health > 0)
            {
                float d = (position - Current.Players[p].Position).sqrMagnitude;
                if (d < distance) { result = p; distance = d; }
            }
            return result;
        }
        void Contact(int p)
        {
            SharedPlayer player = Current.Players[p];
            if (player.Health == 0 || Current.Elapsed < player.ContactUntil || Current.Elapsed < player.ImmuneUntil) return;
            player.Health = Mathf.Max(0, player.Health - 10); player.ContactUntil = Current.Elapsed + .4f;
            if (player.Health == 0) player.ReturnAt = Current.Elapsed + ReturnDelay;
            Current.Players[p] = player;
        }
        void Resolve(SharedShot source)
        {
            SharedPlayer player = Current.Players[source.Player];
            if (source.Life != player.Life || player.Health == 0) { Reject(source, ShotDecision.StaleLife); return; }
            SharedSnapshot pose = source.PoseTick < 0 ? null : history[source.PoseTick % history.Length];
            if (pose == null || pose.Run != source.Run || pose.Tick != source.PoseTick || Current.Tick - source.PoseTick > HistoryTicks)
            { Reject(source, ShotDecision.StalePose); return; }
            if ((source.Ray.origin - (pose.Players[source.Player].Position + Vector3.up * 1.65f)).magnitude > 2.1f)
            { Reject(source, ShotDecision.Invalid); return; }
            // Host derives region and incarnation from the displayed historical pose.
            AimResult aim = pose.Query(source.Ray);
            if (aim.Hit && Current.Enemies[aim.EnemyIndex].Incarnation != aim.SpawnVersion)
            { Reject(source, ShotDecision.StaleEnemy); return; }
            nextShot[source.Player] = Math.Max(nextShot[source.Player] + player.RifleInterval, Current.Elapsed + player.RifleInterval - CombatSimulation.Tick);
            player.Shots++;
            ShotResult shot = new ShotResult(source.Ray.GetPoint(aim.Distance), -1, 0, 0, 0);
            Vector3 deathPosition = default;
            if (aim.Hit)
            {
                SharedEnemy enemy = Current.Enemies[aim.EnemyIndex];
                int requested = player.BodyDamage * (aim.Region == HitRegion.Head ? 2 : 1);
                int actual = Math.Min(requested, enemy.Health), after = enemy.Health - actual;
                shot = new ShotResult(shot.End, aim.EnemyIndex, aim.SpawnVersion, aim.Region, requested, enemy.Health, actual, after);
                enemy.Health = after; Current.Enemies[aim.EnemyIndex] = enemy;
                player.Hits++; player.Damage += actual;
                if (after == 0) { player.Kills++; deathPosition = enemy.Position; Respawn(aim.EnemyIndex); }
            }
            Current.Players[source.Player] = player;
            Resolved?.Invoke(new SharedHit(source, aim.Hit ? ShotDecision.Damage : ShotDecision.Miss, shot, deathPosition));
        }
        void Reject(SharedShot source, ShotDecision reason)
        {
            Rejected++; Resolved?.Invoke(new SharedHit(source, reason,
                new ShotResult(source.Ray.GetPoint(CombatSimulation.RifleRange), -1, 0, 0, 0), default));
        }
        void Respawn(int i)
        {
            Vector3 p = default;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                float edge = (float)random.NextDouble() * 56 - 28; int side = random.Next(4);
                p = side < 2 ? new Vector3(side == 0 ? -28 : 28, 0, edge) : new Vector3(edge, 0, side == 2 ? -28 : 28);
                if ((p - Current.Players[0].Position).sqrMagnitude > 100 && (p - Current.Players[1].Position).sqrMagnitude > 100) break;
            }
            Current.Enemies[i] = new SharedEnemy { Position = p, Health = EnemyHealth, Incarnation = Current.Enemies[i].Incarnation + 1 };
        }
        void Remember() => history[Current.Tick % history.Length].CopyFrom(Current);
        public bool Retry(int run, int p)
        {
            if (run != Current.Run || p < 0 || p > 1 || (Current.State != SharedState.Defeat && Current.State != SharedState.Complete)) return false;
            SharedPlayer player = Current.Players[p]; player.Ready = true; Current.Players[p] = player;
            if (Current.Players[0].Ready && Current.Players[1].Ready) Start(); return true;
        }
        public void Abort(string reason) { Current.State = SharedState.Aborted; AbortReason = reason; pending.Clear(); }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
