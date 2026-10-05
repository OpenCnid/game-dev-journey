using System;

namespace Journey
{
    [Serializable] public sealed class SharedWork
    {
        public long ticks, steeringChecks, contactTests, contactApplications, attractionQueries;
        public double simulationMs, crowdMs, weaponMs, xpMs;
        public double frameSimulationMs, frameCrowdMs, frameWeaponMs, frameXpMs;
        public int frameTicks;
        public void BeginFrame()
        { frameSimulationMs = frameCrowdMs = frameWeaponMs = frameXpMs = 0; frameTicks = 0; }
    }
}
