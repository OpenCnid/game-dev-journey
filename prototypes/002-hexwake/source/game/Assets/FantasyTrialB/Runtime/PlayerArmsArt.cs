using UnityEngine;

namespace GameDevHarness.FantasyTrialB
{
    public sealed class PlayerArmsArt
    {
        public Transform Root;
        static readonly Vector3 Mount = new Vector3(0.07f, -0.05f, 0.19f);

        public static PlayerArmsArt Create(Transform camera, ArtPalette palette)
        {
            var arms = new PlayerArmsArt();
            arms.Root = new GameObject("First person engineer gloves and sleeves").transform;
            arms.Root.SetParent(camera, false);
            arms.Root.localPosition = Mount;
            // Hands close around the starter's grip at camera-local (.17,-.267,.376).
            // The left hand cups the right, leaving the pistol's slide and sights clear.
            BuildHand(arms.Root, palette, "Right firing hand", new Vector3(0.186f, -0.272f, 0.368f), 1);
            BuildHand(arms.Root, palette, "Left supporting hand", new Vector3(0.123f, -0.286f, 0.365f), -1);
            BuildSleeve(arms.Root, palette, "Right forearm", new Vector3(0.217f, -0.349f, 0.27f),
                new Vector3(-38, -14, -12));
            BuildSleeve(arms.Root, palette, "Left forearm", new Vector3(0.065f, -0.362f, 0.271f),
                new Vector3(-36, 25, 19));
            ArtParts.CombineStatic(arms.Root);
            return arms;
        }

        public void Animate(float dt, float gameplayTime, float movementAmount)
        {
            if (dt <= 0) return;
            float movement = Mathf.Clamp01(movementAmount);
            float phase = gameplayTime * 6;
            Root.localPosition = Mount + new Vector3(Mathf.Sin(phase) * 0.0025f * movement,
                Mathf.Sin(phase * 2) * 0.002f * movement, 0);
        }

        static void BuildHand(Transform parent, ArtPalette p, string name, Vector3 position, float side)
        {
            var hand = new GameObject(name).transform;
            hand.SetParent(parent, false);
            hand.localPosition = position;
            hand.localRotation = Quaternion.Euler(-8, side * -9, side * 5);
            ArtParts.Box("Glove palm", hand, Vector3.zero,
                new Vector3(0.047f, 0.068f, 0.035f), p.Cloth, 0.011f);
            ArtParts.Box("Reinforced glove back", hand, new Vector3(side * 0.012f, 0, -0.018f),
                new Vector3(0.026f, 0.05f, 0.009f), p.Black, 0.004f);
            ArtParts.Box("Stitched palm heel", hand, new Vector3(0, -0.025f, 0.012f),
                new Vector3(0.043f, 0.02f, 0.011f), p.Dark, 0.005f);
            // Four separate curled digits, with pads and knuckles rather than a
            // mitten primitive. A small gap between digits preserves the silhouette.
            for (int i = 0; i < 4; i++)
            {
                float y = 0.025f - i * 0.015f;
                float length = i == 3 ? 0.026f : i == 0 ? 0.034f : 0.039f;
                var finger = ArtParts.Box("Glove finger proximal " + i, hand,
                    new Vector3(-side * 0.007f, y, 0.022f),
                    new Vector3(length, 0.011f, 0.016f), p.Cloth, 0.0045f);
                finger.transform.localRotation = Quaternion.Euler(0, side * 12, 0);
                var curl = ArtParts.Box("Glove finger curled tip " + i, hand,
                    new Vector3(-side * (length * 0.5f + 0.004f), y - 0.001f, 0.012f),
                    new Vector3(0.013f, 0.010f, 0.022f), p.Black, 0.004f);
                curl.transform.localRotation = Quaternion.Euler(0, side * 24, 0);
                ArtParts.Box("Individual knuckle pad " + i, hand,
                    new Vector3(side * 0.019f, y, 0.011f),
                    new Vector3(0.01f, 0.012f, 0.022f), p.Dark, 0.004f);
                ArtParts.Box("Finger seam " + i, hand,
                    new Vector3(-side * 0.004f, y + 0.0055f, 0.023f),
                    new Vector3(length * 0.6f, 0.001f, 0.005f), p.Paint, 0.001f);
            }
            var thumbBase = ArtParts.Box("Splayed thumb base", hand,
                new Vector3(-side * 0.025f, 0.020f, -0.009f),
                new Vector3(0.025f, 0.022f, 0.024f), p.Cloth, 0.007f);
            thumbBase.transform.localRotation = Quaternion.Euler(0, side * 25, side * -32);
            var thumbTip = ArtParts.Box("Thumb folded over grip", hand,
                new Vector3(-side * 0.030f, 0.034f, 0.007f),
                new Vector3(0.018f, 0.016f, 0.029f), p.Black, 0.005f);
            thumbTip.transform.localRotation = Quaternion.Euler(-24, side * 28, side * -20);
            ArtParts.Box("Glove wrist", hand, new Vector3(0, -0.046f, -0.017f),
                new Vector3(0.049f, 0.032f, 0.045f), p.Cloth, 0.008f);
            ArtParts.Box("Wrist closure strap", hand, new Vector3(0, -0.045f, -0.041f),
                new Vector3(0.053f, 0.016f, 0.006f), p.Black, 0.003f);
            ArtParts.Box("Strap retaining buckle", hand, new Vector3(side * 0.012f, -0.045f, -0.045f),
                new Vector3(0.017f, 0.013f, 0.004f), p.Steel, 0.002f);
        }

        static void BuildSleeve(Transform parent, ArtPalette p, string name, Vector3 position, Vector3 angles)
        {
            var sleeve = new GameObject(name).transform;
            sleeve.SetParent(parent, false);
            sleeve.localPosition = position;
            sleeve.localRotation = Quaternion.Euler(angles);
            ArtParts.Box("Weathered jacket forearm", sleeve, new Vector3(0, 0, -0.023f),
                new Vector3(0.072f, 0.067f, 0.137f), p.Cloth, 0.015f);
            ArtParts.Box("Tapered sleeve wrist", sleeve, new Vector3(0, 0, 0.055f),
                new Vector3(0.057f, 0.054f, 0.039f), p.Cloth, 0.01f);
            ArtParts.Box("Leather sleeve cuff", sleeve, new Vector3(0, 0, 0.076f),
                new Vector3(0.061f, 0.057f, 0.025f), p.Black, 0.007f);
            ArtParts.Box("Cuff top reinforcement", sleeve, new Vector3(0, 0.029f, 0.077f),
                new Vector3(0.042f, 0.006f, 0.018f), p.Dark, 0.002f);
            ArtParts.Box("Sewn forearm utility panel", sleeve, new Vector3(0, 0.032f, -0.014f),
                new Vector3(0.048f, 0.009f, 0.079f), p.Paint, 0.007f);
            ArtParts.Box("Panel inset fabric", sleeve, new Vector3(0, 0.037f, -0.014f),
                new Vector3(0.033f, 0.002f, 0.061f), p.Cloth, 0.005f);
            for (int s = -1; s <= 1; s += 2)
                ArtParts.Box("Sleeve stitched seam", sleeve, new Vector3(s * 0.029f, 0.026f, -0.026f),
                    new Vector3(0.002f, 0.002f, 0.099f), p.Dark, 0.001f);
            ArtParts.Box("Cuff fastening tab", sleeve, new Vector3(0.032f, 0.006f, 0.068f),
                new Vector3(0.007f, 0.025f, 0.022f), p.Steel, 0.003f);
        }
    }
}
