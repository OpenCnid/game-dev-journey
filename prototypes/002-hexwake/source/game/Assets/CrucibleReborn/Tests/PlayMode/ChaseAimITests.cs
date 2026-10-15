using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseAimITests
    {
        static ChaseRun Scene(ChaseWeaponKind kind, out ChaseEnemy enemy)
        {
            var run = new ChaseRun(); run.Reset(17, kind); run.Start();
            enemy = run.AddEnemy(Vector3.forward * 8, 1000);
            enemy.Position = Vector3.forward * 8 + Vector3.up * enemy.BodyOffset;
            return run;
        }

        [TestCase(ChaseWeaponKind.Stormsplit)]
        [TestCase(ChaseWeaponKind.Frenzy)]
        [TestCase(ChaseWeaponKind.Graveburst)]
        [TestCase(ChaseWeaponKind.Pactbinder)]
        [TestCase(ChaseWeaponKind.BoneLance)]
        [TestCase(ChaseWeaponKind.Sanctifier)]
        public void RealHeadContactDealsDoubleDirectBodyDamage(ChaseWeaponKind kind)
        {
            var body = Scene(kind, out var torso);
            var head = Scene(kind, out var skull);
            Vector3 origin = new Vector3(0, 1.6f, 0);
            Assert.That(body.RequestAttack(0, origin, (torso.Position - origin).normalized,
                torso.Id, null, true), Is.True);
            Assert.That(head.RequestAttack(0, origin, (skull.HeadPosition - origin).normalized,
                skull.Id, null, true), Is.True);
            float bodyDamage = 1000 - torso.Health, headDamage = 1000 - skull.Health;
            Assert.That(bodyDamage, Is.GreaterThan(0));
            Assert.That(headDamage, Is.EqualTo(bodyDamage * 2).Within(.001f));
            Assert.That(body.Hits[0].Critical, Is.False);
            Assert.That(head.Hits[0].Critical, Is.True);
            Assert.That(Vector3.Distance(head.Hits[0].Position, skull.HeadPosition),
                Is.LessThanOrEqualTo(skull.HeadRadius + .05f));
        }

        [Test]
        public void StormsplitCannotAcquireTenDegreeOffCrosshairTarget()
        {
            var run = Scene(ChaseWeaponKind.Stormsplit, out var enemy);
            Vector3 origin = new Vector3(0, enemy.Position.y, 0);
            Vector3 away = Quaternion.Euler(0, 10, 0) * Vector3.forward;
            int ammo = run.Weapons[0].Ammo;
            Assert.That(run.RequestAttack(0, origin, away, enemy.Id, null, false), Is.False);
            Assert.That(run.Weapons[0].Ammo, Is.EqualTo(ammo));
            Assert.That(enemy.Health, Is.EqualTo(1000));
            Assert.That(run.RequestAttack(0, origin, away, enemy.Id, null, true), Is.True);
            Assert.That(run.Weapons[0].Ammo, Is.EqualTo(ammo - 1));
            Assert.That(enemy.Health, Is.EqualTo(1000));
            Assert.That(run.Weapons[0].SuccessfulUses, Is.Zero);
        }
        [Test]
        public void ExpandingHammerSweepContactsTheAimedVisibleBodyNotOnlyItsCenterPoint()
        {
            var run = Scene(ChaseWeaponKind.OrbitHammer, out var enemy);
            enemy.Position = Vector3.forward * 4 + Vector3.up * enemy.BodyOffset;
            var camera = new Vector3(0, 1.6f, 0);
            var muzzle = new Vector3(.25f, 1.38f, 1.02f);
            Assert.That(run.RequestAttack(0, muzzle, (enemy.HeadPosition - muzzle).normalized,
                enemy.Id, null, true, camera, (enemy.HeadPosition - camera).normalized), Is.True);
            run.Advance(1.2f);
            Assert.That(enemy.Health, Is.LessThan(1000));
            Assert.That(run.Weapons[0].SuccessfulUses, Is.EqualTo(1));
            Assert.That(run.Hits.TrueForAll(hit => !hit.Critical), Is.True);
        }

        [Test]
        public void HeadMissAndBlockedCameraOrMuzzleCannotFabricateCrit()
        {
            var run = Scene(ChaseWeaponKind.Stormsplit, out var enemy);
            Vector3 origin = new Vector3(0, 1.6f, 0);
            Vector3 miss = enemy.HeadPosition + Vector3.right * (enemy.HeadRadius + .2f);
            Assert.That(run.RequestAttack(0, origin, (miss - origin).normalized,
                enemy.Id, null, true), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(1000));
            run.Weapons[0].ReadyAt = run.Elapsed;
            run.ClearPath = (a, b) => false;
            Assert.That(run.RequestAttack(0, origin, (enemy.HeadPosition - origin).normalized,
                enemy.Id, null, false), Is.False);
            Assert.That(run.Hits, Is.Empty);
        }

        [Test]
        public void HeadshotBonusDoesNotSpreadToChainOrPassiveTicks()
        {
            var run = Scene(ChaseWeaponKind.Stormsplit, out var first);
            var second = run.AddEnemy(new Vector3(2, first.Position.y, 8), 1000);
            Vector3 origin = new Vector3(0, 1.6f, 0);
            Assert.That(run.RequestAttack(0, origin, (first.HeadPosition - origin).normalized,
                first.Id, null, true), Is.True);
            Assert.That(run.Hits.Single(h => h.Enemy == first).Critical, Is.True);
            Assert.That(run.Hits.Single(h => h.Enemy == second).Critical, Is.False);
            Assert.That(1000 - second.Health, Is.EqualTo(run.Weapons[0].Damage * .8f).Within(.001f));
            run.DamageEnemy(second, 4, ChaseWeaponKind.Inferno);
            Assert.That(run.Hits.Last().Critical, Is.False);
        }
    }
}
