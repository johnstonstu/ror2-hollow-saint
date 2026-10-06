using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.SceneManagement;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace HollowSaint
{
    /// <summary>
    /// Development-only "proving ground". Does nothing unless HS_AUTOPILOT names an output folder.
    /// Hosts a solo run, moves to the Bazaar (no combat director, fixed lighting), makes the Saint
    /// invulnerable, stands it on a fixed mark facing the most open direction, places three
    /// braindead invulnerable target dummies, then plays a fixed segment script. Every segment
    /// starts from the same mark and facing. Shots come from a cloned game camera at fixed
    /// back / front / side offsets, rendered after all pose passes. Then it writes a trace and quits.
    /// </summary>
    internal sealed partial class DevAutopilot : MonoBehaviour
    {
        private string output;
        private readonly StringBuilder trace = new StringBuilder();
        private int errors, pops;
        private bool scripting;
        private bool fire1, fire2, fire3, fire4, jump, sprint;
        private Vector3 move;
        private Vector3? aimTarget;
        private bool prev1, prev2, prev3, prev4, prevJump;
        private float aimPitch;
        private CharacterBody pilot;
        private Vector3 mark; private float markYaw; private Vector3 facing;
        private readonly List<CharacterBody> dummies = new List<CharacterBody>();
        private Camera shotCamera; private RenderTexture shotTarget;
        private float scriptTime;
        private string segment = "-";

        /// <summary>True in a scripted dev run (keeps the event log on for the harness).</summary>
        internal static bool Active;
        private static bool gameLoaded;

        internal static void TryStart()
        {
            string dir = Environment.GetEnvironmentVariable("HS_AUTOPILOT");
            if (string.IsNullOrEmpty(dir)) return;
            Active = true;
            // v0.9.12: never wait on the opening cutscene (Stu was clicking through it by hand). Only
            // after the game has finished loading: skipping during InitializeGameRoutine left the
            // loadout UI and the body's renderers half-initialised (invisible Saint).
            RoR2Application.onLoad += () => gameLoaded = true;
            var go = new GameObject("HollowSaintDevAutopilot");
            DontDestroyOnLoad(go);
            var pilot = go.AddComponent<DevAutopilot>();
            pilot.output = dir;
            IODirectory.CreateDirectory(dir);
            Plugin.Log.LogWarning("HOLLOW_SAINT_AUTOPILOT active, output=" + dir);
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;
            On.RoR2.PlayerCharacterMasterController.FixedUpdate += AfterPlayerInput;
            On.RoR2.PlayerCharacterMasterController.Update += AfterPlayerUpdate;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
            On.RoR2.PlayerCharacterMasterController.FixedUpdate -= AfterPlayerInput;
            On.RoR2.PlayerCharacterMasterController.Update -= AfterPlayerUpdate;
        }

        // The vanilla Update aims the body along the camera every frame; keep the scripted aim instead,
        // so the camera can turn independently (front-facing showcase shots).
        private void AfterPlayerUpdate(On.RoR2.PlayerCharacterMasterController.orig_Update orig, PlayerCharacterMasterController self)
        {
            orig(self);
            if (!scripting || !self.master) return;
            var body = self.master.GetBody();
            if (body && body.inputBank) body.inputBank.aimDirection = ScriptedAim(body.inputBank);
        }

        private Vector3 ScriptedAim(InputBankTest bank)
        {
            if (aimTarget.HasValue) return (aimTarget.Value - bank.aimOrigin).normalized;
            return Quaternion.AngleAxis(aimPitch, Vector3.Cross(Vector3.up, facing)) * facing;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (!scripting) return; // menu/loading noise is vanilla; count only errors during the script
            errors++; trace.AppendLine("ERROR [" + segment + "] " + message);
        }

        private void AfterPlayerInput(On.RoR2.PlayerCharacterMasterController.orig_FixedUpdate orig, PlayerCharacterMasterController self)
        {
            // v0.9.9: the body claims a press (mustKeyPress skills) on its own bank; the vanilla
            // PushState in orig clears that claim because the real pad is idle. Carry it across so a
            // scripted hold behaves like a held button, not a fresh press every tick.
            var preBody = scripting && self.master ? self.master.GetBody() : null;
            var pre = preBody ? preBody.inputBank : null;
            bool c1 = pre && pre.skill1.hasPressBeenClaimed, c2 = pre && pre.skill2.hasPressBeenClaimed;
            bool c3 = pre && pre.skill3.hasPressBeenClaimed, c4 = pre && pre.skill4.hasPressBeenClaimed;
            orig(self);
            if (!scripting || !self.master) return;
            var body = self.master.GetBody();
            var bank = body ? body.inputBank : null;
            if (!bank) return;
            Set(ref bank.skill1, fire1, ref prev1, c1); Set(ref bank.skill2, fire2, ref prev2, c2);
            Set(ref bank.skill3, fire3, ref prev3, c3); Set(ref bank.skill4, fire4, ref prev4, c4);
            Set(ref bank.jump, jump, ref prevJump);
            bank.sprint.down = sprint;
            bank.moveVector = move;
            bank.aimDirection = ScriptedAim(bank);
        }

        private static void Set(ref InputBankTest.ButtonState button, bool down, ref bool previous, bool claimed = false)
        {
            button.hasPressBeenClaimed = down && previous && claimed;
            button.wasDown = previous; button.down = down; previous = down;
        }

        // ------------------------------------------------------------ boot into the arena
        private IEnumerator Start()
        {
            // The intro_skip convar is archived to the player's settings, so the runtime-only flag set
            // in TryStart is used instead; setting it mid-intro also skips (onShouldSkipEnabled).
            while (SceneManager.GetActiveScene().name != "title" || LocalUserManager.GetFirstLocalUser() == null)
            {
                if (gameLoaded && SceneManager.GetActiveScene().name != "title" && SceneManager.GetActiveScene().name != "splash" && !IntroCutsceneController.shouldSkip)
                    IntroCutsceneController.shouldSkip = true;
                yield return null;
            }
            yield return Real(6f);
            RoR2.Console.instance.SubmitCmd((NetworkUser)null, "transition_command \"gamemode ClassicRun; host 0;\"", false);
            float wait = 0f;
            while (!PreGameController.instance && wait < 60f) { wait += Time.unscaledDeltaTime; yield return null; }
            if (!PreGameController.instance) { Finish("no lobby"); yield break; }
            yield return Real(2f);
            // v0.9.12: with the intro skipped the lobby can open before the local user's network user
            // exists; wait for it instead of failing with "no survivor".
            var user = LocalUserManager.GetFirstLocalUser();
            wait = 0f;
            while ((user == null || !user.currentNetworkUser) && wait < 20f) { wait += Time.unscaledDeltaTime; yield return null; user = LocalUserManager.GetFirstLocalUser(); }
            var survivor = SurvivorCatalog.GetSurvivorDef(SurvivorCatalog.GetSurvivorIndexFromBodyIndex(BodyCatalog.FindBodyIndex("HollowSaintBody")));
            if (user == null || !user.currentNetworkUser || !survivor) { Finish("no survivor"); yield break; }
            user.currentNetworkUser.SetSurvivorPreferenceClient(survivor);
            yield return Real(2f);
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "lobby")
            {
                yield return LobbySegments();
                Finish("complete");
                yield break;
            }
            // The lobby can already have launched during the preference-settle wait.
            // Re-read it after the yield instead of dereferencing a destroyed instance.
            if (PreGameController.instance) PreGameController.instance.StartLaunch();
            wait = 0f;
            while ((user.cachedBody == null || !Run.instance) && wait < 90f) { wait += Time.unscaledDeltaTime; yield return null; }
            if (!user.cachedBody) { Finish("no body on stage 1"); yield break; }
            yield return Real(2f);
            // Same arena every run: Titanic Plains by default (neutral daylight, open ground).
            string arena = Environment.GetEnvironmentVariable("HS_ARENA");
            if (string.IsNullOrEmpty(arena)) arena = "golemplains";
            if (SceneManager.GetActiveScene().name != arena)
            {
                var scene = SceneCatalog.FindSceneDef(arena);
                if (!scene) { Finish("no scene " + arena); yield break; }
                Run.instance.AdvanceStage(scene);
                wait = 0f;
                while ((SceneManager.GetActiveScene().name != arena || user.cachedBody == null) && wait < 90f) { wait += Time.unscaledDeltaTime; yield return null; }
                if (!user.cachedBody || SceneManager.GetActiveScene().name != arena) { Finish("did not reach " + arena); yield break; }
            }
            foreach (var director in CombatDirector.instancesList.ToArray()) if (director) director.enabled = false;
            yield return Real(3f);
            pilot = user.cachedBody;
            if (!SetUpArena()) { Finish("arena setup failed"); yield break; }
            yield return Real(2f);
            StartCoroutine(Monitor());
            yield return Script();
            Finish("complete");
        }

        private static IEnumerator Real(float seconds)
        {
            float t = 0f; while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private bool SetUpArena()
        {
            foreach (var director in CombatDirector.instancesList.ToArray()) if (director) director.enabled = false;
            foreach (var member in TeamComponent.GetTeamMembers(TeamIndex.Monster).ToArray())
                if (member && member.body && member.body.healthComponent) member.body.healthComponent.Suicide();
            if (pilot.healthComponent) pilot.healthComponent.godMode = true;
            mark = pilot.footPosition;
            // v0.9.1 repeatable arena: the run's spawn point is random, and polish01 landed on a broken
            // drone that ate the throws. Titanic Plains uses a fixed open mark (javelin05's), and any
            // interactable within 8 m of the mark is removed (dev runs only).
            string fixedMark = Environment.GetEnvironmentVariable("HS_ARENA_MARK");
            if (string.IsNullOrEmpty(fixedMark) && SceneManager.GetActiveScene().name == "golemplains") fixedMark = "-112.88,-139.02,-374.76";
            if (!string.IsNullOrEmpty(fixedMark))
            {
                var parts = fixedMark.Split(',');
                float x, y, z;
                if (parts.Length == 3 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)
                    && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)
                    && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z))
                {
                    mark = new Vector3(x, y, z);
                    TeleportHelper.TeleportBody(pilot, mark);
                }
            }
            int cleared = 0;
            foreach (var purchase in FindObjectsOfType<PurchaseInteraction>())
                if (purchase && (purchase.transform.position - mark).sqrMagnitude < 64f) { Destroy(purchase.gameObject); cleared++; }
            trace.AppendLine("ARENA fixedMark=" + (string.IsNullOrEmpty(fixedMark) ? "none" : fixedMark) + " clearedInteractables=" + cleared);
            // Face the most open direction: 24 rays at chest height, longest clear run wins.
            float best = -1f;
            for (int i = 0; i < 24; i++)
            {
                float yaw = i * 15f;
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float clear = 40f;
                RaycastHit hit;
                if (Physics.Raycast(mark + Vector3.up * 1.2f, dir, out hit, 40f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) clear = hit.distance;
                if (clear > best + 0.5f) { best = clear; markYaw = yaw; }
            }
            facing = Quaternion.Euler(0f, markYaw, 0f) * Vector3.forward;
            trace.AppendLine("ARENA mark=" + mark.ToString("F2") + " yaw=" + markYaw + " clear=" + best.ToString("0.0") + "m");
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            SpawnDummy("LemurianMaster", mark + facing * 9f);
            SpawnDummy("BeetleMaster", mark + facing * 7f + right * 3.5f);
            SpawnDummy("BeetleMaster", mark + facing * 7f - right * 3.5f);
            MakeShotCamera();
            return shotCamera != null;
        }

        private void SpawnDummy(string masterName, Vector3 near)
        {
            var prefab = MasterCatalog.FindMasterPrefab(masterName);
            if (!prefab) { trace.AppendLine("ARENA missing " + masterName); return; }
            Vector3 at = near;
            RaycastHit hit;
            if (Physics.Raycast(near + Vector3.up * 6f, Vector3.down, out hit, 20f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) at = hit.point;
            var master = new MasterSummon
            {
                masterPrefab = prefab, position = at, rotation = Quaternion.LookRotation(-facing),
                summonerBodyObject = null, teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true
            }.Perform();
            if (!master) { trace.AppendLine("ARENA failed " + masterName); return; }
            foreach (var ai in master.GetComponents<BaseAI>()) ai.enabled = false;
            StartCoroutine(HoldDummy(master, at));
        }

        private IEnumerator HoldDummy(CharacterMaster master, Vector3 spot)
        {
            CharacterBody body = null; float t = 0f;
            while (!body && t < 5f) { body = master ? master.GetBody() : null; t += Time.deltaTime; yield return null; }
            if (!body) yield break;
            if (body.healthComponent) body.healthComponent.godMode = true;
            dummies.Add(body);
            // Pin the dummy to its spot: no AI input, and any knockback or drift is undone.
            TeleportHelper.TeleportBody(body, spot);
            while (body && scriptingOrSetup)
            {
                if (body.inputBank) body.inputBank.moveVector = Vector3.zero;
                if ((body.footPosition - spot).sqrMagnitude > 0.25f) TeleportHelper.TeleportBody(body, spot);
                yield return new WaitForSeconds(0.2f);
            }
        }
        private bool scriptingOrSetup = true;

        // ------------------------------------------------------------ fixed shot camera
        private void MakeShotCamera()
        {
            var rigs = CameraRigController.readOnlyInstancesList;
            Camera source = rigs.Count > 0 ? rigs[0].sceneCam : Camera.main;
            if (!source) { trace.AppendLine("ARENA no scene camera"); return; }
            var clone = Instantiate(source.gameObject);
            clone.name = "HollowSaintShotCamera";
            foreach (var behaviour in clone.GetComponentsInChildren<Behaviour>(true))
            {
                string type = behaviour.GetType().Name;
                bool keep = behaviour is Camera || type.Contains("PostProcess");
                if (!keep) behaviour.enabled = false;
            }
            foreach (var cam in clone.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
            shotCamera = clone.GetComponent<Camera>();
            shotCamera.fieldOfView = 50f;
            shotTarget = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            DontDestroyOnLoad(clone);
        }

        private void Shot(string name) { lastShotReal = Time.realtimeSinceStartup; StartCoroutine(ShotRoutine(name)); }

        private IEnumerator ShotRoutine(string name)
        {
            float at = scriptTime;
            yield return new WaitForEndOfFrame(); // every pose pass and line tick has run
            trace.AppendLine(at.ToString("000.00") + " SHOT " + name + SpearMetrics());
            if (!shotCamera || !pilot) yield break;
            Vector3 chest = pilot.corePosition;
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            Render(name + "_back", chest - facing * 4.0f + Vector3.up * 0.8f, chest + Vector3.up * 0.2f);
            Render(name + "_front", chest + facing * 3.4f + right * 1.6f + Vector3.up * 0.5f, chest);
            Render(name + "_side", chest + right * 3.8f + Vector3.up * 0.4f, chest);
            lastShotReal = Time.realtimeSinceStartup;
        }

        /// <summary>v0.9.1: measured javelin pose while the hand spear is shown. yaw/pitch = shaft vs aim in
        /// degrees (yaw + = tip right of aim, pitch + = nose up); clear = closest distance from the eye to
        /// the shaft (butt to tip) in cm, so head clipping is a number, not a guess.</summary>
        private string SpearMetrics()
        {
            var carry = pilot ? pilot.GetComponent<FoundationKit.SpearDischarge.SpearCarry>() : null;
            if (!carry || !carry.HandVisible || !carry.Tip || !pilot.inputBank) return "";
            Vector3 aim = pilot.inputBank.aimDirection.normalized, shaft = carry.ShaftDirection;
            float yaw = Vector3.SignedAngle(Vector3.ProjectOnPlane(aim, Vector3.up), Vector3.ProjectOnPlane(shaft, Vector3.up), Vector3.up);
            float pitch = Mathf.Asin(Mathf.Clamp(shaft.y, -1f, 1f)) * Mathf.Rad2Deg - Mathf.Asin(Mathf.Clamp(aim.y, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 butt = carry.Tail ? carry.Tail.position : carry.GripPosition, tip = carry.Tip.position;
            Vector3 eye = KitUtil.EyePosition(pilot);
            Vector3 seg = tip - butt;
            float t = Mathf.Clamp01(Vector3.Dot(eye - butt, seg) / Mathf.Max(1e-4f, seg.sqrMagnitude));
            float clear = Vector3.Distance(eye, butt + seg * t);
            return " javelin yaw=" + yaw.ToString("0.0") + " pitch=" + pitch.ToString("0.0") + " clear=" + (clear * 100f).ToString("0") + "cm";
        }

        private void Render(string file, Vector3 from, Vector3 at)
        {
            shotCamera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from, Vector3.up));
            shotCamera.targetTexture = shotTarget;
            shotCamera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = shotTarget;
            var image = new Texture2D(shotTarget.width, shotTarget.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, shotTarget.width, shotTarget.height), 0, 0);
            image.Apply(false);
            RenderTexture.active = previous;
            shotCamera.targetTexture = null;
            IOFile.WriteAllBytes(IOPath.Combine(output, file + ".png"), image.EncodeToPNG());
            Destroy(image);
        }

        // ------------------------------------------------------------ helpers
        private void ResetToMark()
        {
            fire1 = fire2 = fire3 = fire4 = jump = sprint = false;
            move = Vector3.zero; aimTarget = null; aimPitch = 0f;
            if (!pilot) return;
            TeleportHelper.TeleportBody(pilot, mark);
            if (pilot.characterMotor) pilot.characterMotor.velocity = Vector3.zero;
            if (pilot.characterDirection) { pilot.characterDirection.yaw = markYaw; pilot.characterDirection.forward = facing; }
        }

        private Vector3 Right => Vector3.Cross(Vector3.up, facing);
        private Vector3 DummyChest(int index) => index < dummies.Count && dummies[index] ? dummies[index].corePosition : mark + facing * 8f + Vector3.up;

        private IEnumerator Segment(string name)
        {
            segment = name;
            if (pilot && pilot.skillLocator) pilot.skillLocator.ResetSkills();
            trace.AppendLine(scriptTime.ToString("000.00") + " SEGMENT " + name);
            ResetToMark();
            yield return Wait(0.6f);
        }

        private IEnumerator Press(int skill)
        {
            switch (skill) { case 1: fire1 = true; break; case 2: fire2 = true; break; case 3: fire3 = true; break; case 4: fire4 = true; break; default: jump = true; break; }
            yield return Wait(0.1f);
            switch (skill) { case 1: fire1 = false; break; case 2: fire2 = false; break; case 3: fire3 = false; break; case 4: fire4 = false; break; default: jump = false; break; }
        }

        private bool CrownOpen() { var d = pilot ? pilot.GetComponent<OpenCircuitPulseDriver>() : null; return d && d.CrownOpen; }

        private IEnumerator WaitCrownEnd()
        {
            float guard = 0f;
            while (CrownOpen() && guard < 14f) { guard += 0.1f; yield return Wait(0.1f); }
        }

        private IEnumerator Wait(float seconds)
        {
            float end = scriptTime + seconds;
            while (scriptTime < end)
            {
                yield return null;
                float before = scriptTime;
                scriptTime += Time.deltaTime;
                if (Mathf.Floor(before * 4f) != Mathf.Floor(scriptTime * 4f)) Sample();
            }
        }

        // ------------------------------------------------------------ the script
        private IEnumerator Script()
        {
            scripting = true;
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "gaze-release")
            {
                yield return GazeReleaseSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "javelin")
            {
                yield return JavelinSegments();
                yield return AimAssistSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "crown")
            {
                // v0.9.12 quick check: the crown spear model while charging, from all three cameras.
                var slot = pilot && pilot.skillLocator ? pilot.skillLocator.special : null;
                if (slot && KitRegistration.OpenCircuitDef) slot.SetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
                yield return Segment("crown-spear");
                yield return Press(4); yield return Wait(1.4f);
                aimTarget = DummyChest(0); fire1 = true;
                fire2 = true; yield return Wait(0.25f); Shot("c-crown-25");
                yield return Wait(0.35f); Shot("c-crown-full"); WideShot("c-crown-full-wide");
                aimPitch = -25f; aimTarget = null; yield return Wait(0.3f); Shot("c-crown-up");
                aimTarget = DummyChest(0);
                fire2 = false; yield return Wait(0.06f); Shot("c-crown-throw"); fire1 = false;
                yield return Wait(1.0f);
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "heads")
            {
                yield return HeadSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "effects")
            {
                yield return EffectSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "items")
            {
                yield return ItemSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "storm")
            {
                yield return StormSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "polish")
            {
                yield return PolishSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "thunder")
            {
                yield return ThunderSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "gaze")
            {
                yield return GazeSegments();
                scripting = false;
                yield break;
            }
            if (Environment.GetEnvironmentVariable("HS_SEGMENTS") == "showcase")
            {
                yield return ShowcaseSegments();
                scripting = false;
                yield break;
            }
            // v0.9.8: Gaze is the default Special; the crown segments below need Open Circuit.
            var crownSlot = pilot && pilot.skillLocator ? pilot.skillLocator.special : null;
            if (crownSlot && KitRegistration.OpenCircuitDef) { crownSlot.SetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement); crownOverride = true; }
            yield return Segment("idle");
            Shot("a-idle");
            yield return Wait(0.5f);

            yield return Segment("crown");
            yield return Press(4);
            yield return Wait(0.5f); Shot("b-unfold");
            yield return Wait(1.0f); Shot("b-hold");
            move = -Right; yield return Wait(0.8f); move = Right; yield return Wait(0.8f); move = Vector3.zero;
            yield return WaitCrownEnd();
            yield return Wait(0.25f); Shot("b-close-1");
            yield return Wait(0.3f); Shot("b-close-2");
            yield return Wait(0.8f); Shot("b-closed");

            yield return Segment("bolt");
            aimTarget = DummyChest(0);
            fire1 = true;
            yield return Wait(0.3f); Shot("c-bolt-start");
            yield return Wait(1.4f); Shot("c-bolt-hold");
            fire1 = false;
            yield return Wait(0.3f); Shot("c-bolt-release");

            // v0.9 Stormspear: hold to charge (off-hand Arc Bolt keeps firing), release to throw.
            yield return Segment("spear");
            aimTarget = DummyChest(0);
            fire2 = true; yield return Wait(0.32f); Shot("d-draw");
            fire1 = true; yield return Wait(0.38f); Shot("d-charge-33");
            yield return Wait(0.65f); Shot("d-charge-66");
            yield return Wait(0.8f); Shot("d-charge-full");
            fire2 = false; yield return Wait(0.04f); Shot("d-throw-05"); yield return Wait(0.06f); Shot("d-throw"); yield return Wait(0.06f); Shot("d-throw-12"); fire1 = false;
            yield return Wait(0.4f); Shot("d-burst");
            yield return Wait(5.4f);
            aimTarget = DummyChest(1);
            yield return Press(2); yield return Wait(0.3f); Shot("d-tap");
            yield return Wait(0.8f); Shot("d-after");

            // v0.9.1: the throw at 1/10 speed so the whip, follow-through, release flash, wake and
            // impact are captured (at full speed a three-camera shot hitch swallows the whole throw).
            yield return Segment("spear-slow");
            aimTarget = DummyChest(0);
            fire2 = true; yield return Wait(2.2f); Shot("s-full");
            Time.timeScale = 0.1f;
            fire2 = false;
            yield return Wait(0.02f); Shot("s-throw-02");
            yield return Wait(0.04f); Shot("s-throw-06");
            yield return Wait(0.04f); Shot("s-throw-10");
            yield return Wait(0.06f); Shot("s-throw-16");
            yield return Wait(0.08f); Shot("s-throw-24");
            yield return Wait(0.12f); Shot("s-throw-36");
            Time.timeScale = 1f;
            yield return Wait(5.5f);
            aimTarget = DummyChest(2);
            Time.timeScale = 0.1f;
            fire2 = true; yield return Wait(0.05f); fire2 = false;
            yield return Wait(0.03f); Shot("s-tap-03");
            yield return Wait(0.06f); Shot("s-tap-09");
            yield return Wait(0.1f); Shot("s-tap-19");
            Time.timeScale = 1f;
            yield return Wait(0.6f);

            yield return Segment("move");
            move = -Right; yield return Wait(0.6f); Shot("e-strafe"); yield return Wait(0.4f);
            move = Right; yield return Press(3); yield return Wait(0.1f); Shot("e-dash"); yield return Wait(0.6f); move = Vector3.zero;
            yield return Segment("jump");
            yield return Press(0); yield return Wait(0.3f); Shot("e-jump"); yield return Wait(1.0f); Shot("e-land");
            yield return Segment("glide");
            sprint = true; move = facing; yield return Wait(1.2f); Shot("e-glide"); sprint = false; move = Vector3.zero;
            yield return Wait(0.8f); Shot("e-glide-stop");

            yield return Segment("crown-combo");
            yield return Press(4); yield return Wait(1.4f);
            aimTarget = DummyChest(0); fire1 = true; yield return Wait(0.6f); Shot("f-crown-bolts");
            fire2 = true; yield return Wait(0.4f); Shot("f-crown-charge"); yield return Wait(0.5f); Shot("f-crown-full");
            fire2 = false; yield return Wait(0.05f); Shot("f-crown-throw-05"); yield return Wait(0.07f); Shot("f-crown-throw"); yield return Wait(0.45f); Shot("f-crown-thunder"); fire1 = false;
            yield return Wait(0.6f);
            move = facing; yield return Press(3); yield return Wait(0.5f); move = Vector3.zero;
            yield return WaitCrownEnd(); yield return Wait(1.3f); Shot("f-after");

            // v0.9.1: the themed shock overlay, forced on the nearest dummies (Electrocutes are rare in
            // a short script, so this checks the look directly).
            yield return Segment("shock");
            var shockedBuff = FoundationKit.Storm.StormServer.ShockedBuff;
            for (int i = 0; i < dummies.Count; i++) if (dummies[i] && shockedBuff) dummies[i].AddTimedBuff(shockedBuff, 2.5f);
            yield return Wait(0.5f); Shot("i-shocked");
            yield return Wait(2.5f); Shot("i-unshocked");

            yield return Segment("pause");
            aimTarget = DummyChest(0); fire1 = true; yield return Wait(0.9f);
            Time.timeScale = 0f; yield return Real(0.5f); Shot("g-paused"); yield return Real(0.5f); Time.timeScale = 1f;
            fire1 = false; yield return Wait(0.5f);

            var skins = pilot.modelLocator && pilot.modelLocator.modelTransform ? pilot.modelLocator.modelTransform.GetComponent<ModelSkinController>() : null;
            for (int skin = 0; skin < 5 && skins; skin++)
            {
                yield return Segment("skin" + skin);
                pilot.skinIndex = (uint)skin;
                skins.ApplySkin(skin);
                yield return Wait(0.6f); Shot("h-skin" + skin + "-idle");
                yield return Press(4); yield return Wait(1.6f);
                aimTarget = DummyChest(0); fire1 = true; yield return Wait(0.8f); Shot("h-skin" + skin + "-crown-bolts"); fire1 = false;
                yield return WaitCrownEnd(); yield return Wait(1.2f);
            }
            yield return GazeSegments();
            scripting = false;
        }

        // Gaze of the Hollow (alternate special), equipped through a skill override for the test.
        private bool crownOverride;
        private IEnumerator GazeSegments()
        {
            var special = pilot && pilot.skillLocator ? pilot.skillLocator.special : null;
            var gaze = FoundationKit.Gaze.GazeRegistration.SkillDef;
            if (!special || !gaze) { trace.AppendLine("GAZE not available"); yield break; }
            if (crownOverride && KitRegistration.OpenCircuitDef) { special.UnsetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement); crownOverride = false; }
            special.SetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);

            yield return Segment("gaze");
            aimTarget = DummyChest(0);
            yield return Press(4);
            yield return Wait(0.25f); Shot("i-launch");
            yield return Wait(0.5f); Shot("i-windup"); WideShot("i-windup-wide");
            yield return Wait(0.35f); Shot("i-ignite");
            fire1 = true; fire2 = true; // must stay blocked while the beam runs
            yield return Wait(0.8f); Shot("i-beam"); WideShot("i-beam-wide");
            aimTarget = DummyChest(1); yield return Wait(1.0f); Shot("i-sweep"); WideShot("i-sweep-wide");
            aimTarget = DummyChest(2); yield return Wait(1.2f); WideShot("i-sweep2-wide");
            fire1 = false; fire2 = false;
            yield return Wait(1.3f); Shot("i-end");
            yield return Wait(1.6f); Shot("i-landed");

            yield return Segment("gaze-recast");
            aimTarget = DummyChest(0);
            yield return Press(4); yield return Wait(2.0f);
            yield return Press(4); yield return Wait(0.2f); Shot("j-recast");
            yield return Wait(1.5f);

            yield return Segment("gaze-step");
            aimTarget = DummyChest(0);
            yield return Press(4); yield return Wait(2.0f);
            move = Right; yield return Press(3); yield return Wait(0.2f); Shot("k-step"); move = Vector3.zero;
            yield return Wait(1.5f);

            var skins = pilot.modelLocator && pilot.modelLocator.modelTransform ? pilot.modelLocator.modelTransform.GetComponent<ModelSkinController>() : null;
            if (skins)
            {
                yield return Segment("gaze-solar");
                pilot.skinIndex = 3u; skins.ApplySkin(3);
                aimTarget = DummyChest(0);
                yield return Press(4); yield return Wait(2.0f); Shot("l-solar"); WideShot("l-solar-wide");
                yield return Wait(3.5f);
                pilot.skinIndex = 0u; skins.ApplySkin(0);
            }
            special.UnsetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);
        }

        private void WideShot(string name) { lastShotReal = Time.realtimeSinceStartup; StartCoroutine(WideShotRoutine(name)); }

        private IEnumerator WideShotRoutine(string name)
        {
            trace.AppendLine(scriptTime.ToString("000.00") + " SHOT " + name);
            yield return new WaitForEndOfFrame();
            if (!shotCamera || !pilot) yield break;
            Vector3 focus = mark + facing * 5f + Vector3.up * 2.5f;
            Render(name, mark - facing * 7f + Right * 9f + Vector3.up * 6f, focus);
        }

        // ------------------------------------------------------------ measurement
        private IEnumerator Monitor()
        {
            var model = pilot.modelLocator ? pilot.modelLocator.modelTransform : null;
            Transform chest = null, handL = null, handR = null;
            if (model) foreach (var t in model.GetComponentsInChildren<Transform>(true))
            { if (t.name == "chest") chest = t; else if (t.name == "L hand") handL = t; else if (t.name == "R hand") handR = t; }
            if (!chest || !handL || !handR) { trace.AppendLine("MONITOR missing bones"); yield break; }
            Vector3 lastL = Vector3.zero, lastR = Vector3.zero, stepL = Vector3.zero, stepR = Vector3.zero; bool primed = false;
            while (pilot)
            {
                yield return new WaitForEndOfFrame();
                if (!chest || Time.deltaTime <= 0f) continue;
                // v0.9.9: a screenshot stalls the frame, so the next frame's motion is a capture artifact.
                if (Time.realtimeSinceStartup - lastShotReal < 0.35f) { primed = false; continue; }
                Vector3 l = chest.InverseTransformPoint(handL.position), r = chest.InverseTransformPoint(handR.position);
                if (primed)
                {
                    Vector3 dl = l - lastL, dr = r - lastR;
                    // A pop is a one-frame jump much larger than the motion either side of it.
                    if (dl.magnitude > 0.1f && stepL.magnitude < 0.03f) { pops++; trace.AppendLine(scriptTime.ToString("000.00") + " POP [" + segment + "] L hand " + (dl.magnitude * 100f).ToString("0.0") + " cm"); }
                    if (dr.magnitude > 0.1f && stepR.magnitude < 0.03f) { pops++; trace.AppendLine(scriptTime.ToString("000.00") + " POP [" + segment + "] R hand " + (dr.magnitude * 100f).ToString("0.0") + " cm"); }
                    stepL = dl; stepR = dr;
                }
                lastL = l; lastR = r; primed = true;
            }
        }

        private void Sample()
        {
            if (!pilot) return;
            var ring = FoundationKit.Vfx.HaloRing.For(pilot);
            var animator = KitAnim.AnimatorOf(pilot);
            string halo = "?";
            if (animator)
            {
                int layer = animator.GetLayerIndex("Halo");
                if (layer >= 0)
                {
                    var info = animator.GetCurrentAnimatorStateInfo(layer);
                    foreach (var name in new[] { "Halo rest", "Open Circuit", "Open Circuit hold", "Open Circuit end" }) if (info.IsName(name)) halo = name;
                    if (animator.IsInTransition(layer)) halo += "*";
                }
            }
            var crownMachine = EntityStateMachine.FindByCustomName(pilot.gameObject, KitRegistration.CrownMachineName);
            var weaponMachine = EntityStateMachine.FindByCustomName(pilot.gameObject, "Weapon");
            if (segment.StartsWith("gaze", StringComparison.Ordinal) || segment.StartsWith("items", StringComparison.Ordinal))
                trace.AppendLine(scriptTime.ToString("000.00") + " [" + segment + "] crownState=" +
                    (crownMachine && crownMachine.state != null ? crownMachine.state.GetType().Name : "-") +
                    " weapon=" + (weaponMachine && weaponMachine.state != null ? weaponMachine.state.GetType().Name : "-") +
                    " height=" + (pilot.footPosition.y - mark.y).ToString("0.0") +
                    " fallFlag=" + ((pilot.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0 ? 1 : 0) +
                    " specialStock=" + (pilot.skillLocator && pilot.skillLocator.special ? pilot.skillLocator.special.stock : -1));
            var sc = StormspearCharge.Of(pilot);
            string spear = sc && sc.Charging ? "charging:" + sc.Form + ":" + sc.Charge01.ToString("0.00") : "idle";
            trace.AppendLine(scriptTime.ToString("000.00") + " [" + segment + "] crown=" + (CrownOpen() ? 1 : 0) +
                " open=" + (ring && ring.Valid ? ring.Openness.ToString("0.00") : "-") + " halo=" + halo + " spear=" + spear +
                " fromMark=" + (pilot.footPosition - mark).magnitude.ToString("0.0"));
        }

        private void Finish(string outcome)
        {
            scripting = false;
            Time.timeScale = 1f;
            IOFile.WriteAllText(IOPath.Combine(output, "trace.txt"), "outcome=" + outcome + " errors=" + errors + " pops=" + pops + " version=" + Plugin.Version + "\n" + trace);
            Plugin.Log.LogWarning("HOLLOW_SAINT_AUTOPILOT finished: " + outcome);
            StartCoroutine(Quit());
        }

        private IEnumerator Quit()
        {
            yield return Real(2f);
            Application.Quit();
        }
    }
}
