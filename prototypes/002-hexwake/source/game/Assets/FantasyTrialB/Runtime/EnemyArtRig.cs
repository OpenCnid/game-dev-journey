using UnityEngine;

namespace GameDevHarness.FantasyTrialB
{
    // Visuals only. The outer owner supplies navigation, collision and combat state.
    public sealed class EnemyArtRig
    {
        public Transform Root;
        public Vector3 HitCenter => Root.position + Vector3.up * 0.85f;

        readonly Transform[] thighs = new Transform[2];
        readonly Transform[] shins = new Transform[2];
        readonly Transform[] knees = new Transform[2];
        readonly Transform[] feet = new Transform[2];
        Transform chassis, sensor;
        float phaseOffset;
        const float LimbLength = 0.49f;

        public static EnemyArtRig Create(Transform parent, ArtPalette palette, int variant)
        {
            bool heavy = (variant & 1) != 0;
            var rig = new EnemyArtRig();
            rig.Root = Group(heavy ? "Bastion relic sentinel" : "Survey relic sentinel", parent);
            rig.phaseOffset = (variant & 7) * 0.71f;
            Material armor = heavy ? palette.White : palette.Paint;
            Material accent = heavy ? palette.Amber : palette.Teal;
            rig.chassis = Group("Suspended armored chassis", rig.Root);
            rig.chassis.localPosition = new Vector3(0, 1.24f, 0);

            ArtParts.Box("Internal engine block", rig.chassis, Vector3.zero,
                new Vector3(0.58f, 0.49f, 0.43f), palette.Dark, 0.06f);
            ArtParts.Box("Lower steel cradle", rig.chassis, new Vector3(0, -0.25f, 0),
                new Vector3(0.63f, 0.12f, 0.4f), palette.Steel);
            ArtParts.Box("Rear power pack", rig.chassis, new Vector3(0, 0.02f, -0.24f),
                new Vector3(0.41f, 0.4f, 0.16f), palette.Black, 0.045f);
            ArtParts.Box("Back pack cover", rig.chassis, new Vector3(0, 0.05f, -0.32f),
                new Vector3(0.32f, 0.28f, 0.045f), armor);
            ArtParts.Box("Top carapace", rig.chassis, new Vector3(0, 0.25f, -0.035f),
                new Vector3(heavy ? 0.84f : 0.67f, 0.13f, 0.48f), armor, 0.045f);

            for (int side = -1; side <= 1; side += 2)
            {
                var cheek = ArtParts.Box("Sloped front armor", rig.chassis,
                    new Vector3(side * 0.2f, 0.02f, 0.25f),
                    new Vector3(0.22f, 0.4f, 0.085f), armor, 0.025f);
                cheek.transform.localRotation = Quaternion.Euler(0, side * -12, side * -6);
                ArtParts.Box("Front steel panel edge", rig.chassis,
                    new Vector3(side * 0.29f, -0.02f, 0.255f),
                    new Vector3(0.035f, 0.31f, 0.055f), palette.Steel, 0.008f);
                ArtParts.Box("Chipped lower panel", rig.chassis,
                    new Vector3(side * 0.2f, -0.16f, 0.301f),
                    new Vector3(0.115f, 0.045f, 0.015f), palette.Steel, 0.004f);
                var shoulder = ArtParts.Box("Outboard shield", rig.chassis,
                    new Vector3(side * 0.39f, 0.08f, -0.01f),
                    new Vector3(0.15f, heavy ? 0.42f : 0.29f, 0.34f), armor);
                shoulder.transform.localRotation = Quaternion.Euler(0, 0, side * -12);
                ArtParts.Box("Service panel contrast", rig.chassis,
                    new Vector3(side * 0.472f, 0.08f, -0.025f),
                    new Vector3(0.02f, 0.16f, 0.19f), palette.Dark, 0.005f);
                ArtParts.Box("Unit identification stripe", rig.chassis,
                    new Vector3(side * 0.2f, 0.11f, 0.303f),
                    new Vector3(0.14f, 0.033f, 0.012f), palette.Brass, 0.002f);
                for (int bolt = 0; bolt < 2; bolt++)
                {
                    var screw = ArtParts.Cylinder("Captive panel bolt", rig.chassis,
                        new Vector3(side * 0.2f, -0.1f + bolt * 0.25f, 0.307f),
                        0.016f, 0.014f, palette.Dark);
                    screw.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
                // Exposed flexible conduit beside each power-pack cover.
                Strut("Upper cable", rig.chassis, new Vector3(side * 0.23f, 0.21f, -0.27f),
                    new Vector3(side * 0.3f, 0.03f, -0.29f), 0.025f, palette.Black);
                Strut("Lower cable", rig.chassis, new Vector3(side * 0.3f, 0.03f, -0.29f),
                    new Vector3(side * 0.22f, -0.2f, -0.24f), 0.025f, palette.Black);
                var hip = ArtParts.Cylinder("Hip bearing", rig.chassis,
                    new Vector3(side * 0.31f, -0.29f, 0), 0.11f, 0.13f, palette.Dark);
                hip.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            for (int vent = 0; vent < 4; vent++)
                ArtParts.Box("Rear cooling louver", rig.chassis,
                    new Vector3(0, -0.08f + vent * 0.057f, -0.347f),
                    new Vector3(0.25f, 0.021f, 0.022f), palette.Steel, 0.004f);

            ArtParts.Cylinder("Core seat", rig.chassis, new Vector3(0, -0.02f, 0.24f),
                0.105f, 0.075f, palette.Brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
            ArtParts.Ring("Core retaining ring", rig.chassis, new Vector3(0, -0.02f, 0.291f),
                0.088f, 0.018f, palette.Steel, 16);
            ArtParts.Cylinder("Encased energy core", rig.chassis, new Vector3(0, -0.02f, 0.294f),
                0.068f, 0.018f, accent).transform.localRotation = Quaternion.Euler(90, 0, 0);
            ArtParts.Box("Core protection bridge", rig.chassis, new Vector3(0, -0.02f, 0.319f),
                new Vector3(0.026f, 0.19f, 0.025f), palette.Dark, 0.004f);
            ArtParts.CombineStatic(rig.chassis);

            // Added after chassis batching so the optic remains independently articulated.
            rig.sensor = Group("Tracking sensor turret", rig.chassis);
            rig.sensor.localPosition = new Vector3(0, 0.39f, 0.025f);
            ArtParts.Cylinder("Sensor swivel", rig.sensor, new Vector3(0, -0.075f, 0),
                0.075f, 0.12f, palette.Brass);
            ArtParts.Box("Sensor steel shell", rig.sensor, Vector3.zero,
                new Vector3(heavy ? 0.4f : 0.29f, 0.24f, 0.25f), palette.Dark, 0.035f);
            ArtParts.Box("Sensor crown plate", rig.sensor, new Vector3(0, 0.12f, -0.012f),
                new Vector3(heavy ? 0.43f : 0.32f, 0.055f, 0.27f), armor);
            ArtParts.Box("Recessed optic glass", rig.sensor, new Vector3(0, 0, 0.129f),
                new Vector3(heavy ? 0.3f : 0.19f, 0.077f, 0.018f), palette.Glass, 0.008f);
            ArtParts.Box("Horizontal tracking optic", rig.sensor, new Vector3(0, 0, 0.14f),
                new Vector3(heavy ? 0.22f : 0.09f, 0.025f, 0.012f), accent, 0.003f);
            ArtParts.Box("Optic brow", rig.sensor, new Vector3(0, 0.067f, 0.146f),
                new Vector3(heavy ? 0.36f : 0.25f, 0.035f, 0.075f), palette.Steel);
            if (!heavy)
            {
                Strut("Short sensor mast", rig.sensor, new Vector3(-0.09f, 0.15f, -0.06f),
                    new Vector3(-0.09f, 0.22f, -0.06f), 0.015f, palette.Steel);
                ArtParts.Box("Mast cap", rig.sensor, new Vector3(-0.09f, 0.225f, -0.06f),
                    new Vector3(0.035f, 0.025f, 0.035f), palette.Brass, 0.004f);
            }
            ArtParts.CombineStatic(rig.sensor);

            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                rig.thighs[i] = Group("Articulated upper leg", rig.Root);
                BuildLimb(rig.thighs[i], palette, armor, side, true);
                rig.shins[i] = Group("Articulated lower leg", rig.Root);
                BuildLimb(rig.shins[i], palette, armor, side, false);
                rig.knees[i] = Group("Knee hinge", rig.Root);
                ArtParts.Cylinder("Knee axle", rig.knees[i], Vector3.zero,
                    0.085f, 0.21f, palette.Dark).transform.localRotation = Quaternion.Euler(0, 0, 90);
                ArtParts.Cylinder("Knee bearing cap", rig.knees[i], new Vector3(side * 0.115f, 0, 0),
                    0.056f, 0.018f, palette.Brass).transform.localRotation = Quaternion.Euler(0, 0, 90);
                ArtParts.Box("Knee strike plate", rig.knees[i], new Vector3(0, 0.018f, 0.085f),
                    new Vector3(0.16f, 0.12f, 0.055f), palette.Steel);
                ArtParts.CombineStatic(rig.knees[i]);

                rig.feet[i] = Group("Grounded articulated foot", rig.Root);
                ArtParts.Box("Rubber traction sole", rig.feet[i], new Vector3(0, -0.065f, 0.035f),
                    new Vector3(0.23f, 0.07f, 0.36f), palette.Black, 0.015f);
                ArtParts.Box("Steel foot housing", rig.feet[i], new Vector3(0, -0.005f, 0.025f),
                    new Vector3(0.22f, 0.08f, 0.32f), palette.Steel);
                ArtParts.Box("Split toe armor", rig.feet[i], new Vector3(0, 0.028f, 0.1f),
                    new Vector3(0.2f, 0.055f, 0.16f), armor, 0.015f);
                ArtParts.Box("Toe groove", rig.feet[i], new Vector3(0, 0.057f, 0.1f),
                    new Vector3(0.015f, 0.009f, 0.14f), palette.Dark, 0.002f);
                ArtParts.Cylinder("Ankle hinge", rig.feet[i], new Vector3(0, 0.015f, -0.055f),
                    0.052f, 0.19f, palette.Dark).transform.localRotation = Quaternion.Euler(0, 0, 90);
                ArtParts.CombineStatic(rig.feet[i]);
            }
            rig.Animate(0, 0);
            return rig;
        }

        static Transform Group(string name, Transform parent)
        {
            var obj = new GameObject(name) { layer = 2 };
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        static void Strut(string name, Transform parent, Vector3 start, Vector3 end,
            float radius, Material material)
        {
            Vector3 delta = end - start;
            var part = ArtParts.Cylinder(name, parent, (start + end) * 0.5f,
                radius, delta.magnitude, material);
            part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
        }

        static void BuildLimb(Transform root, ArtPalette palette, Material armor, int side, bool upper)
        {
            ArtParts.Box("Structural leg spar", root, Vector3.zero,
                new Vector3(0.105f, LimbLength, 0.11f), palette.Dark, 0.012f);
            ArtParts.Box("Layered leg armor", root, new Vector3(0, 0.035f, 0.065f),
                new Vector3(upper ? 0.2f : 0.165f, 0.3f, 0.085f), armor, 0.018f);
            ArtParts.Box("Armor overlap", root, new Vector3(0, -0.095f, 0.109f),
                new Vector3(upper ? 0.17f : 0.14f, 0.075f, 0.025f), palette.Steel, 0.005f);
            ArtParts.Cylinder("Hydraulic barrel", root, new Vector3(side * 0.085f, 0.08f, -0.035f),
                0.033f, 0.24f, palette.Dark);
            ArtParts.Cylinder("Exposed piston rod", root, new Vector3(side * 0.085f, -0.13f, -0.035f),
                0.017f, 0.19f, palette.Steel);
            ArtParts.Box("Piston clevis", root, new Vector3(side * 0.085f, -0.21f, -0.035f),
                new Vector3(0.066f, 0.055f, 0.07f), palette.Brass, 0.008f);
            ArtParts.CombineStatic(root);
        }

        public void Animate(float gameplayTime, float movementSpeed)
        {
            float stride = Mathf.Clamp01(movementSpeed / 2.65f);
            float phase = gameplayTime * 10f + phaseOffset;
            float suspension = Mathf.Cos(phase * 2) * 0.018f * stride;
            chassis.localPosition = new Vector3(0, 1.24f + suspension, 0);
            chassis.localRotation = Quaternion.Euler(2 * stride, 0, Mathf.Sin(phase) * 2 * stride);
            sensor.localRotation = Quaternion.Euler(Mathf.Sin(gameplayTime * 1.4f + phaseOffset) * 3,
                Mathf.Sin(gameplayTime * 0.85f + phaseOffset) * 10, 0);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                float cycle = phase + i * Mathf.PI;
                float swing = Mathf.Sin(cycle);
                Vector3 hip = new Vector3(side * 0.31f, 0.95f + suspension, 0);
                // Feet remain level; only the forward swing lifts the sole off y=0.
                Vector3 ankle = new Vector3(side * 0.31f,
                    0.1f + Mathf.Max(0, Mathf.Cos(cycle)) * 0.115f * stride,
                    swing * 0.21f * stride);
                Vector3 delta = ankle - hip;
                float halfDistance = delta.magnitude * 0.5f;
                float bend = Mathf.Sqrt(Mathf.Max(0, LimbLength * LimbLength - halfDistance * halfDistance));
                Vector3 forwardBend = Vector3.Cross(Vector3.right, delta).normalized;
                Vector3 knee = (hip + ankle) * 0.5f + forwardBend * bend;
                PoseLimb(thighs[i], hip, knee);
                PoseLimb(shins[i], knee, ankle);
                knees[i].localPosition = knee;
                feet[i].localPosition = ankle;
            }
        }

        static void PoseLimb(Transform limb, Vector3 start, Vector3 end)
        {
            limb.localPosition = (start + end) * 0.5f;
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
        }
    }
}
