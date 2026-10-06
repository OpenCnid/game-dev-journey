using System;
using System.IO;
using UnityEngine;

namespace Journey
{
    public enum RangedPhase { Cooldown, Windup }
    [Serializable] public struct RangedAttack
    {
        public RangedPhase Phase;
        public float Until;
        public Vector3 Direction;
        public int Target, TargetLife;
    }
    [Serializable] public struct EnemyBolt
    {
        public Vector3 Position, Direction;
        public float Remaining;
        public int Id, Source, Incarnation, LifeA, LifeB;
    }
    // Snapshot projection: only the host advances it. Pure-chaser fixtures have no ranged state.
    [Serializable] public sealed class RangedState
    {
        public RangedAttack[] Attacks;
        public EnemyBolt[] Bolts = new EnemyBolt[RangedThreats.Capacity];
        public int Count, Fired, Hits, Blocked, Expired, Overflow;
        public RangedState(int enemies) { Attacks = new RangedAttack[enemies]; }
        public void CopyFrom(RangedState s)
        {
            Array.Copy(s.Attacks, Attacks, Attacks.Length); Array.Copy(s.Bolts, Bolts, Bolts.Length);
            Count = s.Count; Fired = s.Fired; Hits = s.Hits; Blocked = s.Blocked; Expired = s.Expired; Overflow = s.Overflow;
        }
        public void ClearCombat() { Count = 0; Array.Clear(Bolts, 0, Bolts.Length); Array.Clear(Attacks, 0, Attacks.Length); }
        public void Write(BinaryWriter w)
        {
            w.Write(Count); w.Write(Fired); w.Write(Hits); w.Write(Blocked); w.Write(Expired); w.Write(Overflow);
            foreach (RangedAttack a in Attacks)
            { w.Write((int)a.Phase); w.Write(a.Until); SharedConnection.Vector(w, a.Direction); w.Write(a.Target); w.Write(a.TargetLife); }
            for (int i = 0; i < Count; i++)
            {
                EnemyBolt b = Bolts[i]; SharedConnection.Vector(w, b.Position); SharedConnection.Vector(w, b.Direction);
                w.Write(b.Remaining); w.Write(b.Id); w.Write(b.Source); w.Write(b.Incarnation); w.Write(b.LifeA); w.Write(b.LifeB);
            }
        }
        public void Read(BinaryReader r)
        {
            Count = r.ReadInt32(); Fired = r.ReadInt32(); Hits = r.ReadInt32(); Blocked = r.ReadInt32(); Expired = r.ReadInt32(); Overflow = r.ReadInt32();
            if (Count < 0 || Count > Bolts.Length || Fired < 0 || Hits < 0 || Blocked < 0 || Expired < 0 || Overflow < 0)
                throw new ArgumentException("Invalid ranged counters");
            for (int i = 0; i < Attacks.Length; i++)
            {
                var a = new RangedAttack { Phase = (RangedPhase)r.ReadInt32(), Until = r.ReadSingle(), Direction = SharedConnection.Vector(r), Target = r.ReadInt32(), TargetLife = r.ReadInt32() };
                if (a.Phase < RangedPhase.Cooldown || a.Phase > RangedPhase.Windup || !(a.Until >= 0 && a.Until <= 610) ||
                    !ValidVector(a.Direction) || a.Phase == RangedPhase.Windup && (!UnitDirection(a.Direction) || a.Target < 0 || a.Target > 1 || a.TargetLife < 1))
                    throw new ArgumentException("Invalid ranged attack");
                Attacks[i] = a;
            }
            for (int i = 0; i < Count; i++)
            {
                var b = new EnemyBolt { Position = SharedConnection.Vector(r), Direction = SharedConnection.Vector(r), Remaining = r.ReadSingle(),
                    Id = r.ReadInt32(), Source = r.ReadInt32(), Incarnation = r.ReadInt32(), LifeA = r.ReadInt32(), LifeB = r.ReadInt32() };
                if (!ValidVector(b.Position) || !UnitDirection(b.Direction) || !(b.Remaining > 0 && b.Remaining <= RangedThreats.Lifetime) ||
                    b.Id < 1 || b.Id > Fired || b.Source < 0 || b.Source >= Attacks.Length || b.Incarnation < 1 || b.LifeA < 0 || b.LifeB < 0)
                    throw new ArgumentException("Invalid enemy bolt");
                for (int j = 0; j < i; j++) if (Bolts[j].Id == b.Id) throw new ArgumentException("Duplicate enemy bolt");
                Bolts[i] = b;
            }
        }
        static bool ValidVector(Vector3 v) => EnemyGeometry.ValidRay(new Ray(v, Vector3.forward));
        static bool UnitDirection(Vector3 v) => ValidVector(v) && Mathf.Abs(v.sqrMagnitude - 1) < .001f && v.y == 0;
    }

    // One concrete ability definition; physical ticks, target lives and damage belong to authority.
    public sealed class RangedThreats
    {
        public const int Capacity = 20, Damage = 15;
        public const float Range = 14, Windup = 1.25f, Cooldown = 2.75f, Speed = 10, Radius = .26f, Lifetime = 3.5f;
        readonly SharedSnapshot snapshot;
        readonly Vector3[] previousPlayers = new Vector3[2];
        readonly int[] previousLives = new int[2];
        RangedState State => snapshot.Ranged;
        public RangedThreats(SharedSnapshot s) { snapshot = s; }
        public void Reset()
        {
            State.ClearCombat(); State.Fired = State.Hits = State.Blocked = State.Expired = State.Overflow = 0;
            for (int i = 0; i < State.Attacks.Length; i++) Respawn(i);
            for (int p = 0; p < 2; p++) { previousPlayers[p] = snapshot.Players[p].Position; previousLives[p] = snapshot.Players[p].Life; }
        }
        public void Respawn(int i)
        { State.Attacks[i] = new RangedAttack { Until = snapshot.Elapsed + .5f + i * .45f }; }
        public bool HoldPosition(int i, int target) => i < State.Attacks.Length &&
            (State.Attacks[i].Phase == RangedPhase.Windup || target >= 0 && (snapshot.Players[target].Position - snapshot.Enemies[i].Position).sqrMagnitude <= Range * Range);
        public void StepAttacks(Func<Vector3, int> nearest)
        {
            for (int i = 0; i < State.Attacks.Length; i++)
            {
                RangedAttack a = State.Attacks[i]; SharedEnemy e = snapshot.Enemies[i];
                if (snapshot.Elapsed + .00001f < a.Until) continue;
                if (a.Phase == RangedPhase.Windup)
                {
                    if (snapshot.Players[a.Target].Health > 0 && snapshot.Players[a.Target].Life == a.TargetLife)
                    {
                        if (State.Count == Capacity) State.Overflow++;
                        else State.Bolts[State.Count++] = new EnemyBolt { Position = e.Position, Direction = a.Direction, Remaining = Lifetime,
                            Id = ++State.Fired, Source = i, Incarnation = e.Incarnation,
                            LifeA = snapshot.Players[0].Health > 0 ? snapshot.Players[0].Life : 0,
                            LifeB = snapshot.Roster == 2 && snapshot.Players[1].Health > 0 ? snapshot.Players[1].Life : 0 };
                    }
                    a.Phase = RangedPhase.Cooldown; a.Until = snapshot.Elapsed + Cooldown;
                }
                else
                {
                    int target = nearest(e.Position);
                    if (target < 0 || (snapshot.Players[target].Position - e.Position).sqrMagnitude > Range * Range) continue;
                    Vector3 direction = snapshot.Players[target].Position - e.Position;
                    a.Direction = direction.sqrMagnitude < .00001f ? Vector3.forward : direction.normalized;
                    a.Target = target; a.TargetLife = snapshot.Players[target].Life;
                    a.Phase = RangedPhase.Windup; a.Until = snapshot.Elapsed + Windup;
                }
                State.Attacks[i] = a;
            }
        }
        public void StepBolts(Func<int, int, bool> damage)
        {
            for (int i = 0; i < State.Count;)
            {
                EnemyBolt b = State.Bolts[i]; Vector3 end = b.Position + b.Direction * (Speed * CombatSimulation.Tick);
                int target = -1; float first = float.PositiveInfinity;
                for (int p = 0; p < snapshot.Roster; p++)
                {
                    SharedPlayer player = snapshot.Players[p];
                    if (player.Health == 0 || player.Life != (p == 0 ? b.LifeA : b.LifeB)) continue;
                    Vector3 prior = previousLives[p] == player.Life ? previousPlayers[p] : player.Position;
                    if (SegmentEntry(b.Position - prior, end - player.Position, out float t) && t < first)
                    { first = t; target = p; }
                }
                b.Position = end; b.Remaining -= CombatSimulation.Tick;
                if (target >= 0)
                { if (damage(target, Damage)) State.Hits++; else State.Blocked++; }
                else if (b.Remaining <= 0) State.Expired++;
                else { State.Bolts[i] = b; i++; continue; }
                State.Count--; State.Bolts[i] = State.Bolts[State.Count]; State.Bolts[State.Count] = default;
            }
            for (int p = 0; p < snapshot.Roster; p++) { previousPlayers[p] = snapshot.Players[p].Position; previousLives[p] = snapshot.Players[p].Life; }
        }
        // Relative swept segment vs player + bolt radius, including player movement between ticks.
        public static bool SegmentEntry(Vector3 start, Vector3 end, out float entry)
        {
            const float radius = .55f + Radius;
            Vector3 delta = end - start; float c = start.sqrMagnitude - radius * radius;
            entry = 0; if (c <= 0) return true;
            float a = delta.sqrMagnitude, b = Vector3.Dot(start, delta), discriminant = b * b - a * c;
            if (a < .000001f || discriminant < 0) return false;
            entry = (-b - Mathf.Sqrt(discriminant)) / a; return entry >= 0 && entry <= 1;
        }
    }
}
