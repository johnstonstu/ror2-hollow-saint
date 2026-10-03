using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // Drives the real GameFoundation14 controller through the crown scenarios and measures the
    // four ring arcs against their rest pose. Animator-only: no game code, no rendering.
    public static class HaloStateCheck
    {
        private static readonly string[] Arcs = { "halo 1", "halo 2", "halo 3", "halo 4" };
        private const float Dt = 1f / 60f;
        private static readonly StringBuilder log = new StringBuilder();
        private static int failures;

        public static void RunBatch()
        {
            string path = Path.GetFullPath("../artifacts/halo-states01/report.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                Scenario("full crown 8 s", new[] { (9.3f, true), (2f, false) }, expectOpenAt: 1.3f);
                Scenario("stun before unfold (0.15 s)", new[] { (0.15f, true), (2f, false) }, expectOpenAt: -1f);
                Scenario("interrupt mid-unfold (0.6 s)", new[] { (0.6f, true), (2f, false) }, expectOpenAt: -1f);
                Scenario("recast while closing", new[] { (9.3f, true), (0.3f, false), (9.3f, true), (2f, false) }, expectOpenAt: 1.3f);
                Scenario("crown expires at hold phase 0.25", new[] { (1.208f + 0.25f, true), (2f, false) }, expectOpenAt: -1f);
                Scenario("crown expires at hold phase 0.5", new[] { (1.208f + 0.5f, true), (2f, false) }, expectOpenAt: -1f);
                Scenario("crown expires at hold phase 0.75", new[] { (1.208f + 0.75f, true), (2f, false) }, expectOpenAt: -1f);
            }
            catch (Exception e) { failures++; log.AppendLine("EXCEPTION " + e); }
            File.WriteAllText(path, (failures == 0 ? "ALL PASS\n" : "FAILURES " + failures + "\n") + log);
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        private static void Scenario(string name, (float seconds, bool open)[] steps, float expectOpenAt)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FoundationBundleBuilder14.Folder + "mdlHollowSaint.prefab");
            var go = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var animator = go.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var all = go.GetComponentsInChildren<Transform>(true);
                var arcs = Arcs.Select(n => all.First(t => t.name == n)).ToArray();
                var rest = arcs.Select(t => t.localPosition).ToArray();
                var restRot = arcs.Select(t => t.localRotation).ToArray();
                animator.Update(0f);
                float time = 0f, maxStep = 0f, maxOpen = 0f, worstStepAt = 0f;
                var last = arcs.Select(t => t.position).ToArray();
                foreach (var step in steps)
                {
                    animator.SetBool("crownOpen", step.open);
                    for (float t = 0f; t < step.seconds; t += Dt)
                    {
                        animator.Update(Dt); time += Dt;
                        for (int i = 0; i < arcs.Length; i++)
                        {
                            float moved = (arcs[i].position - last[i]).magnitude;
                            if (moved > maxStep) { maxStep = moved; worstStepAt = time; }
                            last[i] = arcs[i].position;
                            maxOpen = Mathf.Max(maxOpen, (arcs[i].localPosition - rest[i]).magnitude);
                        }
                    }
                }
                float endPos = 0f, endRot = 0f;
                for (int i = 0; i < arcs.Length; i++)
                {
                    endPos = Mathf.Max(endPos, (arcs[i].localPosition - rest[i]).magnitude);
                    endRot = Mathf.Max(endRot, Quaternion.Angle(arcs[i].localRotation, restRot[i]));
                }
                var info = animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Halo"));
                bool restOk = endPos < 0.001f && endRot < 0.5f;
                bool emptyOk = info.IsName("Halo rest");
                // The authored unfold/close clips peak at about 3.1 cm per 60 Hz frame; the old instant
                // switch added ~7 cm in one frame. Allow 25% over the authored peak.
                bool smooth = maxStep < 0.039f;
                // A fold-back must never open the ring wider than it already was.
                if (name.StartsWith("stun") && maxOpen > 0.3f) smooth = false;
                bool pass = restOk && emptyOk && smooth;
                if (!pass) failures++;
                log.AppendLine((pass ? "PASS " : "FAIL ") + name + ": end offset " + (endPos * 1000f).ToString("0.00") + " mm / " + endRot.ToString("0.00") +
                    " deg, final state " + (emptyOk ? "rest" : "not rest") + ", max open " + (maxOpen * 100f).ToString("0.0") + " cm, largest 60 Hz step " +
                    (maxStep * 100f).ToString("0.00") + " cm at " + worstStepAt.ToString("0.00") + " s");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
