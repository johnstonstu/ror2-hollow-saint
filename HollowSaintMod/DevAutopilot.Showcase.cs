using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HollowSaint.FoundationKit;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.17 (HS_SEGMENTS=showcase): README footage. The kit against killable standing packs (the
    /// Saint invulnerable) through the normal player camera, which is turned to follow the scripted aim.
    /// The build tag is hidden. Each clip writes "CLIP name start|end utc=..." to the trace so
    /// tools\release\Record-Showcase.ps1 can cut the screen recording.
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private readonly List<CharacterBody> live = new List<CharacterBody>();
        private Vector3 cameraLook;
        private float syncFlashUntil;
        /// <summary>When set, the camera looks this way instead of following the aim (front-facing shots).</summary>
        private Vector3? cameraWant;
        private CameraTargetParams.CameraParamsOverrideHandle cameraHandle;
        private bool cameraHandleSet;

        private void OnGUI()
        {
            if (Time.realtimeSinceStartup >= syncFlashUntil) return;
            GUI.depth = -1000;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        }

        private IEnumerator ShowcaseSegments()
        {
            Plugin.HideBuildTag = true;
            RoR2.UI.HUD.cvHudEnable.SetBool(false);
            Application.quitting += () => RoR2.UI.HUD.cvHudEnable.SetBool(true);
            foreach (var d in dummies) if (d && d.healthComponent) { d.healthComponent.godMode = false; d.healthComponent.Suicide(); }
            dummies.Clear();
            cameraLook = facing;
            StartCoroutine(CameraFollow());
            yield return Wait(1.5f);
            // Sync mark for the recording: a full-screen white flash at a logged time.
            syncFlashUntil = Time.realtimeSinceStartup + 0.25f;
            trace.AppendLine(scriptTime.ToString("000.00") + " SYNC utc=" + DateTime.UtcNow.ToString("o"));
            yield return Wait(1.0f);

            if (Environment.GetEnvironmentVariable("HS_SHOWCASE_QUICK") == "gaze")
            {
                // HS_SHOWCASE_SKIN=4 records the Gaze takes on Umbral (purple lightning).
                int showSkin;
                var skinController = pilot.modelLocator && pilot.modelLocator.modelTransform ? pilot.modelLocator.modelTransform.GetComponent<ModelSkinController>() : null;
                if (skinController && int.TryParse(Environment.GetEnvironmentVariable("HS_SHOWCASE_SKIN"), out showSkin) &&
                    showSkin >= 0 && showSkin < skinController.skins.Length)
                {
                    pilot.skinIndex = (uint)showSkin; skinController.ApplySkin(showSkin);
                    trace.AppendLine(scriptTime.ToString("000.00") + " SHOWCASE_SKIN " + showSkin + " " + skinController.skins[showSkin].name);
                    yield return Wait(0.5f);
                }
                yield return GazeTake("gaze-d", null, 7f, 1.4f);
                yield return GazeTake("gaze-e", null, 9f, 2.4f);
                yield return GazeTake("gaze-f", null, 5.5f, 0.8f);
                yield return FinishShowcase();
                yield break;
            }

            // Skins first, while the ground is still clear of corpses.
            yield return SkinLineup();

            // Hero: from the front, the crown opens over the Saint and strikes a ring of enemies.
            if (pilot.skillLocator && pilot.skillLocator.special && KitRegistration.OpenCircuitDef)
            {
                yield return Segment("show-hero");
                var heroSpecial = pilot.skillLocator.special;
                heroSpecial.SetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
                ClearLive(); SpawnRing("LemurianMaster", 7, 6.5f);
                SetCameraDistance(7f, -0.9f);
                aimTarget = null; aimPitch = 0f;
                cameraWant = FrontLook(25f);
                cameraLook = cameraWant.Value;
                yield return Wait(1.6f);
                Clip("hero", true);
                yield return Press(4);
                float heroStart = scriptTime;
                while (scriptTime - heroStart < 5.5f)
                {
                    cameraWant = FrontLook(25f - 50f * (scriptTime - heroStart) / 5.5f);
                    yield return Wait(0.05f);
                }
                Clip("hero", false);
                yield return WaitCrownEnd();
                heroSpecial.UnsetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
                heroSpecial.Reset();
                cameraWant = null;
            }
            SetCameraDistance(8.5f, 0.5f);
            if (Environment.GetEnvironmentVariable("HS_SHOWCASE_QUICK") == "hero") { yield return FinishShowcase(); yield break; }

            yield return Segment("show-bolt");
            ClearLive(); SpawnLive("LemurianMaster", 5, 10f);
            yield return Wait(1.2f);
            Clip("arcbolt", true);
            fire1 = true;
            yield return AimAtLive(4.5f);
            fire1 = false;
            yield return Wait(0.6f);
            Clip("arcbolt", false);
            if (Environment.GetEnvironmentVariable("HS_SHOWCASE_QUICK") == "1") yield break;

            yield return Segment("show-spear");
            ClearLive(); SpawnLive("LemurianMaster", 5, 12f);
            yield return Wait(1.2f);
            Clip("stormspear", true);
            fire2 = true; fire1 = true;
            yield return AimAtLive(2.3f, centre: true);
            fire2 = false;
            yield return AimAtLive(0.4f, centre: true);
            fire1 = false;
            yield return Wait(1.6f);
            SpawnLive("BeetleMaster", 3, 10f);
            yield return Wait(0.8f);
            fire2 = true; yield return AimAtLive(0.12f); fire2 = false;
            yield return Wait(1.6f);
            Clip("stormspear", false);

            yield return Segment("show-step");
            Clip("arcstep", true);
            move = Right; yield return Press(3); yield return Wait(0.55f);
            move = -Right; yield return Press(3); yield return Wait(0.7f);
            pilot.skillLocator.utility.Reset();
            aimPitch = -40f; move = facing; yield return Press(3); yield return Wait(0.25f);
            yield return Press(0); yield return Wait(0.9f);
            aimPitch = 0f; move = Vector3.zero;
            yield return Wait(1.2f);
            Clip("arcstep", false);

            var special = pilot.skillLocator ? pilot.skillLocator.special : null;
            yield return GazeTake("gaze", null, 7f, 1.4f);
            SetCameraDistance(8.5f, 0.5f);
            cameraWant = null;

            if (special && KitRegistration.OpenCircuitDef)
            {
                yield return Segment("show-crown");
                special.SetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
                ClearLive(); SpawnLive("LemurianMaster", 6, 8f);
                yield return Wait(1.0f);
                Clip("opencircuit", true);
                yield return Press(4); yield return Wait(1.2f);
                fire1 = true; move = facing * 0.6f;
                yield return AimAtLive(1.2f);
                move = Vector3.zero;
                SpawnLive("LemurianMaster", 5, 11f);
                fire2 = true;
                yield return AimAtLive(1.0f, centre: true);
                fire2 = false;
                yield return AimAtLive(2.6f);
                fire1 = false;
                yield return WaitCrownEnd(); yield return Wait(0.8f);
                Clip("opencircuit", false);
                special.UnsetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
            }

            yield return Segment("show-storm");
            // Two orbs short of a Thunderbolt, so the clip shows the last Electrocutes lighting them.
            var meter = pilot.GetComponent<DischargeMeter>();
            if (meter) { meter.Consume(); for (int i = 0; i < KitTuning.StormChargeMax - 2; i++) meter.AddCharge(); }
            ClearLive(); SpawnLive("LemurianMaster", 6, 10f);
            yield return Wait(1.0f);
            long bolts = FoundationKit.Storm.StormTelemetry.Thunderbolts;
            Clip("storm", true);
            fire1 = true;
            float start = scriptTime, cycle = 0f, struck = -1f;
            while (scriptTime - start < 30f && (struck < 0f || scriptTime - struck < 3.5f))
            {
                if (live.All(b => !b || !b.healthComponent || !b.healthComponent.alive)) SpawnLive("LemurianMaster", 6, 10f);
                if (struck < 0f && FoundationKit.Storm.StormTelemetry.Thunderbolts > bolts) struck = scriptTime;
                cycle = (cycle + 0.1f) % 6f;
                fire2 = cycle < 2.2f;
                yield return AimAtLive(0.1f, centre: fire2);
            }
            fire1 = fire2 = false;
            trace.AppendLine("SHOWCASE_STORM thunderbolt=" + (struck >= 0f ? (struck - start).ToString("0.0") + "s" : "none"));
            yield return Wait(0.5f);
            Clip("storm", false);

            yield return FinishShowcase();
        }

        private IEnumerator FinishShowcase()
        {
            ClearLive();
            if (cameraHandleSet) { var ctp = pilot.GetComponent<CameraTargetParams>(); if (ctp) ctp.RemoveParamsOverride(cameraHandle, 0.3f); cameraHandleSet = false; }
            RoR2.UI.HUD.cvHudEnable.SetBool(true);
            yield return Wait(1f);
        }

        /// <summary>Close front view, each skin held still: a clip plus one "STILL skinN" mark per skin.</summary>
        private IEnumerator SkinLineup()
        {
            var skins = pilot.modelLocator && pilot.modelLocator.modelTransform ? pilot.modelLocator.modelTransform.GetComponent<ModelSkinController>() : null;
            if (skins)
            {
                yield return Segment("show-skins");
                ClearLive();
                aimTarget = null; aimPitch = 0f;
                SetCameraDistance(5.8f, -1.5f);
                cameraWant = FrontLook(18f, -0.02f);
                yield return Wait(1.5f);
                Clip("skins", true);
                for (int skin = 0; skin < 5; skin++)
                {
                    pilot.skinIndex = (uint)skin; skins.ApplySkin(skin);
                    yield return Wait(1.1f);
                    trace.AppendLine(scriptTime.ToString("000.00") + " STILL skin" + skin + " utc=" + DateTime.UtcNow.ToString("o"));
                    yield return Wait(0.6f);
                }
                Clip("skins", false);
                pilot.skinIndex = 0u; skins.ApplySkin(0);
                cameraWant = null;
            }
        }

        /// <summary>
        /// One Gaze of the Hollow shot on the default special. With no yaw the camera follows the crosshair like
        /// normal play; with a yaw it holds behind and above the Saint looking at the pack, turned by that many degrees.
        /// </summary>
        private IEnumerator GazeTake(string name, float? yaw, float back, float up)
        {
            var special = pilot.skillLocator ? pilot.skillLocator.special : null;
            if (!special) yield break;
            yield return Segment("show-" + name);
            ClearLive(); SpawnLive("LemurianMaster", 7, 10f);
            special.Reset();
            aimTarget = null; aimPitch = 0f;
            SetCameraDistance(back, up);
            yield return Wait(0.8f);
            Vector3 toPack = (LiveCentre() ?? pilot.corePosition + facing * 10f) - pilot.corePosition;
            if (yaw.HasValue)
            {
                cameraWant = (Quaternion.AngleAxis(yaw.Value, Vector3.up) * toPack.normalized + Vector3.down * 0.15f).normalized;
                cameraLook = cameraWant.Value;
            }
            else
            {
                cameraWant = null;
                aimTarget = LiveCentre();
            }
            yield return Wait(0.8f);
            trace.AppendLine(scriptTime.ToString("000.00") + " GAZE " + name + " skill=" + (special.skillDef ? special.skillDef.skillName : "none") +
                " stock=" + special.stock + " cooldown=" + special.cooldownRemaining.ToString("0.0"));
            Clip(name, true);
            aimTarget = LiveCentre();
            fire4 = true; yield return Wait(0.25f); fire4 = false;
            yield return AimAtLive(1.0f, centre: true);
            yield return SweepLive(3.4f);
            yield return Wait(1.6f);
            Clip(name, false);
            yield return Wait(1.0f);
        }

        /// <summary>A look direction from in front of the Saint back at it, turned by yaw degrees and tilted down.</summary>
        private Vector3 FrontLook(float yaw, float down = -0.12f)
        {
            Vector3 back = Quaternion.AngleAxis(yaw, Vector3.up) * -facing;
            return (back + Vector3.up * down).normalized;
        }

        /// <summary>Moves the player camera to the given distance behind / height above the pivot.</summary>
        private void SetCameraDistance(float back, float up)
        {
            var ctp = pilot ? pilot.GetComponent<CameraTargetParams>() : null;
            if (!ctp || !ctp.cameraParams) return;
            if (cameraHandleSet) ctp.RemoveParamsOverride(cameraHandle, 0.4f);
            var data = ctp.cameraParams.data;
            data.idealLocalCameraPos = new Vector3(0f, up, -back);
            cameraHandle = ctp.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest { cameraParamsData = data, priority = 1f }, 0.4f);
            cameraHandleSet = true;
        }

        /// <summary>Spawns a ring of standing enemies around the Saint, leaving the camera side (in front) open.</summary>
        private void SpawnRing(string masterName, int count, float radius)
        {
            var prefab = MasterCatalog.FindMasterPrefab(masterName);
            if (!prefab) { trace.AppendLine("SHOWCASE missing " + masterName); return; }
            for (int i = 0; i < count; i++)
            {
                float angle = 70f + 220f * i / Mathf.Max(1, count - 1);
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * facing;
                Vector3 at = TopGround(pilot.footPosition + dir * radius);
                var master = new MasterSummon
                {
                    masterPrefab = prefab, position = at, rotation = Quaternion.LookRotation(-dir),
                    teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true
                }.Perform();
                if (!master) continue;
                foreach (var ai in master.GetComponents<BaseAI>()) ai.enabled = false;
                StartCoroutine(TrackLive(master, at));
            }
        }

        private void Clip(string name, bool start)
        {
            trace.AppendLine(scriptTime.ToString("000.00") + " CLIP " + name + " " + (start ? "start" : "end") +
                " utc=" + DateTime.UtcNow.ToString("o") + " alive=" + Alive().Count);
        }

        private void SpawnLive(string masterName, int count, float distance)
        {
            var prefab = MasterCatalog.FindMasterPrefab(masterName);
            if (!prefab) { trace.AppendLine("SHOWCASE missing " + masterName); return; }
            Vector3 centre = pilot.footPosition + facing * distance;
            for (int i = 0; i < count; i++)
            {
                float side = (i % 2 == 0 ? 1f : -1f) * (1.5f + 2.2f * ((i + 1) / 2));
                Vector3 at = TopGround(centre + Right * side + facing * ((i % 3) - 1) * 1.5f);
                if (i == 0) trace.AppendLine(scriptTime.ToString("000.00") + " SHOWCASE spawn " + masterName + " x" + count + " at " + at.ToString("F1") + " saint=" + pilot.footPosition.ToString("F1"));
                var master = new MasterSummon
                {
                    masterPrefab = prefab, position = at, rotation = Quaternion.LookRotation(-facing),
                    teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true
                }.Perform();
                if (!master) continue;
                // AI off: a crowding melee pack hides the Saint from the camera.
                foreach (var ai in master.GetComponents<BaseAI>()) ai.enabled = false;
                StartCoroutine(TrackLive(master, at));
            }
        }

        private IEnumerator TrackLive(CharacterMaster master, Vector3 spot)
        {
            CharacterBody body = null; float t = 0f;
            while (!body && t < 5f) { body = master ? master.GetBody() : null; t += Time.deltaTime; yield return null; }
            if (!body) yield break;
            live.Add(body);
            while (body && body.healthComponent && body.healthComponent.alive && scripting)
            {
                if (body.inputBank) body.inputBank.moveVector = Vector3.zero;
                if ((body.footPosition - spot).sqrMagnitude > 4f) TeleportHelper.TeleportBody(body, spot);
                yield return new WaitForSeconds(0.2f);
            }
        }

        /// <summary>The highest walkable surface under a point (Ground's short ray starts inside hills).</summary>
        private static Vector3 TopGround(Vector3 near)
        {
            RaycastHit hit;
            return Physics.Raycast(near + Vector3.up * 40f, Vector3.down, out hit, 80f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? hit.point : near;
        }

        private void ClearLive()
        {
            foreach (var body in live.ToArray()) if (body && body.healthComponent && body.healthComponent.alive) body.healthComponent.Suicide();
            live.Clear();
        }

        private List<CharacterBody> Alive() => live.Where(b => b && b.healthComponent && b.healthComponent.alive).ToList();

        private Vector3? LiveCentre()
        {
            var alive = Alive();
            if (alive.Count == 0) return null;
            Vector3 sum = Vector3.zero;
            foreach (var b in alive) sum += b.corePosition;
            return sum / alive.Count;
        }

        /// <summary>Holds the aim on the nearest living enemy (or the pack's centre) for the duration.</summary>
        private IEnumerator AimAtLive(float seconds, bool centre = false)
        {
            float end = scriptTime + seconds;
            while (scriptTime < end)
            {
                var alive = Alive();
                if (alive.Count > 0)
                    aimTarget = centre ? LiveCentre() : alive.OrderBy(b => (b.corePosition - pilot.corePosition).sqrMagnitude).First().corePosition;
                yield return Wait(Mathf.Min(0.1f, Mathf.Max(0.01f, end - scriptTime)));
            }
        }

        /// <summary>Sweeps the aim across the pack from its leftmost to its rightmost member.</summary>
        private IEnumerator SweepLive(float seconds)
        {
            var alive = Alive();
            if (alive.Count == 0) { yield return Wait(seconds); yield break; }
            var ordered = alive.OrderBy(b => Vector3.Dot(b.corePosition - pilot.corePosition, Right)).ToList();
            Vector3 from = ordered.First().corePosition, to = ordered.Last().corePosition;
            float start = scriptTime;
            while (scriptTime - start < seconds)
            {
                float u = (scriptTime - start) / seconds;
                aimTarget = Vector3.Lerp(from, to, Mathf.PingPong(u * 2f, 1f));
                yield return Wait(0.05f);
            }
        }

        /// <summary>Turns the player camera toward the scripted aim, smoothed so cuts read as play.</summary>
        private IEnumerator CameraFollow()
        {
            while (pilot && scripting)
            {
                yield return null;
                var rigs = CameraRigController.readOnlyInstancesList;
                if (rigs.Count == 0 || !pilot.inputBank) continue;
                Vector3 want;
                if (cameraWant.HasValue) want = cameraWant.Value;
                else
                {
                    want = aimTarget.HasValue
                        ? (aimTarget.Value - pilot.inputBank.aimOrigin).normalized
                        : Quaternion.AngleAxis(aimPitch, Right) * facing;
                    // A slightly downward look keeps the Saint and the ground in frame.
                    want = (want + Vector3.down * 0.12f).normalized;
                }
                cameraLook = Vector3.Slerp(cameraLook, want, 1f - Mathf.Exp(-6f * Time.deltaTime));
                SetCameraLook(rigs[0], cameraLook);
            }
        }

        // Both members are private in the shipped game, so they are reached by reflection.
        private static System.Reflection.FieldInfo camInstanceField;
        private static System.Reflection.MethodInfo setLookMethod;

        private void SetCameraLook(CameraRigController rig, Vector3 look)
        {
            const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var mode = rig ? rig.cameraMode : null;
            if (camInstanceField == null) camInstanceField = typeof(RoR2.CameraModes.CameraModeBase).GetField("camToRawInstanceData", any);
            var map = mode != null && camInstanceField != null ? camInstanceField.GetValue(mode) as System.Collections.IDictionary : null;
            // Keyed by a wrapper around the rig; a solo run has exactly one entry.
            object data = null;
            if (map != null) foreach (System.Collections.DictionaryEntry e in map) { data = e.Value; break; }
            if (data == null) return;
            if (setLookMethod == null) setLookMethod = data.GetType().GetMethod("SetPitchYawFromLookVector", any);
            if (setLookMethod == null) return;
            setLookMethod.Invoke(data, new object[] { look });
        }
    }
}
