using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Journey
{
    public enum RifleUpgrade { None, StrongerHits, FasterFire }
    // Concrete authored effects. Future pickup/player, loot and draw effects get their own consumers.
    public static class GrowthUpgrade
    {
        public static bool Valid(RifleUpgrade value) => value == RifleUpgrade.StrongerHits || value == RifleUpgrade.FasterFire;
        public static string Label(RifleUpgrade value) => value == RifleUpgrade.StrongerHits ? "Stronger hits" : value == RifleUpgrade.FasterFire ? "Faster fire" : "Baseline";
        public static int Damage(RifleUpgrade value) => value == RifleUpgrade.StrongerHits ? 30 : 20;
        public static float Interval(RifleUpgrade value) => value == RifleUpgrade.FasterFire ? .08f : .12f;
    }
    [Serializable] public struct PersonalOffer
    {
        public int Id, Tick, Weapon, AppliedTick;
        public RifleUpgrade First, Selected;
        public RifleUpgrade Card(int index) => index == 1 ? First : index == 2 ?
            (First == RifleUpgrade.StrongerHits ? RifleUpgrade.FasterFire : RifleUpgrade.StrongerHits) : RifleUpgrade.None;
        public bool Pending => Id > 0 && Selected == RifleUpgrade.None;
    }
    public struct GrowthChoice { public int Run, Player, Offer, Weapon; public RifleUpgrade Upgrade; }
    [Serializable] public struct XpOrb
    {
        public int Id, Enemy, Incarnation, SourcePlayer, SourceWeapon, Shot;
        public Vector3 Position;
    }
    public enum GrowthEventKind { DeathOrb, Collected, Entitlement, Applied }
    [Serializable] public struct GrowthEvent
    {
        public int Run, Tick, Sequence, Player, Xp;
        public GrowthEventKind Kind;
        public XpOrb Orb;
        public PersonalOffer Offer;
        public void Write(BinaryWriter w)
        {
            w.Write(Run); w.Write(Tick); w.Write(Sequence); w.Write(Player); w.Write(Xp); w.Write((int)Kind);
            w.Write(Orb.Id); w.Write(Orb.Enemy); w.Write(Orb.Incarnation); w.Write(Orb.SourcePlayer); w.Write(Orb.SourceWeapon); w.Write(Orb.Shot);
            SharedConnection.Vector(w, Orb.Position); WriteOffer(w, Offer);
        }
        public static GrowthEvent Read(BinaryReader r) => new GrowthEvent { Run = r.ReadInt32(), Tick = r.ReadInt32(), Sequence = r.ReadInt32(),
            Player = r.ReadInt32(), Xp = r.ReadInt32(), Kind = (GrowthEventKind)r.ReadInt32(),
            Orb = new XpOrb { Id = r.ReadInt32(), Enemy = r.ReadInt32(), Incarnation = r.ReadInt32(), SourcePlayer = r.ReadInt32(),
                SourceWeapon = r.ReadInt32(), Shot = r.ReadInt32(), Position = SharedConnection.Vector(r) }, Offer = ReadOffer(r) };
        public static void WriteOffer(BinaryWriter w, PersonalOffer o)
        { w.Write(o.Id); w.Write(o.Tick); w.Write(o.Weapon); w.Write(o.AppliedTick); w.Write((int)o.First); w.Write((int)o.Selected); }
        public static PersonalOffer ReadOffer(BinaryReader r) => new PersonalOffer { Id = r.ReadInt32(), Tick = r.ReadInt32(), Weapon = r.ReadInt32(),
            AppliedTick = r.ReadInt32(), First = (RifleUpgrade)r.ReadInt32(), Selected = (RifleUpgrade)r.ReadInt32() };
    }

    // Run-owned ledger, team threshold and personal draws. Collection is separate from effect application.
    public sealed class EarnedGrowth
    {
        public const int Threshold = 10;
        public const float PickupRadius = 2;
        public readonly Dictionary<int, XpOrb> Orbs = new Dictionary<int, XpOrb>();
        readonly HashSet<long> deaths = new HashSet<long>();
        readonly List<int> collected = new List<int>();
        readonly System.Random[] draws = new System.Random[2];
        int orbId, sequence;
        public int Xp { get; private set; }
        public int Level => Xp >= Threshold ? 2 : 1;
        public event Action<GrowthEvent> Changed;
        public void Reset(int seed)
        {
            Orbs.Clear(); deaths.Clear(); collected.Clear(); orbId = sequence = Xp = 0;
            for (int p = 0; p < 2; p++) draws[p] = new System.Random(unchecked(seed * 397 ^ (p + 1) * 7919));
        }
        void Emit(SharedSnapshot s, GrowthEventKind kind, int player, XpOrb orb = default, PersonalOffer offer = default)
        { Changed?.Invoke(new GrowthEvent { Run = s.Run, Tick = s.Tick, Sequence = ++sequence, Kind = kind, Player = player, Xp = Xp, Orb = orb, Offer = offer }); }
        public bool Death(SharedSnapshot s, SharedHit hit)
        {
            if (!hit.Shot.Killed || hit.Source.Run != s.Run) return false;
            long identity = ((long)hit.Shot.EnemyIndex << 32) | (uint)hit.Shot.SpawnVersion;
            if (!deaths.Add(identity)) return false;
            var orb = new XpOrb { Id = ++orbId, Enemy = hit.Shot.EnemyIndex, Incarnation = hit.Shot.SpawnVersion,
                SourcePlayer = hit.Source.Player, SourceWeapon = hit.Source.Weapon, Shot = hit.Source.Event,
                Position = new Vector3(hit.DeathPosition.x, 0, hit.DeathPosition.z) };
            Orbs.Add(orb.Id, orb); Emit(s, GrowthEventKind.DeathOrb, hit.Source.Player, orb); return true;
        }
        public void Collect(SharedSnapshot s)
        {
            if (s.State != SharedState.Running) return;
            collected.Clear();
            foreach (XpOrb orb in Orbs.Values)
                for (int p = 0; p < s.Roster; p++) if (s.Players[p].Health > 0 &&
                    (s.Players[p].Position - orb.Position).sqrMagnitude <= PickupRadius * PickupRadius)
                { collected.Add(orb.Id); Xp++; Emit(s, GrowthEventKind.Collected, p, orb); break; }
            foreach (int id in collected) Orbs.Remove(id);
            s.Xp = Xp; s.Level = Level;
            if (Level == 2) for (int p = 0; p < s.Roster; p++) if (s.Players[p].Offer.Id == 0)
            {
                var offer = new PersonalOffer { Id = p + 1, Tick = s.Tick, Weapon = s.Players[p].Weapon,
                    First = draws[p].Next(2) == 0 ? RifleUpgrade.StrongerHits : RifleUpgrade.FasterFire };
                s.Players[p].Offer = offer; Emit(s, GrowthEventKind.Entitlement, p, offer: offer);
            }
        }
        public void Applied(SharedSnapshot s, int p) => Emit(s, GrowthEventKind.Applied, p, offer: s.Players[p].Offer);
    }
    // Reliable ordered incremental projection, never deletes uncollected orbs to fit a snapshot.
    public sealed class OrbReplica
    {
        public readonly Dictionary<int, XpOrb> Orbs = new Dictionary<int, XpOrb>();
        int run, sequence;
        public void Reset(int generation) { run = generation; sequence = 0; Orbs.Clear(); }
        public bool Apply(GrowthEvent e)
        {
            if (e.Run != run || e.Sequence <= sequence) return false;
            if (e.Sequence != sequence + 1 || !Enum.IsDefined(typeof(GrowthEventKind), e.Kind)) throw new ArgumentException("Growth event sequence mismatch");
            if (e.Kind == GrowthEventKind.DeathOrb)
            {
                if (e.Orb.Id <= 0 || Orbs.ContainsKey(e.Orb.Id) || e.Orb.Position.y != 0 || !EnemyGeometry.ValidRay(new Ray(e.Orb.Position, Vector3.forward)))
                    throw new ArgumentException("Invalid orb");
                Orbs.Add(e.Orb.Id, e.Orb);
            }
            if (e.Kind == GrowthEventKind.Collected && !Orbs.Remove(e.Orb.Id)) throw new ArgumentException("Unknown collected orb");
            sequence = e.Sequence; return true;
        }
    }
}
