using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChasePacingBalanceITests
    {
        [TestCase(8, .35, 8)]
        [TestCase(30, .65, 20)]
        [TestCase(60, 1.2, 40)]
        [TestCase(120, 3, 120)]
        public void OpeningHasHigherRealEmissionAndPopulationThanH(double time, double hRate, int hCap)
        {
            Assert.That(ChaseCalibration.SpawnRateAt(time), Is.GreaterThan(hRate * 2));
            Assert.That(ChaseCalibration.ActiveCapAt(time), Is.GreaterThan(hCap));
        }

        [Test]
        public void HeavyPrecisionWeaponActuallyOutdamagesCommonChainPrimary()
        {
            float common = Damage(ChaseWeaponKind.Stormsplit, ChaseEnemyKind.Demon);
            float heavy = Damage(ChaseWeaponKind.BoneLance, ChaseEnemyKind.Demon);
            Assert.That(heavy, Is.GreaterThan(common * 3));
            Assert.That(ChaseCatalog.Tier(ChaseWeaponKind.BoneLance),
                Is.GreaterThan(ChaseCatalog.Tier(ChaseWeaponKind.Stormsplit)));
        }

        [Test]
        public void HolyWeaponHasActualUndeadMatchupAdvantage()
        {
            Assert.That(Damage(ChaseWeaponKind.Sanctifier, ChaseEnemyKind.Undead),
                Is.EqualTo(Damage(ChaseWeaponKind.Sanctifier, ChaseEnemyKind.Demon) * 1.5f).Within(.001f));
        }

        static float Damage(ChaseWeaponKind weapon, ChaseEnemyKind kind)
        {
            var run = new ChaseRun(); run.Reset(41, weapon); run.Start();
            var enemy = run.AddEnemy(Vector3.forward * 8, 1000, kind);
            enemy.Position = Vector3.forward * 8 + Vector3.up * enemy.BodyOffset;
            Vector3 origin = new Vector3(0, 1.6f, 0);
            Assert.That(run.RequestAttack(0, origin, (enemy.Position - origin).normalized,
                enemy.Id, null, true), Is.True);
            return 1000 - enemy.Health;
        }
    }
}
