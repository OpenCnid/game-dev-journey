using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Journey
{
    public static class BurstStatus
    {
        public static bool Compiled { get; private set; }
        public static void Verify()
        {
            using (var result = new NativeArray<int>(1, Allocator.TempJob))
            {
                new Probe { Result = result }.Schedule().Complete();
                Compiled = result[0] == 1;
            }
        }
        [BurstCompile(CompileSynchronously = true)]
        struct Probe : IJob
        {
            public NativeArray<int> Result;
            public void Execute() { int compiled = 1; Managed(ref compiled); Result[0] = compiled; }
            [BurstDiscard] static void Managed(ref int value) { value = 0; }
        }
    }
}
