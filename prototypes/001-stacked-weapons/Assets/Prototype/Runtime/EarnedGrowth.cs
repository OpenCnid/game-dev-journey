using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Journey
{
    public enum UpgradeId { None, StrongerHits, FasterFire }
    public enum UpgradeTarget { OwnedWeapon, Player, LootPool, DrawPool }
    public enum UpgradeEffect { DirectDamage, FireCadence }
    public readonly struct UpgradeDefinition
    {
        public readonly UpgradeId Id;
        public readonly string Name;
        public readonly UpgradeTarget Target;
        public readonly UpgradeEffect Effect;
        public readonly float Factor;
        public UpgradeDefinition(UpgradeId id, string name, UpgradeTarget target, UpgradeEffect effect, float factor)
        { Id = id; Name = name; Target = target; Effect = effect; Factor = factor; }
    }
    // Content identity/eligibility is separate from the effect consumer. Only these two ordinary definitions exist.
    // Later player/pickup, loot and draw definitions route to their own consumers; they are not rifle stat edits.
    public static class GrowthUpgrade
    {
        static readonly UpgradeDefinition stronger = new UpgradeDefinition(UpgradeId.StrongerHits, "Stronger hits", UpgradeTarget.OwnedWeapon, UpgradeEffect.DirectDamage, 1.5f);
        static readonly UpgradeDefinition faster = new UpgradeDefinition(UpgradeId.FasterFire, "Faster fire", UpgradeTarget.OwnedWeapon, UpgradeEffect.FireCadence, 1.5f);
        public static bool Valid(UpgradeId value) => value == UpgradeId.StrongerHits || value == UpgradeId.FasterFire;
        public static UpgradeDefinition Definition(UpgradeId value) => value == UpgradeId.StrongerHits ? stronger : value == UpgradeId.FasterFire ? faster : throw new ArgumentException("Unknown upgrade definition");
        public static string Label(UpgradeId value) => Valid(value) ? Definition(value).Name : "Baseline";
        public static string Summary(UpgradeId value) => value == UpgradeId.FasterFire ? "+50% fire rate" : value == UpgradeId.StrongerHits ? "+50% damage" : "";
        public static int Damage(UpgradeId value) => Valid(value) && Definition(value).Effect == UpgradeEffect.DirectDamage ? Mathf.RoundToInt(20 * Definition(value).Factor) : 20;
        public static float Interval(UpgradeId value) => Valid(value) && Definition(value).Effect == UpgradeEffect.FireCadence ? .12f / Definition(value).Factor : .12f;
    }
    [Serializable] public struct PersonalOffer
    {
        public int Id, Tick, Weapon, AppliedTick, ControlRevision;
        public UpgradeId First, Selected;
        public UpgradeId Card(int index) => index == 1 ? First : index == 2 ?
            (First == UpgradeId.StrongerHits ? UpgradeId.FasterFire : UpgradeId.StrongerHits) : UpgradeId.None;
        public bool Pending => Id > 0 && Selected == UpgradeId.None;
    }
    public struct GrowthChoice { public int Run, Player, Offer, Weapon; public UpgradeId Upgrade; }
    [Serializable] public struct XpOrb
    {
        public int Id, Enemy, Incarnation, SourcePlayer, SourceWeapon, Shot, Value;
        public Vector3 Position;
    }
    public enum GrowthEventKind { DeathOrb, Collected, Entitlement, Applied, Moved, Merged }
    [Serializable] public struct GrowthEvent
    {
        public int Run, Tick, Sequence, Player, Xp, ConsumedOrb, ControlRevision;
        public GrowthEventKind Kind;
        public XpOrb Orb;
        public PersonalOffer Offer;
        public void Write(BinaryWriter w)
        {
            w.Write(Run); w.Write(Tick); w.Write(Sequence); w.Write(Player); w.Write(Xp); w.Write((int)Kind);
            w.Write(ConsumedOrb); w.Write(ControlRevision);
            w.Write(Orb.Id); w.Write(Orb.Enemy); w.Write(Orb.Incarnation); w.Write(Orb.SourcePlayer); w.Write(Orb.SourceWeapon); w.Write(Orb.Shot);
            w.Write(Orb.Value);
            SharedConnection.Vector(w, Orb.Position); WriteOffer(w, Offer);
        }
        public static GrowthEvent Read(BinaryReader r) => new GrowthEvent { Run = r.ReadInt32(), Tick = r.ReadInt32(), Sequence = r.ReadInt32(),
            Player = r.ReadInt32(), Xp = r.ReadInt32(), Kind = (GrowthEventKind)r.ReadInt32(),
            ConsumedOrb = r.ReadInt32(), ControlRevision = r.ReadInt32(),
            Orb = new XpOrb { Id = r.ReadInt32(), Enemy = r.ReadInt32(), Incarnation = r.ReadInt32(), SourcePlayer = r.ReadInt32(),
                SourceWeapon = r.ReadInt32(), Shot = r.ReadInt32(), Value = r.ReadInt32(), Position = SharedConnection.Vector(r) }, Offer = ReadOffer(r) };
        public static void WriteOffer(BinaryWriter w, PersonalOffer o)
        { w.Write(o.Id); w.Write(o.Tick); w.Write(o.Weapon); w.Write(o.AppliedTick); w.Write(o.ControlRevision); w.Write((int)o.First); w.Write((int)o.Selected); }
        public static PersonalOffer ReadOffer(BinaryReader r) => new PersonalOffer { Id = r.ReadInt32(), Tick = r.ReadInt32(), Weapon = r.ReadInt32(),
            AppliedTick = r.ReadInt32(), ControlRevision = r.ReadInt32(), First = (UpgradeId)r.ReadInt32(), Selected = (UpgradeId)r.ReadInt32() };
    }

    // Run-owned ledger, team threshold and personal draws. Collection is separate from effect application.
    public sealed class EarnedGrowth
    {
        public const int Threshold = 10;
        public const float PickupRadius = 2;
        public const float AttractionRadius = 1.5f, AttractionSpeed = 1.5f, MergeRadius = .3f;
        const int GridWidth = 48;
        readonly int[] heads = new int[GridWidth * GridWidth], tails = new int[GridWidth * GridWidth];
        readonly Dictionary<int, int> links = new Dictionary<int, int>();
        readonly List<int> ordered = new List<int>();
        public readonly Dictionary<int, XpOrb> Orbs = new Dictionary<int, XpOrb>();
        readonly HashSet<long> deaths = new HashSet<long>();
        readonly List<int> collected = new List<int>();
        readonly System.Random[] draws = new System.Random[2];
        int orbId, sequence;
        public int AttractionQueries { get; private set; }
        public int EarnedXp { get; private set; }
        public int UncollectedXp => EarnedXp - Xp;
        public int Xp { get; private set; }
        public int Level => Xp >= Threshold ? 2 : 1;
        public event Action<GrowthEvent> Changed;
        public void Reset(int seed)
        {
            Orbs.Clear(); deaths.Clear(); collected.Clear(); orbId = sequence = Xp = 0;
            EarnedXp = AttractionQueries = 0; ordered.Clear(); links.Clear();
            for (int p = 0; p < 2; p++) draws[p] = new System.Random(unchecked(seed * 397 ^ (p + 1) * 7919));
        }
        void Emit(SharedSnapshot s, GrowthEventKind kind, int player, XpOrb orb = default, PersonalOffer offer = default, int consumedOrb = 0)
        { Changed?.Invoke(new GrowthEvent { Run = s.Run, Tick = s.Tick, Sequence = ++sequence, Kind = kind, Player = player, Xp = Xp, Orb = orb, Offer = offer, ConsumedOrb = consumedOrb, ControlRevision = s.ControlRevision }); }
        public bool Death(SharedSnapshot s, SharedHit hit)
        {
            if (!hit.Shot.Killed || hit.Source.Run != s.Run) return false;
            long identity = ((long)hit.Shot.EnemyIndex << 32) | (uint)hit.Shot.SpawnVersion;
            if (!deaths.Add(identity)) return false;
            var orb = new XpOrb { Id = ++orbId, Enemy = hit.Shot.EnemyIndex, Incarnation = hit.Shot.SpawnVersion,
                SourcePlayer = hit.Source.Player, SourceWeapon = hit.Source.Weapon, Shot = hit.Source.Event, Value = 1,
                Position = new Vector3(hit.DeathPosition.x, 0, hit.DeathPosition.z) };
            Orbs.Add(orb.Id, orb); EarnedXp += orb.Value; Emit(s, GrowthEventKind.DeathOrb, hit.Source.Player, orb); return true;
        }
        static int GridCoordinate(float position) => Mathf.Clamp(Mathf.FloorToInt(position / AttractionRadius) + GridWidth / 2, 0, GridWidth - 1);
        public void AttractAndMerge(SharedSnapshot s)
        {
            AttractionQueries = 0;
            if (s.State != SharedState.Running || Orbs.Count < 2) return;
            Array.Clear(heads, 0, heads.Length); Array.Clear(tails, 0, tails.Length); links.Clear(); ordered.Clear();
            foreach (int id in Orbs.Keys) ordered.Add(id);
            ordered.Sort();
            foreach (int id in ordered)
            {
                Vector3 p = Orbs[id].Position; int cell = GridCoordinate(p.z) * GridWidth + GridCoordinate(p.x);
                links[id] = 0; if (heads[cell] == 0) heads[cell] = id; else links[tails[cell]] = id; tails[cell] = id;
            }
            // Stable oldest-first local attraction, with heavier carriers also eligible.
            // No global pair scan, deletion cap or physics objects; density limits still need capture evidence.
            foreach (int id in ordered)
            {
                if (!Orbs.TryGetValue(id, out XpOrb orb)) continue;
                int x = GridCoordinate(orb.Position.x), z = GridCoordinate(orb.Position.z), targetId = 0;
                for (int dz = -1; dz <= 1 && targetId == 0; dz++) for (int dx = -1; dx <= 1 && targetId == 0; dx++)
                {
                    if (x + dx < 0 || x + dx >= GridWidth || z + dz < 0 || z + dz >= GridWidth) continue;
                    int candidate = heads[(z + dz) * GridWidth + x + dx];
                    while (candidate != 0)
                    {
                        AttractionQueries++;
                        if (candidate != id && Orbs.TryGetValue(candidate, out XpOrb other) &&
                            (other.Value > orb.Value || other.Value == orb.Value && candidate < id) &&
                            (other.Position - orb.Position).sqrMagnitude <= AttractionRadius * AttractionRadius)
                        { targetId = candidate; break; }
                        candidate = links[candidate];
                    }
                }
                if (targetId == 0) continue;
                XpOrb target = Orbs[targetId];
                orb.Position = Vector3.MoveTowards(orb.Position, target.Position, AttractionSpeed * CombatSimulation.Tick);
                if ((orb.Position - target.Position).sqrMagnitude <= MergeRadius * MergeRadius)
                {
                    target.Value = checked(target.Value + orb.Value); Orbs[targetId] = target; Orbs.Remove(id);
                    Emit(s, GrowthEventKind.Merged, target.SourcePlayer, target, consumedOrb: id);
                }
                else
                {
                    Orbs[id] = orb;
                    if (s.Tick % 6 == 0) Emit(s, GrowthEventKind.Moved, orb.SourcePlayer, orb);
                }
            }
        }
        public void Collect(SharedSnapshot s)
        {
            if (s.State != SharedState.Running) return;
            collected.Clear();
            foreach (XpOrb orb in Orbs.Values)
                for (int p = 0; p < s.Roster; p++) if (s.Players[p].Health > 0 &&
                    (s.Players[p].Position - orb.Position).sqrMagnitude <= PickupRadius * PickupRadius)
                { collected.Add(orb.Id); Xp += orb.Value; Emit(s, GrowthEventKind.Collected, p, orb); break; }
            foreach (int id in collected) Orbs.Remove(id);
            s.Xp = Xp; s.Level = Level;
            if (Level == 2) for (int p = 0; p < s.Roster; p++) if (s.Players[p].Offer.Id == 0)
            {
                var offer = new PersonalOffer { Id = p + 1, Tick = s.Tick, Weapon = s.Players[p].Weapon,
                    First = draws[p].Next(2) == 0 ? UpgradeId.StrongerHits : UpgradeId.FasterFire };
                s.Players[p].Offer = offer; Emit(s, GrowthEventKind.Entitlement, p, offer: offer);
            }
        }
        public void Applied(SharedSnapshot s, int p) => Emit(s, GrowthEventKind.Applied, p, offer: s.Players[p].Offer);
    }
    // Reliable ordered incremental projection, never deletes uncollected orbs to fit a snapshot.
    public sealed class OrbReplica
    {
        public readonly Dictionary<int, XpOrb> Orbs = new Dictionary<int, XpOrb>();
        public int UncollectedXp { get; private set; }
        int run, sequence;
        public void Reset(int generation) { run = generation; sequence = UncollectedXp = 0; Orbs.Clear(); }
        public bool Apply(GrowthEvent e)
        {
            if (e.Run != run || e.Sequence <= sequence) return false;
            if (e.Sequence != sequence + 1 || !Enum.IsDefined(typeof(GrowthEventKind), e.Kind)) throw new ArgumentException("Growth event sequence mismatch");
            if (e.Kind == GrowthEventKind.DeathOrb || e.Kind == GrowthEventKind.Moved || e.Kind == GrowthEventKind.Merged)
            {
                if (e.Orb.Id <= 0 || e.Orb.Value < 1 || e.Orb.Position.y != 0 || !EnemyGeometry.ValidRay(new Ray(e.Orb.Position, Vector3.forward)))
                    throw new ArgumentException("Invalid orb");
                if (e.Kind == GrowthEventKind.DeathOrb)
                {
                    if (e.Orb.Value != 1 || Orbs.ContainsKey(e.Orb.Id)) throw new ArgumentException("Duplicate or invalid death XP");
                    Orbs.Add(e.Orb.Id, e.Orb); UncollectedXp += e.Orb.Value;
                }
                else
                {
                    if (!Orbs.TryGetValue(e.Orb.Id, out XpOrb prior) || e.Orb.Enemy != prior.Enemy || e.Orb.Incarnation != prior.Incarnation ||
                        e.Orb.SourcePlayer != prior.SourcePlayer || e.Orb.SourceWeapon != prior.SourceWeapon || e.Orb.Shot != prior.Shot) throw new ArgumentException("Unknown or changed carrier provenance");
                    if (e.Kind == GrowthEventKind.Merged)
                    {
                        if (e.ConsumedOrb == e.Orb.Id || !Orbs.TryGetValue(e.ConsumedOrb, out XpOrb consumed) ||
                            e.Orb.Value != checked(prior.Value + consumed.Value)) throw new ArgumentException("Non-conserving XP merge");
                        Orbs.Remove(e.ConsumedOrb);
                    }
                    else if (e.Orb.Value != prior.Value) throw new ArgumentException("XP changed during movement");
                    Orbs[e.Orb.Id] = e.Orb;
                }
            }
            if (e.Kind == GrowthEventKind.Collected)
            {
                if (!Orbs.TryGetValue(e.Orb.Id, out XpOrb collected) || collected.Value != e.Orb.Value) throw new ArgumentException("Unknown collected value");
                Orbs.Remove(e.Orb.Id); UncollectedXp -= collected.Value;
            }
            sequence = e.Sequence; return true;
        }
    }
}
