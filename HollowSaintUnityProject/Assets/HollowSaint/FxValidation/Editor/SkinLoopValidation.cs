using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.SpearDischarge;
using UnityEngine;

namespace HollowSaint.PreviewValidation
{
    // Success: cached material classifications survive 5,000 native ticks, and
    // emission values remain live. Calibrate allocation accounting first; a Mono
    // counter returning zero for known allocations is unsupported, not a pass.
    public static class SkinLoopValidation
    {
        private static readonly string Output = Path.GetFullPath("../artifacts/skin-loop01");
        public static void RunBatch()
        {
            try
            {
                Directory.CreateDirectory(Output);
                long controlBefore = GC.GetAllocatedBytesForCurrentThread();
                var control = new byte[65536];
                long controlBytes = GC.GetAllocatedBytesForCurrentThread() - controlBefore;
                GC.KeepAlive(control);
                File.WriteAllText(Output + "/counter-control.txt", "knownAllocationBytes=65536\nreportedAllocationBytes=" + controlBytes + "\n");
                bool counterSupported = controlBytes >= 65536;
                if (!counterSupported) Debug.LogWarning("SKIN_LOOP native Mono allocation counter failed known-allocation control; managed allocation totals are unavailable");
                OpenCircuitBuff.Def = KitContent.MakeBuff("Circuit", Color.white, false, false, true);
                ConduitSpearAnchor.OutBuff = KitContent.MakeBuff("Out", Color.white, false, false, true);
                using (var f = new CurrentFlowValidation.Fixture(3))
                {
                    f.Mode("crown"); f.Tick(1f / 60f);
                    var field = typeof(FoundationSkinAnimation).GetField("targets", BindingFlags.Instance | BindingFlags.NonPublic);
                    var targets = ((IEnumerable)field.GetValue(f.skinLights)).Cast<object>().ToArray();
                    object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
                    var cached = targets.Select(t => (Material)Get(t, "Material")).ToArray();
                    var names = new string[100]; for (int i = 0; i < names.Length; i++) names[i] = cached[0].name;
                    int distinctNames = names.Where((name, index) => !names.Take(index).Any(prior => ReferenceEquals(prior, name))).Count();
                    for (int i = 0; i < 100; i++) f.skinLights.Tick(i / 60f, 1f / 60f);
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    for (int i = 0; i < 5000; i++) f.skinLights.Tick(i / 60f, 1f / 60f);
                    long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                    for (int i = 0; i < targets.Length; i++)
                    {
                        if (!ReferenceEquals(Get(targets[i], "Material"), cached[i])) throw new InvalidOperationException("Stable material cache was replaced");
                        int index = (int)Get(targets[i], "Index");
                        if (!ReferenceEquals(cached[i], f.cm.baseRendererInfos[index].defaultMaterial)) throw new InvalidOperationException("Cache differs from active skin material");
                    }
                    // The optimization caches classification only, never emission.
                    var first = f.cm.baseRendererInfos[(int)Get(targets[0], "Index")];
                    Color original = first.defaultMaterial.GetColor("_EmissionColor"), changed = original * 0.5f;
                    var block = new MaterialPropertyBlock();
                    f.skinLights.Tick(10f, 0f); first.renderer.GetPropertyBlock(block);
                    Color expected = block.GetColor("_EmissionColor") * 0.5f;
                    first.defaultMaterial.SetColor("_EmissionColor", changed); f.skinLights.Tick(10f, 0f);
                    first.renderer.GetPropertyBlock(block);
                    Color actual = block.GetColor("_EmissionColor");
                    if (Mathf.Abs(actual.r - expected.r) + Mathf.Abs(actual.g - expected.g) + Mathf.Abs(actual.b - expected.b) > 0.00001f) throw new InvalidOperationException("Emission was incorrectly cached");
                    first.defaultMaterial.SetColor("_EmissionColor", original);
                    File.WriteAllText(Output + "/measurement.txt", "ticks=5000\nclassificationTargets=" + targets.Length + "\nnameReads=100 distinctManagedNameReferences=" + distinctNames + "\nallocationCounterSupported=" + counterSupported + "\n" + (counterSupported ? "managedBytes=" + bytes : "managedBytes=UNAVAILABLE (known 65536-byte allocation reported zero)") + "\nNative Mono direct ticks with stable Solar materials. No gameplay/FPS measurement.\n");
                    if (counterSupported && bytes > 128) throw new InvalidOperationException("Skin loop allocates " + bytes + " managed bytes / 5000 ticks");
                }
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\nClassification cache remains tied to active materials across 5000 ticks; emission values remain live. Native Mono allocation totals unavailable if calibration fails. Not gameplay performance/FPS proof.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
