using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint.PreviewValidation
{
    // Success: native animated curl hinges do not reverse; additive finger
    // handoffs stay below 50 degrees/sec per unit of life intensity at 30/60/144
    // Hz, while authored local rotations restore exactly. A baseline crossfade
    // produces a ~0.95 degree step even at 144 Hz and fails the rate bound.
    public static class FingerFlowValidation
    {
        [Serializable] private class Event { public string state, side; public int layer, fps, frame; public float jumpDegrees, beforeSign, afterSign; }
        [Serializable] private class Report { public int sequences, frames; public float worstFlipJump; public Event[] flips; }
        [Serializable] private class Transition { public string state; public int fps; public float intensity, maximumStep; public float[] steps; public string signs, shares; }
        private static readonly string Output = Path.GetFullPath("../artifacts/finger-flow01");
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Field(object o, string n) => o.GetType().GetField(n, Private | BindingFlags.Public).GetValue(o);
        private static void Set(object o, string n, object v) => o.GetType().GetField(n, Private | BindingFlags.Public).SetValue(o, v);
        private static readonly List<Event> flips = new List<Event>();
        private static int sequences, frames, checks;
        private static void Require(bool ok, string reason) { checks++; if (!ok) throw new InvalidOperationException(reason); }
        // Quaternion.Angle rounds very small steps to zero; atan2 retains them.
        private static float Angle(Quaternion a, Quaternion b)
        {
            Quaternion q = (Quaternion.Inverse(a) * b).normalized;
            return (float)(2.0 * Math.Atan2(Math.Sqrt((double)q.x * q.x + (double)q.y * q.y + (double)q.z * q.z), Math.Abs(q.w)) * 180.0 / Math.PI);
        }

        public static void RunBatch()
        {
            try
            {
                Directory.CreateDirectory(Output); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                flips.Clear(); sequences = frames = checks = 0;
                FoundationArmPose.LifeIntensity = FoundationArmPose.ReactionIntensity = FoundationArmPose.AirIntensity = FoundationArmPose.IdleSwayIntensity = FoundationArmPose.FollowThrough = 1f;
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                using (var inventory = new ArmFlowValidation.Fixture())
                foreach (int fps in new[] { 30, 60, 144 })
                foreach (int layer in new[] { 0, 1, 2, 4 })
                foreach (string state in inventory.animator.runtimeAnimatorController.animationClips.Select(c => c.name.Replace('_', ' ')).Distinct().OrderBy(s => s))
                    if (state != "Empty" && inventory.animator.HasState(layer, Animator.StringToHash(state))) Sequence(state, layer, fps);
                var report = new Report { sequences = sequences, frames = frames, worstFlipJump = flips.Count == 0 ? 0f : flips.Max(e => e.jumpDegrees), flips = flips.ToArray() };
                File.WriteAllText(Output + "/audit.json", JsonUtility.ToJson(report, true));
                File.WriteAllText(Output + "/audit.txt", "AUDIT COMPLETE\nsequences=" + sequences + " frames=" + frames + " flips=" + flips.Count + " worstFlipJump=" + report.worstFlipJump + " deg\nActual native clips plus production procedural layers; explicit game-state adapters.\n");
                var transfers = new List<Transition>();
                foreach (int fps in new[] { 30, 60, 144 })
                foreach (float intensity in new[] { 1f, 2f })
                foreach (string state in new[] { "Empty", "Arc Bolt left", "Arc Bolt right", "Open Circuit arms hold" }) transfers.Add(Transfer(state, fps, intensity));
                File.WriteAllLines(Output + "/transfer-steps.csv", new[] { "state,fps,intensity,maximum_step_degrees,steps,signs,shares" }.Concat(transfers.Select(t => t.state + "," + t.fps + "," + t.intensity + "," + t.maximumStep.ToString("F4") + "," + string.Join(";", t.steps.Select(s => s.ToString("F4"))) + "," + t.signs + "," + t.shares)));
                Require(flips.Count == 0, "Native clips reverse the curl hinge");
                foreach (var t in transfers) Require(t.maximumStep < 50f / t.fps * t.intensity,
                    "Finger handoff exceeds rate bound: " + t.state + "/" + t.fps + "Hz/life" + t.intensity + " step=" + t.maximumStep);
                FingerFlowCapture.Run(Output);
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " clipSequences=" + sequences + " transferCases=" + transfers.Count + " frames=" + frames + "\nNative model12 Animator and exact production pose layers; explicit game adapters, not gameplay.\n");
                Debug.Log("FINGER_TRANSFER_AUDIT cases=" + transfers.Count + " maxStep=" + transfers.Max(t => t.maximumStep));
                Debug.Log("FINGER_FLOW_AUDIT sequences=" + sequences + " flips=" + flips.Count + " worst=" + report.worstFlipJump); EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static Transition Transfer(string state, int fps, float intensity)
        {
            FoundationArmPose.LifeIntensity = intensity;
            FoundationArmPose.ReactionIntensity = FoundationArmPose.AirIntensity = FoundationArmPose.IdleSwayIntensity = 0f;
            using (var f = new ArmFlowValidation.Fixture())
            {
                f.body.SetBuffCount(ConduitSpearAnchor.OutBuff.buffIndex, 1); f.spear.SetActive(false);
                var fingers = f.bones.Where(b => b.name.Contains(".") && new[] { "L index", "L middle", "L ring", "L little", "R index", "R middle", "R ring", "R little" }.Any(b.name.StartsWith)).ToArray();
                foreach (string side in new[] { "left", "right" })
                { var seeds = (float[])Field(Field(f.arms, side), "fingerSeed"); for (int i = 0; i < seeds.Length; i++) seeds[i] = 11.3f * i; }
                f.animator.Rebind(); f.animator.Update(0f); f.animator.Play("Idle combat", 0, 0f); f.animator.Update(0f);
                Quaternion[] Tick(float t)
                {
                    f.Restore(); f.animator.Update(1f / fps);
                    var authored = fingers.Select(b => b.localRotation).ToArray(); f.Tick(1f / fps, t);
                    var result = fingers.Select((b, i) => (Quaternion.Inverse(authored[i]) * b.localRotation).normalized).ToArray();
                    Require(result.All(q => !float.IsNaN(q.x + q.y + q.z + q.w)), "Invalid finger offset");
                    f.Restore();
                    Require(fingers.Select((b, i) => Angle(authored[i], b.localRotation)).All(a => a < 0.001f), "Finger layers accumulate on authored pose");
                    return result;
                }
                Quaternion[] previous = null;
                for (int frame = 0; frame < fps; frame++) previous = Tick(3f + frame / (float)fps);
                var castShares = new List<string>(); var signs = new List<string>();
                void Record()
                {
                    castShares.Add(((float)Field(Field(f.arms, "left"), "cast")).ToString("F3") + "/" + ((float)Field(Field(f.arms, "right"), "cast")).ToString("F3"));
                    signs.Add(Field(Field(f.arms, "left"), "curlSign") + "/" + Field(Field(f.arms, "right"), "curlSign"));
                }
                Record();
                f.Restore();
                f.animator.CrossFadeInFixedTime(state, 0.09f, 4);
                var steps = new List<float>();
                for (int frame = 0; frame < Mathf.CeilToInt(fps * 0.15f); frame++)
                {
                    var offsets = Tick(4f + frame / (float)fps);
                    if (frame == 0 && fps == 144 && intensity == 1f)
                    {
                        int index = Enumerable.Range(0, offsets.Length).OrderByDescending(i => Angle(previous[i], offsets[i])).First();
                        Debug.Log("FINGER_FIRST_STEP " + state + " " + fingers[index].name + " previous=" + previous[index].eulerAngles + " next=" + offsets[index].eulerAngles);
                    }
                    steps.Add(offsets.Select((q, i) => Angle(previous[i], q)).Max()); previous = offsets;
                    Record();
                }
                int mask = state == "Empty" ? 0 : state == "Arc Bolt left" ? 1 : state == "Arc Bolt right" ? 2 : 3;
                foreach (string side in new[] { "left", "right" })
                    Require(Mathf.Abs((float)Field(Field(f.arms, side), "cast") - ((mask & (side == "left" ? 1 : 2)) != 0 ? 0.25f : 1f)) < 0.001f, "Cast suppresses wrong arm or misses fade");
                f.Restore(); f.animator.CrossFadeInFixedTime("Empty", 0.05f, 4);
                for (int frame = 0; frame < Mathf.CeilToInt(fps * 0.4f); frame++) Tick(4.15f + frame / (float)fps);
                Require((float)Field(Field(f.arms, "left"), "cast") == 1f && (float)Field(Field(f.arms, "right"), "cast") == 1f, "Arm never recovers after attack");
                return new Transition { state = state, fps = fps, intensity = intensity, maximumStep = steps.Max(), steps = steps.ToArray(), signs = string.Join(";", signs), shares = string.Join(";", castShares) };
            }
        }

        private static void Sequence(string state, int layer, int fps)
        {
            using (var f = new ArmFlowValidation.Fixture())
            {
                f.body.SetBuffCount(ConduitSpearAnchor.OutBuff.buffIndex, 1); f.spear.SetActive(false);
                var arms = new[] { Field(f.arms, "left"), Field(f.arms, "right") };
                var fingers = arms.Select(a => ((Transform[,])Field(a, "fingers")).Cast<Transform>().Where(t => t).ToArray()).ToArray();
                for (int side = 0; side < 2; side++)
                { var seeds = (float[])Field(arms[side], "fingerSeed"); for (int i = 0; i < seeds.Length; i++) seeds[i] = side * 57.9f + i * 11.3f; }
                float[] lastSign = { (float)Field(arms[0], "curlSign"), (float)Field(arms[1], "curlSign") };
                Quaternion[][] previous = fingers.Select(fs => fs.Select(_ => Quaternion.identity).ToArray()).ToArray();
                f.animator.Rebind(); f.animator.Update(0f); f.animator.Play(layer == 0 ? state : "Idle combat", 0, 0f);
                if (layer != 0) f.animator.Play(state, layer, 0f); f.animator.Update(0f);
                float duration = f.animator.runtimeAnimatorController.animationClips.First(c => c.name.Replace('_', ' ') == state).length;
                int count = Mathf.CeilToInt(Mathf.Max(0.6f, duration) * fps);
                for (int frame = 0; frame < count; frame++)
                {
                    f.Restore(); f.animator.Update(1f / fps);
                    var authored = fingers.Select(fs => fs.Select(b => b.localRotation).ToArray()).ToArray();
                    f.body.characterMotor.velocity = Vector3.forward * 5f;
                    f.presentation.GroundedForPresentation = state != "Jump" && state != "Ascend" && state != "Descend";
                    f.Tick(1f / fps, frame / (float)fps);
                    for (int side = 0; side < 2; side++)
                    {
                        float sign = (float)Field(arms[side], "curlSign");
                        var offsets = fingers[side].Select((b, i) => (Quaternion.Inverse(authored[side][i]) * b.localRotation).normalized).ToArray();
                        if (frame > fps / 6 && sign != lastSign[side])
                            flips.Add(new Event { state = state, layer = layer, side = side == 0 ? "L" : "R", fps = fps, frame = frame,
                                beforeSign = lastSign[side], afterSign = sign, jumpDegrees = offsets.Select((q, i) => Angle(q, previous[side][i])).Max() });
                        lastSign[side] = sign; previous[side] = offsets;
                        Require(offsets.All(q => !float.IsNaN(q.x + q.y + q.z + q.w)), "Clip produces invalid finger rotation: " + state);
                    }
                    f.Restore();
                    Require(fingers.SelectMany((fs, side) => fs.Select((b, i) => Angle(authored[side][i], b.localRotation))).All(a => a < 0.001f), "Clip finger restoration drifts: " + state);
                    frames++;
                }
                sequences++;
            }
        }
    }
}
