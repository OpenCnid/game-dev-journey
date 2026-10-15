using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseProgressionITests
    {
        [Test]
        public void LevelChoicesImproveOwnedCopiesWithoutGrantingLoot()
        {
            var run = new ChaseRun(); run.Start();
            run.Grant(ChaseWeaponKind.BoneLance);
            int count = run.Weapons.Count;
            run.CollectXp(run.Threshold);
            Assert.That(run.Choices.Count, Is.EqualTo(3));
            Assert.That(run.Choices.All(c => !c.IsGrant &&
                run.Weapons.Any(w => w.CopyId == c.CopyId)), Is.True);
            Assert.That(run.Choose(0), Is.True);
            Assert.That(run.Weapons.Count, Is.EqualTo(count));
            Assert.That(run.Level, Is.EqualTo(2));
            Assert.That(run.Weapons.Sum(w => w.Rank), Is.EqualTo(1));
        }

        [Test]
        public void SuccessfulUseAwardsOnlyThatCopyWithoutConsumingPlayerLevel()
        {
            var run = new ChaseRun(); run.Start();
            var first = run.Weapons[0];
            run.Grant(ChaseWeaponKind.Stormsplit);
            var enemy = run.AddEnemy(Vector3.forward * 8, 100000);
            for (int use = 0; use < first.MasteryThreshold; use++)
            {
                first.Ammo = first.Magazine; first.ReadyAt = run.Elapsed;
                Assert.That(run.RequestAttack(0, Vector3.zero, Vector3.forward,
                    enemy.Id, null, true), Is.True);
            }
            Assert.That(run.Phase, Is.EqualTo(ChasePhase.Upgrade));
            Assert.That(run.UpgradeSource, Is.EqualTo(ChaseUpgradeSource.WeaponUse));
            Assert.That(run.Choices.All(c => c.CopyId == first.CopyId && !c.IsGrant), Is.True);
            Assert.That(run.Choose(0), Is.True);
            Assert.That(first.MasteryAwards, Is.EqualTo(1));
            Assert.That(first.SuccessfulUses, Is.Zero);
            Assert.That(run.Level, Is.EqualTo(1));
            Assert.That(run.Xp, Is.Zero);
            Assert.That(run.Weapons[1].Rank, Is.Zero);
        }

        [Test]
        public void MissesAndDirectPassiveDamageCannotFarmMastery()
        {
            var run = new ChaseRun(); run.Start();
            var weapon = run.Weapons[0];
            var enemy = run.AddEnemy(Vector3.back * 8, 100000);
            for (int use = 0; use < 60; use++)
            {
                weapon.Ammo = weapon.Magazine; weapon.ReadyAt = run.Elapsed;
                Assert.That(run.RequestAttack(0, Vector3.zero, Vector3.forward,
                    -1, null, true), Is.True);
                run.DamageEnemy(enemy, 1);
            }
            Assert.That(weapon.SuccessfulUses, Is.Zero);
            Assert.That(run.Phase, Is.EqualTo(ChasePhase.Playing));
        }

        [Test]
        public void WeaponProgressAndMasteryAreFreshAfterReset()
        {
            var run = new ChaseRun(); run.Start();
            run.Weapons[0].SuccessfulUses = 17;
            run.CollectXp(run.Threshold); run.Choose(0);
            run.Reset();
            Assert.That(run.Level, Is.EqualTo(1));
            Assert.That(run.Weapons.Count, Is.EqualTo(1));
            Assert.That(run.Weapons[0].SuccessfulUses, Is.Zero);
            Assert.That(run.Weapons[0].MasteryAwards, Is.Zero);
            Assert.That(run.Weapons[0].Rank, Is.Zero);
        }
    }
}
