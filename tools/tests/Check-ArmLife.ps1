# Success: reactive arm-life springs stay bounded under frame hitches and max intensity,
# settle within ~1 s after a stop, overshoot once, lag down the chain, and map turns,
# landings and casts the right way (FoundationArmLifeMath.cs, no Unity dependency).
# Windows PowerShell 5 compatible (Add-Type compiles C# 5).
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationArmLifeMath.cs'))
$checks = @'
namespace HollowSaint
{
    public static class ArmLifeChecks
    {
        private static void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
        private static float Abs(float v) { return System.Math.Abs(v); }

        private static float MaxAbs(ArmChain c)
        {
            float m = 0f;
            for (int i = 0; i < ArmChain.Levels; i++)
            {
                if (float.IsNaN(c.Swing[i].X) || float.IsNaN(c.Spread[i].X)) return float.NaN;
                m = System.Math.Max(m, System.Math.Max(Abs(c.Swing[i].X), Abs(c.Spread[i].X)));
            }
            return m;
        }

        private static float MaxLocal(ArmChain c, float follow)
        {
            float m = 0f;
            for (int i = 0; i < ArmChain.Levels; i++)
                m = System.Math.Max(m, System.Math.Max(Abs(c.LocalSwing(i, follow)), Abs(c.LocalSpread(i, follow))));
            return m;
        }

        private static void Drive(ArmChain c, ArmMotion m, float side, float reaction, float seconds, float dt)
        {
            for (float t = 0f; t < seconds; t += dt)
            {
                float s, p;
                FoundationArmLifeMath.Targets(m, side, reaction, reaction, out s, out p);
                FoundationArmLifeMath.StepChain(c, s, p, dt);
            }
        }

        public static string Run()
        {
            var report = new System.Text.StringBuilder();
            const float frame = 1f / 60f;

            // 1. Hitch stability: max intensity (2 x 2), violent random input, 0.25 s spikes.
            var rng = new System.Random(7);
            var chain = new ArmChain();
            float worst = 0f, worstLocal = 0f;
            for (int i = 0; i < 4000; i++)
            {
                var m = new ArmMotion();
                m.ForwardAccel = (float)(rng.NextDouble() * 400 - 200);
                m.RightAccel = (float)(rng.NextDouble() * 400 - 200);
                m.YawRate = (float)(rng.NextDouble() * 4000 - 2000);
                m.Speed = (float)(rng.NextDouble() * 40);
                m.Glide = (float)rng.NextDouble();
                m.Airborne = rng.NextDouble() < 0.5;
                m.VerticalSpeed = (float)(rng.NextDouble() * 80 - 40);
                float dt = i % 7 == 0 ? 0.25f : i % 13 == 0 ? 0.0005f : frame;
                float s, p;
                FoundationArmLifeMath.Targets(m, i % 2 == 0 ? -1f : 1f, 4f, 4f, out s, out p);
                FoundationArmLifeMath.StepChain(chain, s, p, dt);
                if (i % 50 == 0) FoundationArmLifeMath.Kick(chain, 5000f, FoundationArmLifeMath.LandingKick(999f, 4f));
                float a = MaxAbs(chain);
                Check(!float.IsNaN(a), "NaN in chain");
                worst = System.Math.Max(worst, a);
                worstLocal = System.Math.Max(worstLocal, MaxLocal(chain, 2f));
            }
            Check(worst <= FoundationArmLifeMath.HardLimit + 0.001f, "Chain exceeded hard limit: " + worst);
            Check(worstLocal <= 14.001f, "Local joint angle exceeded 14 deg: " + worstLocal);
            FoundationArmLifeMath.StepChain(chain, float.NaN, 0f, float.NaN);
            Check(!float.IsNaN(MaxAbs(chain)), "NaN input poisons chain");
            report.AppendLine("  hitch/max-intensity: worst spring " + worst.ToString("0.0") + " deg, worst local " + worstLocal.ToString("0.0") + " deg");

            // 2. Default intensity sprint then hard stop: bounded, then rest within 1 s.
            chain = new ArmChain();
            var run = new ArmMotion(); run.Speed = 12f; run.Glide = 1f;
            Drive(chain, run, -1f, 1f, 1.5f, frame);
            float runSwing = chain.Swing[0].X;
            var stop = new ArmMotion(); stop.ForwardAccel = -30f; stop.Speed = 4f;
            float peak = 0f;
            for (float t = 0f; t < 0.3f; t += frame)
            {
                float s, p;
                FoundationArmLifeMath.Targets(stop, -1f, 1f, 1f, out s, out p);
                FoundationArmLifeMath.StepChain(chain, s, p, frame);
                peak = System.Math.Max(peak, MaxLocal(chain, 1f));
            }
            float settle = -1f, t2 = 0f;
            for (; t2 < 2f; t2 += frame)
            {
                FoundationArmLifeMath.StepChain(chain, 0f, 0f, frame);
                peak = System.Math.Max(peak, MaxLocal(chain, 1f));
                if (settle < 0f && MaxAbs(chain) < 0.3f) settle = t2;
                if (settle >= 0f && MaxAbs(chain) >= 0.3f) settle = -1f;
            }
            Check(runSwing < -5f && runSwing > -9f, "Glide trailing should be modest (-5..-9 deg): " + runSwing);
            Check(peak <= 12f, "Stop reaction (largest joint angle) too large at default: " + peak);
            Check(settle >= 0f && settle <= 1.0f, "Did not settle within 1 s of the stop: " + settle);
            report.AppendLine("  glide trail " + runSwing.ToString("0.0") + " deg; stop peak " + peak.ToString("0.0") + " deg; settles in " + settle.ToString("0.00") + " s");

            // 3. Step response: one small overshoot, then settles; chain lags level by level.
            chain = new ArmChain();
            float[] half = { -1f, -1f, -1f, -1f };
            float over = 0f; int crossings = 0; float prevErr = -10f;
            for (float t = 0f; t < 1.5f; t += frame)
            {
                FoundationArmLifeMath.StepChain(chain, 10f, 0f, frame);
                over = System.Math.Max(over, chain.Swing[0].X - 10f);
                float err = chain.Swing[0].X - 10f;
                if (System.Math.Abs(err) > 0.2f && prevErr * err < 0f) crossings++;
                if (System.Math.Abs(err) > 0.2f) prevErr = err;
                for (int l = 0; l < 4; l++) if (half[l] < 0f && chain.Swing[l].X >= 5f) half[l] = t;
            }
            Check(over > 0.5f && over < 3f, "Upper arm overshoot should be a small settle (0.5-3 deg): " + over);
            Check(crossings <= 2, "More than one settle wobble: " + crossings);
            Check(half[0] < half[1] && half[1] < half[2] && half[2] < half[3], "Follow-through order shoulder > elbow > wrist > fingers");
            report.AppendLine("  step 10 deg: overshoot " + over.ToString("0.0") + " deg; 50% at " + (half[0] * 1000).ToString("0") + "/" + (half[1] * 1000).ToString("0") + "/" + (half[2] * 1000).ToString("0") + "/" + (half[3] * 1000).ToString("0") + " ms");

            // 4. Directions: right turn drags the left arm back and flings it out; right side
            //    slip puts the left arm out and the right arm in; airborne lifts.
            float ls, lp, rs, rp;
            var turn = new ArmMotion(); turn.YawRate = 300f;
            FoundationArmLifeMath.Targets(turn, -1f, 1f, 1f, out ls, out lp);
            FoundationArmLifeMath.Targets(turn, 1f, 1f, 1f, out rs, out rp);
            Check(ls < 0f && rs > 0f && lp > 0f && rp > 0f, "Turn drag direction");
            var slip = new ArmMotion(); slip.RightAccel = 10f;
            FoundationArmLifeMath.Targets(slip, -1f, 1f, 1f, out ls, out lp);
            FoundationArmLifeMath.Targets(slip, 1f, 1f, 1f, out rs, out rp);
            Check(lp > 0f && rp < 0f, "Side acceleration trails the arms opposite");
            var surge = new ArmMotion(); surge.ForwardAccel = 20f;
            FoundationArmLifeMath.Targets(surge, 1f, 1f, 1f, out rs, out rp);
            Check(rs < 0f, "Forward acceleration trails the arms back");
            var air = new ArmMotion(); air.Airborne = true; air.VerticalSpeed = 8f;
            FoundationArmLifeMath.Targets(air, 1f, 1f, 1f, out rs, out rp);
            Check(rp > 3f && rp < 7f, "Airborne lift 3-7 deg: " + rp);
            FoundationArmLifeMath.Targets(air, 1f, 1f, 0f, out rs, out rp);
            Check(rp == 0f && rs == 0f, "Air slider 0 removes lift");

            // 5. Landing dip scales with fall speed, bounded, recovers within 1 s.
            float dipSoft = LandingDip(6f), dipHard = LandingDip(25f), dipCap = LandingDip(200f);
            Check(dipSoft < 0f && dipHard < dipSoft, "Harder landing dips further");
            Check(dipHard > -10f && dipCap >= dipHard - 0.01f, "Landing dip bounded: " + dipHard);
            report.AppendLine("  landing dip: 6 m/s " + dipSoft.ToString("0.0") + " deg, 25 m/s " + dipHard.ToString("0.0") + " deg");

            // 6. Casting arm drops to 25% fast and is back within ~0.2 s after.
            float w = 1f;
            for (int i = 0; i < 6; i++) w = FoundationArmLifeMath.CastWeight(w, true, frame);
            Check(System.Math.Abs(w - 0.25f) < 0.001f, "Cast share 25% within 0.1 s: " + w);
            float back = 0f;
            while (w < 0.999f) { w = FoundationArmLifeMath.CastWeight(w, false, frame); back += frame; }
            Check(back > 0.15f && back < 0.22f, "Cast blend-in ~0.2 s: " + back);
            Check(FoundationArmLifeMath.CastWeight(1f, true, 5f) >= 0.25f, "Cast weight hitch safe");
            Check(FoundationArmLifeMath.CastingArm("Arc Bolt left") == -1 && FoundationArmLifeMath.CastingArm("Arc Bolt right") == 1 &&
                FoundationArmLifeMath.CastingArm("Conduit Spear") == 1 && FoundationArmLifeMath.CastingArm("Open Circuit arms") == 0, "Casting arm map");

            // 7. Idle sway: calm and bounded.
            float maxSway = 0f, minSway = 0f;
            for (float t = 0f; t < 60f; t += 0.05f)
            {
                float s, p;
                FoundationArmLifeMath.Sway(t, 3.1f, 1f, out s, out p);
                maxSway = System.Math.Max(maxSway, System.Math.Max(s, p));
                minSway = System.Math.Min(minSway, System.Math.Min(s, p));
            }
            Check(maxSway > 1f && maxSway <= 1.61f && minSway < -1f, "Idle sway 1-1.6 deg: " + maxSway);
            Check(FoundationArmLifeMath.ClampDt(0.25f) == FoundationArmLifeMath.MaxStep && FoundationArmLifeMath.ClampDt(-1f) == 0f, "dt clamp");

            return "ARM_LIFE_PASS: hitch stability, bounded max intensity, stop settle, overshoot, chain lag, directions, landing, cast share, sway." +
                System.Environment.NewLine + report.ToString();
        }

        private static float LandingDip(float fall)
        {
            var c = new ArmChain();
            FoundationArmLifeMath.Kick(c, 0f, FoundationArmLifeMath.LandingKick(fall, 1f));
            float low = 0f;
            for (float t = 0f; t < 1f; t += 1f / 60f)
            {
                FoundationArmLifeMath.StepChain(c, 0f, 0f, 1f / 60f);
                low = System.Math.Min(low, c.Spread[0].X);
            }
            Check(System.Math.Abs(c.Spread[0].X) < 0.3f, "Landing did not settle in 1 s");
            return low;
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $checks)
[HollowSaint.ArmLifeChecks]::Run()
