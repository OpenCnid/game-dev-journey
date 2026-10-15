using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    // Opt-in observation only. Native input still enters the ordinary controller.
    public sealed class ChaseNativeTelemetry : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public int frame, jumps;
            public int weapons, relics, level;
            public string phase;
            public float time, horizontal, forward;
            public bool grounded, space;
            public Vector3 position, velocity;
        }
        [Serializable] public sealed class Trace { public List<Sample> samples = new List<Sample>(); }
        HorrorChaseController controller;
        string path;
        bool progression, configured, levelRequested, levelObserved;
        readonly Trace trace = new Trace();
        public static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-rebornTelemetry");
            if (index < 0 || index + 1 >= args.Length) return;
            var controller = FindAnyObjectByType<HorrorChaseController>();
            var observer = controller.gameObject.AddComponent<ChaseNativeTelemetry>();
            observer.controller = controller; observer.path = Path.GetFullPath(args[index + 1]);
            observer.progression = Array.IndexOf(args, "-rebornNativeProgression") >= 0;
            Debug.Log("REBORN_NATIVE_TELEMETRY_READY");
        }
        void LateUpdate()
        {
            if (progression && controller.State.Phase == ChasePhase.Playing && !configured)
            {
                configured = true; controller.SpawningEnabled = false; controller.State.Automatic = false;
                controller.State.DropLoot(controller.PlayerPosition + new Vector3(.55f,.3f,1.45f),
                    ChaseLootKind.Weapon, ChaseWeaponKind.BoneLance);
                controller.State.DropLoot(controller.PlayerPosition + new Vector3(-.55f,.3f,1.45f),
                    ChaseLootKind.Relic, relic: ChaseRelicKind.Windstep);
                Debug.Log("REBORN_NATIVE_LOOT_READY");
            }
            if (progression && !levelRequested && controller.State.Weapons.Count == 2 && controller.State.Relics.Count == 1)
            {
                levelRequested = true; controller.State.CollectXp(controller.State.Threshold);
                Debug.Log("REBORN_NATIVE_UPGRADE_READY");
            }
            if (levelRequested && !levelObserved && controller.State.Level == 2)
            { levelObserved = true; Debug.Log("REBORN_NATIVE_LEVEL_CHOSEN"); }
            if (controller.State.Phase != ChasePhase.Playing || trace.samples.Count >= 18000) return;
            trace.samples.Add(new Sample { frame = Time.frameCount, time = (float)controller.State.Elapsed,
                weapons = controller.State.Weapons.Count, relics = controller.State.Relics.Count,
                level = controller.State.Level, phase = controller.State.Phase.ToString(),
                horizontal = Input.GetAxisRaw("Horizontal"), forward = Input.GetAxisRaw("Vertical"),
                space = Input.GetKey(KeyCode.Space), grounded = controller.PlayerGrounded,
                jumps = controller.ObservedJumps, position = controller.PlayerPosition,
                velocity = controller.HorizontalVelocity });
        }
        void OnApplicationQuit()
        {
            if (path == null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(trace));
        }
    }
}
