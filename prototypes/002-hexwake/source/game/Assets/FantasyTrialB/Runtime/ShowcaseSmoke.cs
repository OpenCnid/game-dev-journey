using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GameDevHarness.FantasyTrialB
{
    public sealed class ShowcaseSmoke : MonoBehaviour
    {
        FantasyShowcaseController trial;
        string output;
        int passed;
        bool failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-showcaseOutput");
            if (index < 0 || index + 1 >= args.Length || FindAnyObjectByType<ShowcaseSmoke>() != null) return;
            var controller = FindAnyObjectByType<FantasyShowcaseController>();
            if (controller == null) throw new InvalidOperationException("B scene bootstrap is missing.");
            var driver = controller.gameObject.AddComponent<ShowcaseSmoke>();
            driver.trial = controller;
            driver.output = Path.GetFullPath(args[index + 1]);
        }

        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("SHOWCASE_CASE_FAIL " + name);
            passed++; Debug.Log("SHOWCASE_CASE_PASS " + name);
        }

        void RunCase(Action action)
        {
            if (failed) return;
            try { action(); }
            catch (Exception e) { failed = true; Debug.LogException(e); }
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            Destroy(image);
            Debug.Log("SHOWCASE_CAPTURE " + name);
        }

        void Scenario()
        {
            trial.ResetRun(47068);
            trial.ClearEncounter();
            trial.StartRun();
            trial.State.Automatic = false;
            Physics.SyncTransforms();
        }

        void Fire(int copy, int target = -1, Vector3? ground = null)
        {
            Check(trial.RequestCopy(copy, true, ground, target), "accept-copy-" + copy);
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            trial.ScriptedControl = true;
            Directory.CreateDirectory(output);
            var args = Environment.GetCommandLineArgs();
            int regression = Array.IndexOf(args, "-showcaseRegressionAssembly");
            if (regression >= 0 && regression + 1 < args.Length)
            { yield return RunRegressions(args[regression + 1]); yield break; }
            yield return Capture("00-cinematic-arrival");
            RunCase(() =>
            {
                trial.StartRun();
                Check(trial.VisibleGunCount == 1, "starter-one-visible-gun");
                Check(trial.ViewCamera.GetComponentsInChildren<MeshRenderer>().Length > 8, "detailed-gun-and-player-meshes");
                Check(FindObjectsByType<MeshRenderer>().Length > 50, "layered-world-and-enemies");
            });
            yield return Capture("01-opening-clearing");
            RunCase(() =>
            {
                trial.State.Grant(WeaponKind.Ember); trial.SyncGuns(); trial.AnimateGuns(0);
                Check(trial.VisibleGunCount == 2, "grant-adds-visible-gun");
                var one = trial.Guns[trial.State.Weapons[0].CopyId];
                var two = trial.Guns[trial.State.Weapons[1].CopyId];
                Check(Vector3.Distance(one.Muzzle.position, two.Muzzle.position) > 0.1f, "distinct-muzzle-positions");
            });
            yield return Capture("02-two-independent-guns");
            RunCase(() =>
            {
                trial.State.Grant(WeaponKind.Whirlwind); trial.State.Grant(WeaponKind.Spark);
                trial.SyncGuns(); trial.AnimateGuns(0);
                Check(trial.VisibleGunCount == 4, "four-profile-visible-arsenal");
            });
            yield return Capture("03-four-profile-arsenal");
            RunCase(() =>
            {
                trial.State.Grant(WeaponKind.Ember); trial.State.Grant(WeaponKind.Spark);
                trial.SyncGuns(); trial.AnimateGuns(0);
                Check(trial.VisibleGunCount == 6 && !trial.State.Grant(WeaponKind.Pistol), "six-copies-visible-and-capped");
                trial.AimAt(new Vector3(-9, 3, 18));
            });
            yield return Capture("04-six-guns-west-vista");
            RunCase(() => trial.AimAt(new Vector3(10, 3, 18)));
            yield return Capture("05-six-guns-east-vista");
            RunCase(() =>
            {
                Scenario();
                var enemy = trial.SpawnEnemy(new Vector3(0, 0, 4));
                Check(enemy != null, "enemy-spawns-on-real-navmesh");
                trial.AimAt(enemy.Position);
                Physics.SyncTransforms();
                Check(trial.AimedEnemy(new Ray(trial.ViewCamera.transform.position, trial.ViewCamera.transform.forward)) == enemy.Id,
                    "real-collider-target");
                var ownMuzzle = trial.Guns[trial.State.Weapons[0].CopyId].Muzzle.position;
                Fire(0, enemy.Id);
                Check(enemy.Health == 24 && trial.State.Attacks[0].Origin == ownMuzzle, "pistol-own-muzzle-instant-damage");
                trial.ConsumeFeedback();
            });
            yield return Capture("06-close-sentinel-pistol");
            GameObject barrelCover = null;
            RunCase(() =>
            {
                Scenario();
                trial.State.Grant(WeaponKind.Pistol); trial.SyncGuns();
                var enemy = trial.SpawnEnemy(new Vector3(0, 0, 4));
                trial.AimAt(enemy.Position);
                var first = trial.Guns[trial.State.Weapons[0].CopyId];
                var second = trial.Guns[trial.State.Weapons[1].CopyId];
                barrelCover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barrelCover.name = "Diagnostic near-barrel cover";
                barrelCover.layer = 8;
                barrelCover.transform.position = Vector3.Lerp(first.Muzzle.position, enemy.Position, 0.04f);
                barrelCover.transform.localScale = new Vector3(0.12f, 0.25f, 0.08f);
                Physics.SyncTransforms();
                Check(trial.AimedEnemy(new Ray(trial.ViewCamera.transform.position,
                    trial.ViewCamera.transform.forward)) == enemy.Id, "camera-clear-while-barrel-blocked");
                Fire(0, enemy.Id);
                Check(enemy.Health == 42, "blocked-own-barrel-cannot-damage");
                Fire(1, enemy.Id);
                Check(enemy.Health == 24, "other-copy-clear-barrel-still-hits");
                Check(trial.State.Attacks[0].Origin == first.Muzzle.position &&
                    trial.State.Attacks[1].Origin == second.Muzzle.position, "independent-physical-shot-origins");
                trial.ConsumeFeedback();
                barrelCover.GetComponent<Collider>().enabled = false;
                Destroy(barrelCover); barrelCover = null;
            });
            if (barrelCover != null)
            {
                barrelCover.GetComponent<Collider>().enabled = false;
                Destroy(barrelCover);
            }
            yield return Capture("06b-independent-barrel-cover");
            RunCase(() =>
            {
                Scenario(); trial.State.Grant(WeaponKind.Ember); trial.SyncGuns();
                var first = trial.SpawnEnemy(new Vector3(0, 0, 4));
                var second = trial.SpawnEnemy(new Vector3(0.6f, 0, 5));
                trial.AimAt(first.Position);
                Fire(1, first.Id);
                Check(first.Health == 32 && second.Health == 32, "weaker-fire-gun-cone");
                Check(trial.State.Attacks[0].CopyId == trial.State.Weapons[1].CopyId, "fire-copy-identity");
                trial.ConsumeFeedback();
            });
            yield return Capture("07-fire-gun");
            RunCase(() =>
            {
                Scenario(); trial.State.Grant(WeaponKind.Spark); trial.SyncGuns();
                var enemies = new TrialEnemy[3];
                for (int i = 0; i < 3; i++) enemies[i] = trial.SpawnEnemy(new Vector3(0, 0, 4 + i * 4));
                trial.AimAt(enemies[0].Position);
                Fire(1);
                Check(enemies.All(e => e.Health == 32), "lightning-three-unique-victims");
                Check(trial.State.Attacks[0].Origin == trial.Guns[trial.State.Weapons[1].CopyId].Muzzle.position,
                    "lightning-own-emitter");
                trial.ConsumeFeedback();
            });
            yield return Capture("08-lightning-gun");
            RunCase(() =>
            {
                Scenario(); trial.State.Grant(WeaponKind.Whirlwind); trial.SyncGuns();
                var enemy = trial.SpawnEnemy(new Vector3(0.4f, 0, 5));
                trial.AimAt(new Vector3(0, 0, 5));
                var point = trial.ProjectGround(new Ray(trial.ViewCamera.transform.position, trial.ViewCamera.transform.forward));
                Check(point.HasValue, "projected-real-ground");
                Fire(1, -1, point);
                Check(trial.State.Attacks[0].Origin == trial.Guns[trial.State.Weapons[1].CopyId].Muzzle.position,
                    "wind-gun-projection-origin");
                trial.ConsumeFeedback();
                trial.State.Advance(0.5f); trial.ConsumeFeedback();
                Check(enemy.Health < enemy.MaxHealth && trial.State.Vortices.Count == 1, "vortex-periodic-influence");
            });
            yield return Capture("09-wind-gun");
            RunCase(() =>
            {
                Scenario();
                var enemies = new TrialEnemy[3];
                for (int i = 0; i < 3; i++) enemies[i] = trial.SpawnEnemy(new Vector3(-1 + i, 0, 8));
                foreach (var enemy in enemies) trial.State.DamageEnemy(enemy, 42);
                trial.ConsumeFeedback();
                Check(trial.State.Xp == 0 && trial.PickupCount >= 3, "kills-drop-without-auto-xp");
                trial.Teleport(new Vector3(0, 0.08f, 8));
                trial.Step(0, false);
                Check(trial.State.Phase == FantasyPhase.Upgrade && trial.State.Xp == 12, "real-collection-level-choice");
                double clock = trial.State.Elapsed;
                trial.Step(30, true);
                Check(clock == trial.State.Elapsed, "upgrade-freezes-clock-and-pressure");
            });
            yield return Capture("10-small-gain-choice");
            RunCase(() =>
            {
                int card = trial.State.Choices.FindIndex(c => c.IsGrant && c.Kind != WeaponKind.Pistol);
                Check(card >= 0, "magic-gun-offer");
                trial.SelectChoice(card);
                Check(trial.VisibleGunCount == 2 && trial.State.Phase == FantasyPhase.Playing, "normal-choice-creates-gun");
                trial.PauseRun();
                double clock = trial.State.Elapsed;
                trial.Step(20, true);
                Check(clock == trial.State.Elapsed, "pause-freezes-stage");
            });
            yield return Capture("11-pause");
            RunCase(() =>
            {
                Scenario();
                var early = trial.SpawnEnemy(new Vector3(-2, 0, 8));
                trial.State.Advance(300);
                var middle = trial.SpawnEnemy(new Vector3(0, 0, 8));
                trial.State.Advance(299);
                var late = trial.SpawnEnemy(new Vector3(2, 0, 8));
                Check(early.MaxHealth == 42 && middle.MaxHealth == 84 && late.MaxHealth > 125,
                    "spawn-time-health-scaling");
                Check(early.Speed < middle.Speed && middle.Speed < late.Speed, "bounded-speed-scaling");
                Check(early.MaxHealth == 42, "existing-enemy-health-not-inflated");
                Check(FantasyRun.SpawnIntervalAt(599) < FantasyRun.SpawnIntervalAt(300), "no-midrun-spawn-plateau");
                trial.AimAt(middle.Position);
            });
            yield return Capture("12-late-pressure");
            RunCase(() =>
            {
                Scenario();
                trial.State.Advance(450);
                for (int i = 0; i < 48; i++)
                    trial.SpawnEnemy(new Vector3(-14 + i % 8 * 4, 0, 5 + i / 8 * 2));
                Check(trial.State.Enemies.Count == 48, "late-cap-render-load");
                trial.AimAt(new Vector3(0, 1, 12));
            });
            yield return Capture("12b-late-crowd");
            RunCase(() => trial.State.Advance(150));
            RunCase(() =>
            {
                Check(trial.State.Phase == FantasyPhase.Victory, "timer-victory");
            });
            yield return Capture("13-victory");
            RunCase(() =>
            {
                Scenario();
                for (int i = 0; i < 10; i++) { trial.State.Contact(); if (i < 9) trial.State.Advance(1); }
                Check(trial.State.Phase == FantasyPhase.Defeat && trial.State.Health == 0, "contact-defeat");
            });
            yield return Capture("14-defeat");
            RunCase(() =>
            {
                trial.ResetRun(47069);
                Check(trial.VisibleGunCount == 1 && trial.State.Weapons[0].DamageRanks == 0 && trial.State.Health == 100,
                    "fresh-reset-base-power-and-visibility");
            });
            yield return Capture("15-fresh-arrival");
            File.WriteAllText(Path.Combine(output, "result.json"),
                "{\"schemaVersion\":1,\"channel\":\"showcase-package\",\"passed\":" + passed + ",\"failed\":" + (failed ? 1 : 0) + "}");
            Debug.Log($"SHOWCASE_SMOKE_DONE passed={passed} failed={(failed ? 1 : 0)}");
            Application.Quit(failed ? 1 : 0);
        }

        IEnumerator RunRegressions(string path)
        {
            int failures = 0;
            var assembly = Assembly.LoadFrom(Path.GetFullPath(path));
            foreach (var type in assembly.GetTypes().Where(t => t.Namespace == "GameDevHarness.FantasyTrialB.Tests"))
                foreach (var method in type.GetMethods())
                    foreach (var attribute in method.GetCustomAttributes(false))
                    {
                        string name = attribute.GetType().FullName;
                        if (name != "NUnit.Framework.TestAttribute" && name != "NUnit.Framework.TestCaseAttribute") continue;
                        var arguments = name.EndsWith("TestCaseAttribute")
                            ? (object[])attribute.GetType().GetProperty("Arguments").GetValue(attribute) : new object[0];
                        var parameters = method.GetParameters();
                        for (int i = 0; i < arguments.Length; i++)
                            if (arguments[i] != null && !parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                                arguments[i] = Convert.ChangeType(arguments[i], parameters[i].ParameterType,
                                    System.Globalization.CultureInfo.InvariantCulture);
                        var instance = Activator.CreateInstance(type);
                        try
                        {
                            foreach (var setup in type.GetMethods().Where(m => m.GetCustomAttributes(false)
                                .Any(a => a.GetType().FullName == "NUnit.Framework.SetUpAttribute"))) setup.Invoke(instance, null);
                            method.Invoke(instance, arguments);
                            passed++; Debug.Log("SHOWCASE_REGRESSION_PASS " + method.Name);
                        }
                        catch (Exception e)
                        {
                            failures++;
                            var actual = e is TargetInvocationException invocation ? invocation.InnerException : e;
                            Debug.LogError("SHOWCASE_REGRESSION_FAIL " + method.Name + "\n" + actual);
                        }
                        finally
                        {
                            foreach (var teardown in type.GetMethods().Where(m => m.GetCustomAttributes(false)
                                .Any(a => a.GetType().FullName == "NUnit.Framework.TearDownAttribute"))) teardown.Invoke(instance, null);
                        }
                    }
            File.WriteAllText(Path.Combine(output, "regressions.json"),
                "{\"schemaVersion\":1,\"channel\":\"showcase-player-nunit\",\"passed\":" + passed + ",\"failed\":" + failures + "}");
            Debug.Log($"SHOWCASE_REGRESSIONS_DONE passed={passed} failed={failures}");
            yield return null;
            Application.Quit(failures == 0 && passed > 0 ? 0 : 1);
        }
    }
}
