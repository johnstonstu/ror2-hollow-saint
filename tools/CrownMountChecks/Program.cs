using System;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit.Gaze.Fx;

static class Program
{
    static int checks;
    static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < .0001f;
    static void Main()
    {
        var body = new GameObject().AddComponent<CharacterBody>();
        var model = new Transform { localScale = new Vector3(1.7f, 1.7f, 1.7f) };
        var root = new Transform { name = "halo root", localPosition = new Vector3(.2f, .5f, .3f),
            localRotation = Quaternion.AngleAxis(27f, Vector3.forward), localScale = new Vector3(1.2f, .9f, 1.4f) };
        root.SetParent(model);
        body.modelLocator = new ModelLocator { modelTransform = model };
        body.Socket = new Transform(); body.Socket.SetParent(root);
        var heads = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * .5f;
            heads[i] = new Transform { name = "halo " + (i + 1),
                localPosition = new Vector3(.1f + .4f * MathF.Cos(angle), .25f, .15f + .4f * MathF.Sin(angle)) };
            heads[i].SetParent(root);
        }
        Vector3 center() => (heads[0].position + heads[1].position + heads[2].position + heads[3].position) * .25f;
        Vector3 originalPosition = root.localPosition, originalScale = root.localScale;
        Quaternion originalRotation = root.localRotation;
        var originalHeads = new Vector3[4];
        for (int i = 0; i < heads.Length; i++) originalHeads[i] = heads[i].localPosition;
        var mount = new GazeCrownMount(); mount.Begin(body);
        var target = new Vector3(3f, 8f, -4f);
        for (int i = 0; i < 1000; i++)
        {
            mount.Apply(target, Vector3.forward, 1f, true, .02f, 0f);
            float radius = Vector3.Distance(heads[0].position, center());
            mount.Apply(target, Vector3.forward, 1f, true, 0f, 1f);
            Check(Near(root.localScale, originalScale), "radial pulse preserves root scale/copper thickness");
            Check(MathF.Abs(Vector3.Distance(heads[0].position, center()) / radius - 1.6f) < .0001f, "radial arc expansion bounded to1.6");
            Check(Near(center(), target), "live ring center stays at beam origin");
            mount.Restore();
            Check(Near(heads[0].localPosition, originalHeads[0]), "restore animated arc dock");
            Check(Near(root.localScale, originalScale) && Near(root.localPosition, originalPosition), "restore original scale and position");
            Check(Quaternion.Same(root.localRotation, originalRotation), "restore original rotation");
        }
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f })
        {
            mount.Apply(target, Vector3.forward, 1f, true, .02f, invalid);
            Check(Near(root.localScale, originalScale), "invalid/negative envelope does not expand");
        }
        mount.Apply(target, Vector3.forward, 1f, true, .02f, 100f);
        Check(Near(root.localScale, originalScale), "oversized envelope cannot scale root");
        mount.Release(); mount.Release();
        Check(Near(root.localScale, originalScale) && Near(root.localPosition, originalPosition), "release is idempotent");
        mount.Begin(body); mount.Apply(target, Vector3.forward, 1f, false, .02f, 1f);
        Check(Near(root.localScale, originalScale), "windup/ending does not pulse");
        mount.Release();
        mount.Begin(body); mount.Apply(target, Vector3.forward, 1f, true, .02f, 1f);
        body.modelLocator = null; mount.Apply(target, Vector3.forward, 1f, true, .02f, 1f);
        Check(Near(root.localScale, originalScale), "missing model restores old rig");
        Console.WriteLine($"PASS {checks} production crown mount assertions; hierarchy/rotation adapters, not native Unity animation.");
    }
}
