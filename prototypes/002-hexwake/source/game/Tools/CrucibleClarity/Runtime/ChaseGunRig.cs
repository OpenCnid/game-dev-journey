using System;
using System.Collections.Generic;
using GameDevHarness.FantasyTrialB;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public sealed class ChaseGunRig : IDisposable
    {
        public Transform Root { get; private set; }
        public Transform Muzzle { get; private set; }
        public ChaseWeaponKind Kind { get; private set; }
        public int CopyId { get; private set; }
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly Dictionary<Transform, Dictionary<Material, RebornWeaponGeometry>> drawings =
            new Dictionary<Transform, Dictionary<Material, RebornWeaponGeometry>>();
        readonly List<Transform> leaves = new List<Transform>();
        Transform frame, action, feed;
        Material steel, oxide, brass, hide, ceramic, energy;
        Vector3 mount, actionRest, feedRest;
        float recoil, rotation, reload;

        public static ChaseGunRig Create(Transform parent, ArtPalette palette,
            ChaseWeaponKind kind, int copyId, int slot)
        {
            var rig = new ChaseGunRig { Kind = kind, CopyId = copyId };
            rig.Root = RebornWeaponGeometry.Group(parent, "I " + kind + " engineered copy " + copyId, Vector3.zero);
            // One large primary on the right; five smaller complete silhouettes
            // occupy distinct side bands. Nothing stacks over the aiming aperture.
            Vector3[] mounts = { new Vector3(.25f, -.22f, .38f), new Vector3(-.30f, -.21f, .49f),
                new Vector3(.48f, -.025f, .64f), new Vector3(-.48f, -.025f, .64f),
                new Vector3(.70f, .16f, .86f), new Vector3(-.70f, .16f, .86f) };
            rig.mount = mounts[slot];
            rig.Root.localPosition = rig.mount;
            rig.Root.localScale = Vector3.one * (slot == 0 ? 1 : slot == 1 ? .62f : .48f);
            // Cool silver, blue enamel and warm/payload-colored fittings stay
            // distinct in both the dark nave and against the brighter arena.
            Color payload = ChaseCatalog.Color(kind);
            rig.steel = palette.Make(new Color(.72f, .82f, .88f), .35f, .48f, .06f);
            rig.oxide = palette.Make(new Color(.18f, .27f, .36f), .12f, .36f, .08f);
            rig.brass = palette.Make(Color.Lerp(new Color(.96f, .65f, .27f), payload, .70f), .28f, .48f, .18f);
            rig.hide = palette.Make(new Color(.58f, .28f, .12f), 0, .19f);
            rig.ceramic = palette.Make(Color.Lerp(new Color(.94f, .88f, .70f), payload, .22f), .08f, .32f, .10f);
            rig.energy = palette.Make(payload, .08f, .4f, .90f);
            MachinedSurface(rig.steel); MachinedSurface(rig.oxide); MachinedSurface(rig.brass);
            ChaseMaterials.Weather(rig.hide, 4); ChaseMaterials.Weather(rig.ceramic, 2);
            rig.frame = RebornWeaponGeometry.Group(rig.Root, "I fixed machining", Vector3.zero);
            rig.action = RebornWeaponGeometry.Group(rig.Root, "I exposed " + kind + " action", new Vector3(0, .055f, .20f));
            rig.feed = RebornWeaponGeometry.Group(rig.Root, "I " + kind + " replaceable charge", new Vector3(0, -.025f, .13f));
            rig.actionRest = rig.action.localPosition; rig.feedRest = rig.feed.localPosition;
            rig.Handling();
            rig.Design();
            foreach (var group in rig.drawings)
                foreach (var material in group.Value)
                {
                    Mesh mesh = material.Value.Build(group.Key.name + " " + material.Key.color);
                    rig.meshes.Add(mesh); RebornWeaponGeometry.Render(group.Key, mesh, material.Key);
                }
            rig.drawings.Clear();
            float exit = kind == ChaseWeaponKind.Volley ? .54f :
                kind == ChaseWeaponKind.Frenzy ? .595f :
                kind == ChaseWeaponKind.Cyclone ? .61f :
                kind == ChaseWeaponKind.Abyss ? .63f : .64f;
            rig.Muzzle = RebornWeaponGeometry.Group(rig.Root, "I clear exit aperture", new Vector3(0, .055f, exit));
            return rig;
        }

        static void MachinedSurface(Material material)
        {
            ChaseMaterials.Weather(material, 1);
            // Keep the existing machining relief and AO, but do not multiply
            // clean gun finishes by brown rust. The packed rust map otherwise
            // replaces Make's metallic/gloss values on Standard materials.
            material.mainTexture = null;
            material.SetTexture("_MetallicGlossMap", null);
            material.DisableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_BumpScale", .35f);
            material.SetFloat("_OcclusionStrength", .45f);
        }

        RebornWeaponGeometry Drawing(Transform parent, Material material)
        {
            if (!drawings.TryGetValue(parent, out var batch))
            { batch = new Dictionary<Material, RebornWeaponGeometry>(); drawings.Add(parent, batch); }
            if (!batch.TryGetValue(material, out var geometry))
            { geometry = new RebornWeaponGeometry(); batch.Add(material, geometry); }
            return geometry;
        }

        void Plate(Transform parent, Material material, float width, float[] outline,
            Vector3 offset = default(Vector3), Quaternion rotation = default(Quaternion))
        {
            if (rotation == default(Quaternion)) rotation = Quaternion.identity;
            Drawing(parent, material).Profile(Matrix4x4.TRS(offset, rotation, Vector3.one), width, outline);
        }

        void Bore(Transform parent, Material material, Vector3 p, float length, float radius,
            float opening, float flare = 1, Quaternion rotation = default(Quaternion))
        {
            if (rotation == default(Quaternion)) rotation = Quaternion.identity;
            Drawing(parent, material).Bore(Matrix4x4.TRS(p, rotation, Vector3.one),
                length, radius, opening, flare);
        }

        Transform Leaf(string name, Vector3 pivot)
        {
            var leaf = RebornWeaponGeometry.Group(Root, name, pivot);
            leaves.Add(leaf); return leaf;
        }

        void Handling()
        {
            Plate(frame, hide, .045f, new float[] { -.005f,-.035f, .053f,-.045f, .074f,-.16f,
                .035f,-.18f, -.025f,-.065f });
            Plate(frame, brass, .055f, new float[] { .03f,-.045f, .085f,-.045f,
                .103f,-.075f, .095f,-.10f, .076f,-.097f, .09f,-.076f, .078f,-.057f, .03f,-.057f });
            // Front support pad is at the anatomical support palm, not a common chassis.
            Plate(frame, hide, .052f, new float[] { .245f,-.055f, .335f,-.054f,
                .325f,-.078f, .25f,-.078f });
        }

        void Design()
        {
            // Every entry is a new fabrication drawing. Long open frames expose
            // their actions from behind and above instead of hiding them in domes.
            switch (Kind)
            {
                case ChaseWeaponKind.Stormsplit:
                    Plate(frame, oxide, .054f, new float[] { -.045f,-.027f, .14f,-.03f, .18f,.016f,
                        .53f,.016f, .60f,.04f, .57f,.069f, .17f,.05f, .11f,.083f, -.03f,.058f });
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Plate(frame, ceramic, .018f, new float[] { .17f,.052f, .55f,.064f, .55f,.087f,
                            .19f,.09f }, new Vector3(s * .043f, 0, 0));
                        for (int i = 0; i < 6; i++)
                            Bore(frame, brass, new Vector3(s * .043f,.072f,.23f+i*.048f), .014f,.023f,.016f);
                    }
                    Plate(action, steel, .075f, new float[] { -.10f,-.018f, .06f,-.018f,
                        .09f,.017f, .025f,.039f, -.085f,.034f });
                    Bore(feed, energy, new Vector3(0,-.025f,-.05f), .085f,.026f,.016f);
                    Bore(frame, steel, new Vector3(0,.055f,.57f), .07f,.030f,.017f);
                    break;
                case ChaseWeaponKind.Volley:
                    Plate(frame, brass, .12f, new float[] { -.04f,-.025f, .13f,-.046f,
                        .25f,.008f, .28f,.055f, .19f,.079f, -.025f,.054f });
                    for (int i = -2; i <= 2; i++)
                    {
                        Quaternion yaw = Quaternion.Euler(0, i * 9, 0);
                        Bore(frame, oxide, new Vector3(i*.034f,.055f,.24f), .30f,.021f,.012f,1.13f,yaw);
                    }
                    Plate(action, steel, .17f, new float[] { -.105f,-.02f, .018f,-.025f,
                        .045f,.005f, .005f,.033f, -.1f,.026f });
                    Plate(feed, hide, .11f, new float[] { -.02f,-.01f, .10f,-.015f,
                        .085f,-.10f, -.035f,-.087f });
                    break;
                case ChaseWeaponKind.Sentry:
                    Plate(frame, oxide, .075f, new float[] { -.04f,-.03f, .1f,-.043f, .17f,-.01f,
                        .20f,.16f, .17f,.20f, .11f,.14f, .10f,.035f, -.035f,.02f });
                    Plate(action, ceramic, .12f, new float[] { -.025f,-.01f, .10f,-.01f,
                        .13f,.095f, .08f,.14f, -.005f,.09f });
                    for (int s = -1; s <= 1; s += 2)
                        Plate(frame, brass, .018f, new float[] { .23f,.02f,.38f,.025f,.41f,-.02f,
                            .36f,-.045f,.30f,-.013f,.23f,-.016f }, new Vector3(s*.075f,0,0));
                    Plate(feed, steel, .065f, new float[] { -.025f,-.015f,.085f,-.015f,.09f,-.09f,-.01f,-.115f });
                    Bore(frame, steel, new Vector3(0,.055f,.44f), .20f,.025f,.017f);
                    break;
                case ChaseWeaponKind.Razor:
                    Plate(frame, oxide, .035f, new float[] { -.04f,-.03f,.18f,-.046f,.49f,-.02f,
                        .58f,.015f,.55f,.04f,.20f,.01f,.095f,.03f,-.025f,.025f });
                    Bore(action, brass, new Vector3(0,0,.095f), .017f,.115f,.095f);
                    for (int i = 0; i < 12; i++)
                        Plate(action, steel, .006f, new float[] { .072f,-.01f,.132f,-.007f,
                            .14f,.02f,.087f,.033f }, new Vector3(0,0,.105f),
                            Quaternion.Euler(0,90,i*30));
                    Plate(feed, steel, .11f, new float[] { -.05f,-.04f,.08f,-.04f,.10f,-.09f,-.035f,-.11f });
                    Bore(frame, oxide, new Vector3(0,.055f,.585f), .055f,.029f,.019f);
                    break;
                case ChaseWeaponKind.Frenzy:
                    Plate(frame, steel, .055f, new float[] { -.045f,-.045f,.12f,-.05f,.20f,-.017f,
                        .19f,.028f,.055f,.067f,-.035f,.04f });
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3;
                        Bore(action, oxide, new Vector3(Mathf.Cos(a)*.052f,Mathf.Sin(a)*.052f,.035f),
                            .36f,.016f,.009f);
                    }
                    Bore(action, brass, new Vector3(0,0,.15f), .023f,.078f,.064f);
                    Bore(action, steel, new Vector3(0,0,.345f), .025f,.074f,.063f);
                    Plate(feed, brass, .085f, new float[] { -.08f,-.01f,.03f,-.03f,.055f,-.13f,-.06f,-.15f });
                    break;
                case ChaseWeaponKind.Warcry:
                    Plate(frame, brass, .045f, new float[] { -.05f,-.02f,.13f,-.04f,.31f,-.04f,
                        .39f,.01f,.37f,.043f,.10f,.025f,-.04f,.043f });
                    for (int i = 0; i < 8; i++)
                        Bore(action, hide, new Vector3(0,0,i*.024f), .018f,
                            i%2==0?.071f:.060f,.045f);
                    Bore(frame, brass, new Vector3(0,.055f,.40f), .24f,.057f,.042f,1.7f);
                    Plate(feed, hide, .08f, new float[] { -.05f,-.035f,.06f,-.035f,.06f,-.12f,-.035f,-.09f });
                    break;
                case ChaseWeaponKind.Cyclone:
                    Plate(frame, oxide, .035f, new float[] { -.04f,-.04f,.24f,-.06f,.38f,-.013f,
                        .34f,.015f,.12f,-.016f,.06f,.056f,-.03f,.044f });
                    Bore(frame, steel, new Vector3(0,.055f,.34f), .27f,.092f,.073f,.80f);
                    for (int i = 0; i < 9; i++)
                        Plate(action, brass, .009f, new float[] { .025f,-.015f,.09f,-.01f,
                            .078f,.039f,.035f,.046f },new Vector3(0,0,.16f),Quaternion.Euler(0,90,i*40));
                    Bore(action, steel, new Vector3(0,0,.09f), .20f,.022f,.014f);
                    Bore(feed, energy, new Vector3(.06f,-.025f,-.03f), .16f,.032f,.02f);
                    break;
                case ChaseWeaponKind.Magma:
                    Plate(frame, steel, .054f, new float[] { -.03f,-.025f,.16f,-.053f,.32f,-.045f,
                        .47f,.008f,.43f,.045f,.17f,.008f,.065f,.07f,-.02f,.057f });
                    Bore(action, ceramic, new Vector3(0,0,.035f), .15f,.055f,.038f,1.5f);
                    Bore(action, brass, new Vector3(0,0,.16f), .022f,.087f,.063f);
                    for (int i=0;i<4;i++)
                        Plate(frame, oxide,.017f,new float[] { .26f,-.025f,.43f,.02f,.40f,.035f,.27f,.013f },
                            new Vector3((i-1.5f)*.046f,0,0));
                    Bore(frame, oxide,new Vector3(0,.055f,.47f),.17f,.07f,.047f,.80f);
                    Plate(feed, energy,.052f,new float[] { -.02f,-.02f,.075f,-.02f,.04f,-.115f,-.005f,-.10f });
                    break;
                case ChaseWeaponKind.BoneLance:
                    Plate(frame, ceramic,.043f,new float[] { -.06f,-.024f,.11f,-.035f,.17f,.01f,
                        .57f,.038f,.61f,.074f,.18f,.058f,.08f,.10f,-.045f,.066f });
                    for(int i=0;i<7;i++)
                        Bore(frame, ceramic,new Vector3(0,.055f,.22f+i*.045f),.019f,.046f,.025f);
                    Plate(action, oxide,.052f,new float[] { -.10f,-.026f,.06f,-.022f,.08f,.02f,
                        -.085f,.038f });
                    Plate(feed, ceramic,.065f,new float[] { -.035f,-.02f,.08f,-.02f,.09f,-.10f,.055f,-.14f,-.03f,-.09f });
                    Bore(frame, steel,new Vector3(0,.055f,.57f),.07f,.025f,.013f);
                    break;
                case ChaseWeaponKind.Graveburst:
                    Plate(frame, oxide,.055f,new float[] { -.05f,-.037f,.11f,-.047f,.17f,-.006f,
                        .26f,.005f,.25f,.073f,.11f,.083f,-.035f,.055f });
                    Plate(action, ceramic,.10f,new float[] { -.055f,-.027f,.065f,-.027f,.094f,.016f,
                        .072f,.07f,-.025f,.084f,-.055f,.05f });
                    Plate(action, brass,.108f,new float[] { -.02f,.005f,.057f,.005f,.057f,.025f,-.02f,.025f });
                    Bore(frame, oxide,new Vector3(0,.055f,.29f),.35f,.053f,.031f,.78f);
                    Plate(feed, hide,.05f,new float[] { -.035f,-.02f,.075f,-.035f,.12f,-.09f,.09f,-.12f,-.025f,-.08f });
                    break;
                case ChaseWeaponKind.Sanctifier:
                    Plate(frame, brass,.045f,new float[] { -.035f,-.025f,.12f,-.043f,.26f,.006f,
                        .33f,.14f,.30f,.18f,.26f,.09f,.16f,.025f,.085f,.082f,-.025f,.052f });
                    Plate(action, ceramic,.074f,new float[] { -.04f,-.015f,.07f,-.015f,.09f,.06f,
                        .015f,.15f,-.04f,.07f });
                    for(int s=-1;s<=1;s+=2)
                        Plate(frame, brass,.012f,new float[] { .34f,.035f,.58f,.04f,.59f,.075f,.36f,.09f },
                            new Vector3(s*.043f,0,0));
                    Bore(frame, ceramic,new Vector3(0,.055f,.57f),.07f,.041f,.027f);
                    Bore(feed, energy,new Vector3(0,-.02f,-.035f),.09f,.021f,.01f);
                    break;
                case ChaseWeaponKind.OrbitHammer:
                    Plate(frame, steel,.047f,new float[] { -.04f,-.035f,.14f,-.045f,.36f,-.045f,
                        .48f,.018f,.45f,.053f,.18f,.015f,.08f,.065f,-.035f,.042f });
                    Bore(action, brass,new Vector3(0,0,.10f),.022f,.109f,.086f);
                    for(int i=0;i<3;i++)
                        Plate(action, steel,.055f,new float[] { .06f,-.024f,.13f,-.027f,.145f,.014f,
                            .08f,.023f },new Vector3(0,0,.12f),Quaternion.Euler(0,90,i*120));
                    Plate(feed, oxide,.08f,new float[] { -.06f,-.018f,.08f,-.018f,.05f,-.10f,-.035f,-.10f });
                    Bore(frame, steel,new Vector3(0,.055f,.53f),.11f,.049f,.033f);
                    break;
                case ChaseWeaponKind.Inferno:
                    Plate(frame, oxide,.042f,new float[] { -.06f,-.022f,.13f,-.03f,.25f,.005f,
                        .30f,.063f,.24f,.092f,.10f,.038f,-.05f,.047f });
                    Bore(frame, steel,new Vector3(0,.055f,.26f),.38f,.034f,.022f,1.5f);
                    Bore(frame, brass,new Vector3(.044f,.023f,.24f),.37f,.008f,.004f);
                    Bore(frame, brass,new Vector3(-.044f,.023f,.24f),.37f,.008f,.004f);
                    Bore(feed, hide,new Vector3(.066f,-.064f,-.035f),.15f,.049f,.027f);
                    Plate(action, brass,.076f,new float[] { -.075f,-.025f,.045f,-.025f,.06f,.005f,
                        .02f,.025f,-.06f,.019f });
                    for(int i=0;i<4;i++)
                        Bore(frame, oxide,new Vector3(0,.055f,.38f+i*.055f),.014f,.049f,.036f);
                    break;
                case ChaseWeaponKind.Winterglass:
                    Plate(frame, ceramic,.037f,new float[] { -.04f,-.032f,.15f,-.043f,.28f,.015f,
                        .50f,.023f,.56f,.073f,.52f,.10f,.30f,.06f,.13f,.072f,-.03f,.049f });
                    for(int i=0;i<6;i++)
                    {
                        float a=i*Mathf.PI/3;
                        Plate(frame, steel,.012f,new float[] { .24f,0,.48f,.005f,.50f,.026f,.27f,.035f },
                            new Vector3(Mathf.Cos(a)*.064f,.055f+Mathf.Sin(a)*.064f,0));
                    }
                    Bore(action, energy,new Vector3(0,0,.06f),.16f,.040f,.022f);
                    Bore(frame, ceramic,new Vector3(0,.055f,.55f),.09f,.069f,.044f);
                    Plate(feed, oxide,.057f,new float[] { -.03f,-.018f,.07f,-.018f,.08f,-.12f,.025f,-.15f,-.02f,-.09f });
                    break;
                case ChaseWeaponKind.Pactbinder:
                    Plate(frame, oxide,.061f,new float[] { -.045f,-.035f,.14f,-.047f,.24f,.015f,
                        .27f,.064f,.18f,.09f,.09f,.057f,-.035f,.05f });
                    for(int s=-1;s<=1;s+=2)
                    {
                        Transform jaw=Leaf("I chain mandible "+s,new Vector3(s*.052f,.055f,.32f));
                        Plate(jaw, steel,.024f,new float[] { -.04f,-.015f,.12f,-.04f,.26f,.002f,
                            .27f,.036f,.19f,.065f,.20f,.036f,.13f,.016f,.01f,.034f,-.03f,.025f });
                        for(int i=0;i<5;i++)
                            Bore(frame,brass,new Vector3(s*.06f,.025f,.13f+i*.032f),.015f,.018f,.012f,
                                1,Quaternion.Euler(0,90,i%2*90));
                    }
                    Plate(action,brass,.08f,new float[] { -.085f,-.012f,.045f,-.012f,.02f,.039f,-.075f,.033f });
                    Plate(feed,hide,.067f,new float[] { -.05f,-.015f,.07f,-.02f,.06f,-.115f,-.035f,-.095f });
                    Bore(frame,oxide,new Vector3(0,.055f,.58f),.06f,.025f,.017f);
                    break;
                case ChaseWeaponKind.Abyss:
                    Plate(frame, oxide,.042f,new float[] { -.04f,-.035f,.15f,-.035f,.32f,-.01f,
                        .39f,.022f,.38f,.062f,.16f,.03f,.08f,.071f,-.03f,.046f });
                    Bore(frame,brass,new Vector3(0,.055f,.41f),.22f,.105f,.086f,.90f);
                    for(int i=0;i<8;i++)
                    {
                        Transform iris=Leaf("I aperture blade "+i,new Vector3(0,.055f,.57f));
                        iris.localRotation=Quaternion.Euler(0,0,i*45);
                        Plate(iris,steel,.005f,new float[] { .025f,-.011f,.083f,-.04f,
                            .09f,.011f,.04f,.038f,.014f,.027f },Vector3.zero,Quaternion.Euler(0,90,0));
                    }
                    Bore(action,energy,new Vector3(0,0,.09f),.095f,.046f,.028f);
                    Plate(feed,ceramic,.055f,new float[] { -.055f,-.02f,.085f,-.02f,.06f,-.12f,-.03f,-.10f });
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(Kind));
            }
        }

        public void Fire() { if (Root != null) recoil = 1; }

        public void Reload(float progress)
        {
            if (Root == null) return;
            reload = progress < 0 ? 0 : Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            feed.localPosition = feedRest + new Vector3(0,-.14f * reload,-.035f * reload);
            feed.localRotation = Quaternion.Euler(0,0,-22 * reload);
        }

        public void Animate(float dt, float elapsed, float movement, Vector3 aimPoint)
        {
            if (Root == null || dt <= 0) return;
            recoil = Mathf.MoveTowards(recoil,0,dt*5.5f);
            rotation = (rotation + dt * recoil * 940) % 360;
            Root.localPosition = mount + new Vector3(Mathf.Sin(elapsed*7)*.003f*movement,
                Mathf.Cos(elapsed*14)*.002f*movement-reload*.024f,-recoil*.029f);
            Vector3 direction=Root.parent.InverseTransformPoint(aimPoint)-Root.localPosition;
            Root.localRotation = Quaternion.Euler(
                Mathf.Clamp(-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg,-10,10)-recoil*3+reload*9,
                Mathf.Clamp(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-17,17),reload*8);
            action.localPosition=actionRest;
            action.localRotation=Quaternion.identity; action.localScale=Vector3.one;
            switch(Kind)
            {
                case ChaseWeaponKind.Razor: case ChaseWeaponKind.Frenzy:
                case ChaseWeaponKind.Cyclone: case ChaseWeaponKind.OrbitHammer:
                    action.localRotation=Quaternion.Euler(0,0,rotation); break;
                case ChaseWeaponKind.Warcry:
                    action.localScale=new Vector3(1,1,1-recoil*.32f); break;
                case ChaseWeaponKind.Magma: case ChaseWeaponKind.Graveburst:
                    action.localRotation=Quaternion.Euler(-reload*48,0,recoil*9); break;
                case ChaseWeaponKind.Sentry:
                    action.localPosition+=Vector3.forward*(recoil*.10f+reload*.04f); break;
                case ChaseWeaponKind.Sanctifier: case ChaseWeaponKind.Winterglass: case ChaseWeaponKind.Abyss:
                    action.localRotation=Quaternion.Euler(0,0,recoil*32); break;
                default:
                    action.localPosition+=Vector3.back*(recoil*.045f+reload*.065f); break;
            }
            for(int i=0;i<leaves.Count;i++)
                leaves[i].localRotation=Kind==ChaseWeaponKind.Abyss ?
                    Quaternion.Euler(0,0,i*45+recoil*26+reload*34) :
                    Quaternion.Euler(0,(i==0?-1:1)*(recoil*16+reload*24),0);
        }

        public void Dispose()
        {
            if (Root == null) return;
            Root.gameObject.SetActive(false); UnityEngine.Object.Destroy(Root.gameObject);
            Root=null; Muzzle=null;
            foreach(Mesh mesh in meshes) UnityEngine.Object.Destroy(mesh);
            meshes.Clear();
        }
    }
}
