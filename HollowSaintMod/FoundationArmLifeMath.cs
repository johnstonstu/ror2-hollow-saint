using System;

namespace HollowSaint
{
    // No Unity dependency (tested by tools/tests/Check-ArmLife.ps1): the spring chain and
    // motion-to-angle mapping behind FoundationArmPose's reactive arm layer.
    // All angles are degrees, all rates per second. C# 5 only (Add-Type in PowerShell 5).

    /// <summary>One damped spring channel: position X (deg) and velocity V (deg/s).</summary>
    internal struct ArmSpring
    {
        public float X, V;
    }

    /// <summary>Body-frame motion sampled once per frame by FoundationArmPose.</summary>
    internal struct ArmMotion
    {
        /// <summary>m/s^2 along the heading (+ = speeding up forward).</summary>
        public float ForwardAccel;
        /// <summary>m/s^2 toward body right.</summary>
        public float RightAccel;
        /// <summary>deg/s, + = turning right (clockwise seen from above).</summary>
        public float YawRate;
        /// <summary>Planar speed, m/s.</summary>
        public float Speed;
        /// <summary>0..1 sprint glide pose weight.</summary>
        public float Glide;
        public bool Airborne;
        /// <summary>m/s, + = up.</summary>
        public float VerticalSpeed;
    }

    /// <summary>Per-arm follow-through chain. Level 0 upper arm, 1 forearm, 2 hand, 3 fingers.
    /// Each spring holds that level's world trailing angle and chases the level above it,
    /// so every joint starts a little later and settles a little later than its parent.</summary>
    internal sealed class ArmChain
    {
        public const int Levels = 4;
        /// <summary>Forward (+) / back (-) swing of the limb, per level.</summary>
        public readonly ArmSpring[] Swing = new ArmSpring[Levels];
        /// <summary>Outward-and-up (+) / inward-down (-) spread of the limb, per level.</summary>
        public readonly ArmSpring[] Spread = new ArmSpring[Levels];

        public void Reset()
        {
            for (int i = 0; i < Levels; i++) { Swing[i] = new ArmSpring(); Spread[i] = new ArmSpring(); }
        }

        /// <summary>Rotation the bone at this level adds on top of its parent (deg).</summary>
        public float LocalSwing(int level, float followThrough)
        {
            float v = level == 0 ? Swing[0].X : (Swing[level].X - Swing[level - 1].X) * followThrough;
            return FoundationArmLifeMath.Clamp(v,
                -FoundationArmLifeMath.LocalLimit(level), FoundationArmLifeMath.LocalLimit(level));
        }

        public float LocalSpread(int level, float followThrough)
        {
            float v = level == 0 ? Spread[0].X : (Spread[level].X - Spread[level - 1].X) * followThrough;
            return FoundationArmLifeMath.Clamp(v,
                -FoundationArmLifeMath.LocalLimit(level), FoundationArmLifeMath.LocalLimit(level));
        }
    }

    internal static class FoundationArmLifeMath
    {
        /// <summary>Frame hitch guard: the springs never integrate more than this per frame.</summary>
        internal const float MaxStep = 0.1f;
        internal const float SubStep = 1f / 120f;
        /// <summary>Spring speed cap (deg/s).</summary>
        internal const float MaxVelocity = 400f;
        /// <summary>Soft limit of the reactive target on the upper arm (deg).</summary>
        internal const float TargetLimit = 12f;
        /// <summary>Hard limit of any spring position (deg).</summary>
        internal const float HardLimit = 18f;
        internal const float AccelClamp = 30f;
        internal const float YawClamp = 540f;

        // Level 0..3: frequency (Hz), damping ratio, and how far each follower reaches past
        // its parent at rest (1.25 = forearm trails 25% further than the upper arm).
        private static readonly float[] Hz = { 1.8f, 2.8f, 3.4f, 4.4f };
        private static readonly float[] Zeta = { 0.5f, 0.5f, 0.45f, 0.4f };
        private static readonly float[] Reach = { 1f, 1.15f, 1.1f, 1.05f };
        private static readonly float[] Local = { 14f, 8f, 8f, 6f };

        // Response gains at intensity 1.
        internal const float AccelSwing = 0.35f;     // deg per m/s^2, trails opposite surge
        internal const float AccelSpread = 0.3f;     // deg per m/s^2, trails opposite side slip
        internal const float YawSwing = 0.012f;      // deg per deg/s, arms drag behind the turn
        internal const float YawFling = 3f;          // deg outward at full yaw rate
        internal const float SpeedTrail = 4f;        // deg back at full run
        internal const float GlideTrail = 3f;        // extra deg back while gliding
        internal const float AirLift = 4f;           // deg up when airborne
        internal const float AirLiftFast = 2f;       // extra deg up at 15 m/s vertical
        internal const float AirSwing = -1.5f;       // arms drift slightly back in the air
        internal const float LandKickPerMps = 7f;    // deg/s downward kick per m/s fall speed
        internal const float LandMaxFall = 25f;
        internal const float TakeoffKickPerMps = 3f; // deg/s downward kick per m/s launch speed
        internal const float TakeoffMaxSpeed = 20f;
        internal const float SwayDegrees = 1.6f;     // idle swing amplitude (spread uses 75%)

        // Casting: the casting arm drops to this share of the reactive layer.
        internal const float CastShare = 0.25f;
        internal const float CastFadeOut = 0.08f;
        internal const float CastFadeIn = 0.2f;

        internal static float LocalLimit(int level) { return Local[level]; }

        internal static float Clamp(float v, float lo, float hi) { return v < lo ? lo : v > hi ? hi : v; }
        internal static float Clamp01(float v) { return Clamp(v, 0f, 1f); }

        /// <summary>Hitch-safe step length: 0 for bad input, never above MaxStep.</summary>
        internal static float ClampDt(float dt)
        {
            if (float.IsNaN(dt) || dt <= 0f) return 0f;
            return dt > MaxStep ? MaxStep : dt;
        }

        /// <summary>Frame-rate independent low-pass toward target.</summary>
        internal static float Filter(float current, float target, float rate, float dt)
        {
            return current + (target - current) * (1f - (float)Math.Exp(-rate * dt));
        }

        /// <summary>Smooth saturation: linear near zero, approaches +-limit.</summary>
        internal static float Soft(float x, float limit)
        {
            return limit * (float)Math.Tanh(x / limit);
        }

        /// <summary>Semi-implicit damped spring, sub-stepped at 120 Hz, dt hitch-clamped,
        /// velocity and position capped. NaN input resets the channel.</summary>
        internal static void StepSpring(ref ArmSpring s, float target, float hz, float zeta, float dt)
        {
            dt = ClampDt(dt);
            if (float.IsNaN(target) || float.IsInfinity(target)) target = 0f;
            if (float.IsNaN(s.X) || float.IsNaN(s.V)) { s.X = 0f; s.V = 0f; }
            float w = 2f * (float)Math.PI * hz;
            float k = w * w, c = 2f * zeta * w;
            while (dt > 0f)
            {
                float h = dt > SubStep ? SubStep : dt;
                dt -= h;
                s.V += (k * (target - s.X) - c * s.V) * h;
                s.V = Clamp(s.V, -MaxVelocity, MaxVelocity);
                s.X += s.V * h;
                if (s.X > HardLimit) { s.X = HardLimit; if (s.V > 0f) s.V = 0f; }
                else if (s.X < -HardLimit) { s.X = -HardLimit; if (s.V < 0f) s.V = 0f; }
            }
        }

        /// <summary>Reactive targets for the upper arm. side is -1 for the arm on the body's
        /// left, +1 on the right. reaction scales ground motion, air scales airborne lift.</summary>
        internal static void Targets(ArmMotion m, float side, float reaction, float air,
            out float swing, out float spread)
        {
            float fa = Clamp(m.ForwardAccel, -AccelClamp, AccelClamp);
            float ra = Clamp(m.RightAccel, -AccelClamp, AccelClamp);
            float yaw = Clamp(m.YawRate, -YawClamp, YawClamp);
            float run = Clamp01((m.Speed - 2f) / 10f);
            float s = -AccelSwing * fa + side * YawSwing * yaw - SpeedTrail * run - GlideTrail * Clamp01(m.Glide);
            float p = -side * AccelSpread * ra + YawFling * Math.Abs(yaw) / YawClamp;
            s *= reaction;
            p *= reaction;
            if (m.Airborne)
            {
                p += (AirLift + AirLiftFast * Clamp01(Math.Abs(m.VerticalSpeed) / 15f)) * air;
                s += AirSwing * air;
            }
            swing = Soft(s, TargetLimit);
            spread = Soft(p, TargetLimit);
        }

        /// <summary>Calm idle sway from incommensurate sines (seeded per arm). amount scales it.</summary>
        internal static void Sway(float time, float seed, float amount, out float swing, out float spread)
        {
            double t = time;
            double a = 0.6 * Math.Sin(2.0 * Math.PI * t / 5.3 + seed) + 0.4 * Math.Sin(2.0 * Math.PI * t / 3.1 + 1.7 * seed);
            double b = 0.6 * Math.Sin(2.0 * Math.PI * t / 6.7 + 2.3 * seed) + 0.4 * Math.Sin(2.0 * Math.PI * t / 4.3 + 0.9 * seed);
            swing = (float)(a * SwayDegrees * amount);
            spread = (float)(b * SwayDegrees * 0.75 * amount);
        }

        /// <summary>Steps the whole chain toward the upper-arm targets.</summary>
        internal static void StepChain(ArmChain c, float swingTarget, float spreadTarget, float dt)
        {
            StepSpring(ref c.Swing[0], swingTarget, Hz[0], Zeta[0], dt);
            StepSpring(ref c.Spread[0], spreadTarget, Hz[0], Zeta[0], dt);
            for (int i = 1; i < ArmChain.Levels; i++)
            {
                StepSpring(ref c.Swing[i], c.Swing[i - 1].X * Reach[i], Hz[i], Zeta[i], dt);
                StepSpring(ref c.Spread[i], c.Spread[i - 1].X * Reach[i], Hz[i], Zeta[i], dt);
            }
        }

        /// <summary>Velocity impulse on the upper arm (deg/s); followers pick it up late.</summary>
        internal static void Kick(ArmChain c, float swingVelocity, float spreadVelocity)
        {
            c.Swing[0].V = Clamp(c.Swing[0].V + swingVelocity, -MaxVelocity, MaxVelocity);
            c.Spread[0].V = Clamp(c.Spread[0].V + spreadVelocity, -MaxVelocity, MaxVelocity);
        }

        /// <summary>Downward spread kick (deg/s, negative) for a landing at fallSpeed m/s.</summary>
        internal static float LandingKick(float fallSpeed, float amount)
        {
            return -LandKickPerMps * Clamp(fallSpeed, 0f, LandMaxFall) * amount;
        }

        /// <summary>Downward spread kick (deg/s, negative) for a jump launching at upSpeed m/s.</summary>
        internal static float TakeoffKick(float upSpeed, float amount)
        {
            return -TakeoffKickPerMps * Clamp(upSpeed, 0f, TakeoffMaxSpeed) * amount;
        }

        /// <summary>Casting arm share of the reactive layer: down to CastShare quickly while
        /// the arm casts, back to 1 over CastFadeIn after.</summary>
        internal static float CastWeight(float current, bool casting, float dt)
        {
            dt = ClampDt(dt);
            if (casting) return Math.Max(CastShare, current - dt * (1f - CastShare) / CastFadeOut);
            return Math.Min(1f, current + dt * (1f - CastShare) / CastFadeIn);
        }

        /// <summary>Which arm a gesture state casts with: -1 left, +1 right, 0 both/unknown.</summary>
        internal static int CastingArm(string state)
        {
            switch (state)
            {
                case "Arc Bolt left": return -1;
                case "Arc Bolt right":
                case "Conduit Spear":
                case "Conduit Spear recover": return 1;
                default: return 0;
            }
        }

        internal static readonly string[] OneHandedGestures = { "Arc Bolt left", "Arc Bolt right", "Conduit Spear", "Conduit Spear recover" };
    }
}
