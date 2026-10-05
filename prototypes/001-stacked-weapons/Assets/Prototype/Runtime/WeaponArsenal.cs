using System;
using UnityEngine;

namespace Journey
{
    public enum WeaponSlot { Rifle, Scattergun, Piercer }

    // Three explicit effect consumers, not a generic modifier engine. Slot installation and
    // enhancement stay separate so later compatible additions can open their own upgrade pool.
    public static class WeaponArsenal
    {
        public const int Rifle = 1, Scattergun = 2, Piercer = 4, All = 7;
        public static int Id(int player, WeaponSlot slot) => (int)slot * 2 + player + 1;
        public static WeaponSlot Slot(int id) => (WeaponSlot)((id - 1) / 2);
        public static bool Owns(SharedPlayer player, int owner, int id) => id >= 1 && id <= 6 &&
            (id - 1) % 2 == owner && (player.Arsenal & (1 << (int)Slot(id))) != 0;
        public static int AddedMask(UpgradeId id) => id == UpgradeId.AddScattergun ? Scattergun : id == UpgradeId.AddPiercer ? Piercer : 0;
        public static UpgradeId Missing(UpgradeId first) => first == UpgradeId.AddScattergun ? UpgradeId.AddPiercer : UpgradeId.AddScattergun;
        public static UpgradeId Enhancement(UpgradeId first) => first == UpgradeId.AddScattergun ? UpgradeId.WiderScatter : UpgradeId.DeeperPierce;
        public static string Name(WeaponSlot slot) => slot == WeaponSlot.Rifle ? "Rifle" : slot == WeaponSlot.Scattergun ? "Scattergun" : "Piercer";
        public static string Label(int mask) => mask == All ? "Rifle + scattergun + piercer" : mask == (Rifle | Scattergun) ? "Rifle + scattergun" : mask == (Rifle | Piercer) ? "Rifle + piercer" : "Rifle";
        public static float Interval(SharedPlayer p, WeaponSlot slot) => slot == WeaponSlot.Rifle ? p.RifleInterval : slot == WeaponSlot.Scattergun ? .75f : .65f;
        public static float Range(WeaponSlot slot) => slot == WeaponSlot.Scattergun ? 30 : CombatSimulation.RifleRange;
        public static int Damage(SharedPlayer p, WeaponSlot slot) => slot == WeaponSlot.Rifle ? p.BodyDamage : slot == WeaponSlot.Scattergun ? 12 : 16;
        public static int Rays(SharedPlayer p, WeaponSlot slot) => slot == WeaponSlot.Scattergun ? (p.Specialization == UpgradeId.WiderScatter ? 7 : 5) : 1;
        public static int PierceLimit(SharedPlayer p) => p.Specialization == UpgradeId.DeeperPierce ? 5 : 3;
        public static Ray Fan(Ray aim, int index, int count) => new Ray(aim.origin,
            Quaternion.AngleAxis(count <= 1 ? 0 : Mathf.Lerp(-8, 8, index / (float)(count - 1)), Vector3.up) * aim.direction);

        // One body/head winner per enemy, sorted by distance then stable slot index.
        // Caller-owned storage avoids allocations per trigger; snapshot is immutable during resolution.
        public static int Query(SharedSnapshot pose, Ray ray, float range, AimResult[] hits)
        {
            if (!EnemyGeometry.ValidRay(ray)) return 0;
            int count = 0;
            for (int i = 0; i < pose.Enemies.Length; i++)
            {
                SharedEnemy enemy = pose.Enemies[i]; bool wounded = enemy.Health < pose.Configuration.EnemyHealth;
                bool body = EnemyGeometry.RayCube(ray, EnemyGeometry.Body(enemy.Position, wounded), out float b);
                bool head = EnemyGeometry.RayCube(ray, EnemyGeometry.Head(enemy.Position, wounded), out float h);
                if (!body && !head) continue;
                bool critical = head && (!body || h <= b); float distance = critical ? h : b;
                if (distance > range) continue;
                var hit = new AimResult(i, enemy.Incarnation, critical ? HitRegion.Head : HitRegion.Body, distance);
                int insert = count++;
                while (insert > 0 && hits[insert - 1].Distance > distance) { hits[insert] = hits[insert - 1]; insert--; }
                hits[insert] = hit;
            }
            return count;
        }
        public static void Apply(ref SharedPlayer player, UpgradeId upgrade)
        {
            int added = AddedMask(upgrade);
            if (added != 0)
            { player.Arsenal |= added; if (player.FirstAddition == UpgradeId.None) player.FirstAddition = upgrade; }
            else if (upgrade == Enhancement(player.FirstAddition)) player.Specialization = upgrade;
            else throw new ArgumentException("Upgrade has no eligible arsenal consumer");
        }
        public static bool ValidBuild(SharedSnapshot snapshot, int p)
        {
            SharedPlayer player = snapshot.Players[p]; PersonalOffer offer = player.Offer;
            if (snapshot.Xp < 0 || snapshot.Level < 1 || snapshot.Level > 3 || player.BodyDamage != 20 || player.RifleInterval != .12f) return false;
            if (snapshot.Level == 1) return snapshot.Xp < 10 && offer.Id == 0 && offer.Selected == UpgradeId.None && !player.ChoiceReady && player.Arsenal == Rifle && player.FirstAddition == UpgradeId.None && player.Specialization == UpgradeId.None;
            if (snapshot.Xp < EarnedGrowth.ThresholdFor(snapshot.Level) || offer.Id != (snapshot.Level - 2) * 2 + p + 1 ||
                offer.Weapon != player.Weapon || offer.Tick < 1 || offer.Tick > snapshot.Tick ||
                offer.Selected == UpgradeId.None && (offer.AppliedTick != 0 || offer.ControlRevision != 0 || player.ChoiceReady) ||
                offer.Selected != UpgradeId.None && (!offer.Contains(offer.Selected) || offer.AppliedTick < offer.Tick || offer.AppliedTick > snapshot.Tick || offer.ControlRevision < 1 || offer.ControlRevision > snapshot.ControlRevision)) return false;
            UpgradeId a = snapshot.Level == 2 ? UpgradeId.AddScattergun : Missing(player.FirstAddition);
            UpgradeId b = snapshot.Level == 2 ? UpgradeId.AddPiercer : Enhancement(player.FirstAddition);
            if (!(offer.First == a && offer.Second == b || offer.First == b && offer.Second == a)) return false;
            if (snapshot.Level == 2 && offer.Pending) return player.Arsenal == Rifle && player.FirstAddition == UpgradeId.None && player.Specialization == UpgradeId.None;
            if (AddedMask(player.FirstAddition) == 0) return false;
            if (snapshot.Level == 2 && player.FirstAddition != offer.Selected) return false;
            int expected = Rifle | AddedMask(player.FirstAddition);
            UpgradeId specialization = UpgradeId.None;
            if (snapshot.Level == 3 && !offer.Pending)
            { if (AddedMask(offer.Selected) != 0) expected = All; else specialization = Enhancement(player.FirstAddition); }
            return player.Arsenal == expected && player.Specialization == specialization;
        }
    }

    [Serializable] public sealed class WeaponWork
    {
        public int weapon, player, triggers, rays, targetTests, hits, damage, kills, rejected;
        public string name;
        public WeaponWork(int id) { weapon = id; player = (id - 1) % 2; name = WeaponArsenal.Name(WeaponArsenal.Slot(id)); }
        public void Record(SharedHit hit)
        {
            if (hit.Trigger) triggers++;
            rays += hit.Rays; targetTests += hit.TargetTests;
            if (hit.Decision == ShotDecision.Damage) { hits++; damage += hit.Shot.Damage; if (hit.Shot.Killed) kills++; }
            else if (hit.Decision != ShotDecision.Miss) rejected++;
        }
    }
}
