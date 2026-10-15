using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public enum ChaseWeaponKind
    {
        Stormsplit, Volley, Sentry, Razor, Frenzy, Warcry, Cyclone, Magma,
        BoneLance, Graveburst, Sanctifier, OrbitHammer, Inferno, Winterglass, Pactbinder, Abyss
    }
    public enum ChaseEnemyKind { Undead, Demon, Elite }
    public enum ChasePhase { Ready, Playing, Paused, Upgrade, Victory, Defeat }
    public enum ChaseUpgradeKind { Damage, Cadence, Magazine }
    public enum ChaseUpgradeSource { Level, WeaponUse }
    public struct ChaseSurface
    {
        public Vector3 Point, Normal;
    }

    public sealed class ChaseEnemy
    {
        public int Id;
        public ChaseEnemyKind Kind;
        public Vector3 Position;
        public float Health, MaxHealth, Speed;
        public double SpawnTime, FlashUntil, SlowUntil, StunUntil, BoundUntil;
        public float SizeScale;
        public Vector3 HeadOffset = new Vector3(0, 2.02f, .015f);
        public float LocalHeadRadius = .17f;
        public Quaternion Facing = Quaternion.identity;
        public float BodyHeight => 2.2f * SizeScale;
        public float BodyRadius => .42f * SizeScale;
        public float BodyOffset => BodyHeight / 2;
        public float HeadHeight => HeadOffset.y * SizeScale;
        public float HeadRadius => LocalHeadRadius * SizeScale;
        public Vector3 Feet => Position - Vector3.up * BodyOffset;
        public Vector3 HeadPosition => Feet + Facing * (HeadOffset * SizeScale);
        public bool Alive => Health > 0;
    }
    public sealed class ChaseWeapon
    {
        public ChaseWeaponKind Kind;
        public int CopyId, DamageRanks, CadenceRanks, MagazineRanks, Ammo, Magazine;
        public int SuccessfulUses, MasteryAwards;
        public int PendingAmmoReturns;
        public int MasteryThreshold => 30 + MasteryAwards * 15;
        public int Rank => DamageRanks + CadenceRanks + MagazineRanks;
        public float Damage, BaseDamage, Reach, Cadence;
        public double ReadyAt, ReloadUntil;
        public double ReloadBoostUntil;
    }
    public sealed class ChaseAttack
    {
        public ChaseWeaponKind Kind;
        public int CopyId;
        public Vector3 Origin;
        public readonly List<Vector3> Points = new List<Vector3>();
    }
    public sealed class ChaseHit
    {
        public ChaseEnemy Enemy;
        public ChaseWeaponKind WeaponKind;
        public float Damage;
        public bool Fatal;
        public Vector3 Position;
        public ChaseHitRegion Region;
        public bool Critical;
    }
    public sealed class ChaseField
    {
        public ChaseWeapon Owner;
        public Vector3 Position, Direction;
        public double CreatedAt, ExpiresAt, NextTick;
        public float Radius;
    }
    public sealed class ChaseCorpse
    {
        public Vector3 Position;
        public double CreatedAt;
        public bool Consumed;
    }
    public sealed class ChaseChoice
    {
        public bool IsGrant;
        public ChaseWeaponKind Kind;
        public int CopyId;
        public string Label;
        public ChaseUpgradeKind UpgradeKind;
        public ChaseRelicKind? Relic;
    }

    public sealed class ChaseRun
    {
        public readonly List<ChaseWeapon> Weapons = new List<ChaseWeapon>();
        public readonly List<ChaseEnemy> Enemies = new List<ChaseEnemy>();
        public readonly List<ChaseField> Fields = new List<ChaseField>();
        public readonly List<ChaseCorpse> Corpses = new List<ChaseCorpse>();
        public readonly List<ChaseAttack> Attacks = new List<ChaseAttack>();
        public readonly List<ChaseHit> Hits = new List<ChaseHit>();
        public readonly List<ChaseEnemy> Deaths = new List<ChaseEnemy>();
        public readonly List<ChaseChoice> Choices = new List<ChaseChoice>();
        public readonly List<ChaseLoot> Loot = new List<ChaseLoot>();
        public readonly List<ChaseRelic> Relics = new List<ChaseRelic>();
        public int Health { get; private set; }
        public int Xp { get; private set; }
        public int Threshold { get; private set; }
        public int Level { get; private set; }
        public int Kills { get; private set; }
        public int TotalAttacks { get; private set; }
        public int TotalHits { get; private set; }
        public int DamageTakenCount { get; private set; }
        public int LastDamageAmount { get; private set; }
        public ChaseUpgradeSource UpgradeSource { get; private set; }
        public double Elapsed;
        public int LastAdvanceSubsteps;
        public ChasePhase Phase;
        public bool Automatic = true;
        public Func<Vector3, Vector3, bool> ClearPath = (a, b) => true;
        public Func<Vector3, Vector3, bool> VortexTravelClear = (a, b) => true;
        // Geometry supplies an actual contact point and normal, not a fabricated turn.
        public Func<Vector3, Vector3, ChaseSurface?> SurfaceHit = (a, b) => null;

        private sealed class FieldState
        {
            public Vector3 Anchor;
            public bool BurningGround;
            public bool MasteryPending;
            public readonly Dictionary<int, double> HitReady = new Dictionary<int, double>();
        }
        private sealed class Burn
        {
            public ChaseEnemy Enemy;
            public ChaseWeapon Owner;
            public double Until, NextTick;
        }
        private sealed class FrenzyState { public int Stacks; public double Until; }
        private sealed class Binding { public ChaseWeapon Owner; public double NextTick; }
        private readonly Dictionary<ChaseField, FieldState> fieldStates = new Dictionary<ChaseField, FieldState>();
        private readonly List<Burn> burns = new List<Burn>();
        private readonly Dictionary<ChaseWeapon, FrenzyState> frenzy = new Dictionary<ChaseWeapon, FrenzyState>();
        private readonly Dictionary<ChaseEnemy, Binding> bindings = new Dictionary<ChaseEnemy, Binding>();
        private System.Random random;
        private int nextEnemyId, nextCopyId, nextLootId;
        private double ammoReturnReadyAt;
        private int directEchoHits;
        private double contactReadyAt;
        private const double Epsilon = .0000001;

        public float PlayerSpeedMultiplier
        {
            get
            {
                int stacks = 0;
                foreach (var state in frenzy.Values)
                    if (state.Until > Elapsed) stacks = Math.Max(stacks, state.Stacks);
                return 1 + .06f * stacks;
            }
        }

        public ChaseRun() { Reset(); }
        public void Reset(int seed = 1, ChaseWeaponKind starter = ChaseWeaponKind.Stormsplit)
        {
            Weapons.Clear(); Enemies.Clear(); Fields.Clear(); Corpses.Clear(); Attacks.Clear();
            Hits.Clear(); Deaths.Clear(); Choices.Clear(); fieldStates.Clear(); burns.Clear(); frenzy.Clear(); bindings.Clear();
            Loot.Clear(); Relics.Clear();
            random = new System.Random(seed);
            nextEnemyId = nextCopyId = nextLootId = directEchoHits = 0;
            ammoReturnReadyAt = 0;
            Health = 100; Xp = 0; Threshold = 12; Level = 1;
            Kills = TotalHits = TotalAttacks = 0;
            DamageTakenCount = LastDamageAmount = 0;
            UpgradeSource = ChaseUpgradeSource.Level;
            Elapsed = contactReadyAt = 0;
            Phase = ChasePhase.Ready;
            Grant(starter);
        }
        public void Start() { if (Phase == ChasePhase.Ready) Phase = ChasePhase.Playing; }
        public void Pause() { if (Phase == ChasePhase.Playing) Phase = ChasePhase.Paused; }
        public void Resume() { if (Phase == ChasePhase.Paused) Phase = ChasePhase.Playing; }

        public bool Grant(ChaseWeaponKind kind)
        {
            if (Weapons.Count >= 6 || !Enum.IsDefined(typeof(ChaseWeaponKind), kind)) return false;
            var weapon = new ChaseWeapon { Kind = kind, CopyId = ++nextCopyId, ReadyAt = Elapsed };
            switch (kind)
            {
                case ChaseWeaponKind.Stormsplit: weapon.Damage = 18; weapon.Reach = 26; weapon.Cadence = .65f; weapon.Magazine = 12; break;
                case ChaseWeaponKind.Volley: weapon.Damage = 12; weapon.Reach = 24; weapon.Cadence = .55f; weapon.Magazine = 10; break;
                case ChaseWeaponKind.Sentry: weapon.Damage = 14; weapon.Reach = 20; weapon.Cadence = 1.4f; weapon.Magazine = 4; break;
                case ChaseWeaponKind.Razor: weapon.Damage = 16; weapon.Reach = 30; weapon.Cadence = .35f; weapon.Magazine = 18; break;
                case ChaseWeaponKind.Frenzy: weapon.Damage = 10; weapon.Reach = 24; weapon.Cadence = .28f; weapon.Magazine = 24; break;
                case ChaseWeaponKind.Warcry: weapon.Damage = 22; weapon.Reach = 10; weapon.Cadence = 1.2f; weapon.Magazine = 8; break;
                case ChaseWeaponKind.Cyclone: weapon.Damage = 12; weapon.Reach = 22; weapon.Cadence = 1.6f; weapon.Magazine = 6; break;
                case ChaseWeaponKind.Magma: weapon.Damage = 26; weapon.Reach = 22; weapon.Cadence = 1.1f; weapon.Magazine = 8; break;
                case ChaseWeaponKind.BoneLance: weapon.Damage = 24; weapon.Reach = 32; weapon.Cadence = .7f; weapon.Magazine = 10; break;
                case ChaseWeaponKind.Graveburst: weapon.Damage = 16; weapon.Reach = 24; weapon.Cadence = .8f; weapon.Magazine = 10; break;
                case ChaseWeaponKind.Sanctifier: weapon.Damage = 18; weapon.Reach = 30; weapon.Cadence = .65f; weapon.Magazine = 12; break;
                case ChaseWeaponKind.OrbitHammer: weapon.Damage = 26; weapon.Reach = 18; weapon.Cadence = 1f; weapon.Magazine = 8; break;
                case ChaseWeaponKind.Inferno: weapon.Damage = 14; weapon.Reach = 11; weapon.Cadence = .18f; weapon.Magazine = 30; break;
                case ChaseWeaponKind.Winterglass: weapon.Damage = 9; weapon.Reach = 24; weapon.Cadence = 1.3f; weapon.Magazine = 8; break;
                case ChaseWeaponKind.Pactbinder: weapon.Damage = 20; weapon.Reach = 25; weapon.Cadence = .8f; weapon.Magazine = 10; break;
                case ChaseWeaponKind.Abyss: weapon.Damage = 8; weapon.Reach = 14; weapon.Cadence = 2.4f; weapon.Magazine = 6; break;
            }
            // Provisional deliberately unequal roles, not a uniform DPS budget.
            switch (kind)
            {
                case ChaseWeaponKind.Stormsplit: weapon.Damage = 12; weapon.Cadence = .62f; break;
                case ChaseWeaponKind.Volley: weapon.Damage = 13; weapon.Cadence = .8f; break;
                case ChaseWeaponKind.Frenzy: weapon.Damage = 7; weapon.Cadence = .19f; break;
                case ChaseWeaponKind.Razor: weapon.Damage = 23; weapon.Cadence = .42f; break;
                case ChaseWeaponKind.Warcry: weapon.Damage = 16; weapon.Reach = 7; weapon.Cadence = 1.1f; break;
                case ChaseWeaponKind.Cyclone: weapon.Damage = 9; weapon.Cadence = 1.9f; break;
                case ChaseWeaponKind.Magma: weapon.Damage = 82; weapon.Cadence = 1.8f; weapon.Magazine = 4; break;
                case ChaseWeaponKind.BoneLance: weapon.Damage = 42; weapon.Cadence = .95f; weapon.Magazine = 7; break;
                case ChaseWeaponKind.Graveburst: weapon.Damage = 19; weapon.Cadence = .85f; break;
                case ChaseWeaponKind.Sanctifier: weapon.Damage = 48; weapon.Cadence = 1.05f; weapon.Magazine = 6; break;
                case ChaseWeaponKind.OrbitHammer: weapon.Damage = 45; weapon.Cadence = 1.3f; break;
                case ChaseWeaponKind.Inferno: weapon.Damage = 8; weapon.Reach = 9; weapon.Cadence = .14f; break;
                case ChaseWeaponKind.Winterglass: weapon.Damage = 6; weapon.Cadence = 1.45f; break;
                case ChaseWeaponKind.Pactbinder: weapon.Damage = 23; weapon.Cadence = .9f; break;
                case ChaseWeaponKind.Abyss: weapon.Damage = 4; weapon.Cadence = 2.8f; break;
            }
            weapon.Ammo = weapon.Magazine;
            weapon.BaseDamage = weapon.Damage;
            Weapons.Add(weapon);
            return true;
        }

        public float RelicPower(ChaseRelicKind kind)
        {
            var relic = Relics.Find(r => r.Kind == kind);
            return relic == null ? 0 : 1 + .15f * (Level - 1) + .35f * (relic.Rank - 1);
        }

        public ChaseLoot DropLoot(Vector3 position, ChaseLootKind kind,
            ChaseWeaponKind weapon = ChaseWeaponKind.Stormsplit, ChaseRelicKind relic = ChaseRelicKind.Windstep)
        {
            var loot = new ChaseLoot { Id = ++nextLootId, Position = position,
                Kind = kind, Weapon = weapon, Relic = relic };
            Loot.Add(loot);
            return loot;
        }

        public bool CollectLoot(int id, int replaceCopyId = -1)
        {
            if (Phase != ChasePhase.Playing) return false;
            var loot = Loot.Find(l => l.Id == id);
            if (loot == null) return false;
            if (loot.Kind == ChaseLootKind.Relic)
            {
                var owned = Relics.Find(r => r.Kind == loot.Relic);
                if (owned == null) Relics.Add(new ChaseRelic { Kind = loot.Relic });
                else owned.Rank++;
            }
            else
            {
                int slot = -1;
                if (Weapons.Count == 6)
                {
                    slot = Weapons.FindIndex(w => w.CopyId == replaceCopyId);
                    if (slot < 0) return false;
                    var removed = Weapons[slot];
                    for (int field = Fields.Count - 1; field >= 0; field--)
                        if (Fields[field].Owner == removed) RemoveField(Fields[field]);
                    burns.RemoveAll(b => b.Owner == removed);
                    frenzy.Remove(removed);
                    var released = new List<ChaseEnemy>();
                    foreach (var binding in bindings)
                        if (binding.Value.Owner == removed)
                        { binding.Key.BoundUntil = Elapsed; released.Add(binding.Key); }
                    foreach (var enemy in released) bindings.Remove(enemy);
                    Weapons.RemoveAt(slot);
                }
                if (!Grant(loot.Weapon)) return false;
                if (slot >= 0)
                {
                    var acquired = Weapons[Weapons.Count - 1];
                    Weapons.RemoveAt(Weapons.Count - 1);
                    Weapons.Insert(slot, acquired);
                }
            }
            Loot.Remove(loot);
            return true;
        }

        public ChaseEnemy AddEnemy(Vector3 position, float health = -1, ChaseEnemyKind kind = ChaseEnemyKind.Undead)
        {
            float maximum = health < 0 ? ChaseCalibration.HealthAt(Elapsed) * (kind == ChaseEnemyKind.Elite ? 2 : 1) : health;
            var enemy = new ChaseEnemy { Id = ++nextEnemyId, Kind = kind, Position = position,
                Health = maximum, MaxHealth = maximum, Speed = ChaseCalibration.SpeedAt(Elapsed), SpawnTime = Elapsed, SizeScale = ChaseCalibration.SizeAt(kind, Elapsed) };
            Enemies.Add(enemy);
            return enemy;
        }
        public bool Contact() => TakeHazardDamage(Mathf.RoundToInt(
            ChaseCalibration.ContactDamageAt(Elapsed, DamageTakenCount) *
            (1 + (Health < 50 ? .2f * RelicPower(ChaseRelicKind.VolatileHeart) : 0))));

        // Contact and hazards share one player-wide cooldown. Only accepted hits
        // advance escalation, and the fatal hit reports actual remaining HP lost.
        public bool TakeHazardDamage(int amount)
        {
            if (Phase != ChasePhase.Playing || amount <= 0 || Elapsed + Epsilon < contactReadyAt) return false;
            contactReadyAt = Elapsed + ChaseCalibration.DamageCooldownSeconds;
            LastDamageAmount = Math.Min(Health, amount);
            DamageTakenCount++;
            Health -= LastDamageAmount;
            if (Health == 0) Phase = ChasePhase.Defeat;
            return true;
        }
        public bool Heal(int amount)
        {
            if (Phase != ChasePhase.Playing || Health == 100 || amount <= 0) return false;
            Health = Math.Min(100, Health + amount);
            return true;
        }
        public void DamageEnemy(ChaseEnemy enemy, float damage, ChaseWeaponKind kind = ChaseWeaponKind.Stormsplit)
        {
            if (Phase != ChasePhase.Playing || enemy == null || !enemy.Alive || damage <= 0) return;
            float actual = Mathf.Min(enemy.Health, damage);
            enemy.Health = Mathf.Max(0, enemy.Health - damage);
            enemy.FlashUntil = Elapsed + .14;
            TotalHits++;
            Hits.Add(new ChaseHit { Enemy = enemy, WeaponKind = kind, Damage = actual, Fatal = !enemy.Alive, Position = enemy.Position });
            if (!enemy.Alive)
            {
                Kills++; Deaths.Add(enemy);
                Corpses.Add(new ChaseCorpse { Position = enemy.Position, CreatedAt = Elapsed });
                if (Kills == 3 || Kills == 8 || random.NextDouble() <
                    (enemy.Kind == ChaseEnemyKind.Elite ? .7 : .16))
                {
                    bool weaponDrop = Kills == 3 || (Kills != 8 && random.NextDouble() < .55);
                    DropLoot(enemy.Feet + Vector3.up * .3f,
                        weaponDrop ? ChaseLootKind.Weapon : ChaseLootKind.Relic,
                        (ChaseWeaponKind)random.Next(16), (ChaseRelicKind)random.Next(6));
                }
            }
        }
        private bool Hostile(ChaseEnemy enemy) => enemy.Alive && enemy.BoundUntil <= Elapsed;
        public float EffectiveSpeed(ChaseEnemy enemy)
        {
            if (!enemy.Alive || enemy.StunUntil > Elapsed) return 0;
            float multiplier = enemy.SlowUntil > Elapsed ? .5f : 1;
            foreach (var field in Fields)
            {
                float slow = field.Owner.Kind == ChaseWeaponKind.Cyclone ? .35f :
                    field.Owner.Kind == ChaseWeaponKind.Abyss ? .82f : 1;
                if (slow < multiplier && Hostile(enemy) && InField(field, enemy)) multiplier = slow;
            }
            return enemy.Speed * multiplier * (Hostile(enemy) ? ChaseCalibration.PursuitMultiplierAt(Elapsed) : 1);
        }
        public ChaseEnemy BoundTarget(ChaseEnemy ally)
        {
            return ally != null && ally.Alive && ally.BoundUntil > Elapsed ? Nearest(ally.Position, 40) : null;
        }
        public void ReloadAll()
        {
            if (Phase != ChasePhase.Playing) return;
            foreach (var weapon in Weapons) BeginReload(weapon);
        }
        private void BeginReload(ChaseWeapon weapon)
        {
            if (weapon.Ammo < weapon.Magazine && weapon.ReloadUntil <= Elapsed)
                weapon.ReloadUntil = Elapsed + 1.6 / (1 + .3f * RelicPower(ChaseRelicKind.Quicksilver));
        }

        public ChaseImpact? FindShot(Vector3 origin, Vector3 forward, float reach, float radius = 0)
        {
            ChaseImpact? best = null;
            foreach (var enemy in Enemies)
            {
                if (!Hostile(enemy) || !ChaseAim.Intersect(enemy, origin, forward, reach, radius, out var hit) ||
                    !ClearPath(origin, hit.Point)) continue;
                if (!best.HasValue || hit.Distance < best.Value.Distance ||
                    (hit.Distance == best.Value.Distance && enemy.Id < best.Value.Enemy.Id)) best = hit;
            }
            return best;
        }

        private void DamageDirect(ChaseWeapon weapon, ChaseImpact impact, float multiplier = 1)
        {
            if (!Hostile(impact.Enemy)) return;
            bool head = impact.Region == ChaseHitRegion.Head;
            float risky = Health < 50 ? 1 + .4f * RelicPower(ChaseRelicKind.VolatileHeart) : 1;
            float reload = weapon.ReloadBoostUntil > Elapsed ? 1 + .25f *
                RelicPower(ChaseRelicKind.Quicksilver) : 1;
            DamageEnemy(impact.Enemy, weapon.Damage * multiplier * risky * reload * (head ? 2 : 1), weapon.Kind);
            var recorded = Hits[Hits.Count - 1];
            recorded.Position = impact.Point; recorded.Region = impact.Region; recorded.Critical = head;
            if (head)
            {
                float eye = RelicPower(ChaseRelicKind.Executioner);
                if (eye > 0 && Elapsed >= ammoReturnReadyAt)
                { weapon.PendingAmmoReturns++; ammoReturnReadyAt = Elapsed + .8 / eye; }
                float blood = RelicPower(ChaseRelicKind.BloodGlass);
                if (blood > 0) Heal(Mathf.Max(1, Mathf.RoundToInt(2 * blood)));
            }
            float echo = RelicPower(ChaseRelicKind.EchoSeal);
            if (echo > 0 && ++directEchoHits % 3 == 0)
            {
                ChaseEnemy target = null; float nearest = 5;
                foreach (var enemy in Enemies)
                {
                    float distance = Vector3.Distance(impact.Point, enemy.Position);
                    if (enemy != impact.Enemy && Hostile(enemy) && distance < nearest &&
                        ClearPath(impact.Point, enemy.Position)) { target = enemy; nearest = distance; }
                }
                if (target != null)
                {
                    DamageEnemy(target, weapon.Damage * .35f * echo, weapon.Kind);
                    var strike = new ChaseAttack { Kind = weapon.Kind, CopyId = weapon.CopyId, Origin = impact.Point };
                    strike.Points.Add(target.Position); Attacks.Add(strike);
                }
            }
        }

        public bool RequestAttack(int copyIndex, Vector3 origin, Vector3 forward, int aimedEnemyId,
            Vector3? groundPoint, bool manual, Vector3? aimOrigin = null, Vector3? aimForward = null, bool alternate = false)
        {
            if (Phase != ChasePhase.Playing || copyIndex < 0 || copyIndex >= Weapons.Count) return false;
            var weapon = Weapons[copyIndex];
            if (Elapsed + Epsilon < weapon.ReadyAt || weapon.ReloadUntil > Elapsed || weapon.Ammo <= 0) return false;
            var direction = forward.normalized;
            if (direction.sqrMagnitude == 0) return false;
            var caster = aimOrigin ?? origin;
            var cameraAim = (aimForward ?? forward).normalized;
            var direct = FindShot(caster, cameraAim, weapon.Reach);
            var aimed = direct.HasValue ? direct.Value.Enemy : null;
            bool eligible = direct.HasValue && Vector3.Distance(origin, direct.Value.Point) <= weapon.Reach &&
                ClearPath(origin, direct.Value.Point);
            weapon.PendingAmmoReturns = 0;
            var attack = new ChaseAttack { Kind = weapon.Kind, CopyId = weapon.CopyId, Origin = origin };
            int previousHits = TotalHits;
            switch (weapon.Kind)
            {
                case ChaseWeaponKind.Stormsplit:
                    if (!eligible && !manual) return false;
                    if (eligible) Branch(weapon, direct.Value, attack);
                    else attack.Points.Add(origin + direction * weapon.Reach);
                    break;
                case ChaseWeaponKind.Volley:
                    if (!manual && !eligible) return false;
                    for (int i = -2; i <= 2; i++)
                    {
                        var ray = Quaternion.AngleAxis(i * 9, Vector3.up) * direction;
                        var target = FindShot(origin, ray, weapon.Reach, .025f);
                        attack.Points.Add(target.HasValue ? target.Value.Point : origin + ray * weapon.Reach);
                        if (target.HasValue) DamageDirect(weapon, target.Value);
                    }
                    break;
                case ChaseWeaponKind.Razor:
                    if (!manual && !eligible) return false;
                    Ricochet(weapon, origin, direction, attack);
                    break;
                case ChaseWeaponKind.Frenzy:
                    if (!manual && !eligible) return false;
                    attack.Points.Add(eligible ? direct.Value.Point : origin + direction * weapon.Reach);
                    if (eligible)
                    {
                        DamageDirect(weapon, direct.Value);
                        if (!frenzy.TryGetValue(weapon, out var state)) frenzy.Add(weapon, state = new FrenzyState());
                        if (state.Until <= Elapsed) state.Stacks = 0;
                        state.Stacks = Math.Min(5, state.Stacks + 1);
                        state.Until = Elapsed + 3;
                    }
                    break;
                case ChaseWeaponKind.Warcry:
                case ChaseWeaponKind.Inferno:
                    if (!manual && !eligible) return false;
                    attack.Points.Add(origin + direction * weapon.Reach);
                    foreach (var enemy in Enemies)
                        if (Hostile(enemy) && Vector3.Distance(origin, enemy.Position) <= weapon.Reach
                            && Vector3.Angle(direction, enemy.Position - origin) <= (weapon.Kind == ChaseWeaponKind.Warcry ? 20 : 9)
                            && ClearPath(origin, enemy.Position))
                        {
                            DamageEnemy(enemy, weapon.Damage, weapon.Kind);
                            if (weapon.Kind == ChaseWeaponKind.Warcry) enemy.StunUntil = Elapsed + 1;
                            else Ignite(enemy, weapon);
                        }
                    break;
                case ChaseWeaponKind.Sentry:
                case ChaseWeaponKind.Cyclone:
                case ChaseWeaponKind.Abyss:
                    if (!groundPoint.HasValue || Vector3.Distance(origin, groundPoint.Value) > weapon.Reach) return false;
                    var ground = groundPoint.Value;
                    var groundDirection = ground - origin; groundDirection.y = 0;
                    if (weapon.Kind == ChaseWeaponKind.Cyclone && groundDirection.sqrMagnitude < .000001f) return false;
                    float radius = weapon.Kind == ChaseWeaponKind.Sentry ? 14 : weapon.Kind == ChaseWeaponKind.Cyclone ? 3.3f : 2.4f;
                    if (!manual)
                    {
                        if (weapon.Kind == ChaseWeaponKind.Sentry)
                        { if (Nearest(ground, radius) == null) return false; }
                        else
                        {
                            bool affectsEnemy = false;
                            var prospective = new ChaseField { Owner = weapon, Position = ground, Radius = radius };
                            foreach (var foe in Enemies)
                                if (Hostile(foe) && InField(prospective, foe)) { affectsEnemy = true; break; }
                            if (!affectsEnemy) return false;
                        }
                    }
                    AddField(weapon, ground, weapon.Kind == ChaseWeaponKind.Cyclone ? groundDirection.normalized : Vector3.zero,
                        radius, weapon.Kind == ChaseWeaponKind.Sentry ? 10 : weapon.Kind == ChaseWeaponKind.Cyclone ? 4.5 : 3,
                        weapon.Kind == ChaseWeaponKind.Sentry ? .65 : weapon.Kind == ChaseWeaponKind.Abyss ? .5 : .35);
                    attack.Points.Add(ground);
                    break;
                case ChaseWeaponKind.Magma:
                case ChaseWeaponKind.Winterglass:
                    if (!manual && !eligible) return false;
                    AddField(weapon, origin, direction, weapon.Kind == ChaseWeaponKind.Magma ? 2.8f : 1.4f, 4, .35);
                    attack.Points.Add(origin + direction * weapon.Reach);
                    break;
                case ChaseWeaponKind.BoneLance:
                case ChaseWeaponKind.Sanctifier:
                    if (!manual && !eligible) return false;
                    Pierce(weapon, origin, origin + direction * weapon.Reach, .04f, null);
                    attack.Points.Add(origin + direction * weapon.Reach);
                    break;
                case ChaseWeaponKind.Graveburst:
                    if (alternate)
                    {
                        var corpse = FindCorpse(origin, caster, cameraAim, weapon.Reach);
                        if (corpse == null) return false;
                        corpse.Consumed = true;
                        attack.Points.Add(corpse.Position);
                        foreach (var enemy in Enemies)
                            if (Hostile(enemy) && Vector3.Distance(corpse.Position, enemy.Position) <= 5
                                && ClearPath(corpse.Position, enemy.Position)) DamageEnemy(enemy, weapon.Damage * 3, weapon.Kind);
                    }
                    else
                    {
                        if (!manual && !eligible) return false;
                        attack.Points.Add(eligible ? direct.Value.Point : origin + direction * weapon.Reach);
                        if (eligible) { DamageDirect(weapon, direct.Value); Ignite(aimed, weapon); }
                    }
                    break;
                case ChaseWeaponKind.OrbitHammer:
                    if (!manual && !eligible) return false;
                    var hammerDirection = direction; hammerDirection.y = 0;
                    if (hammerDirection.sqrMagnitude < .000001f) return false;
                    hammerDirection.Normalize();
                    AddField(weapon, origin + hammerDirection, hammerDirection, .9f, 3, .05);
                    fieldStates[Fields[Fields.Count - 1]].Anchor = origin;
                    attack.Points.Add(origin + hammerDirection);
                    break;
                case ChaseWeaponKind.Pactbinder:
                    if (alternate)
                    {
                        if (!eligible || aimed.Kind != ChaseEnemyKind.Demon ||
                            (double)aimed.Health > (double)aimed.MaxHealth * .35) return false;
                        aimed.BoundUntil = Elapsed + 12;
                        aimed.SlowUntil = aimed.StunUntil = Elapsed;
                        bindings[aimed] = new Binding { Owner = weapon, NextTick = Elapsed + .8 };
                        // A newly allied demon must not retain a hostile fire DoT.
                        burns.RemoveAll(b => b.Enemy == aimed);
                    }
                    else
                    {
                        if (!manual && !eligible) return false;
                        if (eligible) { DamageDirect(weapon, direct.Value); aimed.SlowUntil = Elapsed + 2; }
                    }
                    attack.Points.Add(eligible ? direct.Value.Point : origin + direction * weapon.Reach);
                    break;

            }
            float cadence = weapon.Cadence;
            if (weapon.Kind == ChaseWeaponKind.Frenzy && frenzy.TryGetValue(weapon, out var active) && active.Until > Elapsed)
                cadence /= 1 + .15f * active.Stacks;
            weapon.ReadyAt = Elapsed + cadence;
            weapon.Ammo = Math.Min(weapon.Magazine, weapon.Ammo - 1 + weapon.PendingAmmoReturns);
            weapon.PendingAmmoReturns = 0;
            if (weapon.Ammo == 0) BeginReload(weapon);
            TotalAttacks++;
            Attacks.Add(attack);
            // One use per deliberate successful cast, not each victim, chain
            // branch, autonomous turret attack or later damage tick.
            bool usefulPlacement = false;
            foreach (var field in Fields)
                    if (field.Owner == weapon && field.CreatedAt == Elapsed &&
                        (weapon.Kind == ChaseWeaponKind.Sentry || weapon.Kind == ChaseWeaponKind.Cyclone ||
                        weapon.Kind == ChaseWeaponKind.Abyss))
                        foreach (var enemy in Enemies)
                            if (Hostile(enemy) && InField(field, enemy)) usefulPlacement = true;
            if (TotalHits > previousHits || usefulPlacement)
                RecordSuccessfulUse(weapon);
            return true;
        }

        private void RecordSuccessfulUse(ChaseWeapon weapon)
        {
            weapon.SuccessfulUses++;
            if (weapon.SuccessfulUses < weapon.MasteryThreshold || Phase != ChasePhase.Playing) return;
            UpgradeSource = ChaseUpgradeSource.WeaponUse;
            Phase = ChasePhase.Upgrade;
            GenerateChoices(weapon);
        }

        private void CreditProjectile(ChaseField field, int hitsBefore)
        {
            var state = fieldStates[field];
            if (!state.MasteryPending || TotalHits <= hitsBefore) return;
            state.MasteryPending = false;
            RecordSuccessfulUse(field.Owner);
        }

        private ChaseEnemy Nearest(Vector3 from, float reach)
        {
            ChaseEnemy best = null; float distance = reach;
            foreach (var enemy in Enemies)
            {
                if (!Hostile(enemy)) continue;
                float candidate = Vector3.Distance(from, enemy.Position);
                if (candidate <= distance && (best == null || candidate < distance || enemy.Id < best.Id)
                    && ClearPath(from, enemy.Position))
                { best = enemy; distance = candidate; }
            }
            return best;
        }
        private ChaseEnemy FirstOnRay(Vector3 origin, Vector3 direction, float reach, float radius)
        {
            ChaseEnemy best = null; float nearest = reach;
            foreach (var enemy in Enemies)
            {
                if (!Hostile(enemy)) continue;
                var offset = enemy.Position - origin;
                float along = Vector3.Dot(offset, direction);
                if (along < 0 || along > nearest || !HitsBody(enemy, origin, origin + direction * reach, radius)
                    || !ClearPath(origin, enemy.Position)) continue;
                if (best == null || along < nearest || enemy.Id < best.Id) { best = enemy; nearest = along; }
            }
            return best;
        }
        private void Branch(ChaseWeapon weapon, ChaseImpact impact, ChaseAttack attack)
        {
            var first = impact.Enemy;
            var seen = new HashSet<int> { first.Id };
            var parents = new List<ChaseEnemy> { first };
            attack.Points.Add(impact.Point);
            DamageDirect(weapon, impact);
            for (int depth = 0; depth < 2; depth++)
            {
                var next = new List<ChaseEnemy>();
                foreach (var parent in parents)
                    for (int branch = 0; branch < 2; branch++)
                    {
                        ChaseEnemy best = null; float nearest = 6;
                        foreach (var enemy in Enemies)
                        {
                            if (!Hostile(enemy) || seen.Contains(enemy.Id)) continue;
                            float distance = Vector3.Distance(parent.Position, enemy.Position);
                            if (distance <= nearest && (best == null || distance < nearest || enemy.Id < best.Id)
                                && ClearPath(parent.Position, enemy.Position))
                            { best = enemy; nearest = distance; }
                        }
                        if (best == null) break;
                        seen.Add(best.Id); next.Add(best);
                        // Edge pairs preserve each branch origin for presentation.
                        attack.Points.Add(parent.Position); attack.Points.Add(best.Position);
                        DamageEnemy(best, weapon.Damage * .8f, weapon.Kind);
                    }
                parents = next;
            }
        }
        private static float SegmentDistance(Vector3 point, Vector3 start, Vector3 end)
        {
            var segment = end - start;
            float t = segment.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector3.Distance(point, start + segment * t);
        }
        private static bool HitsBody(ChaseEnemy enemy, Vector3 start, Vector3 end, float radius)
        {
            radius = Mathf.Max(radius, enemy.BodyRadius);
            float half = enemy.BodyOffset - enemy.BodyRadius;
            Vector3 segment = end - start;
            float horizontal = segment.x * segment.x + segment.z * segment.z;
            float t = horizontal == 0 ? 0 : Mathf.Clamp01(((enemy.Position.x - start.x) * segment.x +
                (enemy.Position.z - start.z) * segment.z) / horizontal);
            Vector3 point = start + segment * t;
            float dx = point.x - enemy.Position.x, dz = point.z - enemy.Position.z;
            if (dx * dx + dz * dz > radius * radius) return false;
            if (point.y >= enemy.Position.y - half && point.y <= enemy.Position.y + half) return true;
            float length = segment.sqrMagnitude;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 offset = enemy.Position + Vector3.up * (half * side) - start;
                float along = length == 0 ? 0 : Mathf.Clamp01(Vector3.Dot(offset, segment) / length);
                if ((offset - segment * along).sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        private void Pierce(ChaseWeapon weapon, Vector3 start, Vector3 end, float radius, HashSet<int> seen)
        {
            foreach (var enemy in Enemies)
            {
                if (!Hostile(enemy) || (seen != null && seen.Contains(enemy.Id))
                    || !ChaseAim.Intersect(enemy, start, (end - start).normalized,
                        Vector3.Distance(start, end), radius, out var hit) || !ClearPath(start, hit.Point)) continue;
                if (seen != null) seen.Add(enemy.Id);
                float damage = weapon.Damage * (weapon.Kind == ChaseWeaponKind.Sanctifier && enemy.Kind == ChaseEnemyKind.Undead ? 1.5f : 1);
                DamageDirect(weapon, hit, damage / weapon.Damage);
                if (weapon.Kind == ChaseWeaponKind.Sanctifier && !enemy.Alive) Heal(2);
            }
        }
        private void Ricochet(ChaseWeapon weapon, Vector3 origin, Vector3 direction, ChaseAttack attack)
        {
            float remaining = weapon.Reach;
            var seen = new HashSet<int>();
            for (int bounce = 0; bounce <= 3 && remaining > .01f; bounce++)
            {
                var surface = SurfaceHit(origin, origin + direction * remaining);
                var end = surface.HasValue ? surface.Value.Point : origin + direction * remaining;
                Pierce(weapon, origin, end, .035f, seen);
                attack.Points.Add(end);
                remaining -= Vector3.Distance(origin, end);
                if (!surface.HasValue) break;
                direction = Vector3.Reflect(direction, surface.Value.Normal).normalized;
                origin = end + direction * .02f;
                remaining -= .02f;
            }
        }
        private ChaseCorpse FindCorpse(Vector3 from, Vector3 caster, Vector3 aim, float reach)
        {
            ChaseCorpse best = null; float angle = float.MaxValue;
            foreach (var corpse in Corpses)
            {
                if (corpse.Consumed || Vector3.Distance(from, corpse.Position) > reach || !ClearPath(from, corpse.Position)) continue;
                float candidate = Vector3.Angle(aim, corpse.Position - caster);
                if (candidate <= 35 && candidate < angle) { best = corpse; angle = candidate; }
            }
            return best;
        }
        private void Ignite(ChaseEnemy enemy, ChaseWeapon weapon)
        {
            if (!Hostile(enemy)) return;
            var burn = burns.Find(b => b.Enemy == enemy && b.Owner == weapon);
            if (burn == null) burns.Add(burn = new Burn { Enemy = enemy, Owner = weapon, NextTick = Elapsed + .5 });
            burn.Until = Elapsed + 3;
        }
        private void AddField(ChaseWeapon weapon, Vector3 point, Vector3 direction, float radius, double duration, double tick)
        {
            int count = 0; ChaseField oldest = null;
            foreach (var field in Fields)
                if (field.Owner == weapon)
                {
                    count++;
                    if (oldest == null || field.CreatedAt < oldest.CreatedAt) oldest = field;
                }
            if (count >= (weapon.Kind == ChaseWeaponKind.Abyss ? 1 : 3)) RemoveField(oldest);
            var created = new ChaseField { Owner = weapon, Position = point, Direction = direction,
                Radius = radius, CreatedAt = Elapsed, ExpiresAt = Elapsed + duration, NextTick = Elapsed + tick };
            Fields.Add(created);
            fieldStates.Add(created, new FieldState { Anchor = point, MasteryPending =
                weapon.Kind == ChaseWeaponKind.Magma || weapon.Kind == ChaseWeaponKind.Winterglass ||
                weapon.Kind == ChaseWeaponKind.OrbitHammer });
        }
        private void RemoveField(ChaseField field) { Fields.Remove(field); fieldStates.Remove(field); }
        private bool InField(ChaseField field, ChaseEnemy enemy)
        {
            if (field.Owner.Kind == ChaseWeaponKind.Abyss || field.Owner.Kind == ChaseWeaponKind.Cyclone)
            {
                Vector3 delta = enemy.Feet - field.Position;
                if (Mathf.Abs(delta.y) > 1.5f) return false;
                delta.y = 0;
                return delta.sqrMagnitude <= field.Radius * field.Radius &&
                    ClearPath(field.Position + Vector3.up * .2f, enemy.Position);
            }
            return Vector3.Distance(field.Position, enemy.Position) <= field.Radius && ClearPath(field.Position, enemy.Position);
        }

        public void Advance(float dt)
        {
            LastAdvanceSubsteps = 0;
            if (Phase != ChasePhase.Playing || dt <= 0) return;
            double end = Math.Min(600, Elapsed + dt);
            while (Phase == ChasePhase.Playing && Elapsed + Epsilon < end)
            {
                LastAdvanceSubsteps++;
                double next = Math.Min(end, Elapsed + .05);
                foreach (var field in Fields) next = Math.Min(next, Math.Min(field.NextTick, field.ExpiresAt));
                foreach (var burn in burns) next = Math.Min(next, Math.Min(burn.NextTick, burn.Until));
                foreach (var weapon in Weapons)
                    if (weapon.ReloadUntil > Elapsed) next = Math.Min(next, weapon.ReloadUntil);
                foreach (var pair in bindings)
                    if (pair.Key.Alive && pair.Key.BoundUntil > Elapsed)
                        next = Math.Min(next, Math.Min(pair.Value.NextTick, pair.Key.BoundUntil));
                float step = (float)(next - Elapsed);
                Elapsed = next;
                MoveFields(step, next);
                PullEnemies(step);
                foreach (var weapon in Weapons)
                    if (weapon.ReloadUntil > 0 && Elapsed + Epsilon >= weapon.ReloadUntil)
                    {
                        weapon.Ammo = weapon.Magazine; weapon.ReloadUntil = 0;
                        if (RelicPower(ChaseRelicKind.Quicksilver) > 0) weapon.ReloadBoostUntil = Elapsed + 2;
                    }
                for (int i = Fields.Count - 1; i >= 0; i--)
                {
                    var field = Fields[i];
                    if (Elapsed + Epsilon >= field.ExpiresAt) { RemoveField(field); continue; }
                    if (Elapsed + Epsilon < field.NextTick) continue;
                    TickField(field);
                }
                for (int i = burns.Count - 1; i >= 0; i--)
                {
                    var burn = burns[i];
                    if (!Hostile(burn.Enemy)) { burns.RemoveAt(i); continue; }
                    if (Elapsed + Epsilon >= burn.NextTick && burn.NextTick <= burn.Until + Epsilon)
                    {
                        DamageEnemy(burn.Enemy, 6 * burn.Owner.Damage / burn.Owner.BaseDamage, burn.Owner.Kind);
                        burn.NextTick += .5;
                    }
                    if (Elapsed + Epsilon >= burn.Until) burns.RemoveAt(i);
                }
                var expired = new List<ChaseEnemy>();
                foreach (var pair in bindings)
                {
                    var ally = pair.Key;
                    if (!ally.Alive || Elapsed + Epsilon >= ally.BoundUntil) { expired.Add(ally); continue; }
                    if (Elapsed + Epsilon < pair.Value.NextTick) continue;
                    var target = BoundTarget(ally);
                    if (target != null && Vector3.Distance(ally.Position, target.Position) <= 3)
                    {
                        DamageEnemy(target, pair.Value.Owner.Damage, ChaseWeaponKind.Pactbinder);
                        var strike = new ChaseAttack { Kind = ChaseWeaponKind.Pactbinder, CopyId = pair.Value.Owner.CopyId, Origin = ally.Position };
                        strike.Points.Add(target.Position); Attacks.Add(strike); TotalAttacks++;
                    }
                    pair.Value.NextTick += .8;
                }
                foreach (var ally in expired) bindings.Remove(ally);
            }
            if (Phase == ChasePhase.Playing) Elapsed = end;
            if (Health <= 0) Phase = ChasePhase.Defeat;
            else if (Elapsed + Epsilon >= 600) { Elapsed = 600; Phase = ChasePhase.Victory; }
        }
        private void MoveFields(float step, double next)
        {
            for (int i = Fields.Count - 1; i >= 0; i--)
            {
                var field = Fields[i]; var kind = field.Owner.Kind;
                var state = fieldStates[field];
                var before = field.Position;
                if (kind == ChaseWeaponKind.Cyclone) field.Position += field.Direction * (2.4f * step);
                else if (kind == ChaseWeaponKind.Winterglass) field.Position += field.Direction * (3.5f * step);
                else if (kind == ChaseWeaponKind.Magma && !state.BurningGround) field.Position += field.Direction * (5 * step);
                else if (kind == ChaseWeaponKind.OrbitHammer)
                {
                    float age = (float)(next - field.CreatedAt);
                    field.Position = state.Anchor + Quaternion.AngleAxis(age * 360, Vector3.up) * field.Direction * (1 + age * 3);
                }
                if (before == field.Position) continue;
                if (!VortexTravelClear(before, field.Position))
                {
                    field.Position = before;
                    if (kind == ChaseWeaponKind.Magma) BurstMagma(field);
                    else { RemoveField(field); continue; }
                }
                if (kind == ChaseWeaponKind.Magma && !state.BurningGround)
                {
                    foreach (var enemy in Enemies)
                        if (Hostile(enemy) && SegmentDistance(enemy.Position, before, field.Position) <= .9f
                            && ClearPath(before, enemy.Position)) { BurstMagma(field); break; }
                }
                if (kind == ChaseWeaponKind.OrbitHammer)
                {
                    int hitsBefore = TotalHits;
                    foreach (var enemy in Enemies)
                        if (Hostile(enemy) && ChaseAim.Intersect(enemy, before, (field.Position - before).normalized,
                            Vector3.Distance(before, field.Position), field.Radius, out var sweep)
                            && ClearPath(before, sweep.Point)
                            && (!state.HitReady.TryGetValue(enemy.Id, out double ready) || Elapsed + Epsilon >= ready))
                        {
                            DamageEnemy(enemy, field.Owner.Damage, kind);
                            state.HitReady[enemy.Id] = Elapsed + .5;
                        }
                    CreditProjectile(field, hitsBefore);
                }
            }
        }
        private void BurstMagma(ChaseField field)
        {
            var state = fieldStates[field];
            if (state.BurningGround) return;
            state.BurningGround = true; field.Direction = Vector3.zero;
            field.ExpiresAt = Elapsed + 3; field.NextTick = Elapsed + .5;
            int hitsBefore = TotalHits;
            foreach (var enemy in Enemies)
                if (Hostile(enemy) && InField(field, enemy)) DamageEnemy(enemy, field.Owner.Damage, ChaseWeaponKind.Magma);
            CreditProjectile(field, hitsBefore);
            var burst = new ChaseAttack { Kind = ChaseWeaponKind.Magma, CopyId = field.Owner.CopyId, Origin = field.Position };
            burst.Points.Add(field.Position); Attacks.Add(burst);
        }
        private void PullEnemies(float step)
        {
            foreach (var enemy in Enemies)
            {
                if (!Hostile(enemy)) continue;
                ChaseField strongest = null; float strength = 0, nearest = float.MaxValue;
                foreach (var field in Fields)
                {
                    float pull = field.Owner.Kind == ChaseWeaponKind.Cyclone ? 4 : field.Owner.Kind == ChaseWeaponKind.Abyss ? 1.2f / enemy.SizeScale : 0;
                    if (pull == 0) continue;
                    float distance = Vector3.Distance(enemy.Position, field.Position);
                    int tie = strongest == null ? -1 : field.Owner.CopyId.CompareTo(strongest.Owner.CopyId);
                    if (tie == 0) tie = field.CreatedAt.CompareTo(strongest.CreatedAt);
                    if (tie == 0) tie = field.Position.x.CompareTo(strongest.Position.x);
                    if (tie == 0) tie = field.Position.y.CompareTo(strongest.Position.y);
                    if (tie == 0) tie = field.Position.z.CompareTo(strongest.Position.z);
                    if ((strongest == null || pull > strength || (pull == strength && (distance < nearest
                        || (distance == nearest && tie < 0)))) && InField(field, enemy))
                    { strongest = field; strength = pull; nearest = distance; }
                }
                if (strongest == null) continue;
                var target = new Vector3(strongest.Position.x, enemy.Position.y, strongest.Position.z);
                var destination = Vector3.MoveTowards(enemy.Position, target, strength * step);
                if (ClearPath(enemy.Position, destination)) enemy.Position = destination;
            }
        }
        private void TickField(ChaseField field)
        {
            var weapon = field.Owner;
            var state = fieldStates[field];
            if (weapon.Kind == ChaseWeaponKind.Sentry)
            {
                var target = Nearest(field.Position, field.Radius);
                if (target != null)
                {
                    DamageEnemy(target, weapon.Damage, weapon.Kind);
                    var shot = new ChaseAttack { Kind = weapon.Kind, CopyId = weapon.CopyId, Origin = field.Position };
                    shot.Points.Add(target.Position); Attacks.Add(shot); TotalAttacks++;
                }
                field.NextTick += .65;
            }
            else if (weapon.Kind == ChaseWeaponKind.Winterglass)
            {
                int hitsBefore = TotalHits;
                var shards = new ChaseAttack { Kind = weapon.Kind, CopyId = weapon.CopyId, Origin = field.Position };
                var seen = new HashSet<int>();
                for (int i = 0; i < 8; i++)
                {
                    var ray = Quaternion.AngleAxis(i * 45, Vector3.up) * Vector3.forward;
                    var enemy = FirstOnRay(field.Position, ray, 5, .55f);
                    shards.Points.Add(enemy == null ? field.Position + ray * 5 : enemy.Position);
                    if (enemy != null && seen.Add(enemy.Id))
                    { DamageEnemy(enemy, weapon.Damage, weapon.Kind); enemy.SlowUntil = Elapsed + 2; }
                }
                Attacks.Add(shards); TotalAttacks++;
                field.NextTick += .35;
                CreditProjectile(field, hitsBefore);
            }
            else if (weapon.Kind == ChaseWeaponKind.Magma)
            {
                if (!state.BurningGround && Elapsed + .35 >= field.ExpiresAt) BurstMagma(field);
                else
                {
                    if (state.BurningGround)
                        foreach (var enemy in Enemies)
                            if (Hostile(enemy) && InField(field, enemy))
                                DamageEnemy(enemy, 6 * weapon.Damage / weapon.BaseDamage, weapon.Kind);
                    field.NextTick += state.BurningGround ? .5 : .35;
                }
            }
            else
            {
                if (weapon.Kind != ChaseWeaponKind.OrbitHammer)
                    foreach (var enemy in Enemies)
                        if (Hostile(enemy) && InField(field, enemy)) DamageEnemy(enemy, weapon.Damage, weapon.Kind);
                field.NextTick += weapon.Kind == ChaseWeaponKind.OrbitHammer ? .05 : weapon.Kind == ChaseWeaponKind.Abyss ? .5 : .35;
            }
        }

        public void CollectXp(int amount)
        {
            if (Phase != ChasePhase.Playing || amount <= 0) return;
            Xp += amount;
            if (Xp >= Threshold)
            {
                UpgradeSource = ChaseUpgradeSource.Level;
                Phase = ChasePhase.Upgrade; GenerateChoices();
            }
        }
        private void GenerateChoices(ChaseWeapon mastered = null)
        {
            Choices.Clear();
            var pool = new List<ChaseChoice>();
            foreach (var weapon in Weapons)
            {
                if (mastered != null && weapon != mastered) continue;
                foreach (ChaseUpgradeKind upgrade in Enum.GetValues(typeof(ChaseUpgradeKind)))
                    pool.Add(new ChaseChoice { Kind = weapon.Kind, CopyId = weapon.CopyId,
                        UpgradeKind = upgrade, Label = ChaseCatalog.Name(weapon.Kind) + " #" +
                        weapon.CopyId + (upgrade == ChaseUpgradeKind.Damage ? " damage +15% base" :
                            upgrade == ChaseUpgradeKind.Cadence ? " fire rate +7%" : " magazine +2") });
            }
            if (mastered == null)
                foreach (var relic in Relics)
                    pool.Add(new ChaseChoice { Relic = relic.Kind,
                        Label = ChaseRelics.Name(relic.Kind) + " empower effect" });
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int index = random.Next(pool.Count);
                Choices.Add(pool[index]); pool.RemoveAt(index);
            }
        }
        public bool Choose(int index)
        {
            if (Phase != ChasePhase.Upgrade || index < 0 || index >= Choices.Count) return false;

            var choice = Choices[index];
            var weapon = Weapons.Find(w => w.CopyId == choice.CopyId);
            if (choice.IsGrant) return false;
            if (choice.Relic.HasValue)
            {
                if (UpgradeSource != ChaseUpgradeSource.Level) return false;
                var relic = Relics.Find(r => r.Kind == choice.Relic.Value);
                if (relic == null) return false;
                relic.Rank++;
            }
            else if (weapon == null) return false;
            else switch (choice.UpgradeKind)
            {
                case ChaseUpgradeKind.Damage:
                    weapon.DamageRanks++;
                    weapon.Damage = weapon.BaseDamage * (1 + .15f * weapon.DamageRanks);
                    break;
                case ChaseUpgradeKind.Cadence:
                    weapon.CadenceRanks++;
                    weapon.Cadence /= 1.07f;
                    break;
                case ChaseUpgradeKind.Magazine:
                    weapon.MagazineRanks++;
                    weapon.Magazine += 2;
                    weapon.Ammo = Math.Min(weapon.Magazine, weapon.Ammo + 2);
                    break;
            }
            if (UpgradeSource == ChaseUpgradeSource.WeaponUse)
            {
                weapon.SuccessfulUses -= weapon.MasteryThreshold;
                weapon.MasteryAwards++;
            }
            else { Xp -= Threshold; Threshold += 6; Level++; }
            if (Xp >= Threshold)
            {
                UpgradeSource = ChaseUpgradeSource.Level;
                GenerateChoices();
            }
            else { Choices.Clear(); Phase = ChasePhase.Playing; }
            return true;
        }
    }
}
