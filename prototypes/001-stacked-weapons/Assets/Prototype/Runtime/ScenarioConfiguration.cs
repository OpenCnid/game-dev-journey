using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace Journey
{
    public enum ScenarioKind { SoloPrecision = 1, SharedRifle = 2, DeveloperParity = 3 }

    // Starting definitions only. Earned personal build state belongs to the run authority.
    public sealed class ScenarioConfiguration
    {
        public const int Version = 1, MaxEnemies = 35;
        public readonly int Schema, Seed, EnemyCount, EnemyHealth, BodyDamage, HeadMultiplier;
        public readonly ScenarioKind Kind;
        public readonly float Duration, RifleInterval;
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
        public string Name => Kind == ScenarioKind.SoloPrecision ? "Solo precision" : Kind == ScenarioKind.SharedRifle ? "Shared rifle" : "Developer parity";
        public bool Overridden => (overridden ?? (overridden = !SameValues(Named(Kind)))).Value;
        public string Label => Name + (Overridden ? " (override)" : "");
        public string Settings => FormattableString.Invariant($"{EnemyCount} drones / {EnemyHealth} HP · Body {BodyDamage} / Head {HeadDamage} (x{HeadMultiplier}) · {RifleRate:0.##}/s ({RifleInterval:0.####}s)");

        public ScenarioConfiguration(int schema, ScenarioKind kind, int seed, int enemyCount, int enemyHealth,
            int bodyDamage, int headMultiplier, float duration, float rifleInterval)
        {
            if (schema != Version) throw new ArgumentException("Unsupported scenario version.");
            if (kind < ScenarioKind.SoloPrecision || kind > ScenarioKind.DeveloperParity) throw new ArgumentException("Unknown scenario.");
            if (enemyCount < 1 || enemyCount > MaxEnemies) throw new ArgumentException("Scenario supports 1–35 drones.");
            if (enemyHealth < 1 || enemyHealth > 10000 || bodyDamage < 1 || bodyDamage > 1000 || headMultiplier < 1 || headMultiplier > 4)
                throw new ArgumentException("HP must be 1–10000, body damage 1–1000 and head multiplier 1–4.");
            if (!(duration >= 1 && duration <= 600) || !(rifleInterval >= 1f / 120 && rifleInterval <= 2))
                throw new ArgumentException("Duration must be 1–600 seconds and rifle rate 0.5–120/s; values must be finite.");
            Schema = schema; Kind = kind; Seed = seed; EnemyCount = enemyCount; EnemyHealth = enemyHealth;
            BodyDamage = bodyDamage; HeadMultiplier = headMultiplier; Duration = duration; RifleInterval = rifleInterval;
        }
        public static ScenarioConfiguration Named(ScenarioKind kind)
        {
            if (kind == ScenarioKind.DeveloperParity) return new ScenarioConfiguration(Version, kind, 271, 12, 90, 15, 3, 60, .25f);
            return new ScenarioConfiguration(Version, kind, 137, kind == ScenarioKind.SoloPrecision ? 35 : 12,
                kind == ScenarioKind.SoloPrecision ? 40 : 60, 20, 2, 60, .12f);
        }
        public ScenarioConfiguration With(int? seed = null, int? count = null, int? hp = null, int? damage = null,
            int? head = null, float? duration = null, float? interval = null) => new ScenarioConfiguration(Schema, Kind,
                seed ?? Seed, count ?? EnemyCount, hp ?? EnemyHealth, damage ?? BodyDamage, head ?? HeadMultiplier, duration ?? Duration, interval ?? RifleInterval);
        public bool SameValues(ScenarioConfiguration other) => other != null && Schema == other.Schema && Kind == other.Kind && Seed == other.Seed &&
            EnemyCount == other.EnemyCount && EnemyHealth == other.EnemyHealth && BodyDamage == other.BodyDamage &&
            HeadMultiplier == other.HeadMultiplier && Duration == other.Duration && RifleInterval == other.RifleInterval;
        public void Write(BinaryWriter w)
        {
            w.Write(Schema); w.Write((int)Kind); w.Write(Seed); w.Write(EnemyCount); w.Write(EnemyHealth);
            w.Write(BodyDamage); w.Write(HeadMultiplier); w.Write(Duration); w.Write(RifleInterval);
        }
        public static ScenarioConfiguration Read(BinaryReader r) => new ScenarioConfiguration(r.ReadInt32(), (ScenarioKind)r.ReadInt32(),
            r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadSingle(), r.ReadSingle());
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
                    value == "developer-parity" ? ScenarioKind.DeveloperParity : throw new ArgumentException("Unknown -scenario.");
            }
            ScenarioConfiguration result = Named(kind);
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
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
        public int version, seed, enemies, enemyHp, bodyDamage, headMultiplier, headDamage, run;
        public float duration, rifleInterval, rifleRate;
        public string name, identity, session;
        public ScenarioEvidence(ScenarioConfiguration c, Guid session = default, int run = 0)
        {
            version = c.Schema; name = c.Label; identity = c.Identity; this.session = session.ToString("N"); this.run = run;
            seed = c.Seed; enemies = c.EnemyCount; enemyHp = c.EnemyHealth; bodyDamage = c.BodyDamage; headMultiplier = c.HeadMultiplier;
            headDamage = c.HeadDamage; duration = c.Duration; rifleInterval = c.RifleInterval; rifleRate = c.RifleRate;
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
            foreach (SharedPlayer player in incoming.Players)
                if (player.BodyDamage != Configuration.BodyDamage || player.RifleInterval != Configuration.RifleInterval)
                    throw new ArgumentException("Snapshot weapon configuration mismatch.");
            if (incoming.Run < 1 || incoming.Run > current.Run + 1 || incoming.Tick < 0 ||
                !(incoming.Elapsed >= 0 && incoming.Elapsed <= Configuration.Duration + CombatSimulation.Tick) ||
                incoming.State < SharedState.Running || incoming.State > SharedState.Aborted)
                throw new ArgumentException("Invalid snapshot generation/state.");
            return current.State != SharedState.Aborted && (incoming.Run == current.Run + 1 || incoming.Run == current.Run && incoming.Tick >= current.Tick);
        }
        public static void RequireEnd(BinaryReader r)
        { if (r.BaseStream.Position != r.BaseStream.Length) throw new ArgumentException("Unexpected trailing packet data."); }
    }
}
