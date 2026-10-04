using HollowSaint.FoundationKit.Gaze.Fx;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// Per-body beam contract shared by the states (fixed step) and the presentation (per frame).
    /// The beam direction is steered toward the aim at a capped turn rate on every machine; the
    /// origin is a fixed offset from the body's core along the beam, so the server (which may not
    /// animate the model) and every client agree on where the beam starts. The halo is moved to
    /// that same point (GazeCrownMount), so the visual crown is where the damage comes from.
    /// Order 140: after the pose passes (99-101), before HaloRing refits the ring (150).
    /// </summary>
    [DefaultExecutionOrder(140)]
    [DisallowMultipleComponent]
    public sealed class GazeBeam : MonoBehaviour
    {
        public enum Phase { Idle, Windup, Beam, Ending }

        private const float WindupTurnDegreesPerSecond = 540f;

        private CharacterBody body;
        private Vector3 direction = Vector3.forward;
        private Vector3 shownDirection = Vector3.forward;
        private float phaseStart;
        private float nextContactFx;
        private readonly GazeCrownMount mount = new GazeCrownMount();
        private GazeBeamFx fx;
        private GazeEmpowermentFx empowerment;

        public Phase Current { get; private set; }
        public float PhaseAge { get { return Time.time - phaseStart; } }
        public Vector3 Direction { get { return direction; } }
        public Vector3 Origin { get { return OriginFor(direction); } }
        public CharacterBody Body { get { return body; } }
        /// <summary>Server-confirmed channel lifetime including earned extensions.</summary>
        public float BeamDuration { get; private set; }
        /// <summary>Frozen level baseline; extending lifetime never shrinks growth.</summary>
        public float ProgressionDuration { get; private set; }
        /// <summary>Confirmed entry-pulse launches, capped at five for presentation.</summary>
        public int RampSteps { get; private set; }
        public void SetRampSteps(int successfulLaunches) { RampSteps = GazeRampPolicy.Steps(successfulLaunches); }

        // Cosmetic network traffic is bounded independently of attack speed / damage cadence.
        internal bool ClaimContactFxTick()
        {
            if (Time.fixedTime < nextContactFx) return false;
            nextContactFx = Time.fixedTime + 0.15f;
            return true;
        }

        public static GazeBeam For(CharacterBody body)
        {
            if (!body) return null;
            var beam = body.GetComponent<GazeBeam>();
            if (!beam) beam = body.gameObject.AddComponent<GazeBeam>();
            return beam;
        }

        public Vector3 OriginFor(Vector3 dir)
        {
            Vector3 core = body ? body.corePosition : transform.position;
            return core + Vector3.up * GazeTuning.CrownUp + dir * GazeTuning.CrownForward;
        }

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
        }

        public void Begin(Vector3 aim)
        {
            Begin(aim, GazeTuning.BeamSeconds);
        }

        public void Begin(Vector3 aim, float beamDuration)
        {
            SetRampSteps(0);
            SetProgressionDuration(beamDuration);
            SetBeamDuration(beamDuration);
            direction = aim.sqrMagnitude > 1e-4f ? aim.normalized : transform.forward;
            shownDirection = direction;
            Current = Phase.Windup;
            nextContactFx = 0f;
            phaseStart = Time.time;
            mount.Begin(body);
            Presentation(f => f.Begin(SkinFxPalette.ForBody(body)));
        }
        public void SetProgressionDuration(float duration)
        {
            if (GazeDurationPolicy.ValidSnapshot(duration)) ProgressionDuration = duration;
        }

        public void SetBeamDuration(float duration)
        {
            if (!float.IsNaN(duration) && !float.IsInfinity(duration) && duration > 0f)
                BeamDuration = duration;
        }

        /// <summary>Fixed step. Turns freely during the wind-up so the beam ignites where the
        /// player is aiming, then at TurnDegreesPerSecond.</summary>
        public void Steer(Vector3 aim, float dt, bool free)
        {
            if (aim.sqrMagnitude < 1e-4f) return;
            float rate = (free ? WindupTurnDegreesPerSecond : GazeTuning.TurnDegreesPerSecond) * Mathf.Deg2Rad * dt;
            direction = Vector3.RotateTowards(direction, aim.normalized, rate, 0f).normalized;
        }

        public void Ignite()
        {
            if (Current != Phase.Windup) return;
            Current = Phase.Beam;
            phaseStart = Time.time;
            Presentation(f => f.Ignite());
        }

        public void End()
        {
            SetRampSteps(0);
            if (Current == Phase.Idle || Current == Phase.Ending) return;
            Current = Phase.Ending;
            phaseStart = Time.time;
            Presentation(f => f.End());
        }

        private void Update()
        {
            mount.Restore();
        }

        private void LateUpdate()
        {
            if (Current == Phase.Idle) return;
            float dt = Time.deltaTime;
            float follow = 1f - Mathf.Exp(-30f * Mathf.Max(0f, dt));
            shownDirection = Vector3.Slerp(shownDirection, direction, follow).normalized;
            Vector3 origin = OriginFor(shownDirection);
            float age = PhaseAge;

            float weight;
            switch (Current)
            {
                case Phase.Windup: weight = Mathf.Clamp01(age / 0.45f); break;
                case Phase.Ending: weight = 1f - Mathf.Clamp01(age / GazeTuning.EndSeconds); break;
                default: weight = 1f; break;
            }
            try
            {
                if (!empowerment) empowerment = GetComponent<GazeEmpowermentFx>();
                float expansion = empowerment && Current == Phase.Beam ? empowerment.CrownExpansion : 0f;
                mount.Apply(origin, shownDirection, weight, Current == Phase.Beam, dt, expansion);
                if (fx == null) fx = new GazeBeamFx(this);
                fx.Render(Current, age, origin, shownDirection, mount, dt);
            }
            catch (System.Exception error)
            {
                KitLog.Event("GAZE_FX_ERROR", error.ToString());
            }

            if (Current == Phase.Ending && age >= GazeTuning.EndSeconds)
            {
                Current = Phase.Idle;
                mount.Release();
                Presentation(f => f.Stop());
            }
        }

        private void OnDisable()
        {
            SetRampSteps(0);
            if (fx != null) fx.Stop();
            mount.Release();
            Current = Phase.Idle;
        }

        private void OnDestroy()
        {
            mount.Release();
            if (fx != null) fx.Dispose();
            fx = null;
        }

        /// <summary>Presentation never breaks the skill: errors are logged once per kind and skipped.</summary>
        private void Presentation(System.Action<GazeBeamFx> action)
        {
            try
            {
                if (fx == null) fx = new GazeBeamFx(this);
                action(fx);
            }
            catch (System.Exception error)
            {
                KitLog.Event("GAZE_FX_ERROR", error.ToString());
            }
        }
    }
}
