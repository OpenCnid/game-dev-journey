using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public enum ChaseWeaponTier { Common, Uncommon, Rare, Mythic }
    public static class ChaseCatalog
    {
        private static readonly string[] Names = {
            "Stormsplit", "Volley", "Sentry", "Razor", "Frenzy", "Warcry",
            "Cyclone", "Magma", "Bone Lance", "Graveburst", "Sanctifier", "Orbit Hammer",
            "Inferno", "Winterglass", "Pactbinder", "Abyss"
        };
        private static readonly string[] Classes = {
            "Amazon", "Assassin", "Barbarian", "Druid", "Necromancer", "Paladin", "Sorceress", "Warlock"
        };
        private static readonly string[] Descriptions = {
            "A lightning strike branches twice into nearby foes. Each enemy is struck once.",
            "Five horizontal shots fan outward; each lane stops at its first foe or wall.",
            "Place a surface sentry that shoots nearby foes. Three sentries per gun; oldest replaced.",
            "A spinning razor pierces foes and ricochets up to three times off real surfaces.",
            "Successful repeater hits build five stacks of faster fire and movement; stacks fade after three seconds.",
            "A broad damaging cry stuns nearby foes for one second.",
            "A traveling cyclone deals repeated damage, strongly pulls foes and reduces movement by 65%.",
            "A rolling magma ball bursts on a foe or wall, leaving damaging burning ground.",
            "A narrow bone lance pierces every foe along a clear straight line.",
            "Ignite a foe. Alternate fire consumes a nearby corpse for a large explosion.",
            "A piercing holy beam deals extra damage to undead and heals on a killing hit.",
            "A hammer spirals outward, striking foes along its swept path.",
            "A sustained fire cone ignites every exposed foe with a three-second burn.",
            "A traveling cold orb emits eight shard rays and slows foes for two seconds.",
            "A curse shot slows foes. Alternate fire binds a demon below 35% health for twelve seconds.",
            "Place a small abyss that gently draws nearby foes inward. One field per gun; larger threats resist its pull."
        };
        private static readonly Color[] Colors = {
            new Color(.35f, .8f, 1), new Color(.9f, .75f, .2f),
            new Color(.55f, .4f, 1), new Color(.85f, .9f, 1),
            new Color(1, .3f, .16f), new Color(1, .65f, .15f),
            new Color(.3f, 1, .75f), new Color(1, .28f, .08f),
            new Color(.8f, .95f, .6f), new Color(.6f, 1, .2f),
            new Color(1, .9f, .45f), new Color(1, .75f, .3f),
            new Color(1, .4f, .1f), new Color(.4f, .8f, 1),
            new Color(.9f, .35f, .85f), new Color(.65f, .3f, 1)
        };

        public static string Name(ChaseWeaponKind kind) => Names[(int)kind];
        public static string ClassName(ChaseWeaponKind kind) => Classes[(int)kind / 2];
        public static string Description(ChaseWeaponKind kind) => Descriptions[(int)kind];
        public static Color Color(ChaseWeaponKind kind) => Colors[(int)kind];
        public static ChaseWeaponTier Tier(ChaseWeaponKind kind)
        {
            switch (kind)
            {
                case ChaseWeaponKind.Stormsplit:
                case ChaseWeaponKind.Volley:
                case ChaseWeaponKind.Frenzy:
                case ChaseWeaponKind.Warcry: return ChaseWeaponTier.Common;
                case ChaseWeaponKind.Sentry:
                case ChaseWeaponKind.Razor:
                case ChaseWeaponKind.Inferno:
                case ChaseWeaponKind.Winterglass: return ChaseWeaponTier.Uncommon;
                case ChaseWeaponKind.Cyclone:
                case ChaseWeaponKind.BoneLance:
                case ChaseWeaponKind.Graveburst:
                case ChaseWeaponKind.Pactbinder: return ChaseWeaponTier.Rare;
                default: return ChaseWeaponTier.Mythic;
            }
        }
        public static string Tradeoff(ChaseWeaponKind kind)
        {
            switch (kind)
            {
                case ChaseWeaponKind.Stormsplit: return "Light single-target damage; clustered enemies feed its chain.";
                case ChaseWeaponKind.Volley: return "Close fan burst; spread wastes damage at range.";
                case ChaseWeaponKind.Sentry: return "Sustained placement damage, but stationary and slow to redeploy.";
                case ChaseWeaponKind.Razor: return "Rapid piercing ricochets; strongest beside walls.";
                case ChaseWeaponKind.Frenzy: return "Weak opening hits; accuracy builds extreme firing speed.";
                case ChaseWeaponKind.Warcry: return "Very short reach; broad control rescues surrounded runners.";
                case ChaseWeaponKind.Cyclone: return "Weak immediate damage; powerful lane control and repeated exposure.";
                case ChaseWeaponKind.Magma: return "Devastating area burst; slow cycle and a small magazine.";
                case ChaseWeaponKind.BoneLance: return "Heavy precision headshots and piercing; narrow aim and slower reload rhythm.";
                case ChaseWeaponKind.Graveburst: return "Modest solo shots; corpses turn it into an explosive build.";
                case ChaseWeaponKind.Sanctifier: return "Heavy piercing damage; especially lethal to undead, slow fire.";
                case ChaseWeaponKind.OrbitHammer: return "High close-range sweeps; distant targets escape its spiral.";
                case ChaseWeaponKind.Inferno: return "Huge close crowd damage; brief reach demands risky positioning.";
                case ChaseWeaponKind.Winterglass: return "Low direct strength; shard placement and slows multiply crowd value.";
                case ChaseWeaponKind.Pactbinder: return "Moderate shots; weakened demons become temporary allies.";
                case ChaseWeaponKind.Abyss: return "Rare control engine; negligible solo damage and long cooldown.";
                default: throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
