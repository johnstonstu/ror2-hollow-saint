using HollowSaint.FoundationKit.Stormspear;
using HollowSaint.FoundationKit.Stormspear.Fx;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.SpearDischarge
{
    /// <summary>
    /// v0.9: the fitted spear model on the right-hand grip socket is the HAND-form lightning spear.
    /// It exists only while a hand-form Stormspear charge is held (plus a short window after the
    /// release for the throw); it grows from the palm with Charge01 and is driven bright by
    /// SpearVisual. Everything is read from StormspearCharge, so local and remote players match.
    /// The crown-form spear is a separate visual (StormspearFx); in the crown no spear pose plays
    /// and both arms stay free for Arc Bolt.
    /// The fitted hand socket follows the final animated pose; no independent grip rotation.
    /// v0.9.14: the spear is held and thrown from the LEFT hand by default (it is on the left trigger),
    /// with Arc Bolt leaving the right hand while it charges. The bundle only carries the right grip
    /// socket and right-arm carry clips, so the left side is built at runtime: the grip socket is the
    /// right one mirrored through the rig's bind pose, every pose key is mirrored across the body, the
    /// carry clips are skipped and the left fingers close on the shaft procedurally.
    /// Config "Spear hand" switches back to the right hand (live).
    /// </summary>
    [DefaultExecutionOrder(170)]
    [DisallowMultipleComponent]
    public sealed class SpearCarry : MonoBehaviour
    {
        // v0.9.3 javelin flow. The hold is no longer a fixed pose that snaps in: the hand draws the
        // spear up from the hip over the shoulder, the wind-up deepens with the charge, the torso
        // leans back into it, the off hand reaches toward the target, and the whole pose breathes.
        // The throw is an overhand arc: torso leads, elbow comes through high, the spear leaves the
        // hand at the apex (StormspearTuning.HandReleaseDelay, the projectile fires there too), then
        // the arm follows through across the body while the off hand pulls back to the hip.
        private const float ThrowWindow = 0.72f;       // pose/arm ownership after the release
        private const float FollowEnd = 0.42f, BlendIn = 0.16f, BlendOut = 0.26f;
        private const float DrawTime = 0.30f;          // hip to cocked, eased in and out (no snap on the press)
        private const float TapMinLength = 0.55f;      // a tap throw still shows a real spear for the whip
        // v0.9.8 camera read: the gameplay camera sits right behind the character, so the spear is
        // held OUT to the right of the halo and tilted nose-up, where its whole length shows beside
        // the body instead of end-on behind the head. While Arc Bolt fires from the outstretched left
        // hand the spear draws further back ("bolting" blend); otherwise it sits raised beside you.
        private const float BesideYaw = 6f, BesideNoseUp = 26f, BackYaw = 10f, BackNoseUp = 16f;
        private const float ThrowReleaseNormalized = 6f / 19f; // release frame of the "Conduit Spear" clip
        private const string HeldState = "Spear held", ThrowState = "Conduit Spear";

        private static float ReleaseTime { get { return Mathf.Max(0.02f, StormspearTuning.HandReleaseDelay); } }

        /// <summary>Config "Spear hand": true holds and throws the hand spear with the left hand (default).</summary>
        public static bool LeftHanded = true;
        /// <summary>Muzzle of the hand that holds the spear (effect fallbacks when the model is hidden).</summary>
        public static string SpearMuzzle { get { return LeftHanded ? "MuzzleLeft" : "MuzzleRight"; } }
        /// <summary>Bone name prefix of the spear arm ("L " or "R ").</summary>
        public static string SpearArmPrefix { get { return LeftHanded ? "L " : "R "; } }
        private const string LeftSocketName = "SpearGripSocketL";
        // Procedural grip for the left hand (no mirrored clip in the bundle): degrees per finger segment.
        private static readonly float[] GripCurl = { 76f, 90f, 64f };
        private static readonly string[] FingerNames = { "index", "middle", "ring", "little" };

        private CharacterBody body;
        private StormspearCharge charge;
        private StormspearFx fx;
        private FoundationArmPose armPose;
        private Transform model, socket, upperarm, forearm, hand, chest;
        // upperarm/forearm/hand = the SPEAR arm; leftUpper/leftFore/leftHand = the GUIDE (off) arm.
        // The names predate v0.9.14, when the spear arm was always the right one.
        private Transform leftUpper, leftFore, leftHand;
        private bool left;          // resolved side: spear in the left hand
        private bool resolvedFor;   // LeftHanded when last resolved (a failed mirror falls back to the right)
        private float side = 1f;    // +1 right-handed, -1 left-handed (mirrors every key across the body)
        private readonly Transform[] gripFingers = new Transform[12];
        private readonly Quaternion[] savedGrip = new Quaternion[12];
        private bool appliedGrip;
        private float gripW, gripCurlSign = 1f;
        private bool loggedGrip;
        private GameObject visual;
        private SpearVisual weaponFx;
        private Vector3 baseScale = Vector3.one;
        private int lengthAxis;
        private float baseLength = 1f;
        private float holdStarted, throwStarted = -10f, seenRelease = -1f;
        private bool holding, wasCharging, warned, dropped, kicked;
        private float poseW, guideW;
        private bool loggedRig, applied, appliedLeft;
        private Quaternion savedUpper, savedFore, savedHand, savedChest, savedLU, savedLF, savedLH;
        // Last frame's pose in the aim basis (x forward, y up, z right; hand key in arm lengths),
        // captured when the throw starts so the whip leaves from wherever the hand actually was.
        private Vector3 lastKey, lastDirKey, fromKey, fromDirKey, lastGuideKey, fromGuideKey;
        private float lastTwist, lastLean, fromTwist, fromLean;
        private float boltW, guideKick, lastBolt = -10f;

        /// <summary>Arc Bolt fired from the off hand while a hand spear charges: the off hand kicks back
        /// a little and the spear stays drawn back for a moment (ArcBoltState skips its arm gesture then).</summary>
        internal static void OffHandShot(CharacterBody owner)
        {
            var carry = owner ? owner.GetComponent<SpearCarry>() : null;
            if (!carry) return;
            carry.guideKick = 1f;
            carry.lastBolt = Time.time;
        }
        /// <summary>A hand spear is charging (the off hand is the guide arm, Arc Bolt fires from it).</summary>
        internal static bool HoldingFor(CharacterBody owner)
        {
            var carry = owner ? owner.GetComponent<SpearCarry>() : null;
            return carry && carry.holding;
        }

        /// <summary>The spear is in the left hand (resolved; the config can change it live).</summary>
        public bool Left { get { return left; } }
        /// <summary>The spear is (or would be) in the left hand of this body; Arc Bolt uses the other hand.</summary>
        internal static bool SpearInLeft(CharacterBody owner)
        {
            var carry = owner ? owner.GetComponent<SpearCarry>() : null;
            return carry && carry.visual ? carry.left : LeftHanded;
        }
        /// <summary>The grip socket of the hand spear, or null (recall lines anchor to it).</summary>
        internal static Transform GripSocketOf(CharacterBody owner)
        {
            var carry = owner ? owner.GetComponent<SpearCarry>() : null;
            if (carry && carry.socket) return carry.socket;
            return KitUtil.ResolveSocket(owner, "SpearGripSocket");
        }
        /// <summary>True while the hand pose / throw owns the spear arm (arm pose and torso accents read this).</summary>
        public bool Casting { get { return holding || Time.time - throwStarted < ThrowWindow; } }
        public bool HandVisible { get; private set; }
        /// <summary>Right hand closed on the spear (hold or until the spear leaves the hand).</summary>
        public bool Gripping { get { return holding || Time.time - throwStarted < ReleaseTime; } }
        public Transform Tip { get; private set; }
        public Transform Contact { get; private set; }
        /// <summary>Butt of the spear (the model grips it ~39% from here already).</summary>
        public Transform Tail { get; private set; }
        public Vector3 GripPosition { get { return socket ? socket.position : KitFx.Socket(body, left ? "MuzzleLeft" : "MuzzleRight"); } }
        public Vector3 Center { get { return Tip && Contact ? (Tip.position + Contact.position) * 0.5f : GripPosition; } }
        /// <summary>Tip and grip as last shown in the hand (the release flash and the projectile use these
        /// right after the model hides at the apex).</summary>
        public Vector3 LastTip { get; private set; }
        public Vector3 LastGrip { get; private set; }
        /// <summary>The spear left the hand this recently (seconds), or a large number.</summary>
        public float SinceThrow { get { return throwStarted > 0f ? Time.time - throwStarted : 100f; } }
        public Vector3 ShaftDirection { get { return Tip && socket ? (Tip.position - socket.position).normalized : body.inputBank.aimDirection; } }

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            fx = StormspearFx.For(body); // lazy: KitRegistration is not edited for the new component
        }

        private void LateUpdate()
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive)
            { DropOnDeath(); Clear(); return; }
            if (!charge) charge = StormspearCharge.Of(body);
            if (!charge) { SetVisible(false); return; }
            if (!Resolve()) return;
            if (!fx) fx = StormspearFx.For(body);

            float now = Time.time;
            bool handCharge = charge.Charging && charge.Form == SpearForm.Hand;
            float since = now - charge.LastReleaseTime;
            bool handRelease = !charge.Charging && charge.LastReleaseForm == SpearForm.Hand && since < ReleaseTime;
            var characterModel = model.GetComponent<CharacterModel>();
            bool hidden = characterModel && characterModel.invisibilityCount > 0;

            // Edges: a release can arrive without ever seeing a charge frame (state swap), so key on the timestamp.
            if (handCharge && !holding) BeginHold(now);
            if (!Mathf.Approximately(charge.LastReleaseTime, seenRelease))
            {
                seenRelease = charge.LastReleaseTime;
                if (charge.LastReleaseForm == SpearForm.Hand && since < 0.25f) BeginThrow(now);
                else EndHold(false);
            }
            if (!handCharge && holding) EndHold(!charge.Charging && since > 0.25f); // cancelled or swapped to crown
            if (!holding && throwStarted > 0f && now - throwStarted > ThrowWindow) { throwStarted = -10f; KitAnim.Stop(body, KitAnim.SpearCarryLayer, 0.12f); }
            wasCharging = handCharge;
            TickAim(now, Time.deltaTime);

            bool visible = (handCharge || handRelease) && !hidden;
            SetVisible(visible);
            if (!visible) return;
            if (Tip) { LastTip = Tip.position; LastGrip = socket.position; }

            float c = handCharge ? charge.Charge01 : Mathf.Max(charge.LastReleaseCharge, TapMinLength);
            ApplyScale(c, handCharge ? 0f : since);
            if (!handCharge) c = charge.LastReleaseCharge;
            float lock01 = fx ? fx.LockKick : 0f;
            weaponFx.Powered = true;
            weaponFx.Gain = (0.45f + 0.5f * c) * (1f + 0.3f * lock01) * (charge.Full ? 1.1f : 1f) * (handCharge ? 1f : 1.15f);
        }

        private void BeginHold(float now)
        {
            holding = true; holdStarted = now; throwStarted = -10f;
            // The carry clips are authored on the right arm only; the left hand grips procedurally.
            if (!left) KitAnim.PlayOnBody(body, KitAnim.SpearCarryLayer, HeldState, 2f);
        }

        private void EndHold(bool relax)
        {
            if (!holding) return;
            holding = false;
            KitAnim.Stop(body, KitAnim.SpearCarryLayer, 0.15f);
        }

        private void BeginThrow(float now)
        {
            if (!holding) { holdStarted = now - DrawTime; lastKey = ReadyKey(0f, 0f, 0f); lastDirKey = ReadyDirKey(0f, 0f); lastTwist = 12f; lastLean = -4f; lastGuideKey = GuideKey(0f, 0f); }
            poseW = Mathf.Max(poseW, 0.7f); // a tap throw still shows the whip
            holding = false;
            throwStarted = now;
            kicked = false;
            fromKey = lastKey; fromDirKey = lastDirKey; fromTwist = lastTwist; fromLean = lastLean; fromGuideKey = lastGuideKey;
            // Throw clip under the IK (fingers, wrist), started just before its release frame.
            // Right hand only (the clip and its layer mask are authored on the right arm).
            if (left) return;
            const float duration = 0.3f;
            var animator = KitAnim.AnimatorOf(body);
            if (!KitAnim.PlayOnBody(body, KitAnim.SpearCarryLayer, ThrowState, duration) || !animator) return;
            int layer = animator.GetLayerIndex(KitAnim.SpearCarryLayer);
            float length = KitAnim.ClipLength(animator, ThrowState);
            if (layer < 0 || length <= 0.01f) return;
            int hash = Animator.StringToHash(ThrowState);
            float offset = length * Mathf.Max(0f, ThrowReleaseNormalized - 0.07f);
            if (animator.GetLayerWeight(layer) < 0.05f) animator.PlayInFixedTime(hash, layer, offset);
            else animator.CrossFadeInFixedTime(hash, 0.04f, layer, offset);
        }

        private void ApplyScale(float c, float sinceRelease)
        {
            // Grows from the palm along its length (0.3 to 1.0) and thickens (0.35 to 1.0); thin and
            // flickering at first, a small pop on the release.
            float flicker = 1f + (1f - c) * 0.35f * (Mathf.PerlinNoise(Time.time * 37f, 0.7f) - 0.5f);
            float pop = sinceRelease > 0f ? 1f + 0.12f * Mathf.Clamp01(sinceRelease / ReleaseTime) : 1f;
            float length = Mathf.Lerp(0.3f, 1f, c) * pop * flicker;
            float width = Mathf.Lerp(0.35f, 1f, c) * pop;
            Vector3 s = baseScale * width;
            s[lengthAxis] = baseScale[lengthAxis] * length;
            visual.transform.localScale = s;
        }

        private void SetVisible(bool visible)
        {
            HandVisible = visible;
            if (visual && visual.activeSelf != visible) visual.SetActive(visible);
        }

        private bool Resolve()
        {
            var current = body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!current) return false;
            if (current == model && visual && resolvedFor == LeftHanded) return true;
            if (visual)
            {
                // Side switch (config) or a new model: drop the old pose cleanly first.
                Update();
                if (holding || throwStarted > 0f) KitAnim.Stop(body, KitAnim.SpearCarryLayer, 0.1f);
                Destroy(visual);
            }
            model = current;
            holding = false; throwStarted = -10f; poseW = 0f; guideW = 0f; gripW = 0f;
            applied = appliedLeft = appliedGrip = false;
            var rightSocket = KitUtil.ResolveSocket(body, "SpearGripSocket");
            var rUpper = KitUtil.ResolveSocket(body, "R upperarm");
            var rFore = KitUtil.ResolveSocket(body, "R forearm");
            var rHand = KitUtil.ResolveSocket(body, "R hand");
            var lUpper = KitUtil.ResolveSocket(body, "L upperarm");
            var lFore = KitUtil.ResolveSocket(body, "L forearm");
            var lHand = KitUtil.ResolveSocket(body, "L hand");
            chest = KitUtil.ResolveSocket(body, "chest");
            armPose = model.GetComponent<FoundationArmPose>();
            if (!rightSocket || !FoundationContent.SpearModel)
            {
                if (!warned) { warned = true; Plugin.Log.LogError("SPEAR_CARRY missing fitted socket/model; bundle11 required"); }
                return false;
            }
            left = resolvedFor = LeftHanded;
            Transform mirrored = left && lHand ? MirrorSocket(model, rightSocket, rHand, lHand) : null;
            if (left && !mirrored) { left = false; Plugin.Log.LogWarning("SPEAR_CARRY could not mirror the grip socket; spear stays in the right hand"); }
            side = left ? -1f : 1f;
            socket = left ? mirrored : rightSocket;
            upperarm = left ? lUpper : rUpper; forearm = left ? lFore : rFore; hand = left ? lHand : rHand;
            leftUpper = left ? rUpper : lUpper; leftFore = left ? rFore : lFore; leftHand = left ? rHand : lHand;
            System.Array.Clear(gripFingers, 0, gripFingers.Length);
            if (left)
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name;
                    if (n.Length < 5 || !n.StartsWith("L ", System.StringComparison.Ordinal)) continue;
                    int dot = n.IndexOf('.');
                    if (dot <= 2 || dot + 2 != n.Length) continue;
                    int finger = System.Array.IndexOf(FingerNames, n.Substring(2, dot - 2));
                    int segment = n[dot + 1] - '1';
                    if (finger >= 0 && segment >= 0 && segment < 3) gripFingers[finger * 3 + segment] = t;
                }
            visual = Instantiate(FoundationContent.SpearModel, socket, false);
            Tip = Find(visual, "SpearTip"); Contact = Find(visual, "SpearContact");
            foreach (var t in visual.GetComponentsInChildren<Transform>(true)) if (t.name == "SpearTail") Tail = t;
            weaponFx = visual.AddComponent<SpearVisual>();
            weaponFx.Owner = body;
            if (!Tip || !Contact) return false;
            // Which model axis runs along the shaft, and the native length, for length-wise scaling.
            baseScale = visual.transform.localScale;
            Vector3 along = visual.transform.InverseTransformPoint(Tip.position) - visual.transform.InverseTransformPoint(Contact.position);
            lengthAxis = Mathf.Abs(along.x) >= Mathf.Abs(along.y) && Mathf.Abs(along.x) >= Mathf.Abs(along.z) ? 0 : Mathf.Abs(along.y) >= Mathf.Abs(along.z) ? 1 : 2;
            float native = Vector3.Distance(Tip.position, Contact.position);
            baseLength = native > 0.05f ? Mathf.Clamp(StormspearTuning.HandSpearLength / native, 0.6f, 1.5f) : 1f;
            baseScale[lengthAxis] *= baseLength;
            visual.SetActive(false);
            HandVisible = false;
            Plugin.Log.LogInfo("SPEAR_CARRY hand=" + (left ? "left" : "right") + " socket=" + socket.name
                + " right[" + GripReport(rightSocket, rHand, "R ") + "]" + (mirrored ? " left[" + GripReport(mirrored, lHand, "L ") + "]" : ""));
            return true;
        }

        /// <summary>v0.9.14: a left grip socket mirrored from the fitted right one through the rig's bind
        /// pose (the bundle only has the right socket). The bind pose is symmetric, so the hands' bind
        /// positions give the mirror plane; one axis across the shaft is flipped to keep a proper rotation
        /// (that only rolls the spear about its own shaft, which the javelin IK re-aims anyway).</summary>
        private static Transform MirrorSocket(Transform model, Transform rightSocket, Transform rightHand, Transform leftHand)
        {
            var existing = leftHand.Find(LeftSocketName);
            if (existing) return existing;
            if (!rightHand || rightSocket.parent != rightHand) return null;
            int shaftAxis = ShaftAxis(FoundationContent.SpearModel);
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var bones = smr.bones;
                var binds = smr.sharedMesh ? smr.sharedMesh.bindposes : null;
                if (bones == null || binds == null || binds.Length != bones.Length) continue;
                int r = System.Array.IndexOf(bones, rightHand), l = System.Array.IndexOf(bones, leftHand);
                if (r < 0 || l < 0) continue;
                Matrix4x4 bR = binds[r].inverse, bL = binds[l].inverse; // bone -> mesh space at bind
                Vector3 pR = bR.GetColumn(3), pL = bL.GetColumn(3);
                Vector3 n = pR - pL;
                if (n.sqrMagnitude < 1e-8f) continue;
                n.Normalize();
                Vector3 mid = (pR + pL) * 0.5f;
                Matrix4x4 h = Matrix4x4.identity;
                for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) h[i, j] = (i == j ? 1f : 0f) - 2f * n[i] * n[j];
                Vector3 shift = 2f * Vector3.Dot(n, mid) * n;
                h.SetColumn(3, new Vector4(shift.x, shift.y, shift.z, 1f));
                Matrix4x4 flip = Matrix4x4.identity;
                flip[(shaftAxis + 1) % 3, (shaftAxis + 1) % 3] = -1f;
                Matrix4x4 local = bL.inverse * h * bR * Matrix4x4.TRS(rightSocket.localPosition, rightSocket.localRotation, Vector3.one) * flip;
                Vector3 fwd = local.GetColumn(2), up = local.GetColumn(1);
                if (fwd.sqrMagnitude < 1e-8f || up.sqrMagnitude < 1e-8f) continue;
                var socket = new GameObject(LeftSocketName).transform;
                socket.SetParent(leftHand, false);
                socket.localPosition = local.GetColumn(3);
                socket.localRotation = Quaternion.LookRotation(fwd, up);
                socket.localScale = rightSocket.localScale;
                Plugin.Log.LogInfo("SPEAR_CARRY mirrored grip: right local=" + rightSocket.localPosition.ToString("F3") + " left local=" + socket.localPosition.ToString("F3")
                    + " plane=" + n.ToString("F2") + " det=" + local.determinant.ToString("F2"));
                return socket;
            }
            return null;
        }

/// <summary>Diagnostic: how the shaft sits in the hand (angles to the finger line and the knuckle line,
        /// and which side of the palm). The two hands should match when the mirror is right.</summary>
        private string GripReport(Transform grip, Transform handBone, string prefix)
        {
            Transform middle = null, index = null, little = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == prefix + "middle.1") middle = t;
                else if (t.name == prefix + "index.1") index = t;
                else if (t.name == prefix + "little.1") little = t;
            }
            if (!middle || !index || !little || !Tip || !Contact) return "?";
            // Shaft in the grip socket's frame (the visual is an identity child of whichever socket holds it).
            Vector3 shaftLocal = visual.transform.InverseTransformDirection((Tip.position - Contact.position).normalized);
            Vector3 shaft = grip.TransformDirection(shaftLocal);
            Vector3 fingers = (middle.position - handBone.position).normalized, knuckles = (index.position - little.position).normalized;
            Vector3 palm = Vector3.Cross(fingers, knuckles).normalized;
            Vector3 offset = grip.position - handBone.position;
            return "fingers=" + Vector3.Angle(shaft, fingers).ToString("0") + " knuckles=" + Vector3.Angle(shaft, knuckles).ToString("0")
                + " palmSide=" + (Vector3.Dot(offset, palm) / Mathf.Max(1e-4f, offset.magnitude)).ToString("0.00")
                + " along=" + (Vector3.Dot(offset, fingers) / Mathf.Max(1e-4f, offset.magnitude)).ToString("0.00");
        }

        private static int ShaftAxis(GameObject prefab)
        {
            Transform tip = null, contact = null;
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            { if (t.name == "SpearTip") tip = t; else if (t.name == "SpearContact") contact = t; }
            if (!tip || !contact) return 2;
            Vector3 along = prefab.transform.InverseTransformPoint(tip.position) - prefab.transform.InverseTransformPoint(contact.position);
            return Mathf.Abs(along.x) >= Mathf.Abs(along.y) && Mathf.Abs(along.x) >= Mathf.Abs(along.z) ? 0 : Mathf.Abs(along.y) >= Mathf.Abs(along.z) ? 1 : 2;
        }

        internal static Transform Find(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            Plugin.Log.LogError("SPEAR_MODEL missing marker " + name);
            return null;
        }

        /// <summary>Kept for the arm pass call sites. The javelin pose now runs from SpearCarry's own LateUpdate
        /// (after the arm pass, before the current and the lines), with its own restore in Update, so it never
        /// depends on the arm pass and never accumulates.</summary>
        internal static void ApplyAim(CharacterBody owner) { }
        internal static void ApplyAim(CharacterBody owner, float now, float dt) { }

        /// <summary>Where a hand-form throw leaves from: the tip of the held spear, in the ready pose or
        /// at the apex of the whip (the projectile fires there after HandReleaseDelay).</summary>
        public static bool TryReleasePoint(CharacterBody owner, out Vector3 point)
        {
            var carry = owner ? owner.GetComponent<SpearCarry>() : null;
            if (carry && carry.HandVisible && carry.Tip && (carry.holding || carry.Gripping)) { point = carry.Tip.position; return true; }
            if (carry && carry.SinceThrow < ReleaseTime + 0.1f) { point = carry.LastTip; return true; }
            point = Vector3.zero;
            return false;
        }

        // Undo last frame's pose before the Animator runs (same restore-each-frame rule as the arm pass).
        private void Update()
        {
            if (applied)
            {
                applied = false;
                if (upperarm) upperarm.localRotation = savedUpper;
                if (forearm) forearm.localRotation = savedFore;
                if (hand) hand.localRotation = savedHand;
                if (chest) chest.localRotation = savedChest;
            }
            if (appliedLeft)
            {
                appliedLeft = false;
                if (leftUpper) leftUpper.localRotation = savedLU;
                if (leftFore) leftFore.localRotation = savedLF;
                if (leftHand) leftHand.localRotation = savedLH;
            }
            if (appliedGrip)
            {
                appliedGrip = false;
                for (int i = 0; i < gripFingers.Length; i++) if (gripFingers[i]) gripFingers[i].localRotation = savedGrip[i];
            }
        }

        private static float Ease(float v) { return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v)); }
        private static float EaseOut(float v) { v = 1f - Mathf.Clamp01(v); return 1f - v * v * v; }
        private static float EaseIn(float v) { v = Mathf.Clamp01(v); return v * v; }
        private static Vector3 Bez(Vector3 a, Vector3 b, Vector3 c, float t)
        { float u = 1f - t; return u * u * a + 2f * u * t * b + t * t * c; }

        // ---- pose keys in the aim basis: x forward, y up, z right (hand keys in arm lengths) ----

        /// <summary>Held hand, out to the right of the halo. Beside (bolt 0): raised at the shoulder, a little
        /// back; bolting (bolt 1): drawn further back. Both rise and draw back as the charge builds.</summary>
        private static Vector3 ReadyKey(float c, float breath, float bolt)
        {
            Vector3 beside = new Vector3(-(0.21f + 0.10f * c), 0.45f + 0.04f * c, 0.66f + 0.02f * c);
            Vector3 back = new Vector3(-(0.36f + 0.12f * c), 0.40f + 0.04f * c, 0.58f);
            return Vector3.Lerp(beside, back, bolt) + new Vector3(0f, 0.015f * breath, 0f);
        }
        /// <summary>Where the draw starts: low by the right hip, where the spear forms from the palm.</summary>
        private static readonly Vector3 DrawStart = new Vector3(0.22f, -0.52f, 0.26f);
        /// <summary>Draw control point: the hand swings out and up over the shoulder.</summary>
        private static readonly Vector3 DrawOver = new Vector3(0.12f, 0.62f, 0.64f);
        /// <summary>Release: high and in front of the shoulder, close to the aim line.</summary>
        private static readonly Vector3 ReleaseKey = new Vector3(0.50f, 0.32f, 0.30f); // still right of the head from behind
        private static readonly Vector3 FollowCtrl = new Vector3(0.80f, -0.02f, 0.00f);
        /// <summary>Follow-through: down and across toward the left hip.</summary>
        private static readonly Vector3 FinishKey = new Vector3(0.30f, -0.52f, -0.40f);
        /// <summary>Ready shaft direction (Unity-local in the aim frame, z = aim): nose up, tip turned in.</summary>
        private static Vector3 ReadyDirKey(float c, float bolt)
        {
            float yaw = Mathf.Lerp(BesideYaw, BackYaw, bolt);
            float pitch = Mathf.Lerp(BesideNoseUp + 6f * c, BackNoseUp + 4f * c, bolt);
            Quaternion q = Quaternion.AngleAxis(-yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.right);
            return q * Vector3.forward;
        }
        /// <summary>Off hand (left basis, z is still body-right): reaching toward the target while loaded.</summary>
        /// <summary>Off hand held out in front toward the target (Arc Bolt leaves from it), a little to the left so
        /// it shows beside the body from behind; kick = recoil from a bolt.</summary>
        private static Vector3 GuideKey(float breath, float kick)
        { return new Vector3(0.76f - 0.16f * kick, 0.17f + 0.012f * breath + 0.07f * kick, -0.08f); }
        private static readonly Vector3 GuidePull = new Vector3(-0.12f, -0.44f, -0.16f);

        private static Vector3 ToWorld(Vector3 k, Vector3 fwd, Vector3 up, Vector3 right)
        { return fwd * k.x + up * k.y + right * k.z; }

        /// <summary>Javelin pose, driven by IK on R upperarm / R forearm (grip socket placed, hand turned so
        /// the shaft points where it should), the chest (twist and lean) and the off arm. See the class note.</summary>
        internal void TickAim(float now, float dt)
        {
            if (!upperarm || !forearm || !hand || !socket || !Tip || !Contact || !body || !body.inputBank) return;
            bool inThrow = !holding && throwStarted > 0f && now - throwStarted < ThrowWindow;
            float since = inThrow ? now - throwStarted : 0f;
            bool want = holding || (inThrow && since < FollowEnd);
            float step = float.IsNaN(dt) || float.IsInfinity(dt) ? 0f : Mathf.Clamp(dt, 0f, 0.1f);
            poseW = Mathf.MoveTowards(poseW, want ? 1f : 0f, step / (want ? BlendIn : BlendOut));
            // The guide arm yields to an Arc Bolt gesture playing on it.
            bool leftFree = !(armPose && (left ? armPose.RightCasting : armPose.LeftCasting));
            boltW = Mathf.MoveTowards(boltW, holding && now - lastBolt < 0.7f ? 1f : 0f, step / 0.25f);
            guideKick = Mathf.Max(0f, guideKick - step / 0.14f);
            guideW = Mathf.MoveTowards(guideW, leftFree ? 1f : 0f, step / 0.12f);
            if (poseW <= 0.001f) { poseW = 0f; return; }
            Vector3 aim = body.inputBank.aimDirection;
            if (aim.sqrMagnitude < 0.0001f || float.IsNaN(aim.sqrMagnitude) || float.IsInfinity(aim.sqrMagnitude)) return;
            aim.Normalize();
            Vector3 up = Vector3.up;
            Vector3 flat = Vector3.ProjectOnPlane(aim, up);
            if (flat.sqrMagnitude < 0.0004f) flat = Vector3.ProjectOnPlane(body.characterDirection ? body.characterDirection.forward : model.forward, up);
            flat.Normalize();
            // "right" is the spear side: every key below is authored for a right-handed throw and
            // mirrors across the body by flipping this axis (and the shaft keys' x, and the twist).
            Vector3 right = Vector3.Cross(up, flat) * side;
            Vector3 aimRight = Vector3.Cross(up, aim).normalized;
            Vector3 aimUp = Vector3.Cross(aim, aimRight).normalized;
            float w = Ease(poseW);

            float c = holding ? (charge ? charge.Charge01 : 0f) : (charge ? charge.LastReleaseCharge : 0f);
            Quaternion u0 = upperarm.localRotation, f0 = forearm.localRotation, h0 = hand.localRotation;
            Quaternion c0 = chest ? chest.localRotation : Quaternion.identity;
            savedUpper = u0; savedFore = f0; savedHand = h0; savedChest = c0; applied = true;

            float breath = Mathf.Sin(now * 5.2f);
            float rel = ReleaseTime;
            Vector3 key, dirKey, guideKey;
            float twist, lean, guideAmount;
            Vector3 pole;
            Vector3 readyPole = (up * 0.2f + right * 1.1f - flat * 0.1f).normalized;

            if (!inThrow)
            {
                // Draw: hip -> out and up over the shoulder -> cocked; the wind-up deepens with the charge.
                float draw = Ease((now - holdStarted) / DrawTime);
                key = Bez(DrawStart, DrawOver, ReadyKey(c, breath, boltW), draw);
                Vector3 startDir = Quaternion.AngleAxis(35f, Vector3.right) * Vector3.forward; // carried low, tip down-forward
                dirKey = Vector3.Slerp(startDir, ReadyDirKey(c, boltW), draw);
                dirKey = Quaternion.AngleAxis(-1.2f * breath, Vector3.right) * dirKey;
                // v0.9.14: from the rear camera the head sits a little to the left of the core, so the
                // mirrored pose read ~20 px closer to it; the left hold sits a touch wider, tip turned in less.
                if (left) { key.z += 0.05f * draw; dirKey = Quaternion.AngleAxis(4f * draw, Vector3.up) * dirKey; }
                twist = (8f + 10f * c + 6f * boltW) * draw;
                lean = -(3f + 5f * c) * draw - 0.6f * breath;
                guideKey = GuideKey(breath, guideKick);
                guideAmount = Ease((now - holdStarted) / (DrawTime * 1.4f));
                Vector3 drawPole = (-up * 0.5f + right * 1f + flat * 0.2f).normalized;
                pole = Vector3.Slerp(drawPole, readyPole, draw);
                lastKey = key; lastDirKey = dirKey; lastTwist = twist; lastLean = lean; lastGuideKey = guideKey;
            }
            else if (since < rel)
            {
                // Whip: hand accelerates up and over to the release point; the spear swings onto the aim.
                float p = since / rel;
                float a = EaseIn(p);
                Vector3 over = new Vector3(fromKey.x * 0.35f + 0.05f, 0.78f, 0.50f);
                key = Bez(fromKey, over, ReleaseKey, a);
                dirKey = Vector3.Slerp(fromDirKey, Vector3.forward, Mathf.Pow(p, 1.4f));
                // Torso leads the arm: it is already most of the way round by the release.
                float t01 = EaseOut(since / 0.14f);
                twist = Mathf.Lerp(fromTwist, -22f, t01);
                lean = Mathf.Lerp(fromLean, 12f, t01);
                guideKey = Vector3.Lerp(fromGuideKey, GuidePull, EaseOut(since / 0.14f));
                guideAmount = 1f;
                Vector3 elbowHigh = (up * 0.35f + right * 0.8f + flat * 0.45f).normalized;
                pole = Vector3.Slerp(readyPole, elbowHigh, a);
            }
            else
            {
                // Follow-through: decelerating sweep down and across the body, then the pose blends out.
                float p = Mathf.Clamp01((since - rel) / (FollowEnd - rel));
                float b = EaseOut(p);
                key = Bez(ReleaseKey, FollowCtrl, FinishKey, b);
                Vector3 finishDir = Quaternion.AngleAxis(55f, Vector3.right) * Vector3.forward;
                dirKey = Vector3.Slerp(Vector3.forward, finishDir, b);
                float t01 = EaseOut(since / 0.14f);
                float settle = Ease((since - 0.14f) / 0.40f);
                twist = Mathf.Lerp(Mathf.Lerp(fromTwist, -22f, t01), 0f, settle);
                lean = Mathf.Lerp(Mathf.Lerp(fromLean, 12f, t01), 0f, settle);
                guideKey = Vector3.Lerp(fromGuideKey, GuidePull, EaseOut(since / 0.14f));
                guideAmount = 1f - Ease((since - 0.22f) / 0.30f);
                Vector3 elbowHigh = (up * 0.35f + right * 0.8f + flat * 0.45f).normalized;
                Vector3 elbowDown = (-up + right * 0.4f - flat * 0.2f).normalized;
                pole = Vector3.Slerp(elbowHigh, elbowDown, b);
                if (!kicked) { kicked = true; FoundationMotionPose.Kick(body, -(70f + 70f * c)); }
            }

            // Torso: twist about up (spear shoulder back while loaded), lean about the body right axis.
            if (chest) chest.rotation = Quaternion.AngleAxis(twist * side * w, up) * Quaternion.AngleAxis(lean * side * w, right) * chest.rotation;

            Vector3 S = upperarm.position;
            float L = Vector3.Distance(upperarm.position, forearm.position) + Vector3.Distance(forearm.position, hand.position);
            if (!loggedRig)
            {
                loggedRig = true;
                Plugin.Log.LogInfo("SPEAR_RIG L=" + L.ToString("0.000") + " upper=" + Vector3.Distance(upperarm.position, forearm.position).ToString("0.000")
                    + " tipDist=" + Vector3.Distance(Tip.position, socket.position).ToString("0.000") + " contactDist=" + Vector3.Distance(Contact.position, socket.position).ToString("0.000")
                    + " tipFwdDot=" + Vector3.Dot((Tip.position - Contact.position).normalized, flat).ToString("0.00"));
            }
            float tremble = holding && charge && charge.Full ? 0.012f * L : 0f;
            Vector3 jitter = tremble > 0f ? (right * (Mathf.PerlinNoise(now * 41f, 1.3f) - 0.5f) + up * (Mathf.PerlinNoise(now * 37f, 5.1f) - 0.5f)) * (2f * tremble) : Vector3.zero;
            Vector3 target = S + ToWorld(key, flat, up, right) * L + jitter;
            // Shaft direction keys are Unity-local in the aim frame (z = aim) so they follow the crosshair pitch.
            dirKey.x *= side;
            Vector3 shaftDir = (Quaternion.LookRotation(aim, aimUp) * dirKey).normalized;

            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = socket.position - hand.position;
                // Never ask the wrist for more than 0.86 of the reach: the elbow always keeps a bend
                // (about 55 deg minimum), whatever the aim pitch does to the shoulder.
                Vector3 wrist = S + Vector3.ClampMagnitude(target - offset - S, 0.86f * L);
                SolveArm(upperarm, forearm, hand, wrist, pole);
                Vector3 shaft = (Tip.position - Contact.position).normalized;
                hand.rotation = Quaternion.FromToRotation(shaft, shaftDir) * hand.rotation;
            }

            if (w < 0.999f)
            {
                upperarm.localRotation = Quaternion.Slerp(u0, upperarm.localRotation, w);
                forearm.localRotation = Quaternion.Slerp(f0, forearm.localRotation, w);
                hand.localRotation = Quaternion.Slerp(h0, hand.localRotation, w);
                if (chest) chest.localRotation = Quaternion.Slerp(c0, chest.localRotation, w);
            }

            // Off hand: reaches toward the target while loaded, snaps back to the hip on the throw.
            // Gives way to Arc Bolt whenever the left arm is casting.
            float wl = w * Ease(guideW) * guideAmount * 0.95f;
            if (wl > 0.001f && leftUpper && leftFore && leftHand)
            {
                Quaternion lu0 = leftUpper.localRotation, lf0 = leftFore.localRotation, lh0 = leftHand.localRotation;
                savedLU = lu0; savedLF = lf0; savedLH = lh0; appliedLeft = true;
                Vector3 SL = leftUpper.position;
                float LL = Vector3.Distance(leftUpper.position, leftFore.position) + Vector3.Distance(leftFore.position, leftHand.position);
                Vector3 lt = SL + ToWorld(guideKey, flat, up, right) * LL;
                Vector3 lpole = (-up * 0.7f - right * 0.7f - flat * 0.1f).normalized;
                SolveArm(leftUpper, leftFore, leftHand, lt, lpole);
                leftUpper.localRotation = Quaternion.Slerp(lu0, leftUpper.localRotation, wl);
                leftFore.localRotation = Quaternion.Slerp(lf0, leftFore.localRotation, wl);
                leftHand.localRotation = lh0;
            }
            if (left) ApplyGrip(w, step);
        }

        /// <summary>Left hand only: closes the fingers on the shaft while the spear is in the hand (the
        /// right hand gets this from the carry clips). Curl direction is read from the animated bend.</summary>
        private void ApplyGrip(float w, float step)
        {
            gripW = Mathf.MoveTowards(gripW, Gripping && HandVisible ? 1f : 0f, step / 0.08f);
            float g = Ease(gripW);
            if (g <= 0.001f || !hand) return;
            Transform index = gripFingers[0], little = gripFingers[9], mid = gripFingers[3] ? gripFingers[3] : index;
            if (!index || !little || !mid) return;
            Vector3 handDir = (mid.position - hand.position).normalized;
            Vector3 across = Vector3.ProjectOnPlane(index.position - little.position, handDir);
            if (across.sqrMagnitude < 1e-8f) return;
            across.Normalize();
            // Curl toward the palm side the spear sits on: the sign that moves the middle finger's tip
            // toward the shaft (the grip socket lies on the palm side of the hand).
            Vector3 toShaft = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(socket.position - hand.position, handDir), across);
            Vector3 swing = Vector3.Cross(across, (gripFingers[4] ? gripFingers[4].position : mid.position + handDir * 0.03f) - mid.position);
            if (toShaft.sqrMagnitude > 1e-8f && swing.sqrMagnitude > 1e-10f)
            {
                float sign = Mathf.Sign(Vector3.Dot(swing, toShaft));
                if (!loggedGrip) { loggedGrip = true; Plugin.Log.LogInfo("SPEAR_GRIP curl sign " + sign); }
                gripCurlSign = sign;
            }
            Vector3 axis = across * gripCurlSign;
            for (int i = 0; i < gripFingers.Length; i++) if (gripFingers[i]) savedGrip[i] = gripFingers[i].localRotation;
            appliedGrip = true;
            for (int f = 0; f < 4; f++)
                for (int s = 0; s < 3; s++)
                {
                    var seg = gripFingers[f * 3 + s];
                    if (seg) seg.rotation = Quaternion.AngleAxis(GripCurl[s] * g * (1f - 0.06f * f), axis) * seg.rotation;
                }
        }

        private static void SolveArm(Transform a, Transform b, Transform h, Vector3 target, Vector3 pole)
        {
            Vector3 A = a.position, B = b.position, C = h.position;
            float l1 = (B - A).magnitude, l2 = (C - B).magnitude;
            Vector3 toTarget = target - A;
            float d = toTarget.magnitude;
            if (d < 1e-4f || l1 < 1e-4f || l2 < 1e-4f) return;
            float dc = Mathf.Clamp(d, Mathf.Abs(l1 - l2) + 0.01f, l1 + l2 - 0.005f);
            Vector3 dir = toTarget / d;
            float along = (l1 * l1 - l2 * l2 + dc * dc) / (2f * dc);
            float height = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - along * along));
            Vector3 perp = Vector3.ProjectOnPlane(pole, dir);
            if (perp.sqrMagnitude < 1e-5f) perp = Vector3.ProjectOnPlane(Vector3.down, dir);
            perp.Normalize();
            Vector3 elbow = A + dir * along + perp * height;
            a.rotation = Quaternion.FromToRotation(B - A, elbow - A) * a.rotation;
            Vector3 elbowNow = b.position;
            b.rotation = Quaternion.FromToRotation(h.position - elbowNow, (A + dir * dc) - elbowNow) * b.rotation;
        }

        private void Clear()
        {
            if (holding || throwStarted > 0f) KitAnim.Stop(body, KitAnim.SpearCarryLayer, 0.1f);
            holding = false; throwStarted = -10f; poseW = 0f;
            HandVisible = false;
            if (visual) visual.SetActive(false);
        }

        /// <summary>On death a spear that was being charged falls from the hand as a short-lived prop.</summary>
        private void DropOnDeath()
        {
            if (dropped || !wasCharging || !visual || !visual.activeSelf) return;
            dropped = true;
            wasCharging = false;
            var prop = Instantiate(visual, visual.transform.position, visual.transform.rotation);
            foreach (var behaviour in prop.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(behaviour);
            prop.layer = LayerIndex.debris.intVal;
            var bounds = new Bounds(prop.transform.position, Vector3.zero);
            foreach (var r in prop.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            var col = prop.AddComponent<BoxCollider>();
            col.center = prop.transform.InverseTransformPoint(bounds.center);
            col.size = prop.transform.InverseTransformVector(bounds.size);
            col.size = new Vector3(Mathf.Abs(col.size.x), Mathf.Abs(col.size.y), Mathf.Abs(col.size.z));
            var rb = prop.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.velocity = body.characterMotor ? body.characterMotor.velocity * 0.5f : Vector3.zero;
            rb.angularVelocity = Random.insideUnitSphere * 2f;
            Destroy(prop, 6f);
        }

        private void OnDisable() { Clear(); if (visual) Destroy(visual); }
        private void OnDestroy() { if (visual) Destroy(visual); }
    }
}
