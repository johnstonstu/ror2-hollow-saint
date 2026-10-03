using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // Repairs skin weights on the four split body surfaces. Returns COPIES; source assets are never modified.
    public static class BodyWeightRepair
    {
        const string Prefab = "Assets/HollowSaint/GameFoundation13/mdlHollowSaint.prefab";
        const string ClipFolder = "Assets/HollowSaint/GameFoundation10r1/Clips/";
        static readonly string[] ArmKeys = { "forearm", "hand", "finger", "thumb", "index", "middle", "ring", "pinky" };

        static bool IsArmBone(string n) { n = n.ToLowerInvariant(); return ArmKeys.Any(k => n.Contains(k)); }
        static char Side(string n) { return n.Length > 1 && n[1] == ' ' && (n[0] == 'L' || n[0] == 'R') ? n[0] : '?'; }
        static string Opposite(string n) { return Side(n) == 'L' ? "R" + n.Substring(1) : Side(n) == 'R' ? "L" + n.Substring(1) : n; }

        static int Idx(Transform[] bones, string name)
        {
            for (int i = 0; i < bones.Length; i++) if (bones[i] && bones[i].name == name) return i;
            return -1;
        }

        static void Weights(BoneWeight w, out int[] idx, out float[] wt)
        {
            idx = new[] { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
            wt = new[] { w.weight0, w.weight1, w.weight2, w.weight3 };
        }

        static BoneWeight Make(int[] idx, float[] wt)
        {
            // Sort descending so the 4 strongest go in; zero-weight slots point at index 0.
            var order = Enumerable.Range(0, 4).OrderByDescending(i => wt[i]).ToArray();
            return new BoneWeight
            {
                boneIndex0 = wt[order[0]] > 0 ? idx[order[0]] : 0, weight0 = wt[order[0]],
                boneIndex1 = wt[order[1]] > 0 ? idx[order[1]] : 0, weight1 = wt[order[1]],
                boneIndex2 = wt[order[2]] > 0 ? idx[order[2]] : 0, weight2 = wt[order[2]],
                boneIndex3 = wt[order[3]] > 0 ? idx[order[3]] : 0, weight3 = wt[order[3]],
            };
        }

        static Vector3 BonePos(Mesh m, int i) { return m.bindposes[i].inverse.MultiplyPoint3x4(Vector3.zero); }

        static int DominantBone(BoneWeight w)
        {
            Weights(w, out var idx, out var wt); int best = 0;
            for (int i = 1; i < 4; i++) if (wt[i] > wt[best]) best = i;
            return idx[best];
        }

        // A strip mesh is the separate "outer forearm conductor" skinned mesh (emissive conductor material).
        static bool IsStripMesh(Mesh m) { return m.name.ToLowerInvariant().Contains("forearm conductor"); }

        static char MeshSide(Mesh m, Transform[] bones)
        {
            var v = m.vertices; var c = Vector3.zero; foreach (var p in v) c += p; c /= Mathf.Max(1, v.Length);
            int l = Idx(bones, "L forearm"), r = Idx(bones, "R forearm");
            return (c - BonePos(m, l)).sqrMagnitude < (c - BonePos(m, r)).sqrMagnitude ? 'L' : 'R';
        }

        // Diagnostic alternative (default off): copy the weights of the nearest non-strip body vertex instead of the mirrored left strip weights.
        public static bool SnapToBody = false;

        // meshes: any mix of body surfaces, the R/L "outer forearm conductor" strip meshes and wrist cuff meshes.
        // All must be skinned to `bones` (same order as their bindposes). Returns repaired copies in the same order.
        public static Mesh[] Repair(Mesh[] surfaces, Transform[] bones, StringBuilder report)
        {
            if (report == null) report = new StringBuilder();
            var copies = surfaces.Select(m => UnityEngine.Object.Instantiate(m)).ToArray();
            for (int i = 0; i < copies.Length; i++) { copies[i].name = surfaces[i].name; if (copies[i].bindposes.Length != bones.Length) throw new InvalidOperationException(surfaces[i].name + " bindposes " + copies[i].bindposes.Length + " != bones " + bones.Length); }
            var weights = copies.Select(m => m.boneWeights).ToArray();
            var verts = copies.Select(m => m.vertices).ToArray();

            var strips = new Dictionary<char, int>();
            for (int s = 0; s < copies.Length; s++) if (IsStripMesh(copies[s])) strips[MeshSide(copies[s], bones)] = s;
            report.AppendLine("strip meshes: " + string.Join(", ", strips.Select(kv => kv.Key + "=" + copies[kv.Value].name + "(" + verts[kv.Value].Length + " verts)")));

            int changed = 0, nr = 0; float maxDelta = 0; double sumDist = 0; float maxMirrorDist = 0;
            if (strips.ContainsKey('R') && strips.ContainsKey('L'))
            {
                int rs = strips['R'], ls = strips['L'];
                Vector3 cr = Vector3.zero, cl = Vector3.zero; foreach (var p in verts[rs]) cr += p; foreach (var p in verts[ls]) cl += p;
                cr /= verts[rs].Length; cl /= verts[ls].Length; var d = cl - cr;
                int axis = 0; for (int k = 1; k < 3; k++) if (Mathf.Abs(d[k]) > Mathf.Abs(d[axis])) axis = k;
                float mid = (cl[axis] + cr[axis]) * 0.5f;
                report.AppendLine("lr axis=" + "xyz"[axis] + " mirror plane=" + mid.ToString("F4") + " centroid L=" + cl + " R=" + cr);
                nr = verts[rs].Length;
                report.AppendLine("strip bone weight totals before: R[" + BoneTotals(verts[rs].Length, weights[rs], bones) + "] L[" + BoneTotals(verts[ls].Length, weights[ls], bones) + "]");
                for (int i = 0; i < nr; i++)
                {
                    if (SnapToBody)
                    {
                        int bs = -1, bj = -1; float bdd = float.MaxValue;
                        for (int m = 0; m < copies.Length; m++) { if (IsStripMesh(copies[m]) || copies[m].name.ToLowerInvariant().Contains("cuff")) continue; for (int j = 0; j < verts[m].Length; j++) { float dd = (verts[m][j] - verts[rs][i]).sqrMagnitude; if (dd < bdd) { bdd = dd; bs = m; bj = j; } } }
                        weights[rs][i] = weights[bs][bj]; changed++; continue;
                    }
                    Vector3 q = verts[rs][i]; q[axis] = 2 * mid - q[axis]; q += cl - new Vector3(axis == 0 ? 2 * mid - cr.x : cr.x, axis == 1 ? 2 * mid - cr.y : cr.y, axis == 2 ? 2 * mid - cr.z : cr.z); // mirror, then remove the residual centroid offset between the two strip meshes
                    int best = -1; float bd = float.MaxValue;
                    for (int j = 0; j < verts[ls].Length; j++) { float dd = (verts[ls][j] - q).sqrMagnitude; if (dd < bd) { bd = dd; best = j; } }
                    maxMirrorDist = Mathf.Max(maxMirrorDist, Mathf.Sqrt(bd)); sumDist += Mathf.Sqrt(bd);
                    Weights(weights[ls][best], out var li, out var lw);
                    var ni = new int[4]; var nw = new float[4];
                    for (int k = 0; k < 4; k++)
                    {
                        nw[k] = lw[k]; ni[k] = li[k];
                        if (lw[k] > 0) { int ri = Idx(bones, Opposite(bones[li[k]].name)); if (ri >= 0) ni[k] = ri; }
                    }
                    Weights(weights[rs][i], out var oi, out var ow);
                    var all = new Dictionary<int, float>();
                    for (int k = 0; k < 4; k++) { if (ow[k] > 0) all[oi[k]] = (all.TryGetValue(oi[k], out var x) ? x : 0) + ow[k]; if (nw[k] > 0) all[ni[k]] = (all.TryGetValue(ni[k], out var y) ? y : 0) - nw[k]; }
                    float md = all.Count == 0 ? 0 : all.Values.Max(Mathf.Abs);
                    if (md > 1e-5f) { changed++; maxDelta = Mathf.Max(maxDelta, md); }
                    weights[rs][i] = Make(ni, nw);
                }
            }
            else report.AppendLine("WARN: need both R and L strip meshes");
            if (strips.ContainsKey('R')) report.AppendLine("strip bone weight totals after: R[" + BoneTotals(nr, weights[strips['R']], bones) + "]");
            report.AppendLine("strip vertices changed: " + changed + " of " + nr);
            report.AppendLine("max weight delta: " + maxDelta.ToString("F4"));
            report.AppendLine("mirror match distance max=" + maxMirrorDist.ToString("F5") + " mean=" + (nr > 0 ? sumDist / nr : 0).ToString("F5"));

            // (b) audit forearm/hand/finger vertices
            int audited = 0, normalized = 0, dropped = 0, opposite = 0;
            for (int s = 0; s < copies.Length; s++)
            {
                for (int i = 0; i < weights[s].Length; i++)
                {
                    var bw = weights[s][i]; Weights(bw, out var idx, out var wt);
                    bool arm = false; for (int k = 0; k < 4; k++) if (wt[k] > 0 && bones[idx[k]] && IsArmBone(bones[idx[k]].name)) arm = true;
                    if (!arm) continue; audited++;
                    char side = '?'; float bw0 = 0;
                    for (int k = 0; k < 4; k++) if (wt[k] > bw0 && IsArmBone(bones[idx[k]].name)) { bw0 = wt[k]; side = Side(bones[idx[k]].name); }
                    bool fix = false;
                    for (int k = 0; k < 4; k++)
                    {
                        if (wt[k] <= 0) continue;
                        string n = bones[idx[k]].name;
                        if (side != '?' && IsArmBone(n) && Side(n) != '?' && Side(n) != side) { wt[k] = 0; fix = true; opposite++; }
                        else if (wt[k] < 1e-4f) { wt[k] = 0; fix = true; dropped++; }
                    }
                    float sum = wt.Sum();
                    if (Mathf.Abs(sum - 1f) > 1e-4f || fix)
                    {
                        if (sum <= 0) continue;
                        if (Mathf.Abs(sum - 1f) > 1e-4f) normalized++;
                        for (int k = 0; k < 4; k++) wt[k] /= sum;
                        weights[s][i] = Make(idx, wt);
                    }
                }
                copies[s].boneWeights = weights[s];
            }
            report.AppendLine("audit: arm-weighted vertices=" + audited + " opposite-arm weights removed=" + opposite + " tiny weights dropped=" + dropped + " vertices normalized=" + normalized);
            report.AppendLine("(Unity BoneWeight holds max 4 influences by construction.)");
            return copies;
        }

        static string BoneTotals(int n, BoneWeight[] w, Transform[] bones)
        {
            var d = new SortedDictionary<string, float>();
            for (int i = 0; i < n; i++) { Weights(w[i], out var idx, out var wt); for (int k = 0; k < 4; k++) if (wt[k] > 0) { string nm = bones[idx[k]].name; d[nm] = (d.TryGetValue(nm, out var x) ? x : 0) + wt[k]; } }
            return string.Join(", ", d.Select(kv => kv.Key + "=" + kv.Value.ToString("F1")));
        }

        static Vector3 ClosestOnTri(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a; float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;
            Vector3 bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2; if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
            Vector3 cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6; if (vb <= 0 && d2 >= 0 && d6 <= 0) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4; if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            float den = 1f / (va + vb + vc); return a + ab * (vb * den) + ac * (vc * den);
        }

        // ---------------- batch measurement ----------------
        public static void RunBatch()
        {
            string dir = Path.GetFullPath("../artifacts/body-weights01"); Directory.CreateDirectory(dir);
            var report = new StringBuilder(); GameObject go = null;
            try
            {
                var surfaces = Enumerable.Range(0, 4).Select(i => AssetDatabase.LoadAssetAtPath<Mesh>("Assets/HollowSaint/GameFoundation13/BodySurface" + i + ".asset")).ToArray();
                if (surfaces.Any(m => !m)) throw new InvalidOperationException("missing BodySurface asset");
                go = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
                var all = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var rends = surfaces.Select(m => all.First(r => r.sharedMesh == m)).ToList();
                var bones = rends[0].bones;
                var extra = all.Where(r => r.sharedMesh && !rends.Contains(r) && (IsStripMesh(r.sharedMesh) || r.name.ToLowerInvariant().Contains("forearm conductor") || r.name.ToLowerInvariant().Contains("wrist cuff"))).ToList();
                report.AppendLine("bones=" + bones.Length + " scale=" + go.transform.lossyScale);
                foreach (var r in all.Where(r => r.name.Contains("ARM") || r.name.Contains("cuff")))
                    report.AppendLine("renderer '" + r.name + "' mesh='" + r.sharedMesh.name + "' verts=" + r.sharedMesh.vertexCount + " bones=" + r.bones.Length + " sameBones=" + r.bones.SequenceEqual(bones) + " readable=" + r.sharedMesh.isReadable);
                // Mesh names for imported meshes may be generic; give strip meshes the renderer name so detection works.
                var inputRends = rends.Concat(extra).ToList();
                var inputMeshes = inputRends.Select(r => r.sharedMesh).ToArray();
                if (inputRends.Any(r => !r.bones.SequenceEqual(bones))) report.AppendLine("WARN: some renderers use a different bone array");
                var named = inputRends.Select((r, i) => { var c = UnityEngine.Object.Instantiate(inputMeshes[i]); c.name = extra.Contains(r) ? r.name : inputMeshes[i].name; return c; }).ToArray();
                var fixedMeshes = Repair(named, bones, report);
                SnapToBody = true; var snapReport = new StringBuilder(); var snapMeshes = Repair(named, bones, snapReport); SnapToBody = false;
                report.AppendLine("snap-to-body variant: " + snapReport.ToString().Split('\n').FirstOrDefault(l => l.StartsWith("strip bone weight totals after")));

                var stripR = inputRends.First(r => r.name.StartsWith("R ARM") && r.name.ToLowerInvariant().Contains("forearm conductor"));
                var stripL = inputRends.First(r => r.name.StartsWith("L ARM") && r.name.ToLowerInvariant().Contains("forearm conductor"));
                int stripRi = inputRends.IndexOf(stripR);
                foreach (var side in new[] { 'R', 'L' })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + "Arc_Bolt_" + (side == 'R' ? "right" : "left") + ".anim");
                    var strip = side == 'R' ? stripR : stripL;
                    report.AppendLine("clip " + clip.name + " len=" + clip.length + " fps=" + clip.frameRate);
                    for (int variant = 0; variant < 3; variant++)
                    {
                        if (side == 'L' && variant >= 1) break; // left side is the reference and untouched
                        strip.sharedMesh = (side == 'R' && variant == 2) ? snapMeshes[stripRi] : (side == 'R' && variant == 1) ? fixedMeshes[stripRi] : (side == 'R' ? named[stripRi] : strip.sharedMesh);
                        float worstDist = float.MaxValue, worstSigned = float.MaxValue; int worstF = -1;
                        var perFrame = new StringBuilder(); int fa = Idx(bones, side + " forearm");
                        for (int f = 4; f <= 9; f++)
                        {
                            clip.SampleAnimation(go, f / 24f);
                            var tris = new List<Vector3[]>();
                            for (int s = 0; s < 4; s++)
                            {
                                var m = new Mesh(); rends[s].BakeMesh(m); var vv = m.vertices; var bw = surfaces[s].boneWeights; var ti = surfaces[s].triangles;
                                for (int t = 0; t < ti.Length; t += 3)
                                {
                                    int c = 0; for (int k = 0; k < 3; k++) if (DominantBone(bw[ti[t + k]]) == fa) c++;
                                    if (c >= 2) tris.Add(new[] { rends[s].transform.TransformPoint(vv[ti[t]]), rends[s].transform.TransformPoint(vv[ti[t + 1]]), rends[s].transform.TransformPoint(vv[ti[t + 2]]) });
                                }
                                UnityEngine.Object.DestroyImmediate(m);
                            }
                            var sm = new Mesh(); strip.BakeMesh(sm); var sv = sm.vertices;
                            float md = float.MaxValue, ms = float.MaxValue;
                            foreach (var pv in sv)
                            {
                                var p = strip.transform.TransformPoint(pv); float bd = float.MaxValue, sg = 0;
                                foreach (var tr in tris)
                                {
                                    var q = ClosestOnTri(p, tr[0], tr[1], tr[2]); float dd = (p - q).sqrMagnitude;
                                    if (dd < bd) { bd = dd; sg = Vector3.Dot(p - q, Vector3.Cross(tr[1] - tr[0], tr[2] - tr[0]).normalized); }
                                }
                                bd = Mathf.Sqrt(bd); md = Mathf.Min(md, bd); ms = Mathf.Min(ms, sg >= 0 ? bd : -bd);
                            }
                            UnityEngine.Object.DestroyImmediate(sm);
                            perFrame.Append(" f" + f + ":d=" + (md * 1000).ToString("F2") + ",s=" + (ms * 1000).ToString("F2"));
                            if (md < worstDist) { worstDist = md; worstF = f; }
                            worstSigned = Mathf.Min(worstSigned, ms);
                        }
                        report.AppendLine("MEASURE " + side + " " + (variant == 0 ? "original" : variant == 1 ? "repaired(mirror-left)" : "alt(snap-to-body)") + ": skinRef=forearm-dominant body verts, strip verts=" + strip.sharedMesh.vertexCount + " min dist to forearm surface=" + (worstDist * 1000).ToString("F2") + "mm (frame " + worstF + ") min signed clearance (neg = sunk)=" + (worstSigned * 1000).ToString("F2") + "mm |" + perFrame);
                    }
                }
                File.WriteAllText(Path.Combine(dir, "report.txt"), report.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception e) { report.AppendLine("FAILED " + e); File.WriteAllText(Path.Combine(dir, "report.txt"), report.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
            finally { if (go) UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
