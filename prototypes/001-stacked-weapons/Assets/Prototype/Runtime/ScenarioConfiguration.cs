using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace Journey
{
    public enum ScenarioKind { SoloPrecision = 1, SharedRifle = 2, DeveloperParity = 3, EarnedGrowth = 4, WeaponStack = 5, StackReference = 6, CoopScale = 7 }

    // Starting definitions only. Earned personal build state belongs to the run authority.
    public sealed class ScenarioConfiguration
    {
        public const int Version = 3, MaxEnemies = 1000;
        public readonly int Schema, Seed, EnemyCount, EnemyHealth, BodyDamage, HeadMultiplier;
        public readonly ScenarioKind Kind;
        public readonly float Duration, RifleInterval;
        public readonly int StartingArsenal;
        public readonly UpgradeId StartingSpecialization;
        public float RifleRate => 1f / RifleInterval;
        public int HeadDamage => BodyDamage * HeadMultiplier;
        string identity;
        bool? overridden;
        public string Identity
        {
            get
            {
                if (identity == null) using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                using (var hash = SHA256.Create())
                { Write(writer); writer.Flush(); identity = BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant(); }
                return identity;
            }
        }
        public bool Growth => Kind == ScenarioKind.EarnedGrowth || Kind == ScenarioKind.WeaponStack;
        public bool Scaling => Kind == ScenarioKind.CoopScale;
        public bool Pickups => Growth || Scaling;
        public bool Stacking => Kind == ScenarioKind.WeaponStack || Kind == ScenarioKind.StackReference || Scaling;
        public bool SharedSolo => Pickups || Stacking;
        public int LevelCap => Kind == ScenarioKind.WeaponStack ? 3 : 2;
        public string Name => Kind == ScenarioKind.SoloPrecision ? "Solo precision" : Kind == ScenarioKind.SharedRifle ? "Shared rifle" : Kind == ScenarioKind.DeveloperParity ? "Developer parity" : Kind == ScenarioKind.EarnedGrowth ? "Earned growth" : Kind == ScenarioKind.WeaponStack ? "Weapon stack" : Scaling ? "Co-op scale (diagnostic)" : "Stack reference (diagnostic)";
        public bool Overridden => (overridden ?? (overridden = !SameValues(Named(Kind)))).Value;
        public string Label => Name + (Overridden ? " (override)" : "");
        public string Settings => FormattableString.Invariant($"{EnemyCount} drones / {EnemyHealth} HP · Body {BodyDamage} / Head {HeadDamage} (x{HeadMultiplier}) · {RifleRate:0.##}/s ({RifleInterval:0.####}s)") + (Kind == ScenarioKind.StackReference || Scaling ? " · " + WeaponArsenal.Label(StartingArsenal) + (StartingSpecialization == UpgradeId.None ? "" : " · " + GrowthUpgrade.Label(StartingSpecialization)) : "") + (Scaling ? " · invulnerable / frozen arsenal / collected XP" : "");

        public ScenarioConfiguration(int schema, ScenarioKind kind, int seed, int enemyCount, int enemyHealth,
            int bodyDamage, int headMultiplier, float duration, float rifleInterval, int startingArsenal = WeaponArsenal.Rifle, UpgradeId startingSpecialization = UpgradeId.None)
        {
            if (schema != Version) throw new ArgumentException("Unsupported scenario version.");
            if (kind < ScenarioKind.SoloPrecision || kind > ScenarioKind.CoopScale) throw new ArgumentException("Unknown scenario.");
            if (enemyCount < 1 || enemyCount > (kind == ScenarioKind.CoopScale ? MaxEnemies : 35)) throw new ArgumentException("Ordinary fixtures support 1–35 drones; scale diagnostics support 1–1000.");
            if (enemyHealth < 1 || enemyHealth > 10000 || bodyDamage < 1 || bodyDamage > 1000 || headMultiplier < 1 || headMultiplier > 4)
                throw new ArgumentException("HP must be 1–10000, body damage 1–1000 and head multiplier 1–4.");
            if (!(duration >= 1 && duration <= 600) || !(rifleInterval >= 1f / 120 && rifleInterval <= 2))
                throw new ArgumentException("Duration must be 1–600 seconds and rifle rate 0.5–120/s; values must be finite.");
            Schema = schema; Kind = kind;
            if (Scaling && (seed != 137 || enemyHealth != 60 || bodyDamage != 20 || headMultiplier != 2 || rifleInterval != .12f ||
                startingArsenal != 1 && startingArsenal != 7 && startingArsenal != 255 || startingSpecialization != UpgradeId.None))
                throw new ArgumentException("Scale diagnostics keep fixed combat tuning and 1/3/8 owned instances.");
            if ((kind == ScenarioKind.EarnedGrowth || kind == ScenarioKind.WeaponStack || kind == ScenarioKind.StackReference) &&
                (seed != 137 || enemyCount != 35 || enemyHealth != 60 || bodyDamage != 20 || headMultiplier != 2 || duration != (kind == ScenarioKind.WeaponStack ? 120 : 60) || rifleInterval != .12f))
                throw new ArgumentException("Growth/stack scenarios use fixed test settings; tuning overrides are disabled.");
            if (!Scaling && (startingArsenal != 1 && startingArsenal != 3 && startingArsenal != 5 && startingArsenal != 7 ||
                startingSpecialization != UpgradeId.None && startingSpecialization != UpgradeId.WiderScatter && startingSpecialization != UpgradeId.DeeperPierce ||
                startingSpecialization == UpgradeId.WiderScatter && startingArsenal != 3 || startingSpecialization == UpgradeId.DeeperPierce && startingArsenal != 5 ||
                kind != ScenarioKind.StackReference && (startingArsenal != 1 || startingSpecialization != UpgradeId.None)))
                throw new ArgumentException("Starting loadouts are only valid for explicit stack references.");
            StartingArsenal = startingArsenal; StartingSpecialization = startingSpecialization;
            Seed = seed; EnemyCount = enemyCount; EnemyHealth = enemyHealth;
            BodyDamage = bodyDamage; HeadMultiplier = headMultiplier; Duration = duration; RifleInterval = rifleInterval;
        }
        public static ScenarioConfiguration Named(ScenarioKind kind)
        {
            if (kind == ScenarioKind.CoopScale) return new ScenarioConfiguration(Version, kind, 137, 35, 60, 20, 2, 90, .12f, 7);
            if (kind == ScenarioKind.EarnedGrowth) return new ScenarioConfiguration(Version, kind, 137, 35, 60, 20, 2, 60, .12f);
            if (kind == ScenarioKind.WeaponStack || kind == ScenarioKind.StackReference) return new ScenarioConfiguration(Version, kind, 137, 35, 60, 20, 2, kind == ScenarioKind.WeaponStack ? 120 : 60, .12f);
            if (kind == ScenarioKind.DeveloperParity) return new ScenarioConfiguration(Version, kind, 271, 12, 90, 15, 3, 60, .25f);
            return new ScenarioConfiguration(Version, kind, 137, kind == ScenarioKind.SoloPrecision ? 35 : 12,
                kind == ScenarioKind.SoloPrecision ? 40 : 60, 20, 2, 60, .12f);
        }
        public ScenarioConfiguration With(int? seed = null, int? count = null, int? hp = null, int? damage = null,
            int? head = null, float? duration = null, float? interval = null) => new ScenarioConfiguration(Schema, Kind,
                seed ?? Seed, count ?? EnemyCount, hp ?? EnemyHealth, damage ?? BodyDamage, head ?? HeadMultiplier, duration ?? Duration, interval ?? RifleInterval, StartingArsenal, StartingSpecialization);
        public ScenarioConfiguration Reference(string loadout)
        {
            if (Kind != ScenarioKind.StackReference) throw new ArgumentException("-stackLoadout requires stack-reference");
            int mask = loadout == "rifle" ? 1 : loadout == "all" ? 7 : loadout == "scatter" ? 3 : loadout == "pierce" ? 5 : throw new ArgumentException("Unknown stack reference loadout");
            return new ScenarioConfiguration(Schema, Kind, Seed, EnemyCount, EnemyHealth, BodyDamage, HeadMultiplier, Duration, RifleInterval, mask,
                loadout == "scatter" ? UpgradeId.WiderScatter : loadout == "pierce" ? UpgradeId.DeeperPierce : UpgradeId.None);
        }
        public ScenarioConfiguration Scale(int count, int instances, float duration)
        {
            if (!Scaling || instances != 1 && instances != 3 && instances != 8) throw new ArgumentException("-scaleWeapons requires co-op-scale and 1/3/8 instances.");
            return new ScenarioConfiguration(Schema, Kind, Seed, count, EnemyHealth, BodyDamage, HeadMultiplier, duration, RifleInterval, (1 << instances) - 1);
        }
        public bool SameValues(ScenarioConfiguration other) => other != null && Schema == other.Schema && Kind == other.Kind && Seed == other.Seed &&
            EnemyCount == other.EnemyCount && EnemyHealth == other.EnemyHealth && BodyDamage == other.BodyDamage &&
            HeadMultiplier == other.HeadMultiplier && Duration == other.Duration && RifleInterval == other.RifleInterval && StartingArsenal == other.StartingArsenal && StartingSpecialization == other.StartingSpecialization;
        public void Write(BinaryWriter w)
        {
            w.Write(Schema); w.Write((int)Kind); w.Write(Seed); w.Write(EnemyCount); w.Write(EnemyHealth);
            w.Write(BodyDamage); w.Write(HeadMultiplier); w.Write(Duration); w.Write(RifleInterval);
            w.Write(StartingArsenal); w.Write((int)StartingSpecialization);
        }
        public static ScenarioConfiguration Read(BinaryReader r) => new ScenarioConfiguration(r.ReadInt32(), (ScenarioKind)r.ReadInt32(),
            r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadSingle(), r.ReadSingle(), r.ReadInt32(), (UpgradeId)r.ReadInt32());
        public static float IntervalForRate(float rate)
        {
            if (!(rate >= .5f && rate <= 120)) throw new ArgumentException("Rifle rate must be 0.5–120/s.");
            return Math.Abs(rate - CombatSimulation.DefaultRifleRate) < .00001f ? .12f : 1f / rate;
        }
        public static ScenarioConfiguration FromArguments(string[] args, ScenarioKind fallback)
        {
            ScenarioKind kind = fallback;
            for (int i = 0; i < args.Length; i++) if (args[i] == "-scenario")
            {
                string value = Value(args, i);
                kind = value == "solo-precision" ? ScenarioKind.SoloPrecision : value == "shared-rifle" ? ScenarioKind.SharedRifle :
                    value == "developer-parity" ? ScenarioKind.DeveloperParity : value == "earned-growth" ? ScenarioKind.EarnedGrowth : value == "weapon-stack" ? ScenarioKind.WeaponStack : value == "stack-reference" ? ScenarioKind.StackReference : value == "co-op-scale" ? ScenarioKind.CoopScale : throw new ArgumentException("Unknown -scenario.");
            }
            ScenarioConfiguration result = Named(kind);
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-stackLoadout": result = result.Reference(Value(args, i)); break;
                    case "-scaleWeapons": result = result.Scale(result.EnemyCount, int.Parse(Value(args, i), CultureInfo.InvariantCulture), result.Duration); break;
                    case "-seed": result = result.With(seed: int.Parse(Value(args, i), CultureInfo.InvariantCulture)); break;
                    case "-scenarioEnemies": result = result.With(count: int.Parse(Value(args, i), CultureInfo.InvariantCulture)); break;
                    case "-scenarioHp": result = result.With(hp: int.Parse(Value(args, i), CultureInfo.InvariantCulture)); break;
                    case "-scenarioDamage": result = result.With(damage: int.Parse(Value(args, i), CultureInfo.InvariantCulture)); break;
                    case "-scenarioHead": result = result.With(head: int.Parse(Value(args, i), CultureInfo.InvariantCulture)); break;
                    case "-scenarioDuration": result = result.With(duration: float.Parse(Value(args, i), CultureInfo.InvariantCulture)); break;
                    case "-rifleRate": result = result.With(interval: IntervalForRate(float.Parse(Value(args, i), CultureInfo.InvariantCulture))); break;
                }
            }
            return result;
        }
        static string Value(string[] args, int i) => i + 1 < args.Length ? args[i + 1] : throw new ArgumentException("Missing value for " + args[i]);
    }

    [Serializable] public sealed class ScenarioEvidence
    {
        public int version, seed, enemies, enemyHp, bodyDamage, headMultiplier, headDamage, run, startingArsenal;
        public UpgradeId startingSpecialization;
        public float duration, rifleInterval, rifleRate;
        public string name, identity, session;
        public ScenarioEvidence(ScenarioConfiguration c, Guid session = default, int run = 0)
        {
            version = c.Schema; name = c.Label; identity = c.Identity; this.session = session.ToString("N"); this.run = run;
            seed = c.Seed; enemies = c.EnemyCount; enemyHp = c.EnemyHealth; bodyDamage = c.BodyDamage; headMultiplier = c.HeadMultiplier;
            headDamage = c.HeadDamage; duration = c.Duration; rifleInterval = c.RifleInterval; rifleRate = c.RifleRate;
            startingArsenal = c.StartingArsenal; startingSpecialization = c.StartingSpecialization;
        }
    }

    // Frozen for the connection lifetime. Retry changes run generation, never starting definitions.
    public sealed class SharedScenarioBinding
    {
        public Guid Session { get; private set; }
        public ScenarioConfiguration Configuration { get; private set; }
        public bool Agreed { get; private set; }
        public SharedScenarioBinding(ScenarioConfiguration hostConfiguration = null)
        { Configuration = hostConfiguration; Session = hostConfiguration == null ? Guid.Empty : Guid.NewGuid(); }
        public void Write(BinaryWriter w) { w.Write(Session.ToByteArray()); Configuration.Write(w); }
        public void AcceptOffer(BinaryReader r)
        {
            Guid session = new Guid(r.ReadBytes(16)); var configuration = ScenarioConfiguration.Read(r); RequireEnd(r);
            if (Configuration != null || Session != Guid.Empty || session == Guid.Empty) throw new ArgumentException("Stale or duplicate scenario offer.");
            Session = session; Configuration = configuration; Agreed = true;
        }
        public void AcceptAcknowledgement(BinaryReader r)
        {
            Guid session = new Guid(r.ReadBytes(16)); var configuration = ScenarioConfiguration.Read(r); RequireEnd(r);
            if (Agreed || Configuration == null || session != Session || !Configuration.SameValues(configuration))
                throw new ArgumentException("Stale or mismatched scenario acknowledgement.");
            Agreed = true;
        }
        public bool Matches(SharedSnapshot snapshot) => Agreed && snapshot.Session == Session && Configuration.SameValues(snapshot.Configuration);
        public bool CanAdopt(SharedSnapshot incoming, SharedSnapshot current)
        {
            if (!Matches(incoming)) throw new ArgumentException("Snapshot configuration/session mismatch.");
            if (incoming.Roster != 2) throw new ArgumentException("Invalid shared roster");
            for (int p = 0; p < incoming.Roster; p++)
            {
                SharedPlayer player = incoming.Players[p]; PersonalOffer offer = player.Offer;
                if (player.Weapon != p + 1) throw new ArgumentException("Invalid owned weapon");
                if (Configuration.Scaling)
                {
                    if (incoming.Xp < 0 || incoming.Level != 1 || !offer.Equals(default(PersonalOffer)) || player.ChoiceReady ||
                        player.Arsenal != Configuration.StartingArsenal || player.FirstAddition != UpgradeId.None || player.Specialization != UpgradeId.None ||
                        player.BodyDamage != Configuration.BodyDamage || player.RifleInterval != Configuration.RifleInterval ||
                        incoming.Run == current.Run && incoming.Tick >= current.Tick && incoming.Xp < current.Xp)
                        throw new ArgumentException("Invalid diagnostic build/XP snapshot.");
                }
                else if (Configuration.Growth)
                {
                    if (Configuration.Kind == ScenarioKind.WeaponStack)
                    { if (!WeaponArsenal.ValidBuild(incoming, p)) throw new ArgumentException("Invalid stacked build snapshot"); }
                    else if (incoming.Xp < 0 || incoming.Level != (incoming.Xp >= EarnedGrowth.Threshold ? 2 : 1) ||
                        (incoming.Level == 1 && (offer.Id != 0 || offer.Selected != UpgradeId.None || offer.AppliedTick != 0)) ||
                        (offer.Selected == UpgradeId.None && (offer.AppliedTick != 0 || offer.ControlRevision != 0)) ||
                        (incoming.Level == 2 && (offer.Id != p + 1 || offer.Weapon != player.Weapon ||
                        offer.Tick < 1 || offer.Tick > incoming.Tick || offer.First != UpgradeId.StrongerHits && offer.First != UpgradeId.FasterFire || !offer.Contains(UpgradeId.StrongerHits) || !offer.Contains(UpgradeId.FasterFire))) ||
                        (offer.Selected != UpgradeId.None && (!offer.Contains(offer.Selected) || offer.AppliedTick < offer.Tick || offer.AppliedTick > incoming.Tick || offer.ControlRevision < 1 || offer.ControlRevision > incoming.ControlRevision)) ||
                        (player.ChoiceReady && offer.Selected == UpgradeId.None) ||
                        player.BodyDamage != GrowthUpgrade.Damage(offer.Selected) || player.RifleInterval != GrowthUpgrade.Interval(offer.Selected) || player.Arsenal != 1 || player.FirstAddition != UpgradeId.None || player.Specialization != UpgradeId.None)
                        throw new ArgumentException("Invalid earned build snapshot");
                    PersonalOffer previous = current.Players[p].Offer;
                    if (incoming.Run == current.Run && incoming.Tick >= current.Tick && (incoming.Xp < current.Xp || incoming.Level < current.Level ||
                        current.Players[p].FirstAddition != UpgradeId.None && player.FirstAddition != current.Players[p].FirstAddition ||
                        previous.Id > 0 && incoming.Level == current.Level && (offer.Id != previous.Id || offer.First != previous.First || offer.Second != previous.Second || offer.Weapon != previous.Weapon || offer.Tick != previous.Tick ||
                        previous.Selected != UpgradeId.None && (offer.Selected != previous.Selected || offer.AppliedTick != previous.AppliedTick || offer.ControlRevision != previous.ControlRevision)) || incoming.Level == current.Level && current.Players[p].ChoiceReady && !player.ChoiceReady))
                        throw new ArgumentException("Earned state cannot regress or reroll");
                }
                else if (offer.Id != 0 || incoming.Xp != 0 || incoming.Level != 1 || player.BodyDamage != Configuration.BodyDamage || player.RifleInterval != Configuration.RifleInterval || player.Arsenal != Configuration.StartingArsenal || player.FirstAddition != UpgradeId.None || player.Specialization != Configuration.StartingSpecialization)
                    throw new ArgumentException("Snapshot weapon configuration mismatch.");
            }
            if (incoming.Run < 1 || incoming.Run > current.Run + 1 || incoming.Tick < 0 ||
                !(incoming.Elapsed >= 0 && incoming.Elapsed <= Configuration.Duration + CombatSimulation.Tick) ||
                incoming.State < SharedState.Running || incoming.State > SharedState.Choosing || incoming.ControlRevision < 0 ||
                incoming.State == SharedState.Choosing && (!Configuration.Growth || incoming.Level < 2 || incoming.ChoicePhase != incoming.Level - 1))
                throw new ArgumentException("Invalid snapshot generation/state.");
            return current.State != SharedState.Aborted && (incoming.Run == current.Run + 1 || incoming.Run == current.Run && incoming.Tick >= current.Tick && incoming.ControlRevision >= current.ControlRevision);
        }
        public static void RequireEnd(BinaryReader r)
        { if (r.BaseStream.Position != r.BaseStream.Length) throw new ArgumentException("Unexpected trailing packet data."); }
    }
}
