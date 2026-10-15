using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.FantasyTrialB.Tests
{
    public sealed class FantasyRunTests
    {
        private FantasyRun run;
        private static readonly Vector3 Origin = Vector3.zero;
        private static readonly Vector3 Aim = Vector3.forward;

        [SetUp]
        public void SetUp() { run = new FantasyRun(); run.Start(); }

        private bool Fire(int copy, int enemy = 0, bool manual = false, Vector3? ground = null)
        {
            return run.RequestAttack(copy, Origin, Aim, enemy, ground, manual);
        }

        private void Wind(int copy, Vector3 point)
        {
            Assert.That(Fire(copy, manual: true, ground: point), Is.True);
        }


        [TestCase(-60, 42, 2, 2.4)]
        [TestCase(0, 42, 2, 2.4)]
        [TestCase(300, 84, 2.325, 1.56)]
        [TestCase(600, 126, 2.65, 0.72)]
        [TestCase(900, 126, 2.65, 0.72)]
        public void PressureScalingUsesClampedSurvivalTime(double seconds, float health, float speed, double interval)
        {
            Assert.That(FantasyRun.EnemyHealthAt(seconds), Is.EqualTo(health).Within(0.00001));
            Assert.That(FantasyRun.EnemySpeedAt(seconds), Is.EqualTo(speed).Within(0.00001));
            Assert.That(FantasyRun.SpawnIntervalAt(seconds), Is.EqualTo(interval).Within(0.000001));
        }

        [Test]
        public void SpawnAttributesRemainSnapshotsDuringEngagementAndFrozenPhases()
        {
            var opening = run.AddEnemy(Aim);
            run.DamageEnemy(opening, 18);
            run.Advance(300);
            var middle = run.AddEnemy(Aim);
            var diagnostic = run.AddEnemy(Aim, 7);
            Assert.That(opening.Health, Is.EqualTo(24));
            Assert.That(opening.MaxHealth, Is.EqualTo(42));
            Assert.That(opening.Speed, Is.EqualTo(2));
            Assert.That(opening.SpawnTime, Is.Zero);
            Assert.That(middle.Health, Is.EqualTo(84));
            Assert.That(middle.MaxHealth, Is.EqualTo(84));
            Assert.That(middle.Speed, Is.EqualTo(2.325f));
            Assert.That(middle.SpawnTime, Is.EqualTo(300));
            Assert.That(diagnostic.Health, Is.EqualTo(7));
            Assert.That(diagnostic.MaxHealth, Is.EqualTo(7));
            Assert.That(diagnostic.Speed, Is.EqualTo(middle.Speed));
            run.Pause(); run.Advance(100); run.Resume();
            run.CollectXp(12); run.Advance(100);
            Assert.That(run.Elapsed, Is.EqualTo(300));
            Assert.That(FantasyRun.SpawnIntervalAt(run.Elapsed), Is.EqualTo(1.56).Within(0.000001));
            Assert.That(run.Choose(0), Is.True);
            run.Advance(300);
            var end = run.AddEnemy(Aim);
            Assert.That(end.MaxHealth, Is.EqualTo(126));
            Assert.That(end.Speed, Is.EqualTo(2.65f));
            Assert.That(end.SpawnTime, Is.EqualTo(600));
            Assert.That(middle.Health, Is.EqualTo(84));
            Assert.That(middle.Speed, Is.EqualTo(2.325f));
        }

        [TestCase(WeaponKind.Pistol, 18, 24, 0.42f)]
        [TestCase(WeaponKind.Ember, 10, 8, 1f)]
        [TestCase(WeaponKind.Whirlwind, 3, 18, 1.8f)]
        [TestCase(WeaponKind.Spark, 10, 15, 1.1f)]
        public void ProfilesAndFreshCopiesUseFrozenBaseValues(WeaponKind kind, float damage, float reach, float cadence)
        {
            run.Grant(kind);
            var weapon = run.Weapons[1];
            Assert.That(weapon.BaseDamage, Is.EqualTo(damage));
            Assert.That(weapon.Damage, Is.EqualTo(damage));
            Assert.That(weapon.DamageRanks, Is.Zero);
            Assert.That(weapon.Reach, Is.EqualTo(reach));
            Assert.That(weapon.Cadence, Is.EqualTo(cadence));
            weapon.DamageRanks = 3;
            weapon.Damage = damage * 1.3f;
            run.Grant(kind);
            Assert.That(run.Weapons[2].BaseDamage, Is.EqualTo(damage));
            Assert.That(run.Weapons[2].Damage, Is.EqualTo(damage));
            Assert.That(run.Weapons[2].DamageRanks, Is.Zero);
        }

        [Test]
        public void RepeatedDamageCardsAddRanksOfBaseWithoutCompoundingOrTouchingOtherCopies()
        {
            run.Grant(WeaponKind.Ember); run.Grant(WeaponKind.Whirlwind); run.Grant(WeaponKind.Spark);
            run.Grant(WeaponKind.Pistol); run.Grant(WeaponKind.Ember);
            var ranks = new Dictionary<int, int>();
            foreach (var weapon in run.Weapons) ranks.Add(weapon.CopyId, 0);
            for (int i = 0; i < 12; i++)
            {
                run.CollectXp(run.Threshold);
                var choice = run.Choices[0];
                Assert.That(choice.IsGrant, Is.False);
                ranks[choice.CopyId]++;
                Assert.That(run.Choose(0), Is.True);
                foreach (var weapon in run.Weapons)
                {
                    Assert.That(weapon.DamageRanks, Is.EqualTo(ranks[weapon.CopyId]));
                    Assert.That(weapon.Damage, Is.EqualTo(weapon.BaseDamage * (1 + 0.1f * ranks[weapon.CopyId])).Within(0.00001));
                }
            }
            Assert.That(run.Weapons.Exists(w => w.DamageRanks >= 2), Is.True);
            run.Reset();
            Assert.That(run.Weapons[0].BaseDamage, Is.EqualTo(18));
            Assert.That(run.Weapons[0].DamageRanks, Is.Zero);
        }

        [Test]
        public void MuzzleObstructionAndRangeOverrideCameraVisibilityForPistolAndEmber()
        {
            run.Grant(WeaponKind.Ember);
            var muzzle = new Vector3(1, 0, 0);
            var enemy = run.AddEnemy(new Vector3(1, 0, 4));
            run.ClearPath = (a, b) => a != muzzle;
            Assert.That(run.RequestAttack(0, muzzle, Aim, enemy.Id, null, false, Origin, Aim), Is.False);
            Assert.That(run.RequestAttack(1, muzzle, Aim, enemy.Id, null, false, Origin, Aim), Is.False);
            Assert.That(enemy.Health, Is.EqualTo(42));
            run.ClearPath = (a, b) => a == muzzle;
            Assert.That(run.RequestAttack(0, muzzle, Aim, enemy.Id, null, false, Origin, Vector3.left), Is.True);
            Assert.That(run.RequestAttack(1, muzzle, Aim, enemy.Id, null, false, Origin, Vector3.left), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(14));
            Assert.That(run.Attacks[0].Origin, Is.EqualTo(muzzle));
            Assert.That(run.Attacks[1].Origin, Is.EqualTo(muzzle));
            run.Weapons[0].ReadyAt = run.Elapsed;
            enemy.Position = new Vector3(0, 0, 23);
            Assert.That(run.RequestAttack(0, new Vector3(0, 0, -2), Aim, enemy.Id, null, false, Origin, Aim), Is.False);
        }

        [Test]
        public void SparkCameraAngularAimAndHopPreferenceRemainSeparateFromMuzzleClearPaths()
        {
            run.Grant(WeaponKind.Spark);
            var muzzle = new Vector3(4, 0, 0);
            var first = run.AddEnemy(new Vector3(0, 0, 10));
            var cameraAligned = run.AddEnemy(new Vector3(0, 0, 15));
            var muzzleAligned = run.AddEnemy(new Vector3(4, 0, 10));
            run.ClearPath = (a, b) => a != muzzle;
            Assert.That(run.RequestAttack(1, muzzle, Aim, 0, null, false, Origin, Aim), Is.False);
            run.ClearPath = (a, b) => true;
            Assert.That(run.RequestAttack(1, muzzle, Vector3.right, 0, null, false, Origin, Aim), Is.True);
            Assert.That(run.Attacks[0].Origin, Is.EqualTo(muzzle));
            Assert.That(run.Attacks[0].Points, Is.EqualTo(new[] { first.Position, cameraAligned.Position }));
            Assert.That(first.Health, Is.EqualTo(32));
            Assert.That(cameraAligned.Health, Is.EqualTo(32));
            Assert.That(muzzleAligned.Health, Is.EqualTo(42));
        }

        [Test]
        public void EmberHalfAngleAndWindRadiusUseBProfileBoundaries()
        {
            run.Grant(WeaponKind.Ember);
            var inside = run.AddEnemy(new Vector3(Mathf.Sin(19 * Mathf.Deg2Rad), 0, Mathf.Cos(19 * Mathf.Deg2Rad)) * 4);
            var outside = run.AddEnemy(new Vector3(Mathf.Sin(21 * Mathf.Deg2Rad), 0, Mathf.Cos(21 * Mathf.Deg2Rad)) * 4);
            Assert.That(Fire(1, inside.Id), Is.True);
            Assert.That(inside.Health, Is.EqualTo(32));
            Assert.That(outside.Health, Is.EqualTo(42));
            run.Grant(WeaponKind.Whirlwind);
            Wind(2, new Vector3(0, 0, 5));
            var vortex = run.Vortices[0];
            Assert.That(vortex.ExpiresAt, Is.EqualTo(2.8));
            Assert.That(vortex.NextTick, Is.EqualTo(0.5));
            run.Advance(0.25f);
            Assert.That(vortex.Position.z, Is.EqualTo(5.6f).Within(0.00001));
            vortex.Direction = Vector3.zero;
            var near = run.AddEnemy(vortex.Position + Vector3.right * 1.69f, 100);
            var far = run.AddEnemy(vortex.Position + Vector3.right * 1.71f, 100);
            run.ClearPath = (a, b) => a != near.Position && a != far.Position;
            run.Advance(0.25f);
            Assert.That(near.Health, Is.EqualTo(97));
            Assert.That(far.Health, Is.EqualTo(100));
        }

        [Test]
        public void PistolOnlyHitsAimedLivingClearTargetAndMissSpendsCadence()
        {
            var target = run.AddEnemy(new Vector3(0, 0, 5));
            var neighbor = run.AddEnemy(new Vector3(0, 0, 6));
            Assert.That(Fire(0), Is.False);
            run.ClearPath = (a, b) => false;
            Assert.That(Fire(0, target.Id), Is.False);
            Assert.That(run.Weapons[0].ReadyAt, Is.Zero);
            Assert.That(Fire(0, target.Id, true), Is.True);
            Assert.That(target.Health, Is.EqualTo(42));
            run.ClearPath = (a, b) => true;
            Assert.That(Fire(0, target.Id), Is.False);
            run.Advance(0.42f);
            Assert.That(Fire(0, target.Id), Is.True);
            Assert.That(target.Health, Is.EqualTo(24));
            Assert.That(neighbor.Health, Is.EqualTo(42));
            Assert.That(run.TotalAttacks, Is.EqualTo(2));
            Assert.That(run.TotalHits, Is.EqualTo(1));
        }

        [Test]
        public void PistolReachAndCadenceAreIndependentPerCopy()
        {
            run.Grant(WeaponKind.Pistol);
            var far = run.AddEnemy(new Vector3(0, 0, 24.01f), 36);
            Assert.That(Fire(0, far.Id), Is.False);
            far.Position.z = 24;
            Assert.That(Fire(0, far.Id), Is.True);
            Assert.That(Fire(1, far.Id), Is.True);
            Assert.That(far.Alive, Is.False);
            Assert.That(Fire(0, far.Id), Is.False);
            Assert.That(run.Kills, Is.EqualTo(1));
        }

        [Test]
        public void EmberAutomaticNeedsAimedEnemyButManualEmptyConeConsumesCadence()
        {
            run.Grant(WeaponKind.Ember);
            Assert.That(Fire(1), Is.False);
            Assert.That(Fire(1, manual: true), Is.True);
            Assert.That(run.Attacks[0].Points.Count, Is.EqualTo(1));
            var target = run.AddEnemy(new Vector3(0, 0, 4));
            Assert.That(Fire(1, target.Id), Is.False);
            run.Advance(1f);
            Assert.That(Fire(1, target.Id), Is.True);
            Assert.That(target.Health, Is.EqualTo(32));
        }

        [Test]
        public void EmberUsesForwardConeRangeAndPerVictimCover()
        {
            run.Grant(WeaponKind.Ember);
            var center = run.AddEnemy(new Vector3(0, 0, 4));
            var side = run.AddEnemy(new Vector3(1, 0, 4));
            var outside = run.AddEnemy(new Vector3(4, 0, 4));
            var far = run.AddEnemy(new Vector3(0, 0, 8.1f));
            var blocked = run.AddEnemy(new Vector3(-1, 0, 4));
            run.ClearPath = (a, b) => b != blocked.Position;
            Assert.That(Fire(1, blocked.Id), Is.False);
            Assert.That(Fire(1, center.Id), Is.True);
            Assert.That(center.Health, Is.EqualTo(32));
            Assert.That(side.Health, Is.EqualTo(32));
            Assert.That(outside.Health, Is.EqualTo(42));
            Assert.That(far.Health, Is.EqualTo(42));
            Assert.That(blocked.Health, Is.EqualTo(42));
        }

        [Test]
        public void SparkChoosesAimRatherThanAimedIdOrNearestAndStableIdBreaksTies()
        {
            run.Grant(WeaponKind.Spark);
            var offAxis = run.AddEnemy(new Vector3(0.8f, 0, 3));
            var aligned = run.AddEnemy(new Vector3(0, 0, 9));
            var tied = run.AddEnemy(new Vector3(0, 0, 10));
            // Reverse traversal so an equal-angle choice cannot depend on list order.
            run.Enemies.Reverse();
            Assert.That(Fire(1, offAxis.Id), Is.True);
            Assert.That(run.Attacks[0].Points[0], Is.EqualTo(aligned.Position));
            Assert.That(run.Attacks[0].Points[1], Is.EqualTo(tied.Position));
            Assert.That(offAxis.Health, Is.EqualTo(42));
        }

        [Test]
        public void SparkChainIsUniqueThreeVictimsAndExtendsInitialRange()
        {
            run.Grant(WeaponKind.Spark);
            var first = run.AddEnemy(new Vector3(0, 0, 14));
            var second = run.AddEnemy(new Vector3(0, 0, 19));
            var third = run.AddEnemy(new Vector3(0, 0, 24));
            var fourth = run.AddEnemy(new Vector3(0, 0, 29));
            var fifth = run.AddEnemy(new Vector3(0, 0, 34));
            Assert.That(Fire(1), Is.True);
            var points = run.Attacks[0].Points;
            Assert.That(points, Is.EqualTo(new[] { first.Position, second.Position, third.Position }));
            Assert.That(new HashSet<Vector3>(points).Count, Is.EqualTo(3));
            Assert.That(first.Health, Is.EqualTo(32));
            Assert.That(third.Health, Is.EqualTo(32));
            Assert.That(fourth.Health, Is.EqualTo(42));
            Assert.That(fifth.Health, Is.EqualTo(42));
        }

        [Test]
        public void SparkHopsUseCapturedCasterAimAndClearNeighborPaths()
        {
            run.Grant(WeaponKind.Spark);
            var first = run.AddEnemy(new Vector3(0, 0, 10));
            var nearSide = run.AddEnemy(new Vector3(1, 0, 11));
            var aligned = run.AddEnemy(new Vector3(0, 0, 15));
            var covered = run.AddEnemy(new Vector3(0, 0, 20));
            run.ClearPath = (a, b) => !(a == aligned.Position && b == covered.Position);
            Assert.That(Fire(1), Is.True);
            Assert.That(run.Attacks[0].Points[0], Is.EqualTo(first.Position));
            Assert.That(run.Attacks[0].Points[1], Is.EqualTo(aligned.Position));
            Assert.That(covered.Health, Is.EqualTo(42));
            Assert.That(nearSide.Health, Is.EqualTo(32));
        }

        [Test]
        public void SparkFirstTargetRequiresAimAllowanceReachAndCasterPath()
        {
            run.Grant(WeaponKind.Spark);
            run.AddEnemy(new Vector3(5, 0, 5));
            run.AddEnemy(new Vector3(0, 0, 16));
            var blocked = run.AddEnemy(new Vector3(0, 0, 6));
            run.ClearPath = (a, b) => b != blocked.Position;
            Assert.That(Fire(1), Is.False);
            Assert.That(Fire(1, manual: true), Is.True);
            Assert.That(run.Attacks[0].Points, Is.Empty);
            Assert.That(Fire(1, manual: true), Is.False);
            Assert.That(run.TotalHits, Is.Zero);
        }

        [Test]
        public void InvalidVortexPreservesReadinessAndExistingCap()
        {
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(0, 0, 5));
            run.Advance(0.1f);
            run.Weapons[1].ReadyAt = run.Elapsed;
            Wind(1, new Vector3(0, 0, 5));
            run.Advance(0.1f);
            run.Weapons[1].ReadyAt = run.Elapsed;
            Wind(1, new Vector3(0, 0, 5));
            // Force readiness without advancing through the oldest expiration.
            run.Weapons[1].ReadyAt = run.Elapsed;
            var oldest = run.Vortices[0];
            double readiness = run.Weapons[1].ReadyAt;
            Assert.That(Fire(1, manual: true), Is.False);
            Assert.That(Fire(1, manual: true, ground: Origin), Is.False);
            Assert.That(Fire(1, manual: true, ground: new Vector3(0, 0, 19)), Is.False);
            Assert.That(run.Weapons[1].ReadyAt, Is.EqualTo(readiness));
            Assert.That(run.Vortices.Count, Is.EqualTo(3));
            CollectionAssert.Contains(run.Vortices, oldest);
            run.ClearPath = (a, b) => true;
            Wind(1, new Vector3(0, 0, 5));
            Assert.That(run.Vortices.Count, Is.EqualTo(3));
            CollectionAssert.DoesNotContain(run.Vortices, oldest);
        }

        [Test]
        public void ValidProjectedGroundDoesNotRequireCasterToPlacementPath()
        {
            run.Grant(WeaponKind.Whirlwind);
            run.ClearPath = (a, b) => a != Origin;
            Wind(1, new Vector3(0, 0, 5));
            Assert.That(run.Vortices.Count, Is.EqualTo(1));
            Assert.That(run.Weapons[1].ReadyAt, Is.EqualTo(1.8f).Within(0.000001));
        }

        [Test]
        public void GroundedVortexPullPreservesCreatureHeight()
        {
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(0, 0, 5));
            var enemy = run.AddEnemy(new Vector3(1, 0.7f, 5));
            run.Advance(0.25f);
            Assert.That(enemy.Position.y, Is.EqualTo(0.7f));
            Assert.That(enemy.Position.x, Is.LessThan(1));
        }

        [Test]
        public void VortexAutomaticRequiresClearOccupiedAreaManualAllowsEmpty()
        {
            run.Grant(WeaponKind.Whirlwind);
            Vector3 ground = new Vector3(0, 0, 5);
            Assert.That(Fire(1, ground: ground), Is.False);
            var enemy = run.AddEnemy(ground + Vector3.right);
            run.ClearPath = (a, b) => b != enemy.Position;
            Assert.That(Fire(1, ground: ground), Is.False);
            Assert.That(Fire(1, manual: true, ground: ground), Is.True);
            run.Weapons[1].ReadyAt = run.Elapsed;
            run.ClearPath = (a, b) => true;
            Assert.That(Fire(1, ground: ground), Is.True);
        }

        [Test]
        public void VortexCapsBelongToExactCopies()
        {
            run.Grant(WeaponKind.Whirlwind);
            run.Grant(WeaponKind.Whirlwind);
            for (int i = 0; i < 3; i++)
            {
                Wind(1, new Vector3(0, 0, 5));
                Wind(2, new Vector3(0, 0, 5));
                run.Weapons[1].ReadyAt = run.Weapons[2].ReadyAt = run.Elapsed;
            }
            var otherOldest = run.Vortices[1];
            Wind(1, new Vector3(0, 0, 6));
            Assert.That(run.Vortices.Count, Is.EqualTo(6));
            Assert.That(run.Vortices.FindAll(v => v.Owner == run.Weapons[1]).Count, Is.EqualTo(3));
            CollectionAssert.Contains(run.Vortices, otherOldest);
        }

        [Test]
        public void VortexTicksStartAfterPointFiveAndDamageStacksButPullDoesNot()
        {
            run.Grant(WeaponKind.Whirlwind);
            run.Grant(WeaponKind.Whirlwind);
            var enemy = run.AddEnemy(new Vector3(1, 0, 5), 100);
            Wind(1, new Vector3(0, 0, 5));
            Wind(2, new Vector3(0, 0, 5));
            foreach (var vortex in run.Vortices) vortex.Direction = Vector3.zero;
            var before = enemy.Position;
            run.Advance(0.2f);
            Assert.That(enemy.Health, Is.EqualTo(100));
            Assert.That(Vector3.Distance(before, enemy.Position), Is.EqualTo(0.11f).Within(0.0001));
            run.Advance(0.3f);
            Assert.That(enemy.Health, Is.EqualTo(94));
            run.Advance(0.5f);
            Assert.That(enemy.Health, Is.EqualTo(88));
            Assert.That(run.TotalHits, Is.EqualTo(4));
        }

        [TestCase(0.5f)]
        [TestCase(0f)]
        public void StrongestVortexPullIgnoresInsertionOrder(float x)
        {
            run.Grant(WeaponKind.Whirlwind);
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(-1, 0, 5));
            Wind(2, new Vector3(1, 0, 5));
            var enemy = run.AddEnemy(new Vector3(x, 0, 5), 100);
            run.Advance(0.1f);
            var expected = enemy.Position;
            SetUp();
            run.Grant(WeaponKind.Whirlwind);
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(-1, 0, 5));
            Wind(2, new Vector3(1, 0, 5));
            run.Vortices.Reverse();
            enemy = run.AddEnemy(new Vector3(x, 0, 5), 100);
            run.Advance(0.1f);
            Assert.That(enemy.Position, Is.EqualTo(expected));
            if (x == 0) Assert.That(enemy.Position.x, Is.LessThan(0));
            else Assert.That(enemy.Position.x, Is.GreaterThan(x));
        }

        [Test]
        public void ClearInfluenceCannotPullThroughBlockedMovement()
        {
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(0, 0, 5));
            var enemy = run.AddEnemy(new Vector3(1, 0, 5), 100);
            var before = enemy.Position;
            run.ClearPath = (a, b) => a != before;
            run.Advance(0.5f);
            Assert.That(enemy.Position, Is.EqualTo(before));
            Assert.That(enemy.Health, Is.EqualTo(97));
        }

        [Test]
        public void VortexCoverBlocksDamageAndPullAndSweptTravelDissipates()
        {
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(0, 0, 5));
            var enemy = run.AddEnemy(new Vector3(1, 0, 5), 100);
            var before = enemy.Position;
            run.ClearPath = (a, b) => false;
            run.Advance(0.5f);
            Assert.That(enemy.Health, Is.EqualTo(100));
            Assert.That(enemy.Position, Is.EqualTo(before));
            run.ClearPath = (a, b) => true;
            Vector3 sweepStart = Vector3.zero, sweepEnd = Vector3.zero;
            run.VortexTravelClear = (a, b) =>
            {
                sweepStart = a; sweepEnd = b;
                return !(a.z < 7 && b.z >= 7);
            };
            run.Advance(0.5f);
            Assert.That(sweepStart.z, Is.LessThan(7));
            Assert.That(sweepEnd.z, Is.GreaterThan(7));
            Assert.That(run.Vortices, Is.Empty);
            Assert.That(enemy.Health, Is.EqualTo(100));
            Assert.That(enemy.Position, Is.EqualTo(before));
        }

        [TestCase(2.8f, 5)]
        [TestCase(2.5f, 4)]
        public void VortexExpirationExcludesBoundaryTickAndLargeDeltasProcessAllTicks(float duration, int ticks)
        {
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(0, 0, 5));
            // Stationary diagnostic isolates periodic timing from travel.
            run.Vortices[0].Direction = Vector3.zero;
            run.Vortices[0].ExpiresAt = duration;
            var enemy = run.AddEnemy(new Vector3(0, 0, 5), 100);
            run.Advance(duration);
            Assert.That(enemy.Health, Is.EqualTo(100 - 3 * ticks));
            Assert.That(run.TotalHits, Is.EqualTo(ticks));
            Assert.That(run.Vortices, Is.Empty);
        }

        [Test]
        public void PauseAndUpgradeFreezeAllGameplayClocksAndActions()
        {
            run.Grant(WeaponKind.Whirlwind);
            Wind(1, new Vector3(0, 0, 5));
            Assert.That(Fire(0, manual: true), Is.True);
            Assert.That(run.Contact(), Is.True);
            run.Pause();
            var position = run.Vortices[0].Position;
            run.Advance(200);
            Assert.That(run.Elapsed, Is.Zero);
            Assert.That(run.Vortices[0].Position, Is.EqualTo(position));
            Assert.That(Fire(0, manual: true), Is.False);
            Assert.That(run.Contact(), Is.False);
            Assert.That(run.Heal(20), Is.False);
            run.CollectXp(12);
            Assert.That(run.Xp, Is.Zero);
            run.Resume();
            Assert.That(run.Contact(), Is.False);
            Assert.That(Fire(0, manual: true), Is.False);
            run.CollectXp(12);
            run.Resume(); run.Start(); run.Pause();
            run.Advance(200);
            Assert.That(run.Phase, Is.EqualTo(FantasyPhase.Upgrade));
            Assert.That(run.Elapsed, Is.Zero);
            Assert.That(run.Choose(0), Is.True);
            run.Advance(1);
            Assert.That(run.Contact(), Is.True);
            Assert.That(Fire(0, manual: true), Is.True);
        }

        [Test]
        public void SharedContactGateHealingAndImmediateDefeatAreBounded()
        {
            Assert.That(run.Heal(20), Is.False);
            Assert.That(run.Contact(), Is.True);
            Assert.That(run.Contact(), Is.False);
            run.Advance(0.999f);
            Assert.That(run.Contact(), Is.False);
            run.Advance(0.001f);
            Assert.That(run.Contact(), Is.True);
            Assert.That(run.Heal(1000), Is.True);
            Assert.That(run.Health, Is.EqualTo(100));
            Assert.That(run.Heal(20), Is.False);
            for (int i = 0; i < 10; i++) { run.Advance(1); run.Contact(); }
            Assert.That(run.Health, Is.Zero);
            Assert.That(run.Phase, Is.EqualTo(FantasyPhase.Defeat));
            Assert.That(run.Heal(20), Is.False);
            Assert.That(Fire(0, manual: true), Is.False);
        }

        [Test]
        public void ContactBeforeWinningAdvanceGivesDefeatPriority()
        {
            for (int i = 0; i < 9; i++) { run.Contact(); run.Advance(1); }
            run.Advance(590.5f);
            Assert.That(run.Health, Is.EqualTo(10));
            Assert.That(run.Contact(), Is.True);
            run.Advance(0.5f);
            Assert.That(run.Phase, Is.EqualTo(FantasyPhase.Defeat));
            Assert.That(run.Elapsed, Is.EqualTo(599.5));
        }

        [Test]
        public void SurvivalStopsAtSixHundredAndResultsFreeze()
        {
            run.Advance(599);
            run.Pause(); run.Advance(1000); run.Resume();
            run.Advance(2);
            Assert.That(run.Elapsed, Is.EqualTo(600));
            Assert.That(run.Phase, Is.EqualTo(FantasyPhase.Victory));
            run.Advance(10);
            Assert.That(run.Elapsed, Is.EqualTo(600));
            Assert.That(run.Contact(), Is.False);
            Assert.That(run.Heal(20), Is.False);
        }

        [Test]
        public void KillsEnqueueOnceAndOnlyCollectedXpEarnsSequentialChoices()
        {
            var enemy = run.AddEnemy(Aim);
            run.DamageEnemy(enemy, 42);
            run.DamageEnemy(enemy, 42);
            Assert.That(run.Kills, Is.EqualTo(1));
            Assert.That(run.Deaths, Is.EqualTo(new[] { enemy }));
            Assert.That(run.Xp, Is.Zero);
            run.CollectXp(57);
            Assert.That(run.Phase, Is.EqualTo(FantasyPhase.Upgrade));
            Assert.That(run.Choose(-1), Is.False);
            Assert.That(run.Choose(3), Is.False);
            run.Choose(0);
            Assert.That(run.Xp, Is.EqualTo(45));
            Assert.That(run.Threshold, Is.EqualTo(18));
            run.Choose(0);
            Assert.That(run.Xp, Is.EqualTo(27));
            run.Choose(0);
            Assert.That(run.Xp, Is.EqualTo(3));
            Assert.That(run.Level, Is.EqualTo(4));
            Assert.That(run.Threshold, Is.EqualTo(30));
            Assert.That(run.Phase, Is.EqualTo(FantasyPhase.Playing));
            Assert.That(run.Choices, Is.Empty);
        }

        [Test]
        public void ChoicesAreSeededDistinctAndSinglePistolHasUsefulPool()
        {
            run.Reset(27); run.Start(); run.CollectXp(12);
            var expected = new List<string>();
            foreach (var choice in run.Choices) expected.Add(choice.IsGrant + ":" + choice.Kind + ":" + choice.CopyId);
            Assert.That(expected.Count, Is.EqualTo(3));
            Assert.That(new HashSet<string>(expected).Count, Is.EqualTo(3));
            run.Reset(27); run.Start(); run.CollectXp(12);
            var actual = new List<string>();
            foreach (var choice in run.Choices) actual.Add(choice.IsGrant + ":" + choice.Kind + ":" + choice.CopyId);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void CapExcludesGrantsAndDamageTargetsExactIndependentCopy()
        {
            for (int i = 0; i < 5; i++) Assert.That(run.Grant(WeaponKind.Pistol), Is.True);
            Assert.That(run.Grant(WeaponKind.Ember), Is.False);
            var ids = new HashSet<int>();
            foreach (var weapon in run.Weapons) ids.Add(weapon.CopyId);
            Assert.That(ids.Count, Is.EqualTo(6));
            run.CollectXp(12);
            Assert.That(run.Choices.Count, Is.EqualTo(3));
            foreach (var choice in run.Choices) Assert.That(choice.IsGrant, Is.False);
            int id = run.Choices[0].CopyId;
            Assert.That(run.Choose(0), Is.True);
            foreach (var weapon in run.Weapons)
                Assert.That(weapon.Damage, Is.EqualTo(weapon.CopyId == id ? 19.8f : 18f).Within(0.00001));
        }

        [Test]
        public void GrantsStartAtBaseStateAndResetLosesPowerAndQueues()
        {
            run.Weapons[0].Damage = 100;
            run.Weapons[0].ReadyAt = 100;
            run.Grant(WeaponKind.Pistol);
            Assert.That(run.Weapons[1].Damage, Is.EqualTo(18));
            Assert.That(run.Weapons[1].ReadyAt, Is.Zero);
            run.Grant(WeaponKind.Whirlwind);
            Wind(2, new Vector3(0, 0, 5));
            var enemy = run.AddEnemy(Aim);
            run.DamageEnemy(enemy, 100);
            run.Contact();
            run.CollectXp(12);
            run.Reset();
            Assert.That(run.Phase, Is.EqualTo(FantasyPhase.Ready));
            Assert.That(run.Health, Is.EqualTo(100));
            Assert.That(run.Xp, Is.Zero);
            Assert.That(run.Level, Is.EqualTo(1));
            Assert.That(run.Threshold, Is.EqualTo(12));
            Assert.That(run.Elapsed, Is.Zero);
            Assert.That(run.Kills, Is.Zero);
            Assert.That(run.TotalAttacks, Is.Zero);
            Assert.That(run.TotalHits, Is.Zero);
            Assert.That(run.Weapons.Count, Is.EqualTo(1));
            Assert.That(run.Weapons[0].Damage, Is.EqualTo(18));
            Assert.That(run.Weapons[0].Reach, Is.EqualTo(24));
            Assert.That(run.Weapons[0].Cadence, Is.EqualTo(0.42f));
            Assert.That(run.Enemies, Is.Empty);
            Assert.That(run.Attacks, Is.Empty);
            Assert.That(run.Deaths, Is.Empty);
            Assert.That(run.Vortices, Is.Empty);
            Assert.That(run.Choices, Is.Empty);
            Assert.That(Fire(0, manual: true), Is.False);
            run.Start();
            Assert.That(Fire(0, manual: true), Is.True);
            Assert.That(run.Contact(), Is.True);
        }
    }
}
