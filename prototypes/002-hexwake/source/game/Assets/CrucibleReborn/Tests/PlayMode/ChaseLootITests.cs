using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseLootITests
    {
        [Test]
        public void GuaranteedEarlyEnemyLootIsARealNewBaseCopy()
        {
            var run = new ChaseRun(); run.Start();
            var owned = run.Weapons[0]; owned.DamageRanks = 3;
            owned.Damage = owned.BaseDamage * 1.45f;
            for (int kill = 0; kill < 3; kill++)
                run.DamageEnemy(run.AddEnemy(Vector3.forward * (kill + 4), 1), 1);
            Assert.That(run.Loot.Count, Is.GreaterThanOrEqualTo(1));
            var loot = run.Loot[0];
            Assert.That(loot.Kind, Is.EqualTo(ChaseLootKind.Weapon));
            Assert.That(run.CollectLoot(loot.Id), Is.True);
            Assert.That(run.Weapons.Count, Is.EqualTo(2));
            Assert.That(run.Weapons[1].CopyId, Is.Not.EqualTo(owned.CopyId));
            Assert.That(run.Weapons[1].Rank, Is.Zero);
            Assert.That(owned.DamageRanks, Is.EqualTo(3));
            Assert.That(run.Level, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateDropDoesNotMergeOrUpgradeExistingWeapon()
        {
            var run = new ChaseRun(); run.Start();
            var original = run.Weapons[0];
            var drop = run.DropLoot(Vector3.zero, ChaseLootKind.Weapon, original.Kind);
            Assert.That(run.CollectLoot(drop.Id), Is.True);
            Assert.That(run.Weapons.Count, Is.EqualTo(2));
            Assert.That(run.Weapons[0].Rank, Is.Zero);
            Assert.That(run.Weapons[1].Rank, Is.Zero);
            Assert.That(run.Weapons[1].CopyId, Is.Not.EqualTo(original.CopyId));
        }

        [Test]
        public void FullArsenalRequiresExplicitSelectedCopyReplacement()
        {
            var run = new ChaseRun(); run.Start();
            while (run.Weapons.Count < 6) run.Grant(ChaseWeaponKind.Frenzy);
            int preserved = run.Weapons[0].CopyId;
            int replaced = run.Weapons[2].CopyId;
            var drop = run.DropLoot(Vector3.zero, ChaseLootKind.Weapon, ChaseWeaponKind.BoneLance);
            Assert.That(run.CollectLoot(drop.Id), Is.False);
            Assert.That(run.Loot.Count, Is.EqualTo(1));
            Assert.That(run.CollectLoot(drop.Id, replaced), Is.True);
            Assert.That(run.Weapons.Count, Is.EqualTo(6));
            Assert.That(run.Weapons[0].CopyId, Is.EqualTo(preserved));
            Assert.That(run.Weapons[2].Kind, Is.EqualTo(ChaseWeaponKind.BoneLance));
            Assert.That(run.Weapons[2].Rank, Is.Zero);
        }

        [Test]
        public void RelicLootAndLevelsNeverAddWeaponDamageRanks()
        {
            var run = new ChaseRun(); run.Start();
            var drop = run.DropLoot(Vector3.zero, ChaseLootKind.Relic, relic: ChaseRelicKind.Windstep);
            Assert.That(run.CollectLoot(drop.Id), Is.True);
            Assert.That(run.RelicPower(ChaseRelicKind.Windstep), Is.EqualTo(1));
            run.CollectXp(run.Threshold);
            Assert.That(run.Choose(0), Is.True);
            Assert.That(run.RelicPower(ChaseRelicKind.Windstep), Is.GreaterThan(1));
            run.Reset();
            Assert.That(run.Relics, Is.Empty);
            Assert.That(run.Loot, Is.Empty);
        }
    }
}
