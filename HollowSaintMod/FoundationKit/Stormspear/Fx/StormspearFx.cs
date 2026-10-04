using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.SpearDischarge;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear.Fx
{
    /// <summary>
    /// v0.9 Stormspear presentation driver. One per Hollow Saint body on every machine; reads
    /// StormspearCharge (never input) and owns everything that is not the hand model or the arm
    /// pose: the halo dim and its snap-back, feed tendrils into the palm, the crown spear,
    /// the point light, tick/ready/throw/crackle sounds. Lines are persistent loop lines (not
    /// counted against the one-shot budget) and are only touched while charging, so an idle body
    /// costs one null check per frame. No per-frame allocation.
    /// Runs after BodyCurrentFx (180) so the tendrils never trail the final pose.
    /// </summary>
    [DefaultExecutionOrder(185)]
    [DisallowMultipleComponent]
    public sealed class StormspearFx : MonoBehaviour
    {
        private const int Column = 0, ColumnCount = 4, Shaft = 4, Sheath = 5, Crawl = 6, Tendril = 8, TendrilCount = 6, Braid = 16, LineCount = 21;
        private const float ReleaseHold = 0.12f;     // how long the spear lingers after Released
        private const float HaloFloor = 0.3f;        // halo brightness at full hand charge

        private CharacterBody body;
        private StormspearCharge charge;
        private HaloRing ring;
        private SpearCarry carry;
        private SkinFxPalette palette;
        private readonly LightningLine[] lines = new LightningLine[LineCount];
        private readonly bool[] used = new bool[LineCount];
        private Light glowLight;
        private Transform core, chest, rUpper, rFore; // rUpper/rFore: the spear arm
        private bool armLeft;
        private float haloBrightness = 1f, haloKick, lockKick, readyFlash, cancelAt, lastBegin = -10f, nextGlowBurst;
        private bool crackling, subscribed;

        /// <summary>Halo/ring emission multiplier: 1 idle, down to 0.3 at full hand charge, a brief overshoot on release.</summary>
        public float HaloBrightness { get { return haloBrightness; } }
        /// <summary>1 on a lock-in (1/3, 2/3, full), decaying; read by the hand spear for its brightness step.</summary>
        public float LockKick { get { return lockKick; } }

        public static float HaloOf(CharacterBody who)
        {
            var fx = who ? who.GetComponent<StormspearFx>() : null;
            return fx ? fx.haloShown : 1f;
        }

        public static StormspearFx For(CharacterBody who)
        {
            if (!who) return null;
            var fx = who.GetComponent<StormspearFx>();
            return fx ? fx : who.gameObject.AddComponent<StormspearFx>();
        }

        private void Awake() { body = GetComponent<CharacterBody>(); }

        private void OnDisable()
        {
            Unsubscribe();
            StopCrackle();
            HideAll();
            ShowCrownModel(false);
            haloBrightness = 1f; haloKick = lockKick = readyFlash = 0f;
            if (glowLight) glowLight.enabled = false;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            for (int i = 0; i < lines.Length; i++) if (lines[i]) Destroy(lines[i].gameObject);
            if (glowLight) Destroy(glowLight.gameObject);
            if (crownModel) Destroy(crownModel);
        }

        private void Bind()
        {
            if (subscribed) return;
            if (!charge) charge = GetComponent<StormspearCharge>();
            if (!charge) return;
            charge.Begun += OnBegun;
            charge.Tick += OnTick;
            charge.Released += OnReleased;
            charge.Cancelled += OnCancelled;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || !charge) { subscribed = false; return; }
            charge.Begun -= OnBegun;
            charge.Tick -= OnTick;
            charge.Released -= OnReleased;
            charge.Cancelled -= OnCancelled;
            subscribed = false;
        }

        // ---------------------------------------------------------------- events

        private void OnBegun(StormspearCharge c)
        {
            if (Time.time - lastBegin < 0.25f) return; // a state swap re-enters Begin in the same frame
            lastBegin = Time.time;
            cancelAt = 0f;
            haloKick = 1f;
            lockKick = 0.6f;
            BodyCurrentFx.PulseCore(body, 0.3f);
            EnsureRing();
            // Draw: chest core flares, halo ring briefly brightens, a filament sparks in the palm.
            var pal = Palette();
            Vector3 corePos = core ? core.position : body.corePosition;
            VfxParticles.Burst(corePos, Quaternion.identity, pal.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(0.7f, 0.9f), pal.Arc);
            if (c.Form == SpearForm.Hand)
            {
                Vector3 palm = KitFx.Socket(body, SpearCarry.SpearMuzzleOf(body));
                VfxParticles.Burst(palm, Quaternion.identity, pal.Material(VfxAssets.Spark), 6, 0.2f, new Vector2(1.5f, 4f), new Vector2(0.04f, 0.09f), pal.Core, stretch: 0.06f);
                LightningLine.Spawn(palm, palm + Random.onUnitSphere * 0.3f, 0.1f, 0.25f, 0, palette: pal);
            }
            if (ring && ring.Valid) VfxParticles.Ring(ring.Shape.Center, ring.Shape.Normal, ring.Shape.Radius, ring.Shape.Radius * 1.5f, 0.22f, 0.04f, pal.Material(VfxAssets.Trail), palette: pal);
            Sfx(FanStart(), Beat.ChargeTick);
        }

        private void OnTick(StormspearCharge c, int step)
        {
            lockKick = 1f;
            BodyCurrentFx.PulseCore(body, 0.18f);
            Vector3 at = SpearCenter(c.Form);
            var pal = Palette();
            if (step < 3)
            {
                VfxParticles.Burst(at, Quaternion.identity, pal.Material(VfxAssets.Flash), 1, 0.14f, Vector2.zero, new Vector2(0.5f, 0.7f) * (0.7f + 0.3f * step), pal.Core);
                VfxParticles.Burst(at, Quaternion.identity, pal.Material(VfxAssets.Spark), 10 + 6 * step, 0.25f, new Vector2(2f, 5f), new Vector2(0.05f, 0.1f), pal.Arc, stretch: 0.06f);
                KitSfx.Play(Beat.ChargeTick, gameObject, true);
            }
            else
            {
                // Full: ready flash and sound (the existing Meter Full cue), then the crackle loop.
                readyFlash = 1f;
                // v0.9.1: a short sharp flash plus a ring along the shaft, instead of a 1.4 m white bloom.
                VfxParticles.Burst(at, Quaternion.identity, pal.Material(VfxAssets.Flash), 1, 0.16f, Vector2.zero, new Vector2(0.7f, 0.9f), pal.Arc);
                VfxParticles.Burst(at, Quaternion.identity, pal.Material(VfxAssets.Spark), 16, 0.35f, new Vector2(3f, 7f), new Vector2(0.06f, 0.12f), pal.Arc, stretch: 0.08f);
                if (c.Form == SpearForm.Hand && carry && carry.HandVisible)
                    VfxParticles.Ring(at, carry.ShaftDirection, 0.15f, 0.9f, 0.22f, 0.05f, pal.Material(VfxAssets.Trail), palette: pal);
                VfxParticles.FlashLight(at, pal.Arc, 2.2f, 7f, 0.2f);
                KitSfx.Play(Beat.MeterFull, gameObject, true);
                StartCrackle();
            }
        }

        private void OnReleased(StormspearCharge c)
        {
            cancelAt = 0f;
            StopCrackle();
            haloKick = 1f;
            lockKick = 1f;
            var pal = Palette();
            EnsureRing();
            // Halo snaps back to full with a flash; crown release also kicks the crown arcs.
            if (ring && ring.Valid)
            {
                VfxParticles.Ring(ring.Shape.Center, ring.Shape.Normal, ring.Shape.Radius, ring.Shape.Radius * 2f, 0.25f, 0.05f, pal.Material(VfxAssets.Trail), palette: pal);
                VfxParticles.FlashLight(ring.Shape.Center, pal.Arc, 3f, 7f, 0.2f);
            }
            if (c.LastReleaseForm == SpearForm.Crown)
            {
                var crown = body.GetComponent<CircuitCrownFx>();
                if (crown) crown.Kick();
                Vector3 center, dir; float len;
                CrownFrame(c.LastReleaseCharge, out center, out dir, out len);
                LightningLine.Spawn(center - dir * (len * 0.5f), center + dir * (len * 0.5f + 6f), 0.14f, 0.9f + 0.9f * c.LastReleaseCharge, 1, palette: pal);
                VfxParticles.Burst(center, Quaternion.identity, pal.Material(VfxAssets.Flash), 1, 0.18f, Vector2.zero, new Vector2(0.9f, 1.2f) * (0.7f + 0.5f * c.LastReleaseCharge), pal.Arc);
            }
            else
            {
                // v0.9.3: the hand spear leaves at the apex of the whip; the launch flash, streak and
                // throw sounds wait for it (LateUpdate fires them from the hand at that moment).
                pendingHandRelease = Time.time + Mathf.Max(0f, StormspearTuning.HandReleaseDelay);
                pendingHandCharge = c.LastReleaseCharge;
            }
            BodyCurrentFx.PulseCore(body, 0.25f);
            if (c.LastReleaseForm == SpearForm.Crown) ThrowSounds(c.LastReleaseCharge, true);
        }

        private float pendingHandRelease = -1f, pendingHandCharge;

        private void ThrowSounds(float charge01, bool crown)
        {
            KitSfx.Play(Beat.SpearThrow, gameObject, true);
            // v0.9.1: a charged or crown throw adds the sampled crackle so a 1400% throw sounds bigger than a tap.
            string heavy = KitSfx.SpearThrowHeavy;
            if (!string.IsNullOrEmpty(heavy) && (charge01 >= 0.50f || crown))
                RoR2.Util.PlaySound(heavy, gameObject);
        }

        private void HandReleaseFx(float charge01)
        {
            var pal = Palette();
            if (!carry) carry = body.GetComponent<SpearCarry>();
            bool fresh = carry && carry.SinceThrow < 0.3f;
            {
                Vector3 palm = fresh ? carry.LastGrip : KitFx.Socket(body, SpearCarry.SpearMuzzleOf(body));
                VfxParticles.Burst(palm, Quaternion.identity, pal.Material(VfxAssets.Flash), 1, 0.14f, Vector2.zero, new Vector2(0.6f, 0.8f) * (0.6f + charge01), pal.Arc);
                // Streak: a short launch flash along the throw. v0.9.1: the spear ghost's wake now
                // carries the path, so this is one thin line (the old pair was up to 3.8 wide and
                // washed out the thrower in slow-motion captures).
                Vector3 aim = body.inputBank ? body.inputBank.aimDirection : transform.forward;
                Vector3 from = fresh ? carry.LastTip : palm;
                LightningLine.Spawn(from, from + aim * 5f, 0.12f, 0.6f + 0.6f * charge01, 0, 0.02f, palette: pal);
            }
            ThrowSounds(charge01, false);
        }

        private void OnCancelled(StormspearCharge c)
        {
            StopCrackle();
            cancelAt = Time.time; // confirmed in LateUpdate: a state swap cancels and re-begins in one frame
        }

        // ---------------------------------------------------------------- frame

        private void LateUpdate()
        {
            Bind();
            if (!body || !charge) return;
            bool alive = body.healthComponent && body.healthComponent.alive;
            if (pendingHandRelease > 0f && Time.time >= pendingHandRelease)
            {
                pendingHandRelease = -1f;
                if (alive) HandReleaseFx(pendingHandCharge);
            }
            float dt = Time.deltaTime;
            lockKick = Mathf.Max(0f, lockKick - dt * 4.5f);
            readyFlash = Mathf.Max(0f, readyFlash - dt * 3f);
            haloKick = Mathf.Max(0f, haloKick - dt * 4f);
            if (cancelAt > 0f && Time.time - cancelAt > 0.06f)
            {
                cancelAt = 0f;
                if (!charge.Charging && Time.time - charge.LastReleaseTime > 0.1f) haloKick = 0.6f; // halo flashes back
            }

            bool hand = alive && charge.Charging && charge.Form == SpearForm.Hand;
            bool crown = alive && charge.Charging && charge.Form == SpearForm.Crown;
            float c01 = charge.Charge01;
            float since = Time.time - charge.LastReleaseTime;
            bool crownRelease = alive && !charge.Charging && since < ReleaseHold && charge.LastReleaseForm == SpearForm.Crown;

            // Halo: dims as the power moves into the hand, snaps back (with overshoot) on release/cancel.
            float target = hand ? 1f - (1f - HaloFloor) * c01 : 1f;
            haloBrightness = Mathf.MoveTowards(haloBrightness, target, dt * (target < haloBrightness ? 2.5f : 8f));
            float shown = haloBrightness + 0.45f * haloKick;

            if (!hand && !crown && !crownRelease)
            {
                if (glowLight && glowLight.enabled) glowLight.enabled = false;
                if (anyShown) HideAll();
                ShowCrownModel(false);
                haloShown = haloBrightness + 0.45f * haloKick;
                if (!alive) StopCrackle();
                return;
            }
            haloShown = shown;
            if (!Resolve()) return;

            System.Array.Clear(used, 0, used.Length);
            var pal = Palette();
            float now = Time.time;
            float unit = ring.Unit;
            float gain = 0.85f + 0.3f * lockKick + 0.2f * readyFlash;

            if (hand) { DrawHand(c01, gain, now, dt, pal); ShowCrownModel(false); }
            else DrawCrown(crown ? c01 : charge.LastReleaseCharge, gain, now, dt, pal, crown ? 0f : since, unit);
            Hide();
            UpdateLight(hand ? c01 : crown ? c01 : charge.LastReleaseCharge, hand ? SpearCenter(SpearForm.Hand) : SpearCenterCrown(), pal);
            anyShown = true;
        }

        private bool anyShown;
        private float haloShown = 1f;
        /// <summary>Brightness including the release overshoot (what the ring/orbs should use).</summary>
        public float HaloShown { get { return haloShown; } }

        // ---- HAND: braided arcs core -> shoulder -> raised arm -> spear, tendrils from the halo into the shaft,
        // arcs crawling along the shaft. Everything scales with the charge. The arm current is BodyCurrentFx's.
        // v0.9.1 readability: only the shaft is white-hot. Every feed line (tendrils, braids, sheath)
        // renders cyan-dominant (thin core) and stays thinner than the shaft, so at full charge the
        // spear keeps its silhouette instead of blooming into one white blob. "Full" reads as motion
        // (faster crawl, tip sparks, the pose tremble), not as more width.
        private const float FeedCore = 0.3f;
        private float nextTipSpark;

        private void DrawHand(float c01, float gain, float now, float dt, SkinFxPalette pal)
        {
            if (!ring || !ring.Valid) return;
            bool model = carry && carry.HandVisible && carry.Tip && carry.Contact;
            Vector3 palm = carry && carry.HandVisible ? carry.GripPosition : KitFx.Socket(body, SpearCarry.SpearMuzzleOf(body));
            Vector3 tail = model ? (carry.Tail ? carry.Tail.position : carry.Contact.position) : palm, tip = model ? carry.Tip.position : palm;
            Vector3 axis = tip - tail;
            float len = axis.magnitude;
            Vector3 dir = len > 1e-3f ? axis / len : (body.inputBank ? body.inputBank.aimDirection : transform.forward);
            var shape = ring.Shape;
            Vector3 corePos = core ? core.position : body.corePosition;
            bool full = charge.Full;

            // Shaft: white-hot core line with a cyan sheath, thickening with the charge.
            if (model && len > 0.05f)
            {
                float sw = (0.45f + 0.75f * c01) * gain;
                PlaceLine(Shaft, tail, tip, sw, 0, 0.03f, pal);
                PlaceLine(Sheath, tail, tip, sw * (1.0f + 0.25f * readyFlash), 1, 0.08f, pal, 0.2f);
                Vector3 lateral = Vector3.Cross(dir, Vector3.up);
                if (lateral.sqrMagnitude < 1e-3f) lateral = Vector3.right;
                lateral.Normalize();
                float speed = 1f + 1.4f * c01;
                for (int k = 0; k < 2; k++)
                {
                    float u = Mathf.Repeat(now * (1.6f + 0.5f * k) * speed + k * 0.5f, 1f);
                    float u2 = Mathf.Min(1f, u + 0.2f);
                    Vector3 side = lateral * ((k == 0 ? 1f : -1f) * 0.06f * (0.5f + c01));
                    PlaceLine(Crawl + k, tail + dir * (len * u) + side, tail + dir * (len * u2) + side, (0.35f + 0.4f * c01) * gain, 0, 0.2f, pal, 0.6f);
                }
                if (full && now >= nextTipSpark)
                {
                    nextTipSpark = now + 0.14f;
                    VfxParticles.Burst(tip, Quaternion.identity, pal.Material(VfxAssets.Spark), 3, 0.18f, new Vector2(2f, 5f), new Vector2(0.04f, 0.08f), pal.Core, stretch: 0.06f);
                }
            }

            // Tendrils: nearest ring points jump into the shaft (2 at the draw, 4 at full).
            int count = 2 + Mathf.RoundToInt(2f * c01);
            float angle = shape.AngleOf(model ? Vector3.Lerp(tail, tip, 0.5f) : palm);
            for (int i = 0; i < count; i++)
            {
                float a = angle + (i - (count - 1) * 0.5f) * 0.8f + 0.3f * Mathf.Sin(now * 3f + i * 2.1f);
                float along = (i + 0.5f) / count;
                Vector3 to = model ? Vector3.Lerp(tail, tip, Mathf.Lerp(0.1f, 0.8f, along)) : palm;
                float width = (0.3f + 0.4f * c01) * gain * LightningRhythm.Gain(now, 0.4f * i);
                PlaceLine(Tendril + i, shape.PointAt(a), to, width, 1, 0.25f, pal, FeedCore);
            }

            // Braided feed along the raised arm: core -> elbow -> grip. The direct shoulder -> spear arc
            // only jumps on a lock-in (1/3, 2/3, full) so it punctuates instead of cluttering.
            if (rUpper && rFore)
            {
                float bw = (0.35f + 0.5f * c01) * gain;
                PlaceLine(Braid, corePos, rFore.position, bw * LightningRhythm.Gain(now, 0.3f), 2, 0.22f, pal, FeedCore);
                PlaceLine(Braid + 1, rFore.position, palm, bw * 1.1f * LightningRhythm.Gain(now, 0.7f), 2, 0.2f, pal, FeedCore);
                if (lockKick > 0.25f) PlaceLine(Braid + 2, rUpper.position, model ? Vector3.Lerp(tail, tip, 0.55f) : palm, bw * lockKick, 1, 0.3f, pal, 0.5f);
            }
        }

        // ---- CROWN: current column core -> spine -> crown centre -> spear, spear above the head.
        private void DrawCrown(float c01, float gain, float now, float dt, SkinFxPalette pal, float releaseAge, float unit)
        {
            if (!ring || !ring.Valid) return;
            var shape = ring.Shape;
            Vector3 center, dir; float len;
            CrownFrame(c01, out center, out dir, out len);
            float fade = 1f;
            if (releaseAge > 0f)
            {
                // Launch: the spear leaves along the aim and thins out over the hold window.
                float k = Mathf.Clamp01(releaseAge / ReleaseHold);
                center += dir * (k * 5f);
                fade = 1f - k;
            }
            float half = len * 0.5f;
            Vector3 tail = center - dir * half, tip = center + dir * half;

            // Column: only while charging (the throw has already taken the power).
            if (releaseAge <= 0f)
            {
                Vector3 corePos = core ? core.position : body.corePosition;
                Vector3 chestPos = chest ? chest.position : corePos;
                Vector3 flat = shape.Center - corePos; flat.y = 0f;
                if (flat.sqrMagnitude < 1e-4f) flat = -transform.forward;
                flat.Normalize();
                Vector3 spine = chestPos + flat * (0.17f * unit) + Vector3.up * (0.06f * unit);
                Vector3 dock = shape.Nearest(spine);
                // v0.9.1: the column and tendrils are cyan feed (thin core, thinner than the shaft) and
                // there are fewer of them, so the spear over the crown is the brightest shape up there.
                // v0.9.12: the fitted spear model now carries the silhouette, so the feed is thinner still.
                float w = (0.35f + 0.45f * c01) * gain;
                PlaceLine(Column + 0, corePos, spine, w * LightningRhythm.Gain(now, 0.2f), 1, 0.2f, pal, FeedCore);
                PlaceLine(Column + 1, spine, dock, w * 1.1f * LightningRhythm.Gain(now, 0.6f), 1, 0.18f, pal, FeedCore);
                PlaceLine(Column + 2, dock, shape.Center, w * LightningRhythm.Gain(now, 1.0f), 1, 0.16f, pal, FeedCore);
                PlaceLine(Column + 3, shape.Center, center, w * 1.2f * LightningRhythm.Gain(now, 1.5f), 1, 0.12f, pal, FeedCore);
                // Tendrils from three of the open crown's ring segments to points along the spear.
                const int crownTendrils = 2;
                for (int i = 0; i < crownTendrils; i++)
                {
                    float along = Mathf.Lerp(-0.6f, 0.6f, (i + 0.5f) / crownTendrils);
                    float tw = (0.3f + 0.4f * c01) * gain * LightningRhythm.Gain(now, 0.3f * i);
                    PlaceLine(Tendril + i, shape.PointAt(shape.Angle[i % 4]), center + dir * (half * along), tw, 1, 0.3f, pal, FeedCore);
                }
            }

            // v0.9.12: the same fitted spear model as the hand, the flight and the lodged spear, so the
            // crown spear has a real silhouette from the side and behind (it was only lightning lines).
            PlaceCrownModel(center, dir, len, c01, gain, releaseAge <= 0f, pal);
            float width = (0.6f + 0.8f * c01) * gain * fade;
            PlaceLine(Shaft, tail, tip, width, 0, 0.03f, pal);
            PlaceLine(Sheath, tail, tip, width * (1.0f + 0.25f * readyFlash), 1, 0.08f, pal, 0.2f);
            // Two sheath arcs crawl along the shaft (cyan over the white-hot core).
            Vector3 lateral = Vector3.Cross(dir, Vector3.up);
            if (lateral.sqrMagnitude < 1e-3f) lateral = Vector3.right;
            lateral.Normalize();
            for (int k = 0; k < 2; k++)
            {
                float u = Mathf.Repeat(now * (1.4f + 0.4f * k) + k * 0.5f, 1f);
                float u2 = Mathf.Min(1f, u + 0.18f);
                Vector3 side = lateral * ((k == 0 ? 1f : -1f) * 0.09f * (0.5f + c01));
                PlaceLine(Crawl + k, tail + dir * (len * u) + side, tail + dir * (len * u2) + side, 0.6f * gain * fade * (0.4f + 0.6f * c01), 0, 0.2f, pal, 0.6f);
            }
            // Full: tip sparks and a slow, smaller glow pulse (the old 2.6 m bloom hid the spear).
            if (c01 >= 0.999f && releaseAge <= 0f && now >= nextGlowBurst)
            {
                nextGlowBurst = now + 0.6f;
                VfxParticles.Burst(center, Quaternion.identity, pal.Material(VfxAssets.Flash), 1, 0.3f, Vector2.zero, new Vector2(1.2f, 1.5f), pal.Arc);
            }
            if (c01 >= 0.999f && releaseAge <= 0f && now >= nextTipSpark)
            {
                nextTipSpark = now + 0.14f;
                VfxParticles.Burst(tip, Quaternion.identity, pal.Material(VfxAssets.Spark), 4, 0.2f, new Vector2(2f, 6f), new Vector2(0.05f, 0.1f), pal.Core, stretch: 0.06f);
            }
        }

        // ---- v0.9.12 crown spear model
        private GameObject crownModel;
        private SpearVisual crownVisual;
        private Vector3 crownAxisLocal = Vector3.forward, crownMidLocal;
        private float crownNative = 1f;

        private bool EnsureCrownModel()
        {
            if (crownModel) return true;
            if (!FoundationContent.SpearModel) return false;
            crownModel = Instantiate(FoundationContent.SpearModel);
            crownModel.name = "HS_CrownSpear";
            var tip = SpearCarry.Find(crownModel, "SpearTip");
            Transform tail = null;
            foreach (var t in crownModel.GetComponentsInChildren<Transform>(true)) if (t.name == "SpearTail") tail = t;
            if (!tail) tail = SpearCarry.Find(crownModel, "SpearContact");
            if (tip && tail)
            {
                Vector3 a = crownModel.transform.InverseTransformPoint(tail.position), b = crownModel.transform.InverseTransformPoint(tip.position);
                crownNative = Mathf.Max(0.1f, (b - a).magnitude);
                crownAxisLocal = (b - a).normalized;
                crownMidLocal = (a + b) * 0.5f;
            }
            crownVisual = crownModel.AddComponent<SpearVisual>();
            crownVisual.Owner = body;
            crownVisual.Powered = true;
            crownModel.SetActive(false);
            return true;
        }

        private void ShowCrownModel(bool show)
        {
            if (crownModel && crownModel.activeSelf != show) crownModel.SetActive(show);
        }

        private void PlaceCrownModel(Vector3 center, Vector3 dir, float len, float c01, float gain, bool charging, SkinFxPalette pal)
        {
            // On release the flying ghost takes over at once.
            if (!charging || !EnsureCrownModel()) { ShowCrownModel(false); return; }
            ShowCrownModel(true);
            float scale = len / crownNative;
            var rot = Quaternion.LookRotation(dir) * Quaternion.Inverse(Quaternion.LookRotation(crownAxisLocal));
            crownModel.transform.SetPositionAndRotation(center - rot * (crownMidLocal * scale), rot);
            crownModel.transform.localScale = Vector3.one * scale;
            crownVisual.Owner = body;
            crownVisual.Gain = gain * (0.7f + 0.9f * c01);
        }

        /// <summary>Spear frame above the head: centre, aim direction, current length.</summary>
        private void CrownFrame(float c01, out Vector3 center, out Vector3 dir, out float length)
        {
            Vector3 anchor = ring && ring.Valid ? ring.Shape.Center : KitFx.Socket(body, "Halo");
            center = anchor + Vector3.up * StormspearTuning.CrownSpearHeight;
            dir = body.inputBank ? body.inputBank.aimDirection : transform.forward;
            if (dir.sqrMagnitude < 1e-4f || float.IsNaN(dir.x)) dir = transform.forward;
            dir.Normalize();
            length = StormspearTuning.CrownSpearLength * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(c01));
        }

        private Vector3 SpearCenterCrown()
        {
            Vector3 c, d; float l;
            CrownFrame(charge.Charging ? charge.Charge01 : charge.LastReleaseCharge, out c, out d, out l);
            return c;
        }

        private Vector3 SpearCenter(SpearForm form)
        {
            if (form == SpearForm.Crown) return SpearCenterCrown();
            return carry && carry.HandVisible ? carry.Center : KitFx.Socket(body, SpearCarry.SpearMuzzleOf(body));
        }

        private void UpdateLight(float c01, Vector3 at, SkinFxPalette pal)
        {
            if (!glowLight)
            {
                var go = new GameObject("HS_StormspearLight");
                go.transform.SetParent(transform, false);
                glowLight = go.AddComponent<Light>();
                glowLight.type = LightType.Point;
                glowLight.shadows = LightShadows.None;
            }
            glowLight.enabled = true;
            glowLight.transform.position = at;
            // v0.9.1: stays the arc colour (not white) and capped, so it lights the body cyan without washing it out.
            glowLight.color = pal.Arc;
            glowLight.range = 3f + 4f * c01;
            glowLight.intensity = (0.4f + 1.5f * c01) * (0.85f + 0.3f * lockKick + 0.3f * readyFlash) * (0.9f + 0.2f * Mathf.PerlinNoise(Time.time * 30f, 0.3f));
        }

        // ---------------------------------------------------------------- helpers

        private bool Resolve()
        {
            if (!ring) ring = HaloRing.For(body);
            if (!carry) carry = body.GetComponent<SpearCarry>();
            if (!core) core = KitUtil.ResolveSocket(body, "Core");
            if (!chest) chest = KitUtil.ResolveSocket(body, "Chest");
            // v0.9.14: the feed follows the spear arm (left by default); re-resolve when the hand switches.
            if (!rUpper || !rFore || armLeft != SpearCarry.SpearInLeft(body))
            {
                armLeft = SpearCarry.SpearInLeft(body);
                rUpper = KitUtil.ResolveSocket(body, SpearCarry.SpearArmPrefixOf(body) + "upperarm");
                rFore = KitUtil.ResolveSocket(body, SpearCarry.SpearArmPrefixOf(body) + "forearm");
            }
            if (ring) ring.EnsureFitted();
            return ring && ring.Valid;
        }

        private void EnsureRing() { if (!ring) ring = HaloRing.For(body); if (ring) ring.EnsureFitted(); if (!core) core = KitUtil.ResolveSocket(body, "Core"); }

        private SkinFxPalette Palette()
        {
            var current = SkinFxPalette.ForBody(body);
            if (!ReferenceEquals(current, palette))
            {
                palette = current;
                for (int i = 0; i < lines.Length; i++) if (lines[i]) lines[i].SetPalette(palette);
            }
            return palette;
        }

        private void PlaceLine(int index, Vector3 from, Vector3 to, float width, int branches, float jag, SkinFxPalette pal, float coreScale = 1f)
        {
            if ((to - from).sqrMagnitude < 1e-6f || width <= 0.001f) return;
            var line = lines[index];
            if (!line)
            {
                line = new GameObject("HS_Stormspear" + index).AddComponent<LightningLine>();
                line.transform.SetParent(transform, false);
                line.loop = line.manualTick = true;
                line.drawTime = 0f;
                line.branches = branches;
                line.rejagInterval = 0.045f;
                line.SetPalette(pal);
                lines[index] = line;
            }
            used[index] = true;
            line.gameObject.SetActive(true);
            line.start = from; line.end = to; line.width = width; line.jag = jag; line.coreScale = coreScale;
            line.Tick(Time.deltaTime);
        }

        private void Hide()
        { for (int i = 0; i < lines.Length; i++) if (lines[i] && !used[i] && lines[i].gameObject.activeSelf) lines[i].gameObject.SetActive(false); }

        private void HideAll()
        {
            anyShown = false;
            for (int i = 0; i < lines.Length; i++) if (lines[i]) lines[i].gameObject.SetActive(false);
        }

        private void Sfx(string custom, Beat fallback)
        {
            if (!string.IsNullOrEmpty(custom)) RoR2.Util.PlaySound(custom, gameObject);
            else KitSfx.Play(fallback, gameObject, true);
        }

        private static string FanStart() { return KitSfx.FanStart; }

        private void StartCrackle()
        {
            if (crackling) return;
            string start = KitSfx.FanLoopStart;
            if (string.IsNullOrEmpty(start)) return;
            RoR2.Util.PlaySound(start, gameObject);
            crackling = true;
        }

        private void StopCrackle()
        {
            if (!crackling) return;
            crackling = false;
            string stop = KitSfx.FanLoopStop;
            if (!string.IsNullOrEmpty(stop) && gameObject) RoR2.Util.PlaySound(stop, gameObject);
        }
    }
}
