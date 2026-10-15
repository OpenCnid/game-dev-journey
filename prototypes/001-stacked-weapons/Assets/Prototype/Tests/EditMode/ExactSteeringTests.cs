using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class ExactSteeringTests
    {
        static byte[] Snapshot(SharedSnapshot snapshot)
        {
            using (var stream = new MemoryStream())
            { SharedConnection.WriteSnapshot(new BinaryWriter(stream), snapshot); return stream.ToArray(); }
        }
        static byte[] Outcomes(List<SharedHit> hits)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                foreach (SharedHit hit in hits) SharedConnection.WriteHit(writer, hit);
                return stream.ToArray();
            }
        }
        [TestCase(35, 1)] [TestCase(250, 3)] [TestCase(1000, 8)]
        public void ExactGridPreservesSnapshotsCombatXpAndRetryAtFixedTicks(int enemies, int instances)
        {
            var config = ScenarioConfiguration.Named(ScenarioKind.CoopScale).Scale(enemies, instances, 8);
            Guid session = Guid.NewGuid();
            var reference = new SharedEncounter(config, session);
            var candidate = new SharedEncounter(config, session, exactSteering: true);
            var referenceHits = new List<SharedHit>(); var candidateHits = new List<SharedHit>();
            var referenceGrowth = new List<string>(); var candidateGrowth = new List<string>();
            reference.Resolved += referenceHits.Add; candidate.Resolved += candidateHits.Add;
            reference.Growth.Changed += e => referenceGrowth.Add(JsonUtility.ToJson(e));
            candidate.Growth.Changed += e => candidateGrowth.Add(JsonUtility.ToJson(e));
            reference.Start(); candidate.Start();
            for (int generation = 0; generation < 2; generation++)
            {
                // Known damaged targets ensure genuine death/respawn and conserved pickup work.
                for (int i = 0; i < 8; i++)
                {
                    var enemy = new SharedEnemy { Position = new Vector3((i % 4 - 1) * 2, 0, 4 + i / 4 * 2), Health = 20, Incarnation = 1 };
                    reference.Current.Enemies[i] = candidate.Current.Enemies[i] = enemy;
                }
                int sequence = 0;
                for (int tick = 0; tick < 120; tick++)
                {
                    for (int p = 0; p < 2; p++)
                    {
                        SharedPlayer player = reference.Current.Players[p];
                        player.Position = new Vector3(p * 2 - 1, 0, tick * .02f);
                        reference.Current.Players[p] = candidate.Current.Players[p] = player;
                        if (tick == 0) continue;
                        for (int instance = 0; instance < instances; instance++)
                        {
                            int interval = instance % 3 == 0 ? 8 : instance % 3 == 1 ? 48 : 72;
                            if (tick != 1 && tick % interval != 0) continue;
                            SharedEnemy enemy = reference.Current.Enemies[(tick / 8 + instance) % 8];
                            Vector3 origin = player.Position + Vector3.up * 1.65f;
                            Vector3 target = EnemyGeometry.Head(enemy.Position, enemy.Health < config.EnemyHealth).MultiplyPoint3x4(Vector3.zero);
                            var shot = new SharedShot { Run = reference.Current.Run, Player = p, Life = player.Life,
                                Weapon = WeaponArsenal.Id(p, instance), Event = ++sequence, PoseTick = reference.Current.Tick,
                                Ray = new Ray(origin, (target - origin).normalized) };
                            reference.Submit(shot); candidate.Submit(shot);
                        }
                    }
                    reference.Step(); candidate.Step();
                    Assert.That(Snapshot(candidate.Current), Is.EqualTo(Snapshot(reference.Current)), $"generation {generation}, tick {tick}");
                }
                Assert.That(Outcomes(candidateHits), Is.EqualTo(Outcomes(referenceHits)));
                Assert.That(candidateGrowth, Is.EqualTo(referenceGrowth));
                Assert.That(reference.Current.Players[0].Kills + reference.Current.Players[1].Kills, Is.GreaterThan(0));
                Assert.That(candidate.Work.contactTests, Is.EqualTo(reference.Work.contactTests));
                Assert.That(candidate.Work.attractionQueries, Is.EqualTo(reference.Work.attractionQueries));
                Assert.That(candidate.Work.steeringChecks, Is.LessThan(reference.Work.steeringChecks));
                reference.Current.State = candidate.Current.State = SharedState.Complete;
                for (int p = 0; p < 2; p++)
                { reference.Retry(reference.Current.Run, p); candidate.Retry(candidate.Current.Run, p); }
                Assert.That(Snapshot(candidate.Current), Is.EqualTo(Snapshot(reference.Current)));
            }
        }
        [Test] public void CellEdgesCoincidentAndDensePositionsNeverClipNeighbors()
        {
            var config = ScenarioConfiguration.Named(ScenarioKind.CoopScale).Scale(250, 3, 8);
            Guid session = Guid.NewGuid();
            var reference = new SharedEncounter(config, session);
            var candidate = new SharedEncounter(config, session, exactSteering: true);
            reference.Start(); candidate.Start();
            for (int i = 0; i < config.EnemyCount; i++)
            {
                SharedEnemy e = reference.Current.Enemies[i]; e.Position = new Vector3(-2, 0, -2);
                reference.Current.Enemies[i] = candidate.Current.Enemies[i] = e;
            }
            reference.Step(); candidate.Step();
            Assert.That(candidate.Work.steeringChecks, Is.EqualTo(250 * 250), "Dense cells must retain every candidate.");
            Assert.That(Snapshot(candidate.Current), Is.EqualTo(Snapshot(reference.Current)));
            float[] edges = { -29, -28.0001f, -28, -27.9999f, -2.0001f, -2, -.8001f, -.8f, -.7999f, 28, 29 };
            for (int i = 0; i < config.EnemyCount; i++)
            {
                SharedEnemy e = reference.Current.Enemies[i];
                e.Position = new Vector3(edges[i % edges.Length], i % 3 * .01f, edges[i / edges.Length % edges.Length]);
                reference.Current.Enemies[i] = candidate.Current.Enemies[i] = e;
            }
            for (int tick = 0; tick < 60; tick++)
            {
                reference.Step(); candidate.Step();
                Assert.That(Snapshot(candidate.Current), Is.EqualTo(Snapshot(reference.Current)), "Cell/radius edge tick " + tick);
            }
        }
    }
}
