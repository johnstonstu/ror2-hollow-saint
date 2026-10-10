using System;
using HollowSaint.FoundationKit.ArcStep;
using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>One connected power circuit: core/ring, casting arms and dash heels.
    /// Reads local replicated skill states; follows the final pose, never changes gameplay.</summary>
    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class BodyCurrentFx : MonoBehaviour
    {
        private const int LeftArmFirst = 5, RightArmFirst = LeftArmFirst + ArmCurrentPath.SegmentCount;
        private const int LegFirst = RightArmFirst + ArmCurrentPath.SegmentCount;
        private const int LineCount = LegFirst + 5;
        private readonly AbilityCurrentWindow left = new AbilityCurrentWindow(), right = new AbilityCurrentWindow();
        private readonly LightningLine[] lines = new LightningLine[LineCount];
        private readonly bool[] used = new bool[LineCount];
        private readonly Vector3[] route = new Vector3[ArmCurrentPath.PointCount];
        private readonly Vector3[] surfaceLocal = new Vector3[6];
        private readonly Transform[] bones = new Transform[10];
        private static readonly string[] Names = { "L upperarm", "L forearm", "L hand",
            "R upperarm", "R forearm", "R hand", "pelvis", "L shin", "R shin", "chest" };
        private CharacterBody body;
        private Stormspear.StormspearCharge spear;
        private HaloRing ring;
        private Transform model, core, muzzleL, muzzleR, heelL, heelR;
        private SkinFxPalette palette;
        private float coreUntil, thunderUntil, nextResolve, feedSide, armBoost = 1f;
        private int boostBone = 3; // first bone of the spear arm (0 left, 3 right)
        private bool warned, resolved, feedReady;
        public bool IsActive { get; private set; }
        public float Intensity { get; private set; }
        public float SpearDistance { get; private set; } = 2f;

        private void Awake() { body = GetComponent<CharacterBody>(); ring = HaloRing.For(body); }

        private static BodyCurrentFx For(CharacterBody who)
        {
            if (!who) return null;
            var fx = who.GetComponent<BodyCurrentFx>();
            return fx ? fx : who.gameObject.AddComponent<BodyCurrentFx>();
        }

        internal static uint BeginArm(CharacterBody who, bool isLeft, float duration, float release)
        {
            var fx = For(who);
            return fx ? (isLeft ? fx.left : fx.right).Begin(Time.time, duration, release) : 0;
        }
        internal static void ReleaseArm(CharacterBody who, bool isLeft, uint token)
        {
            var fx = who ? who.GetComponent<BodyCurrentFx>() : null;
            if (fx) (isLeft ? fx.left : fx.right).Release(token, Time.time);
        }
        internal static void CancelArm(CharacterBody who, bool isLeft, uint token)
        {
            var fx = who ? who.GetComponent<BodyCurrentFx>() : null;
            if (fx) (isLeft ? fx.left : fx.right).Cancel(token, Time.time);
        }
        internal static void PulseCore(CharacterBody who, float seconds)
        {
            var fx = For(who);
            if (fx) fx.coreUntil = Mathf.Max(fx.coreUntil, Time.time + Mathf.Clamp(seconds, 0.02f, 3f));
        }
        internal static void Thunder(CharacterBody who, float seconds)
        {
            var fx = For(who);
            if (fx) fx.thunderUntil = seconds > 0f ? Time.time + Mathf.Clamp(seconds, 0.02f, 3f) : 0f;
        }

        private void LateUpdate() { Tick(Time.time, Time.deltaTime); }

        internal void Tick(float now, float dt)
        {
            Array.Clear(used, 0, used.Length);
            IsActive = false;
            Intensity = 0f;
            if (!body || !body.healthComponent || !body.healthComponent.alive)
            { Reset(); return; }
            if (!Resolve() || !ring || !ring.Valid) { feedReady = false; HideUnused(); return; }
            var characterModel = model.GetComponent<CharacterModel>();
            if (characterModel && characterModel.invisibilityCount > 0) { feedReady = false; HideUnused(); return; }
            float lw, lt, lp, rw, rt, rp;
            left.Sample(now, out lw, out lt, out lp);
            right.Sample(now, out rw, out rt, out rp);
            var carry = body.GetComponent<SpearDischarge.SpearCarry>();
            if (!spear) spear = body.GetComponent<Stormspear.StormspearCharge>();
            bool handCharge = spear && spear.Charging && spear.Form == Stormspear.SpearForm.Hand;
            // The javelin feed must read at gameplay distance: the right arm and the core feed run much thicker.
            // v0.9.1: was 1.4 + 1.8c (3.2x at full), which drowned the spear; the spear itself now carries the read.
            armBoost = handCharge ? 1.15f + 0.75f * spear.Charge01 : 1f;
            boostBone = carry && carry.Left ? 0 : 3;
            if (handCharge)
            {
                // Stormspear hand charge: the ability current runs core -> spine -> shoulder -> arm -> palm
                // and thickens with the charge; the lock-ins brighten it through the pulse term.
                // v0.9.14: the spear arm is the left one by default (SpearCarry.Left).
                float c = spear.Charge01;
                float sw = 0.3f + 0.7f * c, sp = (0.35f + 0.65f * c) * LightningRhythm.Pulse(now, SpearDistance);
                if (carry && carry.Left) { lw = Mathf.Max(lw, sw); lt = 1f; lp = sp; }
                else { rw = Mathf.Max(rw, sw); rt = 1f; rp = sp; }
            }
            bool crown = OpenCircuitBuff.Def && body.HasBuff(OpenCircuitBuff.Def);
            bool dash = ArcStepState.IsBodyDashing(body);
            float source = Mathf.Max(Mathf.Max(lw, rw), Mathf.Max(crown ? 0.55f : 0f, dash ? 1f : 0f));
            if (now < coreUntil || now < thunderUntil) source = Mathf.Max(source, 0.8f);
            if (spear && spear.Charging) source = Mathf.Max(source, 0.55f + 0.45f * spear.Charge01);
            if (source <= 0.001f) { feedReady = false; HideUnused(); return; }
            IsActive = true;
            Intensity = source;
            var currentPalette = SkinFxPalette.ForBody(body);
            if (!ReferenceEquals(currentPalette, palette))
            {
                palette = currentPalette;
                foreach (var line in lines) if (line) line.SetPalette(palette);
            }
            VfxAssets.Load();
            float unit = ring.Unit;
            Vector3 up = Vector3.up;
            Vector3 forward = body.characterDirection ? body.characterDirection.forward : model.forward;
            Vector3 back = Vector3.ProjectOnPlane(-forward, up).normalized;
            Vector3 side = Vector3.Cross(up, -back);
            Vector3 shoulder = (bones[0].position + bones[3].position) * 0.5f;
            // Keep the core feed attached to the final torso pose while easing
            // between casting sides. Smoothing world positions would lag behind
            // movement; only the small body-relative lateral offset follows.
            float targetSide = rw >= lw ? 1f : -1f;
            float follow = dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt) ? 1f - Mathf.Exp(-18f * Mathf.Min(dt, 0.1f)) : 0f;
            feedSide = feedReady ? Mathf.Lerp(feedSide, targetSide, follow) : targetSide;
            feedReady = true;
            Vector3 flank = shoulder + side * feedSide * 0.18f * unit;
            Vector3 spine = bones[9].position + back * 0.17f * unit + up * 0.06f * unit;
            Vector3 dock = ring.Shape.Nearest(spine);
            // 1.3.1 (Stu): while the crown has flown up into a Thundercloud (or is otherwise far from the
            // body) the spine feed would stretch to the sky and read as lightning striking the Saint.
            if (Thundercloud.ThundercloudCrownPose.OwnsPresentation(body) || Vector3.Distance(spine, dock) > 2.5f * unit)
            { feedReady = false; HideUnused(); return; }
            float feed = Vector3.Distance(core.position, flank) + Vector3.Distance(flank, spine) + Vector3.Distance(spine, dock);
            Place(0, core.position, flank, 0.24f * source * armBoost * LightningRhythm.Gain(now), 1, dt);
            Place(1, flank, spine, 0.28f * source * armBoost * LightningRhythm.Gain(now, feed * 0.5f), 1, dt);
            Place(2, spine, dock, 0.36f * source * armBoost * LightningRhythm.Gain(now, feed), 1, dt);
            float angle = now * 4f;
            // The ring crackle follows the halo dim: the power has moved into the hand.
            float haloDim = Stormspear.Fx.StormspearFx.HaloOf(body);
            for (int k = 0; k < 2; k++)
                Place(3 + k, ring.Shape.PointAt(angle + k * Mathf.PI),
                    ring.Shape.PointAt(angle + k * Mathf.PI + 0.6f), 0.26f * source * haloDim * LightningRhythm.Gain(now, feed), 1, dt);
            if (crown) { lw = Mathf.Max(lw, 0.22f); rw = Mathf.Max(rw, 0.22f); lt = rt = 1f; }
            bool spearShown = carry && carry.HandVisible && carry.Contact;
            DrawArm(0, LeftArmFirst, spearShown && carry.Left ? carry.Contact : muzzleL, lw, lt, lp, unit, feed, now, dt);
            DrawArm(3, RightArmFirst, spearShown && !carry.Left ? carry.Contact : muzzleR, rw, rt, rp, unit, feed, now, dt);
            if (dash) DrawLegs(spine, back, unit, dt);
            HideUnused();
        }

        private void DrawArm(int bone, int first, Transform muzzle, float weight, float travel,
            float pulse, float unit, float feed, float now, float dt)
        {
            if (weight <= 0.001f || !muzzle) return;
            ArmCurrentPath.Build(route, bones[bone].position, bones[bone + 1].position,
                bones[bone + 2].position, muzzle.position,
                bones[bone].TransformDirection(surfaceLocal[bone]),
                bones[bone + 1].TransformDirection(surfaceLocal[bone + 1]),
                bones[bone + 2].TransformDirection(surfaceLocal[bone + 2]), unit);
            route[0] = ring.Shape.Nearest(route[1]);
            float total = 0f;
            for (int i = 0; i < ArmCurrentPath.SegmentCount; i++) total += Vector3.Distance(route[i], route[i + 1]);
            if (bone == 3) SpearDistance = feed + total;
            float remaining = total * Mathf.Clamp01(travel), passed = 0f;
            for (int i = 0; i < ArmCurrentPath.SegmentCount && remaining > 0.001f; i++)
            {
                float length = Vector3.Distance(route[i], route[i + 1]);
                float share = Mathf.Clamp01(remaining / Mathf.Max(length, 0.001f));
                float midpoint = (passed + length * 0.5f) / Mathf.Max(total, 0.001f);
                float head = Mathf.Clamp01(1f - Mathf.Abs(midpoint - travel) / 0.3f);
                float rhythm = LightningRhythm.Pulse(now, feed + passed + length * 0.5f);
                float width = weight * (0.18f + 0.16f * head + 0.24f * pulse + 0.16f * rhythm) * (bone == boostBone ? armBoost : 1f);
                Place(first + i, route[i], Vector3.Lerp(route[i], route[i + 1], share), width, 2, dt);
                remaining -= length;
                passed += length;
            }
        }

        private void DrawLegs(Vector3 spine, Vector3 back, float unit, float dt)
        {
            Vector3 pelvis = bones[6].position + back * 0.1f * unit;
            Place(LegFirst, spine, pelvis, 0.3f, 1, dt);
            Place(LegFirst + 1, pelvis, bones[7].position + back * 0.06f * unit, 0.25f, 1, dt);
            if (heelL) Place(LegFirst + 2, bones[7].position + back * 0.06f * unit, heelL.position, 0.4f, 2, dt);
            Place(LegFirst + 3, pelvis, bones[8].position + back * 0.06f * unit, 0.25f, 1, dt);
            if (heelR) Place(LegFirst + 4, bones[8].position + back * 0.06f * unit, heelR.position, 0.4f, 2, dt);
        }

        private void Place(int index, Vector3 from, Vector3 to, float width, int branches, float dt)
        {
            if ((to - from).sqrMagnitude < 0.000001f) return;
            used[index] = true;
            if (!lines[index])
            {
                var line = new GameObject("HS_BodyCurrent" + index).AddComponent<LightningLine>();
                line.transform.SetParent(transform, false);
                line.loop = line.manualTick = true;
                line.drawTime = 0f;
                line.branches = branches;
                line.jag = 0.18f;
                line.rejagInterval = 0.04f;
                line.SetPalette(palette);
                lines[index] = line;
            }
            var fx = lines[index];
            fx.gameObject.SetActive(true);
            fx.start = from; fx.end = to; fx.width = width;
            fx.Tick(dt);
        }

        private bool Resolve()
        {
            var current = body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!current) return false;
            if (model == current && resolved) return true;
            if (model == current && Time.unscaledTime < nextResolve) return false;
            nextResolve = Time.unscaledTime + 1f;
            if (model != current) { DestroyLines(); warned = false; feedReady = false; }
            model = current;
            Array.Clear(bones, 0, bones.Length);
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                int index = Array.IndexOf(Names, t.name);
                if (index >= 0) bones[index] = t;
            }
            core = KitUtil.ResolveSocket(body, "Core");
            muzzleL = KitUtil.ResolveSocket(body, "MuzzleLeft");
            muzzleR = KitUtil.ResolveSocket(body, "MuzzleRight");
            heelL = KitUtil.ResolveSocket(body, "HeelL"); heelR = KitUtil.ResolveSocket(body, "HeelR");
            bool valid = core;
            foreach (var bone in bones) valid &= bone != null;
            resolved = valid;
            if (valid) CacheSurfaceDirections();
            if (!valid && !warned)
            { warned = true; Plugin.Log.LogWarning("HOLLOW_SAINT_BODY_CURRENT missing core/limb bones on " + model.name + "; connected current skipped"); }
            return valid;
        }

        private void CacheSurfaceDirections()
        {
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Vector3 forward = body.characterDirection ? body.characterDirection.forward : model.forward;
            bool complete = true;
            for (int i = 0; i < surfaceLocal.Length; i++)
            {
                Vector3 reference = i % 3 == 0 ? -forward : Vector3.down;
                bool found = false;
                foreach (var skin in skins)
                {
                    int index = Array.IndexOf(skin.bones, bones[i]);
                    if (index < 0 || !skin.sharedMesh) continue;
                    var binds = skin.sharedMesh.bindposes;
                    if (index >= binds.Length) continue;
                    surfaceLocal[i] = binds[index].MultiplyVector(
                        skin.transform.InverseTransformDirection(reference)).normalized;
                    found = true;
                    break;
                }
                // Missing skin data keeps a usable route, with an explicit diagnostic.
                if (!found) surfaceLocal[i] = bones[i].InverseTransformDirection(reference);
                complete &= found;
            }
            if (!complete) Plugin.Log.LogWarning("HOLLOW_SAINT_BODY_CURRENT missing skin bind pose; arm surface directions use current pose on " + model.name);
        }

        private void HideUnused()
        { for (int i = 0; i < lines.Length; i++) if (lines[i] && !used[i]) lines[i].gameObject.SetActive(false); }
        private void DestroyLines()
        { for (int i = 0; i < lines.Length; i++) { if (lines[i]) Destroy(lines[i].gameObject); lines[i] = null; } }
        private void Reset()
        { left.Clear(); right.Clear(); coreUntil = thunderUntil = 0f; feedReady = false; IsActive = false; Intensity = 0f; Array.Clear(used, 0, used.Length); HideUnused(); }
        private void OnDisable() { Reset(); DestroyLines(); }
        private void OnDestroy() { DestroyLines(); }
    }
}
