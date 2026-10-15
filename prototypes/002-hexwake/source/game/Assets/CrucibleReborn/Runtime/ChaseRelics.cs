namespace GameDevHarness.CrucibleReborn
{
    public enum ChaseRelicKind { Windstep, Executioner, BloodGlass, VolatileHeart, Quicksilver, EchoSeal }
    public enum ChaseLootKind { Weapon, Relic }

    public sealed class ChaseRelic
    {
        public ChaseRelicKind Kind;
        public int Rank = 1;
    }

    public sealed class ChaseLoot
    {
        public int Id;
        public UnityEngine.Vector3 Position;
        public ChaseLootKind Kind;
        public ChaseWeaponKind Weapon;
        public ChaseRelicKind Relic;
    }

    public static class ChaseRelics
    {
        public static string Name(ChaseRelicKind kind)
        {
            switch (kind)
            {
                case ChaseRelicKind.Windstep: return "Windstep Spurs";
                case ChaseRelicKind.Executioner: return "Executioner's Eye";
                case ChaseRelicKind.BloodGlass: return "Blood Glass";
                case ChaseRelicKind.VolatileHeart: return "Volatile Heart";
                case ChaseRelicKind.Quicksilver: return "Quicksilver Feed";
                case ChaseRelicKind.EchoSeal: return "Echo Seal";
                default: throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static string Description(ChaseRelicKind kind)
        {
            switch (kind)
            {
                case ChaseRelicKind.Windstep: return "Stronger directional air steering; levels strengthen the correction.";
                case ChaseRelicKind.Executioner: return "Direct head hits return ammunition; levels shorten the return cooldown.";
                case ChaseRelicKind.BloodGlass: return "Direct head hits steal health; levels strengthen the heal.";
                case ChaseRelicKind.VolatileHeart: return "Below half health, direct shots gain damage but enemy contact hurts more.";
                case ChaseRelicKind.Quicksilver: return "Shorter reloads and a brief damage boost after completing a reload.";
                case ChaseRelicKind.EchoSeal: return "Every third aimed direct hit echoes to a visible nearby foe for reduced damage.";
                default: throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
