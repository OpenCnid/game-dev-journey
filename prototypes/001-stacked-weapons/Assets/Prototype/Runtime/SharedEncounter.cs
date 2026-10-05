using System;
using System.Collections.Generic;
using UnityEngine;

namespace Journey
{
    public enum SharedState { Waiting, Running, Complete, Defeat, Aborted, Choosing }
    public enum ShotDecision { Damage, Miss, StaleRun, StaleLife, Duplicate, Invalid, StalePose, StaleEnemy, Terminal }

    [Serializable] public struct SharedPlayer
    {
        public Vector3 Position;
        public float Yaw, Pitch, ReturnAt, ImmuneUntil, ContactUntil;
        public int Health, Life, Shots, Hits, Damage, Kills;
        public int Weapon, BodyDamage;
        public int Arsenal;
        public UpgradeId FirstAddition, Specialization;
        public float RifleInterval;
        public bool Ready, ChoiceReady;
        public PersonalOffer Offer;
    }
    [Serializable] public struct SharedEnemy
    {
        public Vector3 Position;
        public int Health, Incarnation;
    }
    public sealed class SharedSnapshot
    {
        public int Run, Tick;
        public int Roster = 2, Xp, Level = 1, ChoicePhase, ControlRevision;
        public float Elapsed;
        public SharedState State;
        public readonly SharedPlayer[] Players = new SharedPlayer[2];
        public Guid Session;
        public ScenarioConfiguration Configuration { get; private set; }
        public SharedEnemy[] Enemies { get; private set; }
        public SharedSnapshot(ScenarioConfiguration configuration = null)
        { Configuration = configuration ?? ScenarioConfiguration.Named(ScenarioKind.SharedRifle); Enemies = new SharedEnemy[Configuration.EnemyCount]; }
        public void CopyFrom(SharedSnapshot other)
        {
            Configuration = other.Configuration; Session = other.Session;
            if (Enemies.Length != other.Enemies.Length) Enemies = new SharedEnemy[other.Enemies.Length];
            Roster = other.Roster; Xp = other.Xp; Level = other.Level;
            ChoicePhase = other.ChoicePhase; ControlRevision = other.ControlRevision;
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
                bool wounded = e.Health < Configuration.EnemyHealth;
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
        public readonly int RayIndex, Rays, TargetTests;
        public readonly bool Trigger;
        public SharedHit(SharedShot source, ShotDecision decision, ShotResult shot, Vector3 deathPosition, bool trigger = false, int rayIndex = 0, int rays = 0, int targetTests = 0)
        { Source = source; Decision = decision; Shot = shot; DeathPosition = deathPosition; Trigger = trigger; RayIndex = rayIndex; Rays = rays; TargetTests = targetTests; }
    }

    // Host-only rules. Presentation receives snapshots; it cannot mutate this encounter.
    public sealed class SharedEncounter
    {
        public const int HistoryTicks = 30, QueueCapacity = 32;
        public const float ReturnDelay = 5, ReturnImmunity = 1;
        public readonly SharedSnapshot Current;
        public ScenarioConfiguration Configuration { get; }
        public EarnedGrowth Growth { get; }
        public SharedWork Work { get; private set; } = new SharedWork();
        readonly List<GrowthChoice> choices = new List<GrowthChoice>(2);
        readonly SharedSnapshot[] history = new SharedSnapshot[HistoryTicks + 1];
        readonly List<SharedShot> pending = new List<SharedShot>(QueueCapacity);
        readonly double[] nextShot = new double[WeaponArsenal.MaxOwnedIds];
        readonly AimResult[] targets = new AimResult[ScenarioConfiguration.MaxEnemies];
        readonly int[] lastEvent = new int[2], lastInput = new int[2];
        readonly float[] inputTime = new float[2];
        readonly Vector3[] velocities;
        System.Random random;
        public event Action<SharedHit> Resolved;
        public int Rejected { get; private set; }
        public string AbortReason { get; private set; } = "";
        public SharedEncounter(ScenarioConfiguration configuration = null, Guid session = default, int roster = 2)
        {
            Configuration = configuration ?? ScenarioConfiguration.Named(ScenarioKind.SharedRifle);
            if (roster < 1 || roster > 2) throw new ArgumentException("Fixed roster must be one or two");
            Growth = Configuration.Pickups ? new EarnedGrowth() : null;
            Current = new SharedSnapshot(Configuration) { Roster = roster, Session = session == Guid.Empty ? Guid.NewGuid() : session };
            velocities = new Vector3[Configuration.EnemyCount];
            for (int i = 0; i < history.Length; i++) history[i] = new SharedSnapshot(Configuration) { Tick = -1 };
        }
        public void Start(bool preparing = false)
        {
            Work = new SharedWork();
            Current.Run++; Current.Tick = 0; Current.Elapsed = 0; Current.State = preparing ? SharedState.Waiting : SharedState.Running;
            Current.ChoicePhase = Current.ControlRevision = 0;
            random = new System.Random(Configuration.Seed); pending.Clear(); choices.Clear(); Growth?.Reset(Configuration.Seed); Current.Xp = 0; Current.Level = 1; Rejected = 0; AbortReason = "";
            Array.Clear(nextShot, 0, nextShot.Length);
            for (int p = 0; p < Current.Roster; p++)
            {
                Current.Players[p] = new SharedPlayer { Position = PlayerSpawn(p), Health = 100, Life = 1,
                    Weapon = p + 1, BodyDamage = Configuration.BodyDamage, RifleInterval = Configuration.RifleInterval,
                    Arsenal = Configuration.StartingArsenal, Specialization = Configuration.StartingSpecialization };
                nextShot[p] = 0; lastEvent[p] = lastInput[p] = 0; inputTime[p] = 0;
            }
            for (int i = 0; i < Configuration.EnemyCount; i++) { Current.Enemies[i] = default; Respawn(i); }
            foreach (SharedSnapshot pose in history) { pose.Run = 0; pose.Tick = -1; }
            Remember();
        }
        public bool Begin()
        {
            if (Current.Run < 1 || Current.State != SharedState.Waiting) return false;
            Current.State = SharedState.Running; Remember(); return true;
        }
        Vector3 PlayerSpawn(int p) => Current.Roster == 1 ? Vector3.zero : Spawn(p);
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
            if (run != Current.Run || p < 0 || p >= Current.Roster || Current.State != SharedState.Running) return false;
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
            else if (shot.Player < 0 || shot.Player >= Current.Roster) rejection = ShotDecision.Invalid;
            else if (shot.Life != Current.Players[shot.Player].Life || Current.Players[shot.Player].Health == 0) rejection = ShotDecision.StaleLife;
            else if (shot.Event <= lastEvent[shot.Player]) rejection = ShotDecision.Duplicate;
            else
            {
                lastEvent[shot.Player] = shot.Event;
                if (!WeaponArsenal.Owns(Current.Players[shot.Player], shot.Player, shot.Weapon) || !EnemyGeometry.ValidRay(shot.Ray) ||
                    Mathf.Abs(shot.Ray.direction.sqrMagnitude - 1) > .001f) rejection = ShotDecision.Invalid;
                else if (pending.Count == QueueCapacity) { Abort("Shot queue capacity exceeded"); rejection = ShotDecision.Terminal; }
            }
            if (rejection.HasValue) { Reject(shot, rejection.Value); return; }
            pending.Add(shot);
        }
        public void Step()
        {
            if (Current.State == SharedState.Choosing) { CommitChoices(); return; }
            if (Current.State != SharedState.Running) return;
            long started = StageProfile.Begin();
            Current.Tick++; Current.Elapsed = Current.Tick * CombatSimulation.Tick;
            ApplyChoices(false);
            StepCombat();
            double ms = StageProfile.End(started);
            Work.frameSimulationMs += ms; Work.simulationMs += ms; Work.frameTicks++; Work.ticks++;
        }
        public bool CommitChoices()
        {
            if (Current.State != SharedState.Choosing || choices.Count == 0) return false;
            ApplyChoices(true); return true;
        }
        void ApplyChoices(bool frozen)
        {
            choices.Sort((a,b) => a.Player.CompareTo(b.Player));
            foreach (GrowthChoice choice in choices)
            {
                SharedPlayer player = Current.Players[choice.Player];
                if (!ValidChoice(choice) || !frozen && player.Offer.Tick >= Current.Tick) continue;
                UpgradeDefinition definition = GrowthUpgrade.Definition(choice.Upgrade);
                if (Configuration.Kind == ScenarioKind.WeaponStack)
                {
                    WeaponArsenal.Apply(ref player, choice.Upgrade);
                    int added = WeaponArsenal.AddedMask(choice.Upgrade);
                    if (added != 0) nextShot[WeaponArsenal.Id(choice.Player, added == WeaponArsenal.Scattergun ? WeaponSlot.Scattergun : WeaponSlot.Piercer) - 1] = Current.Elapsed;
                }
                float interval = definition.Effect == UpgradeEffect.FireCadence ? GrowthUpgrade.Interval(choice.Upgrade) : player.RifleInterval;
                double now = (frozen ? Current.Tick : Current.Tick - 1) * (double)CombatSimulation.Tick;
                double progressRemaining = Math.Max(0, nextShot[choice.Player] - now) / player.RifleInterval;
                nextShot[choice.Player] = now + progressRemaining * interval;
                if (definition.Effect == UpgradeEffect.DirectDamage) player.BodyDamage = GrowthUpgrade.Damage(choice.Upgrade);
                player.RifleInterval = interval;
                player.Offer.Selected = choice.Upgrade; player.Offer.AppliedTick = Current.Tick;
                player.Offer.ControlRevision = ++Current.ControlRevision;
                Current.Players[choice.Player] = player; Growth.Applied(Current, choice.Player);
            }
            choices.Clear();
        }
        void StepCombat()
        {
            long stage = StageProfile.Begin();
            pending.Sort((a, b) => a.Player != b.Player ? a.Player.CompareTo(b.Player) : a.Event.CompareTo(b.Event));
            for (int i = 0; i < pending.Count;)
            {
                SharedShot shot = pending[i];
                if (Current.Tick * (double)CombatSimulation.Tick < nextShot[shot.Weapon - 1]) { i++; continue; }
                pending.RemoveAt(i); Resolve(shot);
            }
            double weaponMs = StageProfile.End(stage); Work.frameWeaponMs += weaponMs; Work.weaponMs += weaponMs;
            stage = StageProfile.Begin();
            Work.steeringChecks += (long)Configuration.EnemyCount * Configuration.EnemyCount;
            Work.contactTests += (long)Configuration.EnemyCount * Current.Roster;
            // Read all positions before applying velocities, matching the local steering rule.
            for (int i = 0; i < Configuration.EnemyCount; i++)
            {
                Vector3 position = Current.Enemies[i].Position, separation = Vector3.zero;
                int target = NearestLiving(position);
                for (int j = 0; j < Configuration.EnemyCount; j++)
                {
                    Vector3 delta = position - Current.Enemies[j].Position; float distance = delta.magnitude;
                    if (j != i && distance > .01f && distance < 1.2f) separation += delta * ((1.2f - distance) / distance);
                }
                velocities[i] = target < 0 ? Vector3.zero : Vector3.ClampMagnitude(
                    (Current.Players[target].Position - position).normalized * 3.1f + separation * 5, 5);
            }
            for (int i = 0; i < Configuration.EnemyCount; i++)
            {
                SharedEnemy e = Current.Enemies[i]; e.Position += velocities[i] * CombatSimulation.Tick;
                e.Position.x = Mathf.Clamp(e.Position.x, -29, 29); e.Position.z = Mathf.Clamp(e.Position.z, -29, 29); Current.Enemies[i] = e;
                for (int p = 0; p < Current.Roster; p++) if ((e.Position - Current.Players[p].Position).sqrMagnitude < 1.25f) Contact(p);
            }
            double crowdMs = StageProfile.End(stage); Work.frameCrowdMs += crowdMs; Work.crowdMs += crowdMs;
            // Wipe wins over both completion and an expiring return.
            if (NearestLiving(Vector3.zero) < 0) Current.State = SharedState.Defeat;
            else if (Current.Elapsed >= Configuration.Duration) Current.State = SharedState.Complete;
            else for (int p = 0; p < Current.Roster; p++)
            {
                SharedPlayer player = Current.Players[p];
                if (player.Health == 0 && Current.Elapsed + .00001f >= player.ReturnAt)
                {
                    player.Health = 100; player.Life++; player.Position = PlayerSpawn(p); player.Yaw = player.Pitch = 0;
                    player.ReturnAt = player.ContactUntil = 0; player.ImmuneUntil = Current.Elapsed + ReturnImmunity;
                    Current.Players[p] = player;
                    for (int instance = 0; instance < WeaponArsenal.MaxInstances; instance++) nextShot[WeaponArsenal.Id(p, instance) - 1] = Current.Elapsed;
                    lastInput[p] = 0; inputTime[p] = 0;
                }
            }
            if (Current.State == SharedState.Running && Growth != null)
            {
                stage = StageProfile.Begin();
                Growth.AttractAndMerge(Current); Growth.Collect(Current);
                Work.attractionQueries += Growth.AttractionQueries;
                double xpMs = StageProfile.End(stage); Work.frameXpMs += xpMs; Work.xpMs += xpMs;
                if (Current.Level - 1 > Current.ChoicePhase)
                { Current.State = SharedState.Choosing; Current.ChoicePhase = Current.Level - 1; Current.ControlRevision++; pending.Clear(); }
            }
            Remember();
        }
        public int NearestLiving(Vector3 position)
        {
            int result = -1; float distance = float.PositiveInfinity;
            for (int p = 0; p < Current.Roster; p++) if (Current.Players[p].Health > 0)
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
            Work.contactApplications++;
            if (!Configuration.Scaling) player.Health = Mathf.Max(0, player.Health - 10);
            player.ContactUntil = Current.Elapsed + .4f;
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
            if (!WeaponArsenal.Owns(pose.Players[source.Player], source.Player, source.Weapon))
            { Reject(source, ShotDecision.Invalid); return; }
            if (WeaponArsenal.Slot(source.Weapon) != WeaponSlot.Rifle) { ResolveSecondary(source, pose); return; }
            // Host derives region and incarnation from the displayed historical pose.
            AimResult aim = pose.Query(source.Ray);
            if (aim.Hit && Current.Enemies[aim.EnemyIndex].Incarnation != aim.SpawnVersion)
            { Reject(source, ShotDecision.StaleEnemy, queried: true); return; }
            nextShot[source.Weapon - 1] = Math.Max(nextShot[source.Weapon - 1], (Current.Tick - 1) * (double)CombatSimulation.Tick) + player.RifleInterval;
            player.Shots++;
            ShotResult shot = new ShotResult(source.Ray.GetPoint(aim.Distance), -1, 0, 0, 0);
            Vector3 deathPosition = default;
            if (aim.Hit)
            {
                SharedEnemy enemy = Current.Enemies[aim.EnemyIndex];
                int requested = player.BodyDamage * (aim.Region == HitRegion.Head ? Configuration.HeadMultiplier : 1);
                int actual = Math.Min(requested, enemy.Health), after = enemy.Health - actual;
                shot = new ShotResult(shot.End, aim.EnemyIndex, aim.SpawnVersion, aim.Region, requested, enemy.Health, actual, after);
                enemy.Health = after; Current.Enemies[aim.EnemyIndex] = enemy;
                player.Hits++; player.Damage += actual;
                if (after == 0)
                {
                    player.Kills++; deathPosition = enemy.Position;
                    Growth?.Death(Current, new SharedHit(source, ShotDecision.Damage, shot, deathPosition));
                    Respawn(aim.EnemyIndex);
                }
            }
            Current.Players[source.Player] = player;
            Resolved?.Invoke(new SharedHit(source, aim.Hit ? ShotDecision.Damage : ShotDecision.Miss, shot, deathPosition, true, 0, 1, pose.Enemies.Length));
        }
        void ResolveSecondary(SharedShot source, SharedSnapshot pose)
        {
            SharedPlayer player = Current.Players[source.Player], firedBuild = pose.Players[source.Player];
            WeaponSlot slot = WeaponArsenal.Slot(source.Weapon);
            int rays = WeaponArsenal.Rays(firedBuild, slot); float range = WeaponArsenal.Range(slot);
            nextShot[source.Weapon - 1] = Math.Max(nextShot[source.Weapon - 1], (Current.Tick - 1) * (double)CombatSimulation.Tick) + WeaponArsenal.Interval(firedBuild, slot);
            player.Shots++;
            bool first = true;
            for (int r = 0; r < rays; r++)
            {
                Ray ray = slot == WeaponSlot.Scattergun ? WeaponArsenal.Fan(source.Ray, r, rays) : source.Ray;
                int count = WeaponArsenal.Query(pose, ray, range, targets);
                int limit = Math.Min(count, slot == WeaponSlot.Piercer ? WeaponArsenal.PierceLimit(firedBuild) : 1);
                if (limit == 0)
                {
                    Resolved?.Invoke(new SharedHit(source, ShotDecision.Miss, new ShotResult(ray.GetPoint(range), -1, 0, 0, 0), default,
                        first, r, first ? rays : 0, first ? rays * pose.Enemies.Length : 0));
                    first = false;
                }
                for (int t = 0; t < limit; t++)
                {
                    AimResult aim = targets[t]; SharedEnemy enemy = Current.Enemies[aim.EnemyIndex];
                    if (enemy.Incarnation != aim.SpawnVersion)
                    {
                        // Every pellet/penetration keeps the pre-trigger incarnation. Never shoot
                        // a fresh respawn from a previous ray in this same trigger.
                        Rejected++;
                        Resolved?.Invoke(new SharedHit(source, ShotDecision.StaleEnemy,
                            new ShotResult(ray.GetPoint(aim.Distance), -1, 0, 0, 0), default, first, r, first ? rays : 0, first ? rays * pose.Enemies.Length : 0));
                        first = false; continue;
                    }
                    int requested = WeaponArsenal.Damage(firedBuild, slot) * (aim.Region == HitRegion.Head ? Configuration.HeadMultiplier : 1);
                    int actual = Math.Min(requested, enemy.Health), after = enemy.Health - actual;
                    var result = new ShotResult(ray.GetPoint(aim.Distance), aim.EnemyIndex, aim.SpawnVersion, aim.Region, requested, enemy.Health, actual, after);
                    enemy.Health = after; Current.Enemies[aim.EnemyIndex] = enemy;
                    player.Hits++; player.Damage += actual; Vector3 death = default;
                    if (after == 0)
                    { player.Kills++; death = enemy.Position; Growth?.Death(Current, new SharedHit(source, ShotDecision.Damage, result, death)); Respawn(aim.EnemyIndex); }
                    Resolved?.Invoke(new SharedHit(source, ShotDecision.Damage, result, death, first, r, first ? rays : 0, first ? rays * pose.Enemies.Length : 0));
                    first = false;
                }
            }
            Current.Players[source.Player] = player;
        }
        void Reject(SharedShot source, ShotDecision reason, bool queried = false)
        {
            Rejected++; Resolved?.Invoke(new SharedHit(source, reason,
                new ShotResult(source.Ray.GetPoint(CombatSimulation.RifleRange), -1, 0, 0, 0), default, false, 0, queried ? 1 : 0, queried ? Configuration.EnemyCount : 0));
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
            Current.Enemies[i] = new SharedEnemy { Position = p, Health = Configuration.EnemyHealth, Incarnation = Current.Enemies[i].Incarnation + 1 };
        }
        void Remember() => history[Current.Tick % history.Length].CopyFrom(Current);
        public bool Retry(int run, int p)
        {
            if (run != Current.Run || p < 0 || p >= Current.Roster || (Current.State != SharedState.Defeat && Current.State != SharedState.Complete)) return false;
            SharedPlayer player = Current.Players[p]; player.Ready = true; Current.Players[p] = player;
            if (Current.Players[0].Ready && (Current.Roster == 1 || Current.Players[1].Ready)) Start(); return true;
        }
        bool ValidChoice(GrowthChoice c) => Growth != null && c.Run == Current.Run && (Current.State == SharedState.Running || Current.State == SharedState.Choosing) &&
            c.Player >= 0 && c.Player < Current.Roster && GrowthUpgrade.Valid(c.Upgrade) &&
            GrowthUpgrade.Definition(c.Upgrade).Target == UpgradeTarget.OwnedWeapon && Current.Players[c.Player].Offer.Pending &&
            Current.Players[c.Player].Offer.Contains(c.Upgrade) &&
            c.Offer == Current.Players[c.Player].Offer.Id && c.Weapon == Current.Players[c.Player].Weapon && c.Weapon == Current.Players[c.Player].Offer.Weapon;
        public bool Select(GrowthChoice choice)
        {
            if (!ValidChoice(choice) || choices.Exists(c => c.Player == choice.Player)) return false;
            choices.Add(choice); return true;
        }
        public bool ContinueChoice(int run, int phase, int p)
        {
            if (run != Current.Run || phase != Current.ChoicePhase || p < 0 || p >= Current.Roster ||
                Current.State != SharedState.Choosing || Current.Players[p].Offer.Selected == UpgradeId.None || Current.Players[p].ChoiceReady) return false;
            Current.Players[p].ChoiceReady = true; Current.ControlRevision++;
            for (int i = 0; i < Current.Roster; i++) if (!Current.Players[i].ChoiceReady) return true;
            Current.State = SharedState.Running; pending.Clear(); Remember(); return true;
        }
        public void Abort(string reason) { Current.State = SharedState.Aborted; AbortReason = reason; pending.Clear(); choices.Clear(); }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
