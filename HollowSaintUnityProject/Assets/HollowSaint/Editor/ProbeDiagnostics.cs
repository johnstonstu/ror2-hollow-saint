using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    public static class ProbeDiagnostics
    {
        public static void Dump()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ProbeAssets.Source + "HollowSaint.fbx");
            var model = Object.Instantiate(source);
            var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name.StartsWith("HF BODY"));
            var baseline = new Mesh();
            skin.BakeMesh(baseline);
            var names = new[] { "root", "pelvis", "L muzzle", "R muzzle", "core socket", "tabard front.3" };
            using (var log = new StreamWriter(Path.Combine(ProbeSetup.Evidence, "transform-diagnostic.txt")))
            {
                log.WriteLine("SKIN " + skin.name + " bones=" + skin.bones.Length + " vertices=" + skin.sharedMesh.vertexCount + " weights=" + skin.sharedMesh.boneWeights.Length + " weighted=" + skin.sharedMesh.boneWeights.Count(w => w.weight0 > 0));
                log.WriteLine("BONES " + string.Join(",", skin.bones.Select(b => b ? b.name : "NULL")));
                foreach (var m in model.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Distinct())
                    log.WriteLine("MATERIAL " + m.name + " PATH " + AssetDatabase.GetAssetPath(m) + " COLOR " + m.color + " EMISSION " + m.GetColor("_EmissionColor") + " BASE " + (m.mainTexture ? m.mainTexture.name : "null"));
                log.WriteLine("ROOT REST: " + model.transform.localPosition + " rot " + model.transform.localEulerAngles + " scale " + model.transform.localScale);
                foreach (string title in new[] { "Idle", "Run_forward", "Arc_Bolt_right" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ProbeAssets.Generated + "Clips/" + title + ".anim");
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == ""))
                        log.WriteLine("ROOT CURVE " + title + " " + binding.type + " " + binding.propertyName);
                    foreach (float t in new[] { 0f, 0.2f })
                    {
                        clip.SampleAnimation(model, t);
                        var sampleMesh = new Mesh();
                        skin.BakeMesh(sampleMesh);
                        float maxShift = baseline.vertices.Zip(sampleMesh.vertices, (a, b) => Vector3.Distance(a, b)).Max();
                        log.WriteLine("SKIN SHIFT " + title + " " + t + " " + maxShift);
                        log.WriteLine(title + " " + t + " ROOT " + model.transform.localPosition + " rot " + model.transform.localEulerAngles + " scale " + model.transform.localScale);
                        foreach (string name in names)
                        {
                            var bone = model.GetComponentsInChildren<Transform>().Single(b => b.name == name);
                            log.WriteLine(name + " world " + bone.position.ToString("F5") + " model " + model.transform.InverseTransformPoint(bone.position).ToString("F5"));
                        }
                    }
                }
            }
        }
    }
}
