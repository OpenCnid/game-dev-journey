using UnityEngine;

namespace GameDevHarness.FantasyTrialB
{
    public sealed class GunArtRig
    {
        public Transform Root, Muzzle;
        public int CopyId;
        public WeaponKind Kind;

        Transform mechanism;
        Vector3 mount;
        Vector3 mechanismRest;
        float recoil;
        float mechanismAngle;

        public static GunArtRig Create(Transform camera, ArtPalette palette, WeaponKind kind, int copyId, int slot)
        {
            var rig = new GunArtRig { CopyId = copyId, Kind = kind };
            rig.Root = new GameObject(kind + " gun copy " + copyId).transform;
            rig.Root.SetParent(camera, false);
            int pair = slot / 2;
            float side = slot % 2 == 0 ? 1 : -1;
            // Meters. The starter stays on the right; acquired copies never move it.
            rig.mount = pair == 0 ? new Vector3(side * 0.24f, -0.23f, 0.62f) :
                pair == 1 ? new Vector3(side * 0.48f, -0.16f, 0.88f) :
                new Vector3(side * 0.7f, -0.025f, 1.17f);
            rig.Root.localPosition = rig.mount;
            rig.Root.localScale = Vector3.one * (pair == 0 ? 1 : pair == 1 ? 0.86f : 0.76f);
            var body = new GameObject("Fixed receiver and furniture").transform;
            body.SetParent(rig.Root, false);
            rig.mechanism = new GameObject(kind == WeaponKind.Pistol ? "Reciprocating slide" : "Charge mechanism").transform;
            rig.mechanism.SetParent(rig.Root, false);
            float aperture;
            switch (kind)
            {
                case WeaponKind.Ember: rig.BuildFire(body, palette); aperture = 0.196f; break;
                case WeaponKind.Whirlwind: rig.BuildWind(body, palette); aperture = 0.196f; break;
                case WeaponKind.Spark: rig.BuildLightning(body, palette); aperture = 0.198f; break;
                default: rig.BuildPistol(body, palette); aperture = 0.162f; break;
            }
            rig.mechanismRest = rig.mechanism.localPosition;
            ArtParts.CombineStatic(body);
            ArtParts.CombineStatic(rig.mechanism);
            rig.Muzzle = new GameObject("Front aperture copy " + copyId).transform;
            rig.Muzzle.SetParent(rig.Root, false);
            rig.Muzzle.localPosition = new Vector3(0, 0, aperture);
            return rig;
        }

        public void Fire()
        {
            recoil = 1;
        }

        public void Animate(float dt, float gameplayTime, float movementAmount, Vector3 aimPoint)
        {
            // The caller owns the clock. A zero delta leaves recoil, aim and sway frozen.
            if (dt <= 0) return;
            recoil = Mathf.MoveTowards(recoil, 0, dt * 7);
            float movement = Mathf.Clamp01(movementAmount);
            float phase = gameplayTime * 6;
            Root.localPosition = mount + new Vector3(Mathf.Sin(phase) * 0.0025f * movement,
                Mathf.Sin(phase * 2) * 0.002f * movement, -recoil * 0.012f);
            // Both aperture and barrel lie on this axis, so the aperture points at the
            // actual world target, not a parallel camera-forward approximation.
            Vector3 direction = Root.parent.InverseTransformPoint(aimPoint) - Root.localPosition;
            if (direction.sqrMagnitude > 0.000001f)
                Root.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            if (Kind == WeaponKind.Pistol)
                mechanism.localPosition = mechanismRest + Vector3.back * (recoil * 0.024f);
            else
            {
                mechanism.localScale = Vector3.one * (1 + recoil * 0.035f);
                if (Kind == WeaponKind.Whirlwind)
                {
                    mechanismAngle = (mechanismAngle + dt * (25 + recoil * 340)) % 360;
                    mechanism.localRotation = Quaternion.AngleAxis(mechanismAngle, Vector3.forward);
                }
            }
        }

        void BuildPistol(Transform body, ArtPalette p)
        {
            ArtParts.Box("Steel frame", body, new Vector3(0, -0.027f, 0.025f),
                new Vector3(0.044f, 0.034f, 0.205f), p.Dark, 0.006f);
            var grip = ArtParts.Box("Back swept polymer grip", body, new Vector3(0, -0.087f, -0.054f),
                new Vector3(0.043f, 0.11f, 0.06f), p.Black, 0.009f);
            grip.transform.localRotation = Quaternion.Euler(-16, 0, 0);
            ArtParts.Box("Magazine floor", body, new Vector3(0, -0.143f, -0.038f),
                new Vector3(0.052f, 0.012f, 0.068f), p.Dark, 0.003f);
            for (int s = -1; s <= 1; s += 2)
            {
                var panel = ArtParts.Box("Textured grip panel", body, new Vector3(s * 0.022f, -0.087f, -0.052f),
                    new Vector3(0.004f, 0.075f, 0.043f), p.Paint, 0.002f);
                panel.transform.localRotation = Quaternion.Euler(-16, 0, 0);
                for (int i = 0; i < 6; i++)
                    ArtParts.Box("Grip checkering ridge", body, new Vector3(s * 0.025f, -0.061f - i * 0.011f, -0.054f + i * 0.002f),
                        new Vector3(0.002f, 0.003f, 0.035f), p.Dark, 0.001f);
                Screw(body, p.Steel, s * 0.026f, -0.055f, -0.058f);
                Screw(body, p.Steel, s * 0.026f, -0.12f, -0.041f);
            }
            ArtParts.Box("Trigger guard lower bow", body, new Vector3(0, -0.075f, 0.004f),
                new Vector3(0.028f, 0.008f, 0.073f), p.Dark, 0.004f);
            var bow = ArtParts.Box("Trigger guard front", body, new Vector3(0, -0.055f, 0.037f),
                new Vector3(0.028f, 0.04f, 0.009f), p.Dark, 0.004f);
            bow.transform.localRotation = Quaternion.Euler(-12, 0, 0);
            var trigger = ArtParts.Box("Curved trigger", body, new Vector3(0, -0.048f, -0.002f),
                new Vector3(0.008f, 0.028f, 0.008f), p.Steel, 0.003f);
            trigger.transform.localRotation = Quaternion.Euler(-23, 0, 0);
            Axial(body, "Fixed barrel", new Vector3(0, 0, 0.074f), 0.012f, 0.17f, p.Steel);
            Axial(body, "Recessed barrel bore", new Vector3(0, 0, 0.1592f), 0.007f, 0.0005f, p.Black);
            ArtParts.Ring("Open muzzle crown", body, new Vector3(0, 0, 0.159f), 0.01f, 0.003f, p.Steel);
            ArtParts.Box("Machined slide", mechanism, new Vector3(0, 0.006f, 0.025f),
                new Vector3(0.046f, 0.04f, 0.249f), p.Steel, 0.006f);
            ArtParts.Box("Matte slide top", mechanism, new Vector3(0, 0.025f, 0.016f),
                new Vector3(0.031f, 0.003f, 0.205f), p.Dark, 0.001f);
            ArtParts.Box("Ejection port dark recess", mechanism, new Vector3(0.0235f, 0.012f, 0.029f),
                new Vector3(0.0015f, 0.019f, 0.039f), p.Black, 0.001f);
            ArtParts.Box("Exposed chamber", mechanism, new Vector3(0.0245f, 0.009f, 0.028f),
                new Vector3(0.001f, 0.009f, 0.029f), p.Brass, 0.001f);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 7; i++)
                    ArtParts.Box("Slide cocking serration", mechanism, new Vector3(s * 0.0235f, 0.005f, -0.079f + i * 0.005f),
                        new Vector3(0.0015f, 0.027f, 0.002f), p.Dark, 0.001f);
            for (int s = -1; s <= 1; s += 2)
                ArtParts.Box("Rear sight notch wing", mechanism, new Vector3(s * 0.013f, 0.034f, -0.077f),
                    new Vector3(0.009f, 0.014f, 0.012f), p.Black, 0.002f);
            ArtParts.Box("Front sight post", mechanism, new Vector3(0, 0.034f, 0.128f),
                new Vector3(0.008f, 0.014f, 0.011f), p.Black, 0.002f);
            ArtParts.Box("Front sight ivory inset", mechanism, new Vector3(0, 0.035f, 0.122f),
                new Vector3(0.003f, 0.005f, 0.001f), p.White, 0.001f);
            ArtParts.Box("Slide release paddle", body, new Vector3(-0.026f, -0.019f, -0.043f),
                new Vector3(0.006f, 0.011f, 0.026f), p.Dark, 0.002f);
        }

        void BuildFire(Transform body, ArtPalette p)
        {
            Furniture(body, p, 0.07f);
            ArtParts.Box("Broad heat jacket", body, new Vector3(0, 0, 0.042f),
                new Vector3(0.112f, 0.084f, 0.192f), p.Paint, 0.013f);
            ArtParts.Box("Jacket dorsal seam", body, new Vector3(0, 0.044f, 0.036f),
                new Vector3(0.034f, 0.008f, 0.174f), p.Steel, 0.003f);
            Axial(body, "Ceramic nozzle", new Vector3(0, 0, 0.148f), 0.033f, 0.075f, p.Dark);
            ArtParts.Ring("Brass nozzle lip", body, new Vector3(0, 0, 0.191f), 0.03f, 0.005f, p.Brass);
            ArtParts.Ring("Hot recessed throat", mechanism, new Vector3(0, 0, 0.187f), 0.018f, 0.002f, p.Amber);
            for (int s = -1; s <= 1; s += 2)
            {
                ArtParts.Box("Heat shield cheek", body, new Vector3(s * 0.057f, 0, 0.03f),
                    new Vector3(0.007f, 0.061f, 0.145f), p.Brass, 0.003f);
                for (int i = 0; i < 7; i++)
                {
                    var vent = ArtParts.Box("Angled cooling slot", body, new Vector3(s * 0.061f, 0.004f, -0.025f + i * 0.018f),
                        new Vector3(0.002f, 0.039f, 0.007f), p.Black, 0.002f);
                    vent.transform.localRotation = Quaternion.Euler(-25, 0, 0);
                }
                Screw(body, p.Steel, s * 0.063f, 0.023f, -0.032f);
                Screw(body, p.Steel, s * 0.063f, -0.022f, 0.095f);
                Axial(body, "Fuel feed pipe", new Vector3(s * 0.041f, -0.047f, 0.041f), 0.005f, 0.149f, p.Brass);
            }
            Axial(body, "Rear fuel reservoir", new Vector3(0, -0.032f, -0.083f), 0.023f, 0.049f, p.Dark);
            ArtParts.Ring("Tank retaining band", body, new Vector3(0, -0.032f, -0.091f), 0.023f, 0.003f, p.Steel);
            ArtParts.Box("Pressure readout", body, new Vector3(-0.025f, 0.026f, -0.044f),
                new Vector3(0.032f, 0.015f, 0.035f), p.Dark, 0.004f);
            ArtParts.Box("Pressure gauge amber stripe", body, new Vector3(-0.025f, 0.034f, -0.044f),
                new Vector3(0.018f, 0.001f, 0.005f), p.Amber, 0.001f);
        }

        void BuildWind(Transform body, ArtPalette p)
        {
            Furniture(body, p, 0.059f);
            ArtParts.Box("Tapered turbine receiver", body, new Vector3(0, -0.012f, -0.01f),
                new Vector3(0.062f, 0.061f, 0.153f), p.Dark, 0.013f);
            for (int s = -1; s <= 1; s += 2)
            {
                var wing = ArtParts.Box("Swept turbine nacelle", body, new Vector3(s * 0.037f, -0.003f, 0.044f),
                    new Vector3(0.019f, 0.046f, 0.125f), p.Paint, 0.007f);
                wing.transform.localRotation = Quaternion.Euler(0, -s * 11, 0);
                Axial(body, "Air pressure conduit", new Vector3(s * 0.027f, -0.04f, 0.052f), 0.005f, 0.155f, p.Steel);
                Screw(body, p.Brass, s * 0.045f, -0.001f, 0.015f);
            }
            ArtParts.Ring("Rear intake rim", body, new Vector3(0, 0, 0.09f), 0.055f, 0.008f, p.Dark);
            ArtParts.Ring("Open turbine nozzle", body, new Vector3(0, 0, 0.187f), 0.058f, 0.009f, p.Steel);
            ArtParts.Ring("Inscribed airflow annulus", body, new Vector3(0, 0, 0.174f), 0.053f, 0.002f, p.Teal);
            mechanism.localPosition = new Vector3(0, 0, 0.129f);
            ArtParts.Ring("Rotating turbine ring", mechanism, Vector3.zero, 0.044f, 0.004f, p.Brass);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                var blade = ArtParts.Box("Twisted turbine vane", mechanism, radial * 0.036f,
                    new Vector3(0.021f, 0.009f, 0.016f), p.Steel, 0.003f);
                blade.transform.localRotation = Quaternion.Euler(0, 24, i * 45 + 28);
                var stay = ArtParts.Box("Nozzle support strut", body, radial * 0.052f + Vector3.forward * 0.137f,
                    new Vector3(0.008f, 0.009f, 0.089f), p.Dark, 0.003f);
                stay.transform.localRotation = Quaternion.Euler(0, 0, i * 45);
            }
            ArtParts.Box("Airflow selector", body, new Vector3(0, 0.024f, -0.043f),
                new Vector3(0.025f, 0.019f, 0.045f), p.Paint, 0.004f);
        }

        void BuildLightning(Transform body, ArtPalette p)
        {
            Furniture(body, p, 0.064f);
            ArtParts.Box("Insulated capacitor chassis", body, new Vector3(0, -0.009f, -0.023f),
                new Vector3(0.075f, 0.072f, 0.137f), p.Paint, 0.009f);
            Axial(body, "Ceramic coil core", new Vector3(0, 0, 0.073f), 0.023f, 0.133f, p.White);
            for (int i = 0; i < 8; i++)
                ArtParts.Ring("Conductive coil winding", body, new Vector3(0, 0, 0.018f + i * 0.015f),
                    0.027f, 0.004f, p.Brass);
            ArtParts.Ring("Coil charge collar", mechanism, new Vector3(0, 0, 0.117f), 0.024f, 0.002f, p.Blue);
            for (int s = -1; s <= 1; s += 2)
            {
                ArtParts.Box("Conductive fork rail", body, new Vector3(s * 0.042f, 0, 0.112f),
                    new Vector3(0.012f, 0.019f, 0.151f), p.Steel, 0.004f);
                var tip = ArtParts.Box("Converging discharge prong", body, new Vector3(s * 0.033f, 0, 0.177f),
                    new Vector3(0.014f, 0.017f, 0.035f), p.Brass, 0.003f);
                tip.transform.localRotation = Quaternion.Euler(0, -s * 28, 0);
                ArtParts.Box("Insulated fork root", body, new Vector3(s * 0.042f, 0, 0.045f),
                    new Vector3(0.022f, 0.035f, 0.029f), p.Black, 0.004f);
                for (int i = 0; i < 4; i++)
                {
                    var cable = ArtParts.Box("Segmented braided power cable", body,
                        new Vector3(s * (0.042f + Mathf.Sin(i * Mathf.PI / 3) * 0.008f), -0.037f, -0.054f + i * 0.031f),
                        new Vector3(0.008f, 0.009f, 0.035f), p.Black, 0.003f);
                    cable.transform.localRotation = Quaternion.Euler(0, s * (i < 2 ? 12 : -12), 0);
                }
                Screw(body, p.Steel, s * 0.039f, 0.016f, -0.053f);
            }
            Axial(body, "Emitter dark recess", new Vector3(0, 0, 0.153f), 0.009f, 0.033f, p.Dark);
            ArtParts.Ring("Emitter front aperture", body, new Vector3(0, 0, 0.195f), 0.01f, 0.003f, p.Steel);
            ArtParts.Ring("Emitter inner corona", mechanism, new Vector3(0, 0, 0.186f), 0.007f, 0.001f, p.Blue);
            ArtParts.Box("Charge meter bezel", body, new Vector3(0, 0.031f, -0.035f),
                new Vector3(0.026f, 0.012f, 0.061f), p.Dark, 0.003f);
            for (int i = 0; i < 3; i++)
                ArtParts.Box("Charge meter cell", body, new Vector3(0, 0.0375f, -0.052f + i * 0.018f),
                    new Vector3(0.017f, 0.001f, 0.006f), p.Blue, 0.001f);
        }

        static void Furniture(Transform body, ArtPalette p, float width)
        {
            var grip = ArtParts.Box("Insulated pistol grip", body, new Vector3(0, -0.084f, -0.056f),
                new Vector3(0.038f, 0.102f, 0.054f), p.Black, 0.009f);
            grip.transform.localRotation = Quaternion.Euler(-14, 0, 0);
            ArtParts.Box("Grip heel", body, new Vector3(0, -0.137f, -0.045f),
                new Vector3(0.046f, 0.012f, 0.059f), p.Steel, 0.003f);
            ArtParts.Box("Underslung mount rail", body, new Vector3(0, -0.047f, 0.009f),
                new Vector3(width, 0.015f, 0.156f), p.Dark, 0.004f);
            for (int i = 0; i < 4; i++)
                ArtParts.Box("Grip rib", body, new Vector3(0, -0.061f - i * 0.017f, -0.025f + i * 0.003f),
                    new Vector3(0.041f, 0.006f, 0.007f), p.Dark, 0.002f);
        }

        static void Axial(Transform parent, string name, Vector3 position, float radius, float length, Material material)
        {
            ArtParts.Cylinder(name, parent, position, radius, length, material).transform.localRotation =
                Quaternion.Euler(90, 0, 0);
        }

        static void Screw(Transform parent, Material material, float x, float y, float z)
        {
            ArtParts.Cylinder("Recessed fastener head", parent, new Vector3(x, y, z), 0.004f, 0.002f, material)
                .transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
    }
}
