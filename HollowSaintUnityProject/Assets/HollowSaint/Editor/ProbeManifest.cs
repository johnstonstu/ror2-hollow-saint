using System;

namespace HollowSaint.Preview.Editor
{
    [Serializable] public sealed class ProbeManifest
    {
        public string source, model;
        public ProbeMaterial[] materials;
        public ProbeClip[] clips;
        public string[] bones;
        public int meshCount;
    }
    [Serializable] public sealed class ProbeMaterial
    {
        public string name, baseMap, emissionMap;
        public float[] color, emission;
        public float metallic, roughness, emissionStrength;
    }
    [Serializable] public sealed class ProbeClip
    {
        public string title, file;
        public int start, end, fps;
        public bool loop;
        public ProbeMarker[] markers;
        public ProbeCurve[] curves;
        public ProbeSample[] samples;
    }
    [Serializable] public sealed class ProbeMarker { public string name; public int frame; }
    [Serializable] public sealed class ProbeCurve { public string name; public float[] values; }
    [Serializable] public sealed class ProbeSample { public int frame; public ProbeBone[] bones; }
    [Serializable] public sealed class ProbeBone { public string name; public float[] position; }
}
