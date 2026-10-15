using System;
using System.Collections.Generic;
using GameDevHarness.FantasyTrialB;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    // Cached real gun assemblies and large physical relic seals, never XP orbs.
    public sealed class ChaseLootArt : IDisposable
    {
        readonly Transform templates;
        readonly ArtPalette palette;
        readonly Material beam, gunGlow, relicGlow, baseMetal;
        readonly Dictionary<ChaseWeaponKind, ChaseGunRig> guns = new Dictionary<ChaseWeaponKind, ChaseGunRig>();
        readonly Dictionary<ChaseRelicKind, Mesh> relics = new Dictionary<ChaseRelicKind, Mesh>();
        readonly Mesh plinth, seal;

        public ChaseLootArt(Transform parent, ArtPalette palette, Material beam)
        {
            this.palette = palette; this.beam = beam;
            templates = new GameObject("J cached floor models").transform;
            templates.SetParent(parent, false);
            gunGlow = palette.Make(new Color(.22f, .85f, 1), .05f, .4f, .9f);
            relicGlow = palette.Make(new Color(1, .76f, .22f), .1f, .5f, .9f);
            baseMetal = palette.Make(new Color(.22f, .26f, .32f), .2f, .35f);
            var footing = new RebornWeaponGeometry();
            footing.Bore(Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)), .045f, .60f, .49f);
            plinth = footing.Build("J wide floor locator ring");
            var frame = new RebornWeaponGeometry();
            frame.Bore(Matrix4x4.identity, .09f, .43f, .34f);
            seal = frame.Build("J large relic frame");
        }

        public static bool CueVisible(Vector3 eye, ChaseLoot loot)
        {
            return !Physics.Linecast(eye, loot.Position + Vector3.up * .45f,
                (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore);
        }

        public GameObject Create(ChaseLoot loot, Transform parent)
        {
            bool weapon = loot.Kind == ChaseLootKind.Weapon;
            var root = new GameObject("J dropped " + (weapon ?
                ChaseCatalog.Name(loot.Weapon) : ChaseRelics.Name(loot.Relic)));
            root.transform.SetParent(parent, false); root.transform.position = loot.Position;
            var ring = Part(root.transform, plinth, weapon ? gunGlow : relicGlow);
            ring.localPosition = Vector3.up * .04f;
            if (weapon)
            {
                if (!guns.TryGetValue(loot.Weapon, out var template))
                {
                    template = ChaseGunRig.Create(templates, palette, loot.Weapon, -1, 0);
                    template.Root.gameObject.SetActive(false); guns.Add(loot.Weapon, template);
                }
                var model = UnityEngine.Object.Instantiate(template.Root.gameObject, root.transform).transform;
                model.name = "J recognizable floor gun";
                model.localPosition = new Vector3(-.22f, .60f, -.20f);
                model.localRotation = Quaternion.Euler(0, 55, 70);
                model.localScale = Vector3.one * 1.8f;
                foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 0;
                model.gameObject.SetActive(true);
            }
            else
            {
                var frame = Part(root.transform, seal, baseMetal);
                frame.localPosition = Vector3.up * .82f;
                if (!relics.TryGetValue(loot.Relic, out var mesh))
                {
                    var glyph = new RebornWeaponGeometry();
                    float[] shape;
                    switch (loot.Relic)
                    {
                        case ChaseRelicKind.Windstep:
                            shape = new float[] { -.30f,-.14f,.04f,-.14f,.30f,.13f,.02f,.03f,.08f,.27f,-.22f,.09f }; break;
                        case ChaseRelicKind.Executioner:
                            shape = new float[] { -.30f,0,0,.19f,.30f,0,0,-.19f }; break;
                        case ChaseRelicKind.BloodGlass:
                            shape = new float[] { -.20f,.28f,.20f,.28f,.06f,0,.20f,-.28f,-.20f,-.28f,-.06f,0 }; break;
                        case ChaseRelicKind.VolatileHeart:
                            shape = new float[] { -.28f,.13f,-.19f,.27f,0,.14f,.19f,.27f,.28f,.13f,0,-.29f }; break;
                        case ChaseRelicKind.Quicksilver:
                            shape = new float[] { .06f,.30f,-.22f,-.04f,-.02f,-.04f,-.06f,-.30f,.23f,.08f,.02f,.08f }; break;
                        case ChaseRelicKind.EchoSeal:
                            shape = new float[] { -.27f,-.16f,-.27f,.16f,-.12f,.16f,-.12f,-.01f,.12f,-.01f,
                                .12f,.16f,.27f,.16f,.27f,-.16f }; break;
                        default: throw new ArgumentOutOfRangeException(nameof(loot.Relic));
                    }
                    glyph.Profile(Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)), .14f, shape);
                    mesh = glyph.Build("J distinct relic " + loot.Relic); relics.Add(loot.Relic, mesh);
                }
                var icon = Part(root.transform, mesh, relicGlow);
                icon.localPosition = Vector3.up * .82f;
            }
            var locator = new GameObject("J steady loot beam").AddComponent<LineRenderer>();
            locator.transform.SetParent(root.transform, false);
            locator.sharedMaterial = beam; locator.useWorldSpace = false; locator.positionCount = 2;
            locator.SetPosition(0, new Vector3(.35f, .12f, .25f));
            locator.SetPosition(1, new Vector3(.35f, 2.4f, .25f));
            locator.startWidth = .085f; locator.endWidth = .025f;
            locator.startColor = weapon ? new Color(.28f, .85f, 1, .70f) : new Color(1, .78f, .25f, .70f);
            locator.endColor = new Color(locator.startColor.r, locator.startColor.g, locator.startColor.b, .08f);
            locator.shadowCastingMode = ShadowCastingMode.Off; locator.receiveShadows = false;
            return root;
        }

        static Transform Part(Transform parent, Mesh mesh, Material material)
        {
            var part = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
            part.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            return part;
        }

        public void Dispose()
        {
            foreach (var gun in guns.Values) gun.Dispose();
            foreach (var mesh in relics.Values) UnityEngine.Object.Destroy(mesh);
            guns.Clear(); relics.Clear();
            UnityEngine.Object.Destroy(plinth); UnityEngine.Object.Destroy(seal);
            UnityEngine.Object.Destroy(templates.gameObject);
        }
    }
}
