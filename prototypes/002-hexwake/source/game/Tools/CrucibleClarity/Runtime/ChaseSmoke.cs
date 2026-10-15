using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace GameDevHarness.CrucibleReborn
{
    public sealed class ChaseSmoke : MonoBehaviour
    {
        HorrorChaseController trial;
        string output, tests;
        int passed, failed;
        bool gameplay;
        bool load;
        bool observerHealth;

        public static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-rebornOutput");
            if (index < 0 || index + 1 >= args.Length || FindAnyObjectByType<ChaseSmoke>() != null) return;
            var controller = FindAnyObjectByType<HorrorChaseController>();
            var smoke = controller.gameObject.AddComponent<ChaseSmoke>();
            smoke.trial = controller; smoke.output = Path.GetFullPath(args[index + 1]);
            index = Array.IndexOf(args, "-rebornTests");
            if (index >= 0 && index + 1 < args.Length) smoke.tests = Path.GetFullPath(args[index + 1]);
            smoke.gameplay = Array.IndexOf(args, "-rebornGameplay") >= 0;
            smoke.load = Array.IndexOf(args, "-rebornLoad") >= 0;
            smoke.observerHealth = Array.IndexOf(args, "-rebornObserverHealth") >= 0;
            controller.ScriptedControl = true; Application.runInBackground = true;
            Application.logMessageReceived += smoke.Exception;
        }
        void Exception(string message, string stack, LogType type)
        {
            if (type == LogType.Exception) Application.Quit(1);
        }
        void OnDestroy() { Application.logMessageReceived -= Exception; }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            if (tests != null) yield return Regressions();
            else if (load) yield return Load();
            else if (gameplay) yield return Gameplay();
            else yield return Presentation();
            Debug.Log($"REBORN_DONE passed={passed} failed={failed}");
            Application.Quit(failed == 0 ? 0 : 1);
        }
        IEnumerator Capture(string name)
        {
            trial.Hud.ObserveState(trial.State, true);
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            Destroy(image); Debug.Log("REBORN_CAPTURE " + name);
        }
        void Scene(ChaseWeaponKind kind = ChaseWeaponKind.Stormsplit)
        {
            trial.ResetRun(71075, kind); trial.SpawningEnabled = false;
            trial.State.Automatic = false; trial.StartRun();
            trial.Teleport(new Vector3(0, .08f, -31));
        }
        IEnumerator Presentation()
        {
            yield return Capture("00-ready");
            Scene();
            yield return Capture("00-live-empty-inventory-hud");
            trial.PauseRun(); trial.Hud.ShowInventory(true);
            yield return Capture("00-pause-empty-inventory");
            trial.ResumeRun();
            trial.AimAt(new Vector3(0, 9, 15)); trial.Step(.02f, false);
            yield return Capture("01-cathedral-nave");
            trial.Teleport(new Vector3(28, 18, 31));
            trial.AimAt(new Vector3(0, 4, -3)); trial.Step(.02f, false);
            yield return Capture("02-cathedral-overview");
            Scene();
            foreach (ChaseEnemyKind kind in Enum.GetValues(typeof(ChaseEnemyKind)))
            {
                trial.ClearEncounter();
                var enemy = trial.SpawnEnemy(new Vector3(0, 0, -23), 1000, kind: kind, bypassCap: true);
                if (enemy == null) throw new InvalidOperationException("Portrait enemy did not spawn.");
                var agent = trial.GetComponentsInChildren<NavMeshAgent>().Single(a =>
                    a.name == kind + " pursuer " + enemy.Id);
                agent.updateRotation = false; agent.isStopped = true; enemy.Speed = 0;
                agent.transform.rotation = Quaternion.Euler(0, 180, 0);
                trial.Teleport(new Vector3(0, .08f, -27));
                trial.AimAt(enemy.HeadPosition); trial.Step(.02f, false);
                yield return Capture("03-" + kind + "-front");
                trial.Teleport(new Vector3(-3, .08f, -25));
                trial.AimAt(enemy.Position); trial.Step(.02f, false);
                yield return Capture("03-" + kind + "-oblique");
                trial.Teleport(new Vector3(0, .08f, -24.8f));
                trial.AimAt(enemy.HeadPosition); trial.Step(.02f, false);
                yield return Capture("03-" + kind + "-head-close");
            }
            foreach (ChaseWeaponKind kind in Enum.GetValues(typeof(ChaseWeaponKind)))
            {
                Scene(kind);
                var enemy = trial.SpawnEnemy(new Vector3(0,0,-27), 1000,
                    kind: kind == ChaseWeaponKind.Pactbinder ? ChaseEnemyKind.Demon : ChaseEnemyKind.Undead,
                    bypassCap: true);
                if (enemy == null) throw new InvalidOperationException("Weapon-context enemy did not spawn.");
                enemy.Speed = 0;
                trial.AimAt(kind == ChaseWeaponKind.Inferno ? enemy.Position : enemy.HeadPosition);
                trial.Step(.02f, false);
                yield return Capture("04-" + kind + "-held");
                // Same model, opposite lighting/background, not a material-only assertion.
                trial.Teleport(new Vector3(28, 18, 31));
                trial.AimAt(new Vector3(0, 4, -3)); trial.Step(.02f, false);
                yield return Capture("04-" + kind + "-held-bright");
                trial.Teleport(new Vector3(0, .08f, -31));
                trial.AimAt(kind == ChaseWeaponKind.Inferno ? enemy.Position : enemy.HeadPosition);
                int hits = trial.State.TotalHits;
                if (!trial.RequestCopy(0, true, enemy.Feet + Vector3.up * .08f))
                    throw new InvalidOperationException("Presented " + kind + " did not actually fire.");
                trial.ConsumeFeedback();
                double deadline = trial.State.Elapsed + 4;
                while (trial.State.TotalHits == hits && trial.State.Elapsed < deadline)
                { trial.Step(Time.deltaTime, false); yield return null; }
                if (trial.State.TotalHits > hits) passed++;
                else { failed++; Debug.LogError("REBORN_WEAPON_HIT_FAIL " + kind); }
                yield return Capture("04-" + kind + "-firing");
                if (kind == ChaseWeaponKind.Pactbinder)
                {
                    trial.State.DamageEnemy(enemy, enemy.Health - enemy.MaxHealth * .3f, kind);
                    trial.State.Weapons[0].ReadyAt = trial.State.Elapsed;
                    if (!trial.RequestCopy(0, true, aimed: enemy.Id, alternate: true) ||
                        enemy.BoundUntil <= trial.State.Elapsed) failed++;
                    else passed++;
                    trial.ConsumeFeedback(); trial.Step(.02f, false);
                    yield return Capture("04-Pactbinder-bound");
                }
                if (kind == ChaseWeaponKind.Graveburst)
                {
                    trial.State.DamageEnemy(enemy, enemy.Health, kind); trial.ConsumeFeedback();
                    trial.State.Weapons[0].ReadyAt = trial.State.Elapsed;
                    if (!trial.RequestCopy(0, true, alternate: true)) failed++; else passed++;
                    trial.ConsumeFeedback(); yield return Capture("04-Graveburst-corpse");
                }
            }
            Scene();
            foreach (ChaseWeaponKind kind in new[] { ChaseWeaponKind.BoneLance, ChaseWeaponKind.Inferno,
                ChaseWeaponKind.Magma, ChaseWeaponKind.Winterglass, ChaseWeaponKind.Pactbinder })
                trial.State.Grant(kind);
            trial.SyncGuns(); trial.Step(.02f, false);
            yield return Capture("05-six-copy-arsenal");
            var weapon = trial.State.DropLoot(trial.PlayerPosition + new Vector3(.55f,.3f,1.45f),
                ChaseLootKind.Weapon, ChaseWeaponKind.BoneLance);
            var relic = trial.State.DropLoot(trial.PlayerPosition + new Vector3(-.55f,.3f,1.45f),
                ChaseLootKind.Relic, relic: ChaseRelicKind.Windstep);
            trial.AimAt(trial.PlayerPosition + new Vector3(0,.3f,1.45f));
            trial.Step(.02f, false); yield return Capture("06-world-loot");
            if (!trial.CollectNearestLoot()) throw new InvalidOperationException("Presented loot could not be collected.");
            trial.Step(.02f, false); yield return Capture("06-relic-loot");
            trial.State.CollectXp(trial.State.Threshold);
            yield return Capture("07-level-choice");
            trial.SelectChoice(0); trial.PauseRun();
            trial.Hud.ShowInventory(true);
            yield return Capture("08-pause-inventory");
            trial.Hud.ShowInventory(false);
            yield return Capture("08-pause-settings");
            yield return Clarity();
        }

        IEnumerator Clarity()
        {
            foreach (ChaseWeaponKind kind in Enum.GetValues(typeof(ChaseWeaponKind)))
            {
                Scene();
                var drop = trial.State.DropLoot(trial.PlayerPosition + new Vector3(0, .08f, 1.75f),
                    ChaseLootKind.Weapon, kind);
                trial.AimAt(drop.Position + Vector3.up * .70f);
                trial.Step(.02f, false);
                yield return Capture("09-floor-gun-" + kind);
            }
            foreach (ChaseRelicKind kind in Enum.GetValues(typeof(ChaseRelicKind)))
            {
                Scene();
                var drop = trial.State.DropLoot(trial.PlayerPosition + new Vector3(0, .08f, 1.75f),
                    ChaseLootKind.Relic, relic: kind);
                trial.AimAt(drop.Position + Vector3.up * .80f); trial.Step(.02f, false);
                yield return Capture("10-floor-relic-" + kind);
                if (!trial.CollectNearestLoot() || !trial.State.Relics.Exists(r => r.Kind == kind))
                    throw new InvalidOperationException("J presented relic acquisition failed.");
                trial.PauseRun(); trial.Hud.ShowInventory(true);
                yield return Capture("10-owned-relic-" + kind);
            }
            Scene();
            var gunLoot = trial.State.DropLoot(new Vector3(-1.3f, .08f, -22), ChaseLootKind.Weapon, ChaseWeaponKind.Magma);
            var relicLoot = trial.State.DropLoot(new Vector3(1.3f, .08f, -22), ChaseLootKind.Relic, relic: ChaseRelicKind.BloodGlass);
            trial.AddPickup(new Vector3(0, .2f, -22), false);
            trial.AimAt(new Vector3(0, .6f, -22)); trial.Step(.02f, false);
            yield return Capture("11-loot-approach");
            trial.Teleport(new Vector3(-4, .08f, -25));
            trial.AimAt(new Vector3(0, .6f, -22)); trial.Step(.02f, false);
            yield return Capture("11-loot-oblique");
            Scene(ChaseWeaponKind.BoneLance);
            trial.State.CollectXp(trial.State.Threshold);
            trial.State.Choices.Clear();
            foreach (ChaseUpgradeKind kind in Enum.GetValues(typeof(ChaseUpgradeKind)))
                trial.State.Choices.Add(new ChaseChoice { Kind = ChaseWeaponKind.BoneLance,
                    CopyId = trial.State.Weapons[0].CopyId, UpgradeKind = kind });
            yield return Capture("12-weapon-gain-cost");
            trial.SelectChoice(0);
            foreach (ChaseRelicKind kind in Enum.GetValues(typeof(ChaseRelicKind)))
            {
                var drop = trial.State.DropLoot(trial.PlayerPosition, ChaseLootKind.Relic, relic: kind);
                trial.State.CollectLoot(drop.Id);
            }
            for (int row = 0; row < 2; row++)
            {
                trial.State.CollectXp(trial.State.Threshold);
                trial.State.Choices.Clear();
                for (int i = 0; i < 3; i++)
                    trial.State.Choices.Add(new ChaseChoice { Relic = (ChaseRelicKind)(row * 3 + i) });
                yield return Capture("12-relic-gain-cost-" + row);
                trial.SelectChoice(0);
            }
            trial.PauseRun(); trial.Hud.ShowInventory(true);
            yield return Capture("13-pause-six-relics");
            trial.ResumeRun();
            for (int i = 1; i < 6; i++) trial.State.Grant(ChaseWeaponKind.BoneLance);
            trial.SyncGuns(); trial.PauseRun(); trial.Hud.ShowInventory(true);
            yield return Capture("13-pause-six-duplicate-guns");
            trial.ResumeRun();
            trial.State.DropLoot(trial.PlayerPosition + new Vector3(0, .08f, 1.75f),
                ChaseLootKind.Weapon, ChaseWeaponKind.Inferno);
            trial.AimAt(trial.PlayerPosition + new Vector3(0, .7f, 1.75f));
            trial.Step(.02f, false);
            yield return Capture("14-full-inventory-replacement");
            foreach (ChaseLootKind kind in Enum.GetValues(typeof(ChaseLootKind)))
            {
                Scene();
                var drop = trial.State.DropLoot(trial.PlayerPosition + new Vector3(0, .08f, 1.7f),
                    kind, ChaseWeaponKind.Magma, ChaseRelicKind.BloodGlass);
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "J capture-only physical occluder"; wall.layer = 8;
                wall.transform.position = trial.PlayerPosition + new Vector3(0, 1.25f, .85f);
                wall.transform.localScale = new Vector3(3, 2.4f, .15f);
                var wallMaterial = new Material(Shader.Find("Standard"));
                wallMaterial.color = new Color(.25f, .23f, .21f);
                wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
                trial.AimAt(drop.Position + Vector3.up * .7f);
                Physics.SyncTransforms(); trial.Step(.02f, false);
                if (trial.NearestLoot != drop || ChaseLootArt.CueVisible(trial.ViewCamera.transform.position, drop))
                    throw new InvalidOperationException("Nearest " + kind + " cue was not occluded.");
                yield return Capture("15-blocked-nearest-" + kind);
                wall.SetActive(false); Physics.SyncTransforms(); trial.Step(.02f, false);
                if (!ChaseLootArt.CueVisible(trial.ViewCamera.transform.position, drop))
                    throw new InvalidOperationException("Unblocked " + kind + " cue did not return.");
                yield return Capture("15-visible-nearest-" + kind);
                Destroy(wall); Destroy(wallMaterial);
            }
        }
        [Serializable] sealed class PressureSample
        {
            public float time, closest, wallSeconds;
            public int spawned, active, kills, contacts, level;
        }
        [Serializable] sealed class PressureTrace
        {
            public bool observerHealth;
            public List<PressureSample> samples = new List<PressureSample>();
            public List<ChaseSpawnRecord> arrivals = new List<ChaseSpawnRecord>();
        }
        IEnumerator Gameplay()
        {
            Scene(ChaseWeaponKind.BoneLance);
            trial.SpawningEnabled = true; trial.State.Automatic = true;
            var trace = new PressureTrace { observerHealth = observerHealth };
            float started = Time.realtimeSinceStartup;
            int waypoint = 0, sampleAt = 10;
            while (trial.State.Elapsed < 120 && Time.realtimeSinceStartup - started < 180)
            {
                if (trial.State.Phase == ChasePhase.Defeat) break;
                if (trial.State.Phase == ChasePhase.Upgrade)
                { trial.SelectChoice(0); yield return null; continue; }
                Vector3 destination = trial.Arena.OuterRoute[waypoint];
                Vector3 movement = destination - trial.PlayerPosition; movement.y = 0;
                if (movement.magnitude < .6f)
                    waypoint = (waypoint + 1) % trial.Arena.OuterRoute.Length;
                ChaseEnemy target = null; float closest = float.PositiveInfinity;
                foreach (var enemy in trial.State.Enemies)
                    if (enemy.Alive && enemy.BoundUntil <= trial.State.Elapsed)
                    {
                        float distance = Vector3.Distance(enemy.Feet, trial.PlayerPosition);
                        if (distance < closest) { target = enemy; closest = distance; }
                    }
                if (target != null) trial.AimAt(target.HeadPosition);
                Vector3 direction = movement.normalized;
                Vector3 origin = trial.PlayerPosition + Vector3.up * .9f;
                if (Physics.CheckSphere(origin, .35f, 1 << 10, QueryTriggerInteraction.Ignore) ||
                    Physics.SphereCast(origin, .35f, direction, out _, 1.4f, 1 << 10, QueryTriggerInteraction.Ignore))
                {
                    Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                    for (int sign = 1; sign >= -1; sign -= 2)
                    {
                        Vector3 candidate = trial.PlayerPosition + side * (1.2f * sign);
                        if (!Physics.CheckSphere(candidate + Vector3.up * .9f, .35f, (1 << 8) | (1 << 10)) &&
                            Physics.Raycast(candidate + Vector3.up * .3f, Vector3.down, 3, 1 << 9))
                        { direction = side * sign; break; }
                    }
                }
                trial.MoveRunner(direction, true, Time.deltaTime);
                // Explicit pressure-observer mode preserves arrivals, real
                // contact acceptance, damage escalation and the survival clock.
                // It is not evidence that an ordinary player survived this run.
                if (observerHealth) trial.State.Heal(100);
                trial.Step(Time.deltaTime, false);
                if (trial.NearestLoot != null) trial.CollectNearestLoot();
                if (trial.State.Elapsed >= sampleAt)
                {
                    trace.samples.Add(new PressureSample { time = (float)trial.State.Elapsed,
                        closest = float.IsInfinity(closest) ? -1 : closest,
                        wallSeconds = Time.realtimeSinceStartup - started, spawned = trial.SpawnedTotal,
                        active = trial.EnemyCount, kills = trial.State.Kills,
                        contacts = trial.ContactsObserved, level = trial.State.Level });
                    Debug.Log("REBORN_PRESSURE time=" + trial.State.Elapsed.ToString("0.0") +
                        " spawned=" + trial.SpawnedTotal + " active=" + trial.EnemyCount +
                        " closest=" + closest.ToString("0.0") + " contacts=" + trial.ContactsObserved);
                    if (sampleAt == 30 || sampleAt == 60 || sampleAt == 120)
                        yield return Capture("09-opening-" + sampleAt);
                    sampleAt += 10;
                }
                yield return null;
            }
            trace.arrivals.AddRange(trial.SpawnRecords);
            File.WriteAllText(Path.Combine(output, "pressure.json"), JsonUtility.ToJson(trace, true));
            if (trial.State.Elapsed >= 120) passed++;
            else { failed++; Debug.LogError("REBORN_GAMEPLAY_FAIL opening ended at " + trial.State.Elapsed +
                " phase=" + trial.State.Phase); }
            yield return Capture("09-opening-end");
        }
        [Serializable] sealed class LoadSample
        {
            public int quality, active, frames, detailed, instanced;
            public float meanMs, p95Ms;
            public float transitionFrameMs;
        }
        [Serializable] sealed class LoadTrace { public List<LoadSample> samples = new List<LoadSample>(); }
        IEnumerator Load()
        {
            Scene(); trial.State.Elapsed = 480;
            trial.Teleport(new Vector3(0, 14, -31));
            trial.AimAt(new Vector3(0, 1.5f, 0));
            for (int i = 0; i < 1200; i++)
            {
                var slots = trial.Arena.SpawnSlots[i % trial.Arena.SpawnSlots.Length];
                Vector3 point = slots[(i / trial.Arena.SpawnSlots.Length) % slots.Length];
                if (trial.SpawnEnemy(point, 1000, kind: (ChaseEnemyKind)(i % 3), bypassCap: true) == null)
                { failed++; Debug.LogError("REBORN_LOAD_SPAWN_FAIL index=" + i); }
            }
            var trace = new LoadTrace();
            foreach (int quality in new[] { 2, 0 })
            {
                trial.SetGraphics(quality);
                var durations = new List<float>();
                bool complete = false, coverage = true;
                bool primed = false;
                float transitionMs = 0;
                Action<int> observe = count => {
                    coverage &= count == trial.EnemyCount &&
                        trial.Swarm.DetailedCount + trial.Swarm.InstancedCount == count;
                    if (!primed)
                    {
                        primed = true; transitionMs = Time.unscaledDeltaTime * 1000;
                        return;
                    }
                    durations.Add(Time.unscaledDeltaTime * 1000);
                    complete = durations.Count >= 240;
                };
                trial.EnemyPresentationComplete += observe;
                try
                {
                    float deadline = Time.realtimeSinceStartup + 120;
                    while (!complete && Time.realtimeSinceStartup < deadline)
                    { trial.Step(Time.deltaTime, false); yield return null; }
                }
                finally { trial.EnemyPresentationComplete -= observe; }
                if (complete && coverage && trial.EnemyCount >= 1000) passed++;
                else { failed++; Debug.LogError("REBORN_LOAD_FRAME_FAIL quality=" + quality); }
                float mean = durations.Count == 0 ? 0 : durations.Average();
                durations.Sort();
                trace.samples.Add(new LoadSample { quality = quality, active = trial.EnemyCount,
                    frames = durations.Count, detailed = trial.Swarm.DetailedCount, instanced = trial.Swarm.InstancedCount,
                    meanMs = mean, transitionFrameMs = transitionMs, p95Ms = durations.Count == 0 ? 0 :
                        durations[Mathf.Min(durations.Count - 1, (int)(durations.Count * .95f))] });
                Debug.Log("REBORN_LOAD quality=" + quality + " active=" + trial.EnemyCount +
                    " frames=" + durations.Count + " meanMs=" + mean.ToString("0.00"));
                yield return Capture("10-load-quality-" + quality);
            }
            File.WriteAllText(Path.Combine(output, "load.json"), JsonUtility.ToJson(trace, true));
        }
        IEnumerator Regressions()
        {
            var assembly = Assembly.LoadFrom(tests);
            foreach (var suite in assembly.GetTypes().Where(t =>
                t.Namespace == "GameDevHarness.FantasyTrialB.Tests" ||
                t.Namespace == "GameDevHarness.CrucibleReborn.Tests"))
            {
                var instance = Activator.CreateInstance(suite);
                var methods = suite.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                try
                {
                    InvokeFixtures(methods, instance, "OneTimeSetUpAttribute");
                    foreach (var method in methods)
                    foreach (var arguments in Cases(suite, method))
                    {
                        var parameters = method.GetParameters();
                        for (int i = 0; i < arguments.Length; i++)
                            if (arguments[i] != null && !parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                                arguments[i] = parameters[i].ParameterType.IsEnum
                                    ? Enum.ToObject(parameters[i].ParameterType, arguments[i])
                                    : Convert.ChangeType(arguments[i], parameters[i].ParameterType, CultureInfo.InvariantCulture);
                        try
                        {
                            try { InvokeFixtures(methods, instance, "SetUpAttribute"); method.Invoke(instance, arguments); }
                            finally { InvokeFixtures(methods, instance, "TearDownAttribute"); }
                            passed++; Debug.Log("REBORN_TEST_PASS " + suite.Name + "." + method.Name);
                        }
                        catch (Exception error)
                        {
                            failed++; Debug.LogError("REBORN_TEST_FAIL " + suite.Name + "." + method.Name + "\n" + error);
                        }
                    }
                }
                finally { InvokeFixtures(methods, instance, "OneTimeTearDownAttribute"); }
                yield return null;
            }
            File.WriteAllText(Path.Combine(output, "regressions.json"),
                $"{{\"passed\":{passed},\"failed\":{failed}}}");
        }
        static void InvokeFixtures(MethodInfo[] methods, object instance, string name)
        {
            foreach (var method in methods.Where(m => m.GetCustomAttributes(false)
                .Any(a => a.GetType().FullName == "NUnit.Framework." + name))) method.Invoke(instance, null);
        }
        static IEnumerable<object[]> Cases(Type suite, MethodInfo method)
        {
            foreach (var attribute in method.GetCustomAttributes(false))
            {
                string name = attribute.GetType().FullName;
                if (name == "NUnit.Framework.TestAttribute") yield return new object[0];
                else if (name == "NUnit.Framework.TestCaseAttribute")
                    yield return (object[])attribute.GetType().GetProperty("Arguments").GetValue(attribute);
                else if (name == "NUnit.Framework.TestCaseSourceAttribute")
                {
                    var sourceName = (string)attribute.GetType().GetProperty("SourceName").GetValue(attribute);
                    const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                    var values = (IEnumerable)suite.GetProperty(sourceName, flags).GetValue(null);
                    foreach (var value in values) yield return new[] { value };
                }
            }
        }
    }
}
