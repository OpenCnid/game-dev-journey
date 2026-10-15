using System;

namespace GameDevHarness.CrucibleReborn
{
    // Enemy attributes are sampled at arrival; pressure follows the live run clock.
    public static class ChaseCalibration
    {
        public const int MaximumActiveEnemies = 1200;
        public const double OpeningGraceSeconds = 3;
        public const double DamageCooldownSeconds = .75;
        private static readonly double[] Times = { 3, 30, 60, 120, 240, 360, 480, 600 };
        private static readonly double[] Health = { 24, 30, 40, 72, 126, 168, 252, 252 };
        private static readonly double[] Speed = { 2.8, 3.3, 3.9, 4.8, 6, 6.8, 7.8, 7.8 };
        private static readonly double[] Rate = { 2.5, 4.5, 7, 10, 14, 24, 30, 30 };
        private static readonly double[] Cap = { 24, 48, 90, 180, 550, 1000, 1200, 1200 };

        public static float SizeAt(ChaseEnemyKind kind, double elapsed)
        {
            float baseline = kind == ChaseEnemyKind.Elite ? 1.3f : kind == ChaseEnemyKind.Demon ? 1.15f : .95f;
            return baseline * (1 + .1f * UnityEngine.Mathf.Clamp01((float)elapsed / 480));
        }

        private static double Sample(double elapsed, double[] values)
        {
            if (elapsed <= Times[0]) return values[0];
            for (int i = 1; i < Times.Length; i++)
                if (elapsed <= Times[i])
                    return values[i - 1] + (values[i] - values[i - 1])
                        * (elapsed - Times[i - 1]) / (Times[i] - Times[i - 1]);
            return values[values.Length - 1];
        }

        public static float HealthAt(double elapsed) => (float)Sample(elapsed, Health);
        public static float SpeedAt(double elapsed) => (float)Sample(elapsed, Speed);
        // Forty-second surges and relief windows start after the first minute.
        // Fade them in over thirty seconds so early pressure does not jump.
        private static double PressureWaveAt(double elapsed)
        {
            double age = Math.Max(0, elapsed - 60);
            return Math.Min(1, age / 30) * Math.Sin(age * Math.PI / 20);
        }
        public static float PursuitMultiplierAt(double elapsed)
        {
            double age = Math.Max(0, elapsed - 60);
            return (float)(1 + .075 * Math.Min(1, age / 30) + .175 * PressureWaveAt(elapsed));
        }
        public static double SpawnRateAt(double elapsed) => elapsed < OpeningGraceSeconds ? 0
            : Sample(elapsed, Rate) * (1 + .35 * PressureWaveAt(elapsed));
        public static int ActiveCapAt(double elapsed) => elapsed < OpeningGraceSeconds ? 0 : (int)Math.Floor(Sample(elapsed, Cap));
        public static int ContactDamageAt(double elapsed, int receivedHits = 0) => 12
            + 2 * (int)Math.Floor(Math.Max(0, Math.Min(600, elapsed)) / 60)
            + 2 * Math.Max(0, Math.Min(10, receivedHits));
        public static int ActivePortalsAt(double elapsed) => elapsed < 20 ? 2 : elapsed < 60 ? 3 : elapsed < 180 ? 4 : 6;
    }
}
