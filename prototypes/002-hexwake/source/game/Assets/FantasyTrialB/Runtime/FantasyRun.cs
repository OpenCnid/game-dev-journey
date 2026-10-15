using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevHarness.FantasyTrialB
{
    public enum WeaponKind { Pistol, Ember, Whirlwind, Spark }
    public enum FantasyPhase { Ready, Playing, Paused, Upgrade, Victory, Defeat }

    public sealed class TrialEnemy
    {
        public int Id;
        public Vector3 Position;
        public float Health, MaxHealth, Speed;
        public double SpawnTime;
        public bool Alive => Health > 0;
    }

    public sealed class TrialWeapon
    {
        public WeaponKind Kind;
        public int CopyId;
        public float Damage, BaseDamage, Reach, Cadence;
        public int DamageRanks;
        public double ReadyAt;
    }

    public sealed class TrialVortex
    {
        public TrialWeapon Owner;
        public Vector3 Position, Direction;
        public double CreatedAt, ExpiresAt, NextTick;
    }

    public sealed class TrialAttack
    {
        public WeaponKind Kind;
        public int CopyId;
        public Vector3 Origin;
        public List<Vector3> Points = new List<Vector3>();
    }

    public sealed class UpgradeChoice
    {
        public bool IsGrant;
        public WeaponKind Kind;
        public int CopyId;
        public string Label;
    }

    // Numerical calibration belongs to frozen fantasy-trial-B.json, not full-game balance.
    public sealed class FantasyRun
    {
        public readonly List<TrialWeapon> Weapons = new List<TrialWeapon>();
        public readonly List<TrialEnemy> Enemies = new List<TrialEnemy>();
        public readonly List<TrialVortex> Vortices = new List<TrialVortex>();
        public readonly List<TrialAttack> Attacks = new List<TrialAttack>();
        public readonly List<TrialEnemy> Deaths = new List<TrialEnemy>();
        public readonly List<UpgradeChoice> Choices = new List<UpgradeChoice>();
        public int Health { get; private set; }
        public int Xp { get; private set; }
        public int Threshold { get; private set; }
        public int Level { get; private set; }
        public int Kills { get; private set; }
        public int TotalAttacks { get; private set; }
        public int TotalHits { get; private set; }
        public double Elapsed;
        public FantasyPhase Phase;
        public bool Automatic = true;
        public Func<Vector3, Vector3, bool> ClearPath = (a, b) => true;
        public Func<Vector3, Vector3, bool> VortexTravelClear = (a, b) => true;
        private System.Random random;
        private int nextEnemyId, nextCopyId;
        private double contactReadyAt;
        private const double Epsilon = 0.0000001;

        public FantasyRun() { Reset(); }

        public void Reset(int seed = 1)
        {
            Weapons.Clear(); Enemies.Clear(); Vortices.Clear();
            Attacks.Clear(); Deaths.Clear(); Choices.Clear();
            random = new System.Random(seed);
            nextEnemyId = nextCopyId = 0;
            Health = 100; Xp = 0; Threshold = 12; Level = 1;
            Kills = TotalHits = TotalAttacks = 0;
            Elapsed = contactReadyAt = 0;
            Phase = FantasyPhase.Ready;
            // Automatic and geometry delegates are settings, not run-earned power.
            Grant(WeaponKind.Pistol);
        }

        public void Start() { if (Phase == FantasyPhase.Ready) Phase = FantasyPhase.Playing; }
        public void Pause() { if (Phase == FantasyPhase.Playing) Phase = FantasyPhase.Paused; }
        public void Resume() { if (Phase == FantasyPhase.Paused) Phase = FantasyPhase.Playing; }

        public static float EnemyHealthAt(double survivalSeconds)
        {
            double p = Math.Max(0, Math.Min(1, survivalSeconds / 600));
            return (float)(42 * (1 + 2 * p));
        }

        public static float EnemySpeedAt(double survivalSeconds)
        {
            double p = Math.Max(0, Math.Min(1, survivalSeconds / 600));
            return (float)(2 + 0.65 * p);
        }

        public static double SpawnIntervalAt(double survivalSeconds)
        {
            double p = Math.Max(0, Math.Min(1, survivalSeconds / 600));
            return 2.4 + (0.72 - 2.4) * p;
        }

        public TrialEnemy AddEnemy(Vector3 position, float health = -1)
        {
            float maximum = health < 0 ? EnemyHealthAt(Elapsed) : health;
            var enemy = new TrialEnemy { Id = ++nextEnemyId, Position = position,
                Health = maximum, MaxHealth = maximum, Speed = EnemySpeedAt(Elapsed), SpawnTime = Elapsed };
            Enemies.Add(enemy);
            return enemy;
        }

        public bool Grant(WeaponKind kind)
        {
            if (Weapons.Count >= 6 || !Enum.IsDefined(typeof(WeaponKind), kind)) return false;
            var weapon = new TrialWeapon { Kind = kind, CopyId = ++nextCopyId, ReadyAt = Elapsed };
            switch (kind)
            {
                case WeaponKind.Pistol: weapon.Damage = 18; weapon.Reach = 24; weapon.Cadence = 0.42f; break;
                case WeaponKind.Ember: weapon.Damage = 10; weapon.Reach = 8; weapon.Cadence = 1f; break;
                case WeaponKind.Whirlwind: weapon.Damage = 3; weapon.Reach = 18; weapon.Cadence = 1.8f; break;
                case WeaponKind.Spark: weapon.Damage = 10; weapon.Reach = 15; weapon.Cadence = 1.1f; break;
            }
            weapon.BaseDamage = weapon.Damage;
            Weapons.Add(weapon);
            return true;
        }

        public bool Contact()
        {
            if (Phase != FantasyPhase.Playing || Elapsed + Epsilon < contactReadyAt) return false;
            contactReadyAt = Elapsed + 1;
            Health = Math.Max(0, Health - 10);
            if (Health == 0) Phase = FantasyPhase.Defeat;
            return true;
        }

        public bool Heal(int amount)
        {
            if (Phase != FantasyPhase.Playing || Health == 100 || amount <= 0) return false;
            Health = Math.Min(100, Health + amount);
            return true;
        }

        public void DamageEnemy(TrialEnemy enemy, float damage)
        {
            if (Phase != FantasyPhase.Playing || enemy == null || !enemy.Alive || damage <= 0) return;
            enemy.Health = Mathf.Max(0, enemy.Health - damage);
            TotalHits++;
            if (!enemy.Alive) { Kills++; Deaths.Add(enemy); }
        }

        public bool RequestAttack(int copyIndex, Vector3 origin, Vector3 forward,
            int aimedEnemyId, Vector3? groundPoint, bool manual,
            Vector3? aimOrigin = null, Vector3? aimForward = null)
        {
            if (Phase != FantasyPhase.Playing || copyIndex < 0 || copyIndex >= Weapons.Count) return false;
            var weapon = Weapons[copyIndex];
            if (Elapsed + Epsilon < weapon.ReadyAt) return false;
            var aim = forward.normalized;
            var cameraOrigin = aimOrigin ?? origin;
            var cameraAim = (aimForward ?? forward).normalized;
            var aimed = Enemies.Find(e => e.Id == aimedEnemyId && e.Alive);
            bool aimedEligible = aimed != null && Vector3.Distance(origin, aimed.Position) <= weapon.Reach
                && ClearPath(origin, aimed.Position);
            var attack = new TrialAttack { Kind = weapon.Kind, CopyId = weapon.CopyId, Origin = origin };
            switch (weapon.Kind)
            {
                case WeaponKind.Pistol:
                    if (!manual && !aimedEligible) return false;
                    attack.Points.Add(aimedEligible ? aimed.Position : origin + aim * weapon.Reach);
                    if (aimedEligible) DamageEnemy(aimed, weapon.Damage);
                    break;
                case WeaponKind.Ember:
                    if (!manual && !aimedEligible) return false;
                    attack.Points.Add(origin + aim * weapon.Reach);
                    foreach (var enemy in Enemies)
                        if (enemy.Alive && aim.sqrMagnitude > 0 && Vector3.Distance(origin, enemy.Position) <= weapon.Reach
                            && Vector3.Angle(aim, enemy.Position - origin) <= 20 && ClearPath(origin, enemy.Position))
                            DamageEnemy(enemy, weapon.Damage);
                    break;
                case WeaponKind.Whirlwind:
                    if (!groundPoint.HasValue || Vector3.Distance(origin, groundPoint.Value) > weapon.Reach) return false;
                    Vector3 direction = groundPoint.Value - origin;
                    direction.y = 0;
                    if (direction.sqrMagnitude < 0.000001f) return false;
                    if (!manual && !Enemies.Exists(e => e.Alive && Vector3.Distance(e.Position, groundPoint.Value) <= 1.7f
                        && ClearPath(groundPoint.Value, e.Position))) return false;
                    // Replace only after validating the cast, and only the oldest of this copy.
                    TrialVortex oldest = null;
                    int count = 0;
                    foreach (var vortex in Vortices)
                        if (vortex.Owner == weapon)
                        {
                            count++;
                            if (oldest == null || vortex.CreatedAt < oldest.CreatedAt) oldest = vortex;
                        }
                    if (count >= 3) Vortices.Remove(oldest);
                    Vortices.Add(new TrialVortex { Owner = weapon, Position = groundPoint.Value,
                        Direction = direction.normalized, CreatedAt = Elapsed, ExpiresAt = Elapsed + 2.8,
                        NextTick = Elapsed + 0.5 });
                    attack.Points.Add(groundPoint.Value);
                    break;
                case WeaponKind.Spark:
                    var victims = new HashSet<int>();
                    TrialEnemy target = FindSparkTarget(origin, cameraOrigin, cameraAim, weapon.Reach, 22, victims);
                    if (target == null && !manual) return false;
                    while (target != null && victims.Count < 3)
                    {
                        victims.Add(target.Id);
                        attack.Points.Add(target.Position);
                        DamageEnemy(target, weapon.Damage);
                        target = FindSparkTarget(target.Position, cameraOrigin, cameraAim, 5f, 180, victims);
                    }
                    break;
            }
            weapon.ReadyAt = Elapsed + weapon.Cadence;
            TotalAttacks++;
            Attacks.Add(attack);
            return true;
        }

        private TrialEnemy FindSparkTarget(Vector3 from, Vector3 caster, Vector3 aim,
            float reach, float allowance, HashSet<int> excluded)
        {
            if (aim.sqrMagnitude == 0) return null;
            TrialEnemy best = null;
            float bestAngle = float.MaxValue;
            foreach (var enemy in Enemies)
            {
                if (!enemy.Alive || excluded.Contains(enemy.Id) || Vector3.Distance(from, enemy.Position) > reach
                    || !ClearPath(from, enemy.Position)) continue;
                // Rank every hop against the same captured caster aim, not a new hop direction.
                float angle = Vector3.Angle(aim, enemy.Position - caster);
                if (angle > allowance) continue;
                if (best == null || angle < bestAngle || (angle == bestAngle && enemy.Id < best.Id))
                { best = enemy; bestAngle = angle; }
            }
            return best;
        }

        public void Advance(float delta)
        {
            if (Phase != FantasyPhase.Playing || delta <= 0) return;
            double end = Math.Min(600, Elapsed + delta);
            while (Elapsed + Epsilon < end)
            {
                double next = end;
                foreach (var vortex in Vortices)
                    next = Math.Min(next, Math.Min(vortex.NextTick, vortex.ExpiresAt));
                float step = (float)(next - Elapsed);
                for (int i = Vortices.Count - 1; i >= 0; i--)
                {
                    var vortex = Vortices[i];
                    var destination = vortex.Position + vortex.Direction * (2.4f * step);
                    if (!VortexTravelClear(vortex.Position, destination)) Vortices.RemoveAt(i);
                    else vortex.Position = destination;
                }
                foreach (var enemy in Enemies)
                {
                    if (!enemy.Alive) continue;
                    TrialVortex strongest = null;
                    float nearest = float.MaxValue;
                    foreach (var vortex in Vortices)
                    {
                        float distance = Vector3.Distance(vortex.Position, enemy.Position);
                        if (distance > 1.7f || !ClearPath(vortex.Position, enemy.Position)) continue;
                        int tie = strongest == null ? -1 : vortex.Owner.CopyId.CompareTo(strongest.Owner.CopyId);
                        if (tie == 0) tie = vortex.CreatedAt.CompareTo(strongest.CreatedAt);
                        if (tie == 0) tie = vortex.Position.x.CompareTo(strongest.Position.x);
                        if (tie == 0) tie = vortex.Position.y.CompareTo(strongest.Position.y);
                        if (tie == 0) tie = vortex.Position.z.CompareTo(strongest.Position.z);
                        if (strongest == null || distance < nearest || (distance == nearest && tie < 0))
                        { strongest = vortex; nearest = distance; }
                    }
                    if (strongest != null)
                    {
                        var pullTarget = new Vector3(strongest.Position.x, enemy.Position.y, strongest.Position.z);
                        var destination = Vector3.MoveTowards(enemy.Position, pullTarget, 0.55f * step);
                        if (ClearPath(enemy.Position, destination)) enemy.Position = destination;
                    }
                }
                Elapsed = next;
                for (int i = Vortices.Count - 1; i >= 0; i--)
                {
                    var vortex = Vortices[i];
                    if (Elapsed + Epsilon >= vortex.ExpiresAt) { Vortices.RemoveAt(i); continue; }
                    if (Elapsed + Epsilon < vortex.NextTick) continue;
                    foreach (var enemy in Enemies)
                        if (enemy.Alive && Vector3.Distance(enemy.Position, vortex.Position) <= 1.7f
                            && ClearPath(vortex.Position, enemy.Position))
                            DamageEnemy(enemy, vortex.Owner.Damage);
                    vortex.NextTick += 0.5;
                }
            }
            // Parent resolves contact before advancing the winning frame.
            if (Health <= 0) Phase = FantasyPhase.Defeat;
            else if (Elapsed + Epsilon >= 600) { Elapsed = 600; Phase = FantasyPhase.Victory; }
        }

        public void CollectXp(int amount)
        {
            if (Phase != FantasyPhase.Playing || amount <= 0) return;
            Xp += amount;
            if (Xp >= Threshold) { Phase = FantasyPhase.Upgrade; GenerateChoices(); }
        }

        private void GenerateChoices()
        {
            Choices.Clear();
            var pool = new List<UpgradeChoice>();
            if (Weapons.Count < 6)
                foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
                    pool.Add(new UpgradeChoice { IsGrant = true, Kind = kind, CopyId = 0, Label = "Acquire " + kind });
            foreach (var weapon in Weapons)
                pool.Add(new UpgradeChoice { Kind = weapon.Kind, CopyId = weapon.CopyId,
                    Label = weapon.Kind + " #" + weapon.CopyId + " damage +10% base" });
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int index = random.Next(pool.Count);
                Choices.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }

        public bool Choose(int index)
        {
            if (Phase != FantasyPhase.Upgrade || index < 0 || index >= Choices.Count) return false;
            var choice = Choices[index];
            if (choice.IsGrant)
            {
                if (!Grant(choice.Kind)) return false;
            }
            else
            {
                var weapon = Weapons.Find(w => w.CopyId == choice.CopyId);
                if (weapon == null) return false;
                weapon.DamageRanks++;
                weapon.Damage = weapon.BaseDamage * (1 + 0.1f * weapon.DamageRanks);
            }
            Xp -= Threshold;
            Threshold += 6;
            Level++;
            if (Xp >= Threshold) GenerateChoices();
            else { Choices.Clear(); Phase = FantasyPhase.Playing; }
            return true;
        }
    }
}
