using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseRelicsITests
    {
        static ChaseRun Equipped(ChaseRelicKind relic)
        {
            var run = new ChaseRun(); run.Start();
            var drop = run.DropLoot(Vector3.zero, ChaseLootKind.Relic, relic: relic);
            Assert.That(run.CollectLoot(drop.Id), Is.True);
            return run;
        }
        static void Headshot(ChaseRun run)
        {
            var target = run.AddEnemy(Vector3.forward * 8, 1000);
            target.Position = Vector3.forward * 8 + Vector3.up * target.BodyOffset;
            var origin = new Vector3(0, 1.6f, 0);
            Assert.That(run.RequestAttack(0, origin, (target.HeadPosition - origin).normalized,
                target.Id, null, true), Is.True);
        }
        [Test]
        public void ExecutionerReturnsARealRoundForHeadButNotBodyOrPassiveHit()
        {
            var run = Equipped(ChaseRelicKind.Executioner);
            int rounds = run.Weapons[0].Ammo;
            Headshot(run);
            Assert.That(run.Weapons[0].Ammo, Is.EqualTo(rounds));
            run.DamageEnemy(run.Enemies[0], 2);
            Assert.That(run.Weapons[0].Ammo, Is.EqualTo(rounds));
        }
        [Test]
        public void BloodGlassRestoresRealMissingHealthOnAnatomicalHeadContact()
        {
            var run = Equipped(ChaseRelicKind.BloodGlass);
            run.TakeHazardDamage(30); int health = run.Health;
            Headshot(run);
            Assert.That(run.Health, Is.GreaterThan(health));
        }
        [Test]
        public void VolatileHeartChangesActualLowHealthDamageAndContactCost()
        {
            var run = Equipped(ChaseRelicKind.VolatileHeart);
            run.TakeHazardDamage(60);
            Headshot(run);
            Assert.That(run.Hits[0].Damage, Is.EqualTo(run.Weapons[0].Damage * 2 * 1.4f).Within(.001f));
            run.Advance((float)ChaseCalibration.DamageCooldownSeconds + .01f);
            int expected = Mathf.RoundToInt(ChaseCalibration.ContactDamageAt(
                run.Elapsed, run.DamageTakenCount) * 1.2f);
            Assert.That(run.Contact(), Is.True);
            Assert.That(run.LastDamageAmount, Is.EqualTo(expected));
        }
        [Test]
        public void VolatileHeartDoesNotIncreaseContactCostAtHighHealth()
        {
            var run = Equipped(ChaseRelicKind.VolatileHeart);
            int expected = ChaseCalibration.ContactDamageAt(run.Elapsed, run.DamageTakenCount);
            Assert.That(run.Contact(), Is.True);
            Assert.That(run.LastDamageAmount, Is.EqualTo(expected));
        }
        [Test]
        public void EchoSealAddsOneReducedNearbyHitOnTheThirdAimedCast()
        {
            var run = Equipped(ChaseRelicKind.EchoSeal);
            var first = run.AddEnemy(Vector3.forward * 8, 1000);
            first.Position += Vector3.up * first.BodyOffset;
            var nearby = run.AddEnemy(new Vector3(2, first.Position.y, 8), 1000);
            var weapon = run.Weapons[0];
            var origin = new Vector3(0, 1.6f, 0);
            for (int cast = 0; cast < 3; cast++)
            {
                weapon.ReadyAt = run.Elapsed;
                Assert.That(run.RequestAttack(0, origin, (first.HeadPosition - origin).normalized,
                    first.Id, null, true), Is.True);
            }
            Assert.That(1000 - nearby.Health, Is.EqualTo(weapon.Damage * (3 * .8f + .35f)).Within(.001f));
            Assert.That(weapon.SuccessfulUses, Is.EqualTo(3));
        }
        [Test]
        public void QuicksilverShortensReloadAndActuallyBoostsTheNextAttack()
        {
            var run = Equipped(ChaseRelicKind.Quicksilver);
            var weapon = run.Weapons[0]; weapon.Ammo--;
            run.ReloadAll();
            Assert.That(weapon.ReloadUntil, Is.LessThan(1.6));
            run.Advance((float)weapon.ReloadUntil + .01f);
            Assert.That(weapon.Ammo, Is.EqualTo(weapon.Magazine));
            Headshot(run);
            Assert.That(run.Hits[0].Damage, Is.GreaterThan(weapon.Damage * 2));
        }
        [Test]
        public void RelicPowerGrowsWithPlayerLevelsAndResetRemovesInventory()
        {
            var run = Equipped(ChaseRelicKind.Windstep);
            float power = run.RelicPower(ChaseRelicKind.Windstep);
            run.CollectXp(run.Threshold); run.Choose(0);
            Assert.That(run.RelicPower(ChaseRelicKind.Windstep), Is.GreaterThan(power));
            run.Pause(); double time = run.Elapsed;
            run.Advance(5); Assert.That(run.Elapsed, Is.EqualTo(time));
            run.Reset(); Assert.That(run.Relics, Is.Empty);
        }
    }
}
