using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    // Controller calls HandleInput once in Update and Draw from OnGUI.
    public sealed class ChaseHud
    {
        static readonly float[] ZoomFactors = { 0.75f, 1f, 1.5f, 2.25f };
        static readonly Color Ink = new Color(0.025f, 0.025f, 0.03f, 0.96f);
        static readonly Color DeepBlood = new Color(0.16f, 0.045f, 0.035f, 0.84f);
        static readonly Color Blood = new Color(0.62f, 0.19f, 0.14f);
        static readonly Color Bone = new Color(0.9f, 0.85f, 0.74f);
        static readonly Color Gold = new Color(0.61f, 0.49f, 0.31f);
        static readonly Color Friendly = new Color(0.62f, 0.73f, 0.63f);
        readonly List<ChaseEnemy> visible = new List<ChaseEnemy>(32);
        readonly List<Rect> placedEnemyBars = new List<Rect>(32);
        readonly List<ChaseLoot> visibleLoot = new List<ChaseLoot>();
        readonly List<Rect> lootLabels = new List<Rect>();
        readonly Dictionary<int, double> reloadStarted = new Dictionary<int, double>();
        readonly Dictionary<int, double> reloadEnds = new Dictionary<int, double>();
        const int MapCells = 12;
        readonly int[] mapThreats = new int[MapCells * MapCells];
        readonly int[] mapAllies = new int[MapCells * MapCells];
        readonly bool[] mapElites = new bool[MapCells * MapCells];
        GUIStyle heading, text, small, button, centered, card, primaryButton, resetButton, gain, cost;
        Texture2D buttonNormal, buttonHover, buttonActive;
        int zoomIndex = 1, inspectedClass;
        ChaseWeaponKind starter;
        float lastAudibleVolume = 0.8f;
        float lastHealth = 100, hurtUntil;
        ChasePhase previousPhase;
        bool focused = true, choiceArmed, clearFocus;
        public bool InventoryOpen { get; private set; } = true;
        public void ShowInventory(bool show) { InventoryOpen = show; }
        public float Zoom => ZoomFactors[zoomIndex];
        public int GraphicsPreset => ChaseGraphics.CurrentPreset;
        public int SelectedCopyIndex { get; set; }
        public bool MapOpen { get; private set; }
        public int InspectedClass => inspectedClass;
        public int OcclusionQueries { get; private set; }
        public void ChangeZoom(int steps) { zoomIndex = Mathf.Clamp(zoomIndex + steps, 0, 3); }
        public void InspectClass(int index) { inspectedClass = Mathf.Clamp(index, 0, 7); }

        public void ToggleMap(HorrorChaseController c)
        {
            if (c.State.Phase == ChasePhase.Playing)
            {
                MapOpen = true;
                InventoryOpen = false;
                c.MapExpanded = true;
                c.PauseRun();
            }
            else if (c.State.Phase == ChasePhase.Paused)
            {
                MapOpen = false;
                c.ResumeRun();
            }
        }

        public void ObserveState(ChaseRun state, bool selectionReleased)
        {
            var phase = state.Phase;
            if (lastHealth > state.Health) hurtUntil = Time.unscaledTime + 0.18f;
            lastHealth = state.Health;
            foreach (var gun in state.Weapons)
                if (gun.ReloadUntil > state.Elapsed &&
                    (!reloadEnds.TryGetValue(gun.CopyId, out double end) || end != gun.ReloadUntil))
                {
                    reloadStarted[gun.CopyId] = state.Elapsed;
                    reloadEnds[gun.CopyId] = gun.ReloadUntil;
                }
            if (phase != previousPhase)
            {
                choiceArmed = false;
                clearFocus = true;
                if (phase != ChasePhase.Paused) MapOpen = false;
                previousPhase = phase;
                if (phase == ChasePhase.Paused && !MapOpen) InventoryOpen = true;
                if (phase == ChasePhase.Ready)
                {
                    reloadStarted.Clear(); reloadEnds.Clear();
                    if (state.Weapons.Count > 0) starter = state.Weapons[0].Kind;
                    inspectedClass = (int)starter / 2;
                    SelectedCopyIndex = 0;
                }
            }
            if (phase == ChasePhase.Upgrade && selectionReleased) choiceArmed = true;
        }

        public bool ChoiceArmed => choiceArmed;
        public float AmmoFill(ChaseWeapon gun, double elapsed)
        {
            if (gun.ReloadUntil > elapsed && reloadStarted.TryGetValue(gun.CopyId, out double began))
                return Mathf.Clamp01((float)((elapsed - began) / Math.Max(.001, gun.ReloadUntil - began)));
            return gun.Ammo / (float)Math.Max(1, gun.Magazine);
        }

        public void HandleInput(HorrorChaseController c)
        {
            ObserveState(c.State, !Input.GetKey(KeyCode.Alpha1) && !Input.GetKey(KeyCode.Alpha2) &&
                !Input.GetKey(KeyCode.Alpha3) && !Input.GetMouseButton(0));
            var phase = c.State.Phase;
            if (!Application.isFocused)
            {
                if (focused && phase == ChasePhase.Playing) c.PauseRun();
                focused = false;
                return;
            }
            focused = true;
            if (phase != ChasePhase.Playing)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) ||
                Input.GetKeyDown(KeyCode.KeypadPlus)) ChangeZoom(1);
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                ChangeZoom(-1);
            if (Input.GetKeyDown(KeyCode.M)) { ToggleMap(c); return; }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (phase == ChasePhase.Playing) c.PauseRun();
                else if (phase == ChasePhase.Paused) { MapOpen = false; c.ResumeRun(); }
                return;
            }
            if (phase == ChasePhase.Upgrade)
            {
                if (choiceArmed)
                    for (int i = 0; i < c.State.Choices.Count && i < 3; i++)
                        if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                        { c.SelectChoice(i); return; }
            }
            if (phase != ChasePhase.Playing) return;
            if (Input.GetKeyDown(KeyCode.E)) c.CollectNearestLoot();
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                c.RequestMobility();
            if (Input.GetKeyDown(KeyCode.R)) c.ReloadAll();
            if (Input.GetKeyDown(KeyCode.F)) c.State.Automatic = !c.State.Automatic;
            for (int i = 0; i < c.State.Weapons.Count && i < 6; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) SelectedCopyIndex = i;
        }

        void Styles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            card = new GUIStyle(small) { fontSize = 16, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, wordWrap = true };
            centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            gain = new GUIStyle(heading) { fontSize = 30 };
            cost = new GUIStyle(text) { fontSize = 22 };
            gain.normal.textColor = new Color(.75f, 1f, .84f);
            cost.normal.textColor = Bone;
            buttonNormal = SolidTexture(new Color(0.13f, 0.115f, 0.09f));
            buttonHover = SolidTexture(new Color(0.25f, 0.20f, 0.135f));
            buttonActive = SolidTexture(new Color(0.08f, 0.055f, 0.04f));
            button.normal.background = buttonNormal;
            button.hover.background = buttonHover;
            button.active.background = buttonActive;
            button.focused.background = buttonHover;
            button.normal.textColor = Bone;
            button.hover.textColor = Bone;
            button.active.textColor = Gold;
            button.focused.textColor = Bone;
            primaryButton = new GUIStyle(button);
            primaryButton.normal.background = SolidTexture(new Color(.30f, .22f, .11f));
            primaryButton.hover.background = SolidTexture(new Color(.38f, .28f, .14f));
            primaryButton.focused.background = primaryButton.hover.background;
            resetButton = new GUIStyle(button);
            resetButton.normal.background = SolidTexture(new Color(.24f, .055f, .04f));
            resetButton.hover.background = SolidTexture(new Color(.34f, .075f, .055f));
            resetButton.focused.background = resetButton.hover.background;
            heading.normal.textColor = Bone;
            text.normal.textColor = small.normal.textColor = centered.normal.textColor = card.normal.textColor = Bone;
        }

        static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        static void Fill(Rect r, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void Bar(Rect r, float fraction, Color color)
        {
            Fill(r, new Color(0.12f, 0.10f, 0.085f, 0.9f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height), color);
        }

        static void Panel(Rect rect, bool selected = false)
        {
            Fill(rect, selected ? DeepBlood : Ink);
            Color edge = selected ? Gold : new Color(Gold.r, Gold.g, Gold.b, 0.38f);
            Fill(new Rect(rect.x, rect.y, rect.width, 1), edge);
            Fill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), edge);
            Fill(new Rect(rect.x, rect.y, 1, rect.height), edge);
            Fill(new Rect(rect.xMax - 1, rect.y, 1, rect.height), edge);
        }

        void MasterVolume(HorrorChaseController c, Rect rect)
        {
            float volume = Mathf.Clamp01(c.MasterVolume);
            if (volume > 0.001f) lastAudibleVolume = volume;
            Panel(rect);
            GUI.Label(new Rect(rect.x + 14, rect.y + 5, 230, 28),
                $"MASTER VOLUME  {Mathf.RoundToInt(volume * 100)}%", small);
            float next = GUI.HorizontalSlider(new Rect(rect.x + 244, rect.y + 12, rect.width - 358, 22),
                volume, 0f, 1f);
            if (!Mathf.Approximately(next, volume))
            {
                if (next > 0.001f) lastAudibleVolume = next;
                c.SetMasterVolume(next);
            }
            string mute = volume <= 0.001f ? "UNMUTE" : "MUTE";
            if (GUI.Button(new Rect(rect.xMax - 102, rect.y + 4, 92, rect.height - 8), mute, button))
                c.SetMasterVolume(volume <= 0.001f ? Mathf.Max(0.25f, lastAudibleVolume) : 0f);
        }

        public void Draw(HorrorChaseController c)
        {
            Styles();
            small.fontSize = c.State.Phase == ChasePhase.Playing ? 14 : 17;
            MapOpen = c.MapExpanded;
            if (clearFocus) { GUI.FocusControl(null); clearFocus = false; }
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            // A 1280x720 canvas at 720p, and 1728x720 at 1920x800.
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            float width = Screen.width / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            try
            {
                SelectedCopyIndex = Mathf.Clamp(SelectedCopyIndex, 0, Mathf.Max(0, c.State.Weapons.Count - 1));
                if (c.State.Phase == ChasePhase.Playing) Gameplay(c, width, scale);
                else
                {
                    MenuBackdrop(width);
                    // Menus occupy 84% of the former canvas; keep the live cathedral
                    // visible around their edges without changing any hit-test APIs.
                    GUI.matrix = Matrix4x4.TRS(new Vector3(width * .08f * scale, 57.6f * scale, 0),
                        Quaternion.identity, new Vector3(scale * .84f, scale * .84f, 1));
                    if (c.State.Phase == ChasePhase.Ready) Ready(c, width);
                    else if (c.State.Phase == ChasePhase.Upgrade) Upgrades(c, width);
                    else if (c.State.Phase == ChasePhase.Paused) Pause(c, width);
                    else Results(c, width);
                }
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }

        static void MenuBackdrop(float width)
        {
            Fill(new Rect(0, 0, width, 720), new Color(0.018f, 0.015f, 0.012f, 0.72f));
            Panel(new Rect(width / 2 - 520, 64, 1040, 592));
            Fill(new Rect(width / 2 - 72, 63, 144, 2), Gold);
            Fill(new Rect(width / 2 - 24, 655, 48, 2), Blood);
        }

        void Gameplay(HorrorChaseController c, float width, float scale)
        {
            EnemyBars(c, width, scale);
            Panel(new Rect(20, 18, 310, 52));
            int seconds = Math.Max(0, 600 - (int)c.State.Elapsed);
            GUI.Label(new Rect(30, 21, 292, 25),
                $"SURVIVE {seconds / 60:00}:{seconds % 60:00}   LEVEL {c.State.Level}", text);
            GUI.Label(new Rect(30, 46, 292, 22),
                $"{c.EnemyCount} threats  |  {c.State.Kills} kills  |  {(c.State.Automatic ? "AUTO" : "MANUAL")} [F]", small);
            Panel(new Rect(20, 78, 310, 30));
            bool mobilityReady = c.MobilityRemaining <= 0;
            string mobilityLabel = string.IsNullOrEmpty(c.MobilityLabel) ? "MOBILITY" : c.MobilityLabel.ToUpperInvariant();
            string mobilityState = mobilityReady ? "READY" : $"{c.MobilityRemaining:0.0}s";
            Fill(new Rect(20, 78, 2, 30), mobilityReady ? Gold : Blood);
            GUI.Label(new Rect(30, 82, 220, 24), mobilityLabel + "  [SHIFT]", small);
            GUI.Label(new Rect(252, 82, 70, 24), mobilityState, small);
            Map(c, new Rect(width - 180, 18, 160, 160), false);
            GUI.Label(new Rect(width - 180, 182, 160, 22), $"M map | +/- {Zoom:0.##}x", small);
            Fill(new Rect(width / 2 - 5, 359, 10, 1), Bone);
            Fill(new Rect(width / 2, 355, 1, 10), Bone);
            if (!string.IsNullOrEmpty(c.FeedbackText))
                GUI.Label(new Rect(30, 590, 680, 24), c.FeedbackText, text);
            var loot = c.NearestLoot;
            if (loot != null && !ChaseLootArt.CueVisible(c.ViewCamera.transform.position, loot)) loot = null;
            WorldLootLabels(c, width, scale, loot);
            if (loot != null)
            {
                bool weapon = loot.Kind == ChaseLootKind.Weapon;
                var prompt = new Rect(width / 2 - 390, 450, 780, 136);
                Panel(prompt);
                Fill(new Rect(prompt.x + 18, prompt.y + 40, 56, 56), Gold);
                GUI.Label(new Rect(prompt.x + 18, prompt.y + 48, 56, 40), "E", centered);
                GUI.Label(new Rect(prompt.x + 92, prompt.y + 10, 660, 26),
                    weapon ? "GUN  /  " + ChaseCatalog.Tier(loot.Weapon) : "RELIC", text);
                GUI.Label(new Rect(prompt.x + 92, prompt.y + 38, 660, 38),
                    weapon ? ChaseCatalog.Name(loot.Weapon) : ChaseRelics.Name(loot.Relic), heading);
                string action = weapon && c.State.Weapons.Count == 6 ?
                    "Add this gun. Lose " + ChaseCatalog.Name(c.State.Weapons[SelectedCopyIndex].Kind) +
                        " (gun " + (SelectedCopyIndex + 1) + ")." :
                    weapon ? "Pick up this gun" : ChaseRelics.Description(loot.Relic);
                GUI.Label(new Rect(prompt.x + 92, prompt.y + 82, 660, 48), action, text);
            }
            float arsenalWidth = Mathf.Min(width - 40, 1020);
            Panel(new Rect(20, 644, 220, 52));
            GUI.Label(new Rect(30, 650, 200, 26), $"HEALTH  {c.State.Health:0} / 100", text);
            Bar(new Rect(30, 684, 200, 4), c.State.Health / 100f, Blood);
            if (c.State.Weapons.Count > 0)
            {
                var gun = c.State.Weapons[SelectedCopyIndex];
                bool reloading = gun.ReloadUntil > c.State.Elapsed;
                const float x = 252;
                Panel(new Rect(x, 644, 340, 52));
                string label = reloading ? $"RELOADING  {gun.ReloadUntil - c.State.Elapsed:0.0}s" : $"AMMO  {gun.Ammo} / {gun.Magazine}";
                GUI.Label(new Rect(x + 10, 650, 320, 26), $"{label}  [R]", text);
                float fill = AmmoFill(gun, c.State.Elapsed);
                Bar(new Rect(x + 10, 684, 320, 4), fill, reloading ? Gold : Bone);
            }
            GUI.Label(new Rect(20, 615, 380, 24), "Escape: inventory / pause", small);
            Bar(new Rect(20, 710, arsenalWidth - 6, 2), c.State.Xp / (float)Math.Max(1, c.State.Threshold), Gold);
            float observedDamage = Time.unscaledTime < hurtUntil
                ? Mathf.Clamp01((hurtUntil - Time.unscaledTime) / 0.18f) : 0f;
            DamageEdges(width, Mathf.Max(observedDamage, Mathf.Clamp01(c.DamageFlash)));
        }

        void WorldLootLabels(HorrorChaseController c, float width, float scale, ChaseLoot nearest)
        {
            if (Event.current.type != EventType.Repaint) return;
            visibleLoot.Clear(); lootLabels.Clear();
            foreach (var drop in c.State.Loot)
                if (drop != nearest && (drop.Position - c.PlayerPosition).sqrMagnitude < 18 * 18)
                    visibleLoot.Add(drop);
            visibleLoot.Sort((a, b) => (a.Position - c.PlayerPosition).sqrMagnitude.CompareTo(
                (b.Position - c.PlayerPosition).sqrMagnitude));
            foreach (var drop in visibleLoot)
            {
                if (lootLabels.Count == 4) break;
                var p = c.ViewCamera.WorldToScreenPoint(drop.Position + Vector3.up * 1.45f);
                if (p.z <= 0 || !ChaseLootArt.CueVisible(c.ViewCamera.transform.position, drop)) continue;
                var r = new Rect(p.x / scale - 125, (Screen.height - p.y) / scale - 68, 250, 68);
                if (r.x < 20 || r.xMax > width - 20 || r.y < 210 || r.yMax > 590 ||
                    (nearest != null && r.Overlaps(new Rect(width / 2 - 390, 450, 780, 136)))) continue;
                bool overlap = false;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    overlap = false;
                    foreach (var placed in lootLabels) if (r.Overlaps(placed)) { overlap = true; break; }
                    if (!overlap) break;
                    r.y -= 76;
                }
                if (r.y < 150) continue;
                if (overlap) continue;
                lootLabels.Add(r); Panel(r);
                bool weapon = drop.Kind == ChaseLootKind.Weapon;
                GUI.Label(new Rect(r.x + 10, r.y + 5, 230, 24), weapon ? "GUN" : "RELIC", text);
                GUI.Label(new Rect(r.x + 10, r.y + 31, 230, 32),
                    weapon ? ChaseCatalog.Name(drop.Weapon) : ChaseRelics.Name(drop.Relic), text);
            }
        }

        static void DamageEdges(float width, float flash)
        {
            if (flash <= 0.001f) return;
            float outer = 0.28f + flash * 0.46f;
            float inner = flash * 0.24f;
            Color blood = new Color(0.55f, 0.005f, 0.012f, outer);
            Color stain = new Color(0.25f, 0f, 0.008f, inner);
            Fill(new Rect(0, 0, width, 18), blood);
            Fill(new Rect(0, 702, width, 18), blood);
            Fill(new Rect(0, 0, 18, 720), blood);
            Fill(new Rect(width - 18, 0, 18, 720), blood);
            Fill(new Rect(18, 18, width - 36, 10), stain);
            Fill(new Rect(18, 692, width - 36, 10), stain);
            Fill(new Rect(18, 28, 10, 664), stain);
            Fill(new Rect(width - 28, 28, 10, 664), stain);
            Fill(new Rect(28, 28, 86, 18 + flash * 28), stain);
            Fill(new Rect(width - 114, 28, 86, 18 + flash * 28), stain);
            Fill(new Rect(28, 674 - flash * 28, 110, 18 + flash * 28), stain);
            Fill(new Rect(width - 138, 674 - flash * 28, 110, 18 + flash * 28), stain);
        }

        void EnemyBars(HorrorChaseController c, float width, float scale)
        {
            if (c.ViewCamera == null || Event.current.type != EventType.Repaint) return;
            OcclusionQueries = 0;
            visible.Clear();
            var camera = c.ViewCamera;
            ChaseEnemy target = null;
            int aimedId = c.SelectedEnemyId;
            foreach (var enemy in c.State.Enemies)
            {
                if (enemy.Alive && enemy.Id == aimedId) target = enemy;
                if (!enemy.Alive || (enemy.Position - c.PlayerPosition).sqrMagnitude > 32 * 32) continue;
                Vector3 p = camera.WorldToViewportPoint(enemy.Position);
                if (p.z <= 0 || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) continue;
                int at = 0;
                float distance = (enemy.Position - c.PlayerPosition).sqrMagnitude;
                while (at < visible.Count &&
                    (visible[at].Position - c.PlayerPosition).sqrMagnitude <= distance) at++;
                if (at >= 64) continue;
                visible.Insert(at, enemy);
                if (visible.Count > 64) visible.RemoveAt(64);
            }
            if (target != null && visible.Remove(target)) visible.Insert(0, target);
            placedEnemyBars.Clear();
            var targetPanel = new Rect(width / 2 - 150, 82, 300, 44);
            var minimap = new Rect(width - 180, 18, 160, 160);
            foreach (var enemy in visible)
            {
                if (placedEnemyBars.Count >= 32) break;
                Vector3 p = camera.WorldToScreenPoint(enemy.HeadPosition + Vector3.up * .12f);
                if (p.z <= 0) continue;
                float x = p.x / scale - 48, y = (Screen.height - p.y) / scale - 32;
                if (x < 0 || x + 96 > width || y < 112 || y + 28 > 585) continue;
                var barRegion = new Rect(x - 15, y - 2, 150, 38);
                if ((target != null && targetPanel.Overlaps(barRegion)) || minimap.Overlaps(barRegion)) continue;
                bool overlaps = false;
                foreach (var placed in placedEnemyBars)
                    if (placed.Overlaps(barRegion)) { overlaps = true; break; }
                if (overlaps) continue;
                // Bound HUD-only visibility work, after proximity and label-space
                // rejection. Every enemy body still simulates and renders.
                OcclusionQueries++;
                if (Physics.Linecast(camera.transform.position, enemy.Position,
                    (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore)) continue;
                placedEnemyBars.Add(barRegion);
                bool bound = enemy.BoundUntil > c.State.Elapsed;
                Color color = bound ? Friendly : enemy.FlashUntil > c.State.Elapsed ? Color.white :
                    enemy.Kind == ChaseEnemyKind.Elite ? Gold : Blood;
                Bar(new Rect(x, y + 21, 96, 7), enemy.Health / Math.Max(1f, enemy.MaxHealth), color);
                GUI.Label(new Rect(x - 15, y - 2, 150, 23),
                    (bound ? "ALLY" : enemy.Kind.ToString().ToUpperInvariant()) + " #" + enemy.Id, small);
                Fill(new Rect(x + 47, y + 28, 2, 7), color);
                Fill(new Rect(x + 45, y + 33, 6, 3), color);
            }
            if (target == null) return;
            bool ally = target.BoundUntil > c.State.Elapsed;
            Vector3 body = camera.WorldToScreenPoint(target.Position);
            if (body.z > 0)
            {
                float bx = body.x / scale, by = (Screen.height - body.y) / scale;
                Fill(new Rect(bx - 18, by - 9, 3, 18), Gold);
                Fill(new Rect(bx + 15, by - 9, 3, 18), Gold);
            }
            GUI.Label(new Rect(width / 2 + 18, 342, 230, 26),
                "#" + target.Id + " " + (ally ? "ALLY" : target.Kind.ToString().ToUpperInvariant()), small);
            Panel(targetPanel);
            GUI.Label(new Rect(width / 2 - 140, 85, 280, 27),
                $"{(ally ? "BOUND ALLY" : target.Kind.ToString().ToUpperInvariant())} #{target.Id}   {target.Health:0}/{target.MaxHealth:0}", centered);
            Bar(new Rect(width / 2 - 140, 116, 280, 4), target.Health / Math.Max(1f, target.MaxHealth),
                ally ? Friendly : target.FlashUntil > c.State.Elapsed ? Color.white : Gold);
        }

        void Map(HorrorChaseController c, Rect rect, bool expanded)
        {
            Panel(rect);
            if (c.Arena == null) return;
            float radius = (expanded ? 60f : 32f) / Zoom;
            Vector3 center = expanded && Zoom <= 1 ? c.Arena.Bounds.center : c.PlayerPosition;
            GUI.BeginGroup(rect);
            float pixels = rect.width / (radius * 2);

            foreach (var obstacle in c.Arena.MapObstacles)
            {
                Fill(new Rect((obstacle.xMin - center.x + radius) * pixels,
                    (center.z + radius - obstacle.yMax) * pixels,
                    obstacle.width * pixels, obstacle.height * pixels), new Color(0.36f, 0.33f, 0.28f));
            }
            int active = c.ActivePortals;
            for (int i = 0; i < c.Arena.Portals.Length; i++)
            {
                Vector2 p = MapPoint(c.Arena.Portals[i], center, radius, rect.width);
                double age = c.State.Elapsed - c.PortalWarnedAt[i];
                bool warning = c.PortalWarnedAt[i] >= 0 && age >= 0 && age <= 2;
                float size = warning ? 10 + Mathf.Sin(Time.unscaledTime * 5) * 2 : 7;
                Fill(new Rect(p.x - size / 2, p.y - size / 2, size, size),
                    warning ? Bone : i < active ? Blood : new Color(0.27f, 0.20f, 0.16f));
            }
            Array.Clear(mapThreats, 0, mapThreats.Length);
            Array.Clear(mapAllies, 0, mapAllies.Length);
            Array.Clear(mapElites, 0, mapElites.Length);
            int threats = 0, allies = 0;
            foreach (var enemy in c.State.Enemies)
            {
                if (!enemy.Alive) continue;
                bool bound = enemy.BoundUntil > c.State.Elapsed;
                if (bound) allies++; else threats++;
                Vector2 p = MapPoint(enemy.Position, center, radius, rect.width);
                if (p.x < 0 || p.y < 0 || p.x >= rect.width || p.y >= rect.height) continue;
                int cell = (int)(p.y / rect.height * MapCells) * MapCells +
                    (int)(p.x / rect.width * MapCells);
                if (bound) mapAllies[cell]++;
                else
                {
                    mapThreats[cell]++;
                    mapElites[cell] |= enemy.Kind == ChaseEnemyKind.Elite;
                }
            }
            // At most 144 hostile density marks + 144 ally marks, independent
            // of population. No per-enemy GUI labels or icons at late load.
            float cellSize = rect.width / MapCells;
            for (int cell = 0; cell < mapThreats.Length; cell++)
            {
                float x = (cell % MapCells + 0.5f) * cellSize;
                float y = (cell / MapCells + 0.5f) * cellSize;
                if (mapThreats[cell] > 0)
                {
                    float size = Mathf.Min(cellSize * 0.75f, 4 + Mathf.Sqrt(mapThreats[cell]) * 2);
                    Fill(new Rect(x - size / 2, y - size / 2, size, size),
                        mapElites[cell] ? Gold : Blood);
                }
                if (mapAllies[cell] > 0)
                    Fill(new Rect(x + 3, y - 3, 5, 5), Friendly);
            }
            Vector2 player = MapPoint(c.PlayerPosition, center, radius, rect.width);
            Vector3 forward = c.ViewCamera == null ? Vector3.forward : c.ViewCamera.transform.forward;
            Vector2 direction = new Vector2(forward.x, -forward.z).normalized;
            for (int i = 0; i < 12; i++)
                Fill(new Rect(player.x + direction.x * i - 1, player.y + direction.y * i - 1, 3, 3), Friendly);
            Fill(new Rect(player.x - 4, player.y - 4, 8, 8), Bone);
            GUI.Label(new Rect(8, 4, 25, 24), "N", small);
            Fill(new Rect(0, rect.height - 26, rect.width, 26), Ink);
            GUI.Label(new Rect(6, rect.height - 25, rect.width - 12, 24),
                expanded ? $"Threats {threats} | Allies {allies}" : $"T {threats}  A {allies}", small);
            GUI.EndGroup();
        }

        static Vector2 MapPoint(Vector3 p, Vector3 center, float radius, float size)
        {
            return new Vector2((p.x - center.x + radius) * size / (2 * radius),
                (center.z + radius - p.z) * size / (2 * radius));
        }

        void Ready(HorrorChaseController c, float width)
        {
            float left = (width - 1200) / 2;
            GUI.Label(new Rect(left, 22, 1200, 42), "HEXWAKE / CHOOSE YOUR STARTING INVENTION", heading);
            GUI.Label(new Rect(left, 72, 1200, 40), "Start with one gun. Enemy loot grants copies and relics; levels and successful use improve your build.", text);
            for (int i = 0; i < 8; i++)
            {
                var kind = (ChaseWeaponKind)(i * 2);
                Rect r = new Rect(left + (i % 4) * 302, 128 + (i / 4) * 58, 290, 48);
                GUI.backgroundColor = i == inspectedClass ? Gold : Color.white;
                if (GUI.Button(r, (i == inspectedClass ? "[Selected] " : "") + ChaseCatalog.ClassName(kind), button))
                { InspectClass(i); c.PlayerFeedback?.Play(ChasePlayerCue.ClassSelect); }
            }
            GUI.backgroundColor = Color.white;
            for (int pair = 0; pair < 2; pair++)
            {
                var kind = (ChaseWeaponKind)(inspectedClass * 2 + pair);
                float x = left + pair * 612;
                Panel(new Rect(x, 260, 588, 283), starter == kind);
                GUI.Label(new Rect(x + 20, 279, 548, 45), ChaseCatalog.Name(kind), heading);
                GUI.Label(new Rect(x + 20, 332, 548, 116), ChaseCatalog.Tier(kind) + " / " +
                    ChaseCatalog.Description(kind) + "\n" + ChaseCatalog.Tradeoff(kind), text);
                if (GUI.Button(new Rect(x + 20, 470, 548, 52),
                    starter == kind ? "SELECTED STARTER" : "Select as starting weapon", button))
                { starter = kind; c.SetStarter(kind); }
            }
            GUI.Label(new Rect(left, 550, 1200, 48),
                "WASD move | Mouse aim | Space hop | Shift mobility | F auto/manual | R reload | 1-6 select copy\nE collect loot / replace selected full-slot copy | M map/pause | Upgrade: click or 1-3", small);
            MasterVolume(c, new Rect(left, 606, 1200, 40));
            if (GUI.Button(new Rect(left, 660, 800, 42), "Begin with " + ChaseCatalog.Name(starter), primaryButton))
            { c.SetStarter(starter); c.StartRun(); }
            GUI.backgroundColor = Color.white;
            if (GUI.Button(new Rect(left + 824, 660, 376, 42), "Quit", button)) Application.Quit();
        }

        void Upgrades(HorrorChaseController c, float width)
        {
            float left = (width - 1200) / 2;
            GUI.Label(new Rect(left, 32, 1200, 42), "Choose an upgrade", heading);
            GUI.Label(new Rect(left, 86, 1200, 36), "Time is frozen. Pick one.", text);
            int count = c.State.Choices.Count;
            float stride = 1200f / Math.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                var choice = c.State.Choices[i];
                float x = left + i * stride;
                Panel(new Rect(x, 154, stride - 16, 470));
                float inner = stride - 56;
                bool relic = choice.Relic.HasValue;
                int slot = c.State.Weapons.FindIndex(w => w.CopyId == choice.CopyId) + 1;
                GUI.Label(new Rect(x + 20, 176, inner, 32), relic ? "RELIC" : "GUN " + slot, text);
                GUI.Label(new Rect(x + 20, 220, inner, 75), relic ? ChaseRelics.Name(choice.Relic.Value) :
                    ChaseCatalog.Name(choice.Kind), heading);
                string addition = relic ? RelicGain(choice.Relic.Value) :
                    choice.UpgradeKind == ChaseUpgradeKind.Damage ?
                        "+" + (c.State.Weapons[slot - 1].BaseDamage * .15f).ToString("0.#") + " damage per hit" :
                    choice.UpgradeKind == ChaseUpgradeKind.Cadence ? "+7% fire rate" : "+2 rounds";
                GUI.Label(new Rect(x + 20, 320, inner, 115), addition, gain);
                GUI.Label(new Rect(x + 20, 467, inner, 76),
                    relic && choice.Relic.Value == ChaseRelicKind.VolatileHeart ?
                        "- More contact damage below half health" : "Nothing lost", cost);
                GUI.enabled = choiceArmed;
                if (GUI.Button(new Rect(x + 20, 568, inner, 44), "Choose " + (i + 1), button))
                { c.SelectChoice(i); GUI.enabled = true; return; }
                GUI.enabled = true;
            }
        }

        internal static string RelicGain(ChaseRelicKind kind)
        {
            switch (kind)
            {
                case ChaseRelicKind.Windstep: return "+ Stronger air steering";
                case ChaseRelicKind.Executioner: return "+ Faster headshot ammo return";
                case ChaseRelicKind.BloodGlass: return "+ More healing from headshots";
                case ChaseRelicKind.VolatileHeart: return "+ More shot damage below half health";
                case ChaseRelicKind.Quicksilver: return "+ Faster reloads and reload boost";
                case ChaseRelicKind.EchoSeal: return "+ Stronger third-hit echo";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        void Pause(HorrorChaseController c, float width)
        {
            float left = (width - 1200) / 2;
            GUI.Label(new Rect(left, 20, 1200, 42), "Paused - time is frozen", heading);
            if (GUI.Button(new Rect(left, 78, 390, 48), InventoryOpen ? "Inventory [Selected]" : "Inventory", button))
                InventoryOpen = true;
            if (GUI.Button(new Rect(left + 410, 78, 390, 48), !InventoryOpen ? "Map / settings [Selected]" : "Map / settings", button))
                InventoryOpen = false;
            if (GUI.Button(new Rect(left + 820, 78, 380, 48), "Resume [Escape]", primaryButton))
            { MapOpen = false; c.ResumeRun(); return; }
            if (InventoryOpen) { Inventory(c, left); return; }
            Map(c, new Rect(left, 148, 470, 470), true);
            GUI.Label(new Rect(left, 626, 510, 62), "Ivory: you | Sage: facing / bound allies\nBlood: threats / active cubes | Brass: elites\nStone: actual obstacle footprints", small);
            float x = left + 548;
            GUI.Label(new Rect(x, 148, 652, 38), $"MAP ZOOM  {Zoom:0.##}x  [+ / -]", heading);
            GUI.enabled = zoomIndex > 0;
            if (GUI.Button(new Rect(x, 194, 310, 48), "-  Zoom out", button)) ChangeZoom(-1);
            GUI.enabled = zoomIndex < 3;
            if (GUI.Button(new Rect(x + 330, 194, 310, 48), "+  Zoom in", button)) ChangeZoom(1);
            GUI.enabled = true;
            GUI.Label(new Rect(x, 263, 652, 36), "GRAPHICS / APPLIES IMMEDIATELY", text);
            string[] names = { "Low", "Medium", "High" };
            for (int i = 0; i < 3; i++)
            {
                GUI.backgroundColor = GraphicsPreset == i ? Gold : Color.white;
                if (GUI.Button(new Rect(x + i * 220, 306, 210, 48),
                    names[i] + (GraphicsPreset == i ? " [Selected]" : ""), button))
                { c.SetGraphics(i); }
            }
            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(x, 369, 640, 92), ChaseGraphics.Description(GraphicsPreset), text);
            MasterVolume(c, new Rect(x, 482, 640, 42));
            GUI.Label(new Rect(x, 547, 640, 50), "Time is frozen. Resume returns to play.\nM / Escape also resumes.", small);
            if (GUI.Button(new Rect(x, 620, 310, 48), "Reset to starter menu", resetButton))
            { MapOpen = false; c.ResetRun(); }
            GUI.backgroundColor = Color.white;
            if (GUI.Button(new Rect(x + 330, 620, 310, 48), "Quit", button)) Application.Quit();
        }

        void Inventory(HorrorChaseController c, float left)
        {
            for (int i = 0; i < 6; i++)
            {
                var r = new Rect(left + i % 3 * 405, 148 + i / 3 * 160, 390, 148);
                Panel(r, i == SelectedCopyIndex && i < c.State.Weapons.Count);
                if (i >= c.State.Weapons.Count)
                {
                    GUI.Label(new Rect(r.x + 16, r.y + 14, 358, 32), "Gun " + (i + 1) + " - empty", text);
                    GUI.Label(new Rect(r.x + 16, r.y + 58, 358, 62), "Pick up a gun from the floor", text);
                    continue;
                }
                var gun = c.State.Weapons[i];
                GUI.Label(new Rect(r.x + 16, r.y + 10, 358, 30),
                    "Gun " + (i + 1) + " - " + ChaseCatalog.Name(gun.Kind), text);
                GUI.Label(new Rect(r.x + 16, r.y + 44, 358, 25),
                    "Rank " + gun.Rank + "  /  " + ChaseCatalog.Tier(gun.Kind), small);
                GUI.Label(new Rect(r.x + 16, r.y + 71, 358, 25),
                    $"Damage {gun.Damage:0.#}  /  Fire {1f / gun.Cadence:0.#}/s  /  Rounds {gun.Magazine}", small);
                GUI.Label(new Rect(r.x + 16, r.y + 102, 358, 29),
                    $"Use {gun.SuccessfulUses}/{gun.MasteryThreshold}" +
                    (i == SelectedCopyIndex ? "  /  [Selected]" : ""), small);
            }
            GUI.Label(new Rect(left, 477, 1200, 32), "Relics", heading);
            if (c.State.Relics.Count == 0)
                GUI.Label(new Rect(left, 530, 1200, 44), "No relics yet. Look for the large gold seals.", text);
            for (int i = 0; i < c.State.Relics.Count; i++)
            {
                var relic = c.State.Relics[i];
                var r = new Rect(left + i % 3 * 405, 520 + i / 3 * 80, 390, 72);
                Panel(r);
                GUI.Label(new Rect(r.x + 12, r.y + 6, 366, 28),
                    ChaseRelics.Name(relic.Kind) + "  /  Rank " + relic.Rank, text);
                GUI.Label(new Rect(r.x + 12, r.y + 36, 366, 32), RelicGain(relic.Kind), small);
            }
        }

        void Results(HorrorChaseController c, float width)
        {
            float x = (width - 800) / 2;
            Panel(new Rect(x, 160, 800, 400));
            GUI.Label(new Rect(x + 30, 190, 740, 70),
                c.State.Phase == ChasePhase.Victory ? "SURVIVED THE ARRIVAL" : "OVERRUN / TRY ANOTHER INVENTION", heading);
            GUI.Label(new Rect(x + 30, 283, 740, 75),
                $"Survived {c.State.Elapsed:0.0}s | Kills {c.State.Kills} | Level {c.State.Level}\nRun-earned copies are reset on retry.", text);
            if (GUI.Button(new Rect(x + 30, 392, 740, 52), "Retry with " + ChaseCatalog.Name(starter), button))
            { c.ResetRun(starter: starter); c.StartRun(); }
            if (GUI.Button(new Rect(x + 30, 469, 360, 48), "Choose another starter", button)) c.ResetRun();
            if (GUI.Button(new Rect(x + 410, 469, 360, 48), "Quit", button)) Application.Quit();
        }
    }
}
