using System;
using System.Collections.Generic;
using GameDevHarness.FantasyTrialB;
using UnityEngine;
using UnityEngine.AI;

namespace GameDevHarness.CrucibleReborn
{
    public sealed class ChaseArena : IDisposable
    {
        public Transform Root;
        public readonly List<NavMeshBuildSource> Sources = new List<NavMeshBuildSource>();
        public Bounds Bounds;
        public Vector3 PlayerStart;
        public Vector3[] Portals, ClimbPoints, KiteRoute, OuterRoute, FigureEightRoute, GapJumpRoute;
        public Vector3[][] SpawnSlots, ClimbApproaches, VerticalRoutes;
        public Vector3[][][] ClimbRoutes, ExitRoutes;
        public float[] LevelHeights;
        public Rect[] MapObstacles;
        public string[] LandmarkNames;
        public Light Sun { get; private set; }
        public int MeshTriangles { get; private set; }
        public readonly Rect[] FurnaceVents = { new Rect(-12, -2, 4, 8), new Rect(8, -2, 4, 8) };
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Renderer> details = new List<Renderer>();
        readonly Renderer[] portalCues = new Renderer[6], ventCues = new Renderer[2];
        readonly double[] warned = { -100, -100, -100, -100, -100, -100 };
        readonly MaterialPropertyBlock cueProperties = new MaterialPropertyBlock();
        Material stone, paleStone, iron, ash, bone, glow;

        public static ChaseArena Create(Transform parent, ArtPalette palette, int seed)
        {
            var arena = new ChaseArena();
            arena.Root = new GameObject("I shattered furnace cathedral").transform;
            arena.Root.SetParent(parent, false);
            arena.Bounds = new Bounds(Vector3.zero + Vector3.up * 10, new Vector3(100, 36, 92));
            arena.PlayerStart = new Vector3(0, .05f, -31);
            arena.stone = palette.Make(new Color(.42f, .38f, .33f), 0, .16f);
            arena.paleStone = palette.Make(new Color(.66f, .58f, .45f), 0, .12f);
            arena.iron = palette.Make(new Color(.26f, .22f, .19f), .8f, .24f);
            arena.ash = palette.Make(new Color(.3f, .27f, .25f), 0, .08f);
            arena.bone = palette.Make(new Color(.76f, .66f, .46f), 0, .18f);
            arena.glow = palette.Make(new Color(1, .18f, .025f), 0, .2f, 2);
            ChaseMaterials.Weather(arena.stone, 0);
            ChaseMaterials.Weather(arena.paleStone, 2);
            ChaseMaterials.Weather(arena.iron, 1);
            ChaseMaterials.WeatherAsh(arena.ash);
            ChaseMaterials.Weather(arena.bone, 2);
            arena.BuildGround();
            arena.BuildArchitecture();
            arena.BuildTraversal();
            arena.BuildPortals();
            arena.BuildVents();
            arena.BuildRoutes();
            ChaseDebris.Build(arena.Root, arena.bone, arena.ash, arena.iron, arena.Sources);
            ChaseScenery.Build(arena.Root, arena.stone, arena.iron);
            var sun = new GameObject("I dusk through collapsed vaults");
            sun.transform.SetParent(arena.Root, false);
            sun.transform.rotation = Quaternion.Euler(38, -32, 0);
            arena.Sun = sun.AddComponent<Light>();
            arena.Sun.type = LightType.Directional;
            arena.Sun.color = new Color(1, .78f, .56f);
            arena.Sun.intensity = 1.25f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.31f, .29f, .29f);
            RenderSettings.ambientEquatorColor = new Color(.23f, .21f, .2f);
            RenderSettings.ambientGroundColor = new Color(.13f, .11f, .1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.2f, .16f, .14f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = .006f;
            return arena;
        }

        // Same mesh supplies static physics and NavMesh input. Small bevels never
        // enter the walking surface: paving shares a continuous collision bed.
        Renderer Place(string name, RebornEnvironmentGeometry kit, Material material,
            bool solid = true, int layer = 8, bool cosmetic = false)
        {
            Mesh mesh = kit.Mesh(name);
            meshes.Add(mesh); MeshTriangles += mesh.triangles.Length / 3;
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (solid)
            {
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                Sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh, transform = go.transform.localToWorldMatrix, area = 0 });
            }
            if (cosmetic) details.Add(renderer);
            return renderer;
        }

        void CollisionBed(Vector3 center, Vector3 size)
        {
            var go = new GameObject("I continuous paving bed") { layer = 9 };
            go.transform.SetParent(Root, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider>().size = size;
            Sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                size = size, transform = go.transform.localToWorldMatrix, area = 0 });
        }

        void BuildGround()
        {
            CollisionBed(new Vector3(0, -.35f, 0), new Vector3(96, .7f, 88));
            // Distinct 12x8-meter chunks cull independently. Offset joints, chipped
            // outlines, charcoal insets and exposed edges replace a visible slab.
            for (int cx = 0; cx < 8; cx++)
                for (int cz = 0; cz < 11; cz++)
                {
                    var paving = new RebornEnvironmentGeometry();
                    for (int x = 0; x < 3; x++)
                        for (int z = 0; z < 4; z++)
                            paving.Block(new Vector3(-46 + cx * 12 + x * 4, -.16f,
                                -43 + cz * 8 + z * 2), new Vector3(3.96f, .32f, 1.96f),
                                Quaternion.identity, .12f);
                    Place("I jointed nave paving " + cx + "-" + cz, paving,
                        (cx + cz) % 4 == 0 ? ash : stone, false, 9);
                }
        }

        void BuildArchitecture()
        {
            var masonry = new RebornEnvironmentGeometry();
            var ribs = new RebornEnvironmentGeometry();
            var metal = new RebornEnvironmentGeometry();
            // Six transverse nave frames, deliberately incomplete above clear
            // combat lanes. The 16m opening leaves >5m agent head clearance.
            for (int bay = 0; bay < 6; bay++)
            {
                float z = -25 + bay * 10;
                masonry.PointedArch(new Vector3(0, 0, z), 8, 5.5f, .75f, 1.2f,
                    Quaternion.identity, 14, bay == 2 || bay == 4);
                if (bay == 0 || bay == 3 || bay == 5)
                    ribs.PointedArch(new Vector3(0, 0, z + 1.8f), 8.1f, 5.5f,
                        .3f, 3.5f, Quaternion.identity, 14, bay == 3);
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 8.7f;
                    for (int row = 0; row < 7; row++)
                        masonry.Block(new Vector3(x, .4f + row * .79f, z),
                            new Vector3(1.5f, .76f, 1.7f), Quaternion.identity, .13f);
                    // Stepped, sloping masonry buttress: visibly transfers load.
                    masonry.BrokenPier(new Vector3(side * 11.2f, 0, z), 2.3f, 4.8f, 2.4f,
                        Quaternion.Euler(0, side * 14, 0));
                    ribs.PointedArch(new Vector3(side * 22, 5, z), 4.1f, 5.5f, .48f, .8f,
                        Quaternion.identity, 10, bay % 3 == 1);
                    for (int offset = -1; offset <= 1; offset += 2)
                        masonry.BrokenPier(new Vector3(side * 22 + offset * 4.7f, 0, z),
                            1.2f, 10.7f, 1.4f, Quaternion.identity);
                }
            }
            // Remaining diagonal vault ribs between bays, not sealed ceilings.
            for (int i = 0; i < 4; i++)
            {
                ribs.PointedArch(new Vector3(0, 0, -20 + i * 10), 6.5f, 7,
                    .38f, .42f, Quaternion.Euler(0, 35, 0), 12, i == 2);
                ribs.PointedArch(new Vector3(0, 0, -20 + i * 10), 6.5f, 7,
                    .38f, .42f, Quaternion.Euler(0, -35, 0), 12, i == 1);
            }
            // Thick perimeter walls are coursed and breached; entry combat
            // stays inside them. Uneven tops have no flat safe camping ledge.
            for (int side = -1; side <= 1; side += 2)
                for (int z = -40; z <= 40; z += 5)
                {
                    masonry.BrokenPier(new Vector3(side * 47, 0, z), 4.9f,
                        5.4f + (z + 40) % 3, 1.9f, Quaternion.Euler(0, 90, 0));
                    for (int k = 0; k < 4; k++)
                        metal.Tube(new[] { new Vector3(side * 45.9f, 0, z + k - 1.5f),
                            new Vector3(side * 45.9f, 3.3f, z + k - 1.5f),
                            new Vector3(side * 45.6f, 3.8f, z + k - 1.5f) },
                            new[] { .045f, .045f, .006f }, 6);
                }
            for (int side = -1; side <= 1; side += 2)
                for (int x = -44; x <= 44; x += 4)
                    masonry.BrokenPier(new Vector3(x, 0, side * 43), 3.96f,
                        5.1f + (x + 44) % 3, 1.8f, Quaternion.identity);
            Place("I load-bearing broken masonry", masonry, stone);
            Place("I pale voussoirs and diagonal vault ribs", ribs, paleStone);
            Place("I forged perimeter teeth", metal, iron, false);
            MapObstacles = new Rect[12];
            for (int i = 0; i < 6; i++)
            {
                MapObstacles[i * 2] = new Rect(-12.5f, -26.3f + i * 10, 4.6f, 2.6f);
                MapObstacles[i * 2 + 1] = new Rect(7.9f, -26.3f + i * 10, 4.6f, 2.6f);
            }
        }

        void Deck(RebornEnvironmentGeometry kit, float x, float z, float width, float length, float y)
        {
            for (float a = -width / 2; a < width / 2 - .1f; a += 2)
                for (float b = -length / 2; b < length / 2 - .1f; b += 2)
                    kit.Block(new Vector3(x + a + 1, y - .3f, z + b + 1),
                        new Vector3(2, .6f, 2), Quaternion.identity, 0);
        }

        void Stair(RebornEnvironmentGeometry kit, Vector3 foot, Vector3 direction, float rise, float run, float width)
        {
            int steps = Mathf.RoundToInt(rise / .25f);
            Quaternion rotation = Quaternion.LookRotation(direction);
            for (int i = 0; i < steps; i++)
            {
                float height = (i + 1) * rise / steps;
                kit.Block(foot + direction * ((i + .5f) * run / steps) + Vector3.up * (height / 2),
                    new Vector3(width, height, run / steps), rotation, 0);
            }
        }

        void BuildTraversal()
        {
            var decks = new RebornEnvironmentGeometry();
            var trim = new RebornEnvironmentGeometry();
            for (int side = -1; side <= 1; side += 2)
            {
                Deck(decks, side * 22, 2, 8, 32, 5);
                Stair(decks, new Vector3(side * 22, 0, -30), Vector3.forward, 5, 16, 8);
                Stair(decks, new Vector3(side * 22, 0, 34), Vector3.back, 5, 16, 8);
                for (int z = -12; z <= 16; z += 4)
                    for (int edge = -1; edge <= 1; edge += 2)
                        trim.Block(new Vector3(side * 22 + edge * 3.9f, 4.45f, z),
                            new Vector3(.3f, .8f, 3.9f), Quaternion.identity);
            }
            Deck(decks, 0, 14, 36, 8, 5);
            Deck(decks, 0, -17, 8, 8, 2.5f);
            Stair(decks, new Vector3(-14, 0, -17), Vector3.right, 2.5f, 10, 8);
            Stair(decks, new Vector3(14, 0, -17), Vector3.left, 2.5f, 10, 8);
            Place("I gallery decks and twenty-riser flights", decks, paleStone, true, 9);
            Place("I gallery edge cornices", trim, stone, false);
            // Every raised surface has two independent walkable approaches;
            // debris remains <=1.6m, inside EnemyMotor's real jump envelope.
            ClimbPoints = new[] { new Vector3(-22, 5, 0), new Vector3(22, 5, 0),
                new Vector3(0, 5, 14), new Vector3(0, 2.5f, -17) };
            LevelHeights = new[] { 0f, 2.5f, 5f };
            LandmarkNames = new[] { "West ossuary gallery", "East furnace gallery",
                "Broken choir crossing", "Coal altar" };
        }

        void BuildPortals()
        {
            Portals = new[] { new Vector3(-36, 0, -32), new Vector3(36, 0, -32),
                new Vector3(-37, 0, 30), new Vector3(37, 0, 30),
                new Vector3(-4, 0, 38), new Vector3(4, 0, 38) };
            SpawnSlots = new Vector3[Portals.Length][];
            for (int i = 0; i < Portals.Length; i++)
            {
                Vector3 p = Portals[i];
                var portal = new RebornEnvironmentGeometry();
                portal.PointedArch(p, 2, 1.1f, .45f, .7f, Quaternion.identity, 9);
                // Tall hollow portals do not cap a climbable solid cube.
                portal.BrokenPier(p + Vector3.left * 2.23f, .7f, 1.15f, .9f, Quaternion.identity);
                portal.BrokenPier(p + Vector3.right * 2.23f, .7f, 1.15f, .9f, Quaternion.identity);
                Place("I furnace arrival gate " + i, portal, iron);
                var cue = new RebornEnvironmentGeometry();
                cue.Block(p + Vector3.up * .025f, new Vector3(2.5f, .025f, 1.2f), Quaternion.identity, 0);
                portalCues[i] = Place("I arrival warning inlay " + i, cue, glow, false);
                SpawnSlots[i] = new Vector3[12];
                for (int j = 0; j < 12; j++)
                    SpawnSlots[i][j] = p + new Vector3((j % 4 - 1.5f) * .8f, .05f, -2.5f - j / 4 * 1.1f);
            }
        }

        void BuildVents()
        {
            var frame = new RebornEnvironmentGeometry();
            for (int i = 0; i < FurnaceVents.Length; i++)
            {
                Rect r = FurnaceVents[i];
                for (float z = r.yMin; z <= r.yMax; z += .5f)
                    frame.Block(new Vector3(r.center.x, .02f, z),
                        new Vector3(r.width, .04f, .08f), Quaternion.identity, 0);
                var glowKit = new RebornEnvironmentGeometry();
                glowKit.Block(new Vector3(r.center.x, .01f, r.center.y),
                    new Vector3(r.width, .015f, r.height), Quaternion.identity, 0);
                ventCues[i] = Place("I furnace vent emissive " + i, glowKit, glow, false);
            }
            Place("I flush wrought vent gratings", frame, iron, false);
        }

        void BuildRoutes()
        {
            OuterRoute = new[] { new Vector3(-32, 0, -36), new Vector3(32, 0, -36),
                new Vector3(32, 0, 37), new Vector3(-32, 0, 37), new Vector3(-32, 0, -36) };
            KiteRoute = new[] { new Vector3(-16, 0, -31), new Vector3(16, 0, -31),
                new Vector3(16, 0, 7), new Vector3(-16, 0, 7), new Vector3(-16, 0, -31) };
            FigureEightRoute = new[] { new Vector3(0, 0, 0), new Vector3(-16, 0, 0),
                new Vector3(-16, 0, 7), new Vector3(0, 0, 7), new Vector3(0, 0, 0),
                new Vector3(16, 0, 0), new Vector3(16, 0, -8), new Vector3(0, 0, -8),
                new Vector3(0, 0, 0) };
            // Short OPTIONAL jump across a low ash rib pile; continuous walking
            // bypass exists on either side rather than a disconnected nav island.
            GapJumpRoute = new[] { new Vector3(-5, 0, 25), new Vector3(0, 1.4f, 25),
                new Vector3(5, 0, 25) };
            VerticalRoutes = new Vector3[4][];
            ClimbApproaches = new Vector3[4][];
            ClimbRoutes = new Vector3[4][][];
            ExitRoutes = new Vector3[4][][];
            for (int i = 0; i < 4; i++)
            {
                Vector3[] a, b;
                if (i < 2)
                {
                    float x = i == 0 ? -22 : 22;
                    a = new[] { new Vector3(x, 0, -31), new Vector3(x, 2.5f, -22),
                        new Vector3(x, 5, -13), ClimbPoints[i] };
                    b = new[] { new Vector3(x, 0, 35), new Vector3(x, 2.5f, 26),
                        new Vector3(x, 5, 17), ClimbPoints[i] };
                }
                else if (i == 2)
                {
                    a = new[] { new Vector3(-22, 0, 35), new Vector3(-22, 2.5f, 26),
                        new Vector3(-22, 5, 12), new Vector3(0, 5, 12), ClimbPoints[i] };
                    b = new[] { new Vector3(22, 0, 35), new Vector3(22, 2.5f, 26),
                        new Vector3(22, 5, 12), new Vector3(0, 5, 12), ClimbPoints[i] };
                }
                else
                {
                    a = new[] { new Vector3(-15, 0, -17), new Vector3(-9, 1.25f, -17), ClimbPoints[i] };
                    b = new[] { new Vector3(15, 0, -17), new Vector3(9, 1.25f, -17), ClimbPoints[i] };
                }
                ClimbApproaches[i] = new[] { a[0], b[0] };
                ClimbRoutes[i] = new[] { a, b };
                var ra = (Vector3[])a.Clone(); Array.Reverse(ra);
                var rb = (Vector3[])b.Clone(); Array.Reverse(rb);
                ExitRoutes[i] = new[] { ra, rb };
                VerticalRoutes[i] = a;
            }
        }

        public bool TryGetClimbApproach(int landmark, int approach, out Vector3 point)
        {
            Vector3 authored = Root.TransformPoint(ClimbApproaches[landmark][approach]);
            if (NavMesh.SamplePosition(authored, out var hit, .2f, NavMesh.AllAreas) &&
                Mathf.Abs(hit.position.y - authored.y) <= .15f)
            { point = hit.position; return true; }
            point = default; return false;
        }

        public void Telegraph(int portal, double elapsed) { warned[portal] = elapsed; }
        public void Animate(double elapsed, int activePortals)
        {
            for (int i = 0; i < portalCues.Length; i++)
            {
                double age = elapsed - warned[i];
                float intensity = age >= 0 && age < 2 ? 1.5f + .6f * Mathf.Sin((float)age * 16) :
                    i < activePortals ? .65f : .04f;
                Tint(portalCues[i], intensity);
            }
            for (int i = 0; i < ventCues.Length; i++)
            {
                float phase = VentPhase(i, elapsed);
                Tint(ventCues[i], phase < 2 ? .18f + phase * .65f : phase < 4 ? 2.2f : .025f);
            }
        }
        void Tint(Renderer target, float intensity)
        {
            cueProperties.Clear();
            cueProperties.SetColor("_EmissionColor", new Color(1, .13f, .012f) * intensity);
            target.SetPropertyBlock(cueProperties);
        }
        static float VentPhase(int index, double elapsed) => (float)((elapsed + index * 4) % 12);
        public int HazardDamageAt(Vector3 feet, double elapsed)
        {
            Vector3 local = Root.InverseTransformPoint(feet);
            if (local.y > .5f || local.y < -.3f) return 0;
            for (int i = 0; i < FurnaceVents.Length; i++)
                if (FurnaceVents[i].Contains(new Vector2(local.x, local.z)) &&
                    VentPhase(i, elapsed) >= 2 && VentPhase(i, elapsed) < 4) return 8;
            return 0;
        }
        public void SetQuality(int preset)
        {
            foreach (Renderer renderer in details) renderer.enabled = preset > 0;
            ChaseScenery.SetQuality(preset);
        }
        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
            foreach (Mesh mesh in meshes) UnityEngine.Object.Destroy(mesh);
            meshes.Clear(); Sources.Clear();
            ChaseDebris.Dispose();
            ChaseScenery.Dispose();
        }
    }
}
