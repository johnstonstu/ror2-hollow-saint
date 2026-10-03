using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoR2;
using HollowSaint.FoundationKit.Vfx;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.PreviewValidation
{
    public static class SkinLightValidation
    {
        private static readonly string Output = Path.GetFullPath("../artifacts/skin-light01");
        private static int checks;
        private static void Require(bool ok, string reason) { checks++; if (!ok) throw new InvalidOperationException(reason); }
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        public static void RunBatch()
        {
            AssetBundle bundle = null;
            try
            {
                Directory.CreateDirectory(Output);
                bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../artifacts/foundation/bundle13/hollowsaintassets"));
                Require(bundle, "Bundle13 failed to load"); FoundationLightMaps.Load(bundle);
                var model = bundle.LoadAsset<GameObject>("mdlHollowSaint");
                var spear = bundle.LoadAsset<GameObject>("mdlConduitSpear");
                Require(model && spear, "Model or spear missing");
                Require(Enumerable.Range(0, 4).All(i => model.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r => r.sharedMesh.name == "BodySurface" + i && r.sharedMaterials.Length == 1 && r.sharedMesh.subMeshCount == 1) == 1), "Serialized body surface inventory differs from game audit");
                Geometry(); Maps(bundle);
                foreach (uint skin in new uint[] { 0, 1, 2, 3, 4 }) Skin(model, spear, skin);
                File.WriteAllText(Output + "/verification.txt", "ALL PASS\nassertions=" + checks + " skins=5\nActual bundle13, exact game material tuning/tints and skin-light animation; explicit CharacterModel/current adapters. Static submesh attributes/indices and rig/controller match model12. Native render review; not game bloom or gameplay.\n");
                bundle.Unload(true); bundle = null; EditorApplication.Exit(0);
            }
            catch (Exception error) { if (bundle) bundle.Unload(true); File.WriteAllText(Output + "/verification.txt", "FAILED\n" + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static void Geometry()
        {
            const string prefix = "Assets/HollowSaint/GameFoundation";
            var before = AssetDatabase.LoadAssetAtPath<GameObject>(prefix + "12/mdlHollowSaint.prefab");
            var after = AssetDatabase.LoadAssetAtPath<GameObject>(prefix + "13/mdlHollowSaint.prefab");
            Require(before.GetComponent<Animator>().runtimeAnimatorController == after.GetComponent<Animator>().runtimeAnimatorController, "Controller changed");
            foreach (var t in before.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(t, before.transform);
                var other = path.Length == 0 ? after.transform : after.transform.Find(path);
                Require(other && t.localPosition == other.localPosition && t.localRotation == other.localRotation && t.localScale == other.localScale, "Authored rig transform changed " + path);
                var r = t.GetComponent<SkinnedMeshRenderer>();
                if (r && r.sharedMaterials.Length == 1)
                    Require(other.GetComponent<SkinnedMeshRenderer>().sharedMesh == r.sharedMesh, "Unrelated mesh changed " + path);
            }
            var old = before.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.sharedMaterials.Length == 4);
            var source = old.sharedMesh;
            var originalVertices = source.vertices; var originalNormals = source.normals; var originalWeights = source.boneWeights; var originalTangents = source.tangents;
            int count = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                string name = slot == 0 ? old.name : "HS_BodySurface" + slot;
                var r = after.GetComponentsInChildren<SkinnedMeshRenderer>().Single(x => x.name == name);
                var mesh = r.sharedMesh; var indices = source.GetTriangles(slot); var ids = indices.Distinct().OrderBy(x => x).ToArray();
                Require(mesh.subMeshCount == 1 && r.sharedMaterials.Length == 1, "Not a single-material surface");
                Require(r.sharedMaterial == old.sharedMaterials[slot], "Surface material assignment changed");
                Require(mesh.vertexCount == ids.Length, "Unused vertices retained"); count += mesh.vertexCount;
                Require(mesh.bindposes.SequenceEqual(source.bindposes), "Bindposes changed");
                Require(r.bones.Select(b => b.name).SequenceEqual(old.bones.Select(b => b.name)), "Bone order changed");
                Require(r.localBounds == old.localBounds && mesh.bounds == source.bounds, "Bounds changed");
                var vertices = mesh.vertices; var normals = mesh.normals; var weights = mesh.boneWeights; var tangents = mesh.tangents;
                for (int i = 0; i < ids.Length; i++)
                {
                    Require(vertices[i] == originalVertices[ids[i]], "Position changed");
                    Require(normals[i] == originalNormals[ids[i]], "Normal changed");
                    Require(weights[i].Equals(originalWeights[ids[i]]), "Skin weights changed");
                    if (originalTangents.Length > 0) Require(tangents[i] == originalTangents[ids[i]], "Tangent changed");
                }
                Require(mesh.triangles.Select(i => ids[i]).SequenceEqual(indices), "Triangle order/geometry changed");
                for (int channel = 0; channel < 8; channel++)
                {
                    var a = new System.Collections.Generic.List<Vector4>(); var b = new System.Collections.Generic.List<Vector4>();
                    source.GetUVs(channel, a); mesh.GetUVs(channel, b);
                    Require(a.Count == 0 ? b.Count == 0 : b.SequenceEqual(ids.Select(i => a[i])), "UV channel changed " + channel);
                }
            }
            Require(count <= source.vertexCount * 1.05f, "Excess skinning vertex duplication");
            Require(after.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterials.Length == 1), "Remaining multi-material renderer");
            Require(after.GetComponentsInChildren<Renderer>().Length == before.GetComponentsInChildren<Renderer>().Length + 3, "Unexpected renderer inventory");
        }

        private static void Maps(AssetBundle bundle)
        {
            var mask = bundle.LoadAsset<Texture2D>("HS_BodyLightMask"); var pixels = Read(mask);
            var original = Decode("Assets/HollowSaint/Source/v31_probe03/body_base.png");
            var oldEmission = Decode("Assets/HollowSaint/Source/v31_probe03/body_emission.png");
            int lit = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                Require(Mathf.Abs(c.r - c.g) < 0.005f && Mathf.Abs(c.g - c.b) < 0.005f, "Mask retains cyan");
                Require(Mathf.Abs(c.r - oldEmission[i].maxColorComponent) < 0.005f, "Emission brightness/coverage changed");
                if (c.maxColorComponent > 0.01f) lit++;
            }
            Require(lit > 100 && lit < pixels.Length / 20, "Emission leaks across armor");
            foreach (string name in Enumerable.Range(0, 3).SelectMany(t => new[] { "HS_BodySkin" + t + "_0", "HS_BodySkin" + t + "_1" }))
            {
                var texture = bundle.LoadAsset<Texture2D>(name);
                Require(texture && texture.width == mask.width && texture.height == mask.height && texture.mipmapCount > 1, "Skin atlas missing/mismatched " + name);
                Require(!texture.isReadable, "Runtime CPU texture copy retained " + name);
                Color[] baked = Read(texture);
                int theme = name[11] - '0', plate = name[13] - '0';
                Color[,] armor = { { new Color(0.18f, 0.22f, 0.17f), new Color(0.58f, 0.4f, 0.2f) }, { new Color(0.25f, 0.21f, 0.16f), new Color(0.85f, 0.65f, 0.27f) }, { new Color(0.12f, 0.1f, 0.18f), new Color(0.07f, 0.055f, 0.12f) } };
                Color[] arc = { new Color(0.45f, 1f, 0.72f), new Color(1f, 0.72f, 0.22f), new Color(0.75f, 0.35f, 1f) };
                for (int i = 0; i < original.Length; i += 31)
                {
                    Color c = original[i];
                    if (oldEmission[i].maxColorComponent <= 1f / 255f)
                    {
                        Color expected = (c.linear * armor[theme, plate].linear).gamma; expected.a = c.a;
                        Require(Vector4.Distance(expected, baked[i]) < 0.008f, "Armor texture changed outside authored lights " + name);
                    }
                    else if (c.g > c.r * 1.25f && c.b > c.r * 1.25f && baked[i].maxColorComponent > 0.1f)
                    {
                        Color expected = (new Color(c.maxColorComponent, c.maxColorComponent, c.maxColorComponent).linear * arc[theme].linear).gamma; expected.a = c.a;
                        Require(Vector4.Distance(expected, baked[i]) < 0.008f, "Diffuse light palette/brightness mismatch " + name);
                    }
                }
            }
        }

        private static Color[] Decode(string file)
        {
            var image = new Texture2D(2, 2); image.LoadImage(File.ReadAllBytes(file)); var result = image.GetPixels(); UnityEngine.Object.DestroyImmediate(image); return result;
        }

        private static Color[] Read(Texture source)
        {
            var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; var image = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            try { Graphics.Blit(source, target); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); image.Apply(); return image.GetPixels(); }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
        }

        private static void Skin(GameObject model, GameObject spear, uint skin)
        {
            using (var f = new ArmFlowValidation.Fixture(model, spear))
            {
                f.body.skinIndex = skin; FoundationMaterials.Apply(f.model);
                var cm = f.model.AddComponent<CharacterModel>(); cm.body = f.body;
                cm.baseRendererInfos = f.model.GetComponentsInChildren<Renderer>().Select(r => {
                    Material source = r.sharedMaterial; Material material = ModelTints.Apply(source, skin);
                    if (skin == 0 || skin == 1) Require(material.GetTexture("_EmissionMap") == source.GetTexture("_EmissionMap"), "Cyan source atlas changed");
                    if (skin > 1 && source.mainTexture && source.mainTexture.name == "body_base")
                    {
                        Require(material.GetTexture("_EmissionMap").name == "HS_BodyLightMask", "Atlas emission was not neutralized");
                        Color arc = skin == 2 ? new Color(0.45f, 1f, 0.72f) : skin == 3 ? new Color(1f, 0.72f, 0.22f) : new Color(0.75f, 0.35f, 1f);
                        Require(Vector3.Distance((Vector3)(Vector4)material.GetColor("_EmissionColor"), (Vector3)(Vector4)(arc * 0.9f)) < 0.001f, "Atlas palette mismatch");
                        Require(material.color == Color.white, "Baked armor/light palette multiplied twice");
                    }
                    r.sharedMaterials = new[] { material }; // Actual game's one-material application contract.
                    return new CharacterModel.RendererInfo { renderer = r, defaultMaterial = material };
                }).ToArray();
                var current = f.root.AddComponent<BodyCurrentFx>();
                foreach (var info in cm.baseRendererInfos)
                {
                    var block = new MaterialPropertyBlock(); block.SetColor("_EmissionColor", info.defaultMaterial.GetColor("_EmissionColor"));
                    block.SetFloat("_HS_TestOtherWriter", 0.37f); info.renderer.SetPropertyBlock(block);
                }
                var lightAnimation = f.model.AddComponent<FoundationSkinAnimation>(); Invoke(lightAnimation, "Start");
                f.Pose("Idle combat", "held", 0.4f); f.Tick(1f / 60f, 10f);
                foreach (bool active in new[] { false, true, false })
                {
                    typeof(BodyCurrentFx).GetProperty("IsActive").SetValue(current, active);
                    typeof(BodyCurrentFx).GetProperty("Intensity").SetValue(current, active ? 0.8f : 0f);
                    lightAnimation.Tick(10f, 0.1f); // Settled swatch/palette check; recovery has its own frame-rate test.
                    var block = new MaterialPropertyBlock();
                    foreach (var info in cm.baseRendererInfos.Where(i => i.defaultMaterial.GetTexture("_EmissionMap") && i.defaultMaterial.IsKeywordEnabled("_EMISSION")))
                    {
                        info.renderer.GetPropertyBlock(block); Color c = block.GetColor("_EmissionColor"); Color expected = info.defaultMaterial.GetColor("_EmissionColor");
                        Require(Mathf.Abs(block.GetFloat("_HS_TestOtherWriter") - 0.37f) < 0.00001f, "Other writer's property block erased");
                        int index = Array.IndexOf(cm.baseRendererInfos, info);
                        Color pulse = expected * (1f + 1.6f * 0.8f * LightningRhythm.Pulse(10f, index * 0.035f));
                        Require(Vector4.Distance(c, active ? pulse : expected) < 0.0001f, "Atlas pulse/rest mismatch skin=" + skin);
                    }
                    if (active) Capture(f, skin);
                }
                Invoke(lightAnimation, "OnDisable");
            }
        }

        private static void Capture(ArmFlowValidation.Fixture f, uint skin)
        {
            var light = new GameObject("Skin light review key").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(30, 140, 0);
            RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.36f);
            var camera = new GameObject("Skin light review").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.05f, 0.065f, 0.08f);
            var target = new RenderTexture(640, 800, 24); camera.targetTexture = target; var image = new Texture2D(640, 800, TextureFormat.RGB24, false);
            Vector3 center = f.Bone("chest").position; Vector3 front = Vector3.ProjectOnPlane(f.Bone("core socket").position - center, Vector3.up).normalized;
            for (int rear = 0; rear < 2; rear++)
            {
                camera.transform.position = center + front * (rear == 0 ? 3.2f : -3.2f) - Vector3.Cross(Vector3.up, front) * 1.8f + Vector3.up * 0.6f; camera.transform.LookAt(center + Vector3.down * 0.15f);
                camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 640, 800), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/skin" + skin + (rear == 0 ? "-front.png" : "-rear.png"), image.EncodeToPNG()); RenderTexture.active = null;
            }
            camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(light.gameObject);
        }
        public static void AuditBatch()
        {
            AssetBundle bundle = null;
            try
            {
                Directory.CreateDirectory(Output);
                bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../artifacts/foundation/bundle" + NativeRig.Version + "/hollowsaintassets"));
                if (!bundle) throw new InvalidOperationException("Audit bundle failed to load");
                if (NativeRig.Version == "13") FoundationLightMaps.Load(bundle);
                var model = bundle.LoadAsset<GameObject>("mdlHollowSaint");
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                string[] rows = renderers.SelectMany(r => r.sharedMaterials.Where(m => m).Select(m =>
                    r.name + "\t" + m.name + "\t" + m.shader.name + "\t" + m.IsKeywordEnabled("_EMISSION") + "\t" +
                    (m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString("F4") : "none") + "\t" + (m.mainTexture ? m.mainTexture.name : "none"))).ToArray();
                File.WriteAllLines(Output + "/bundle-materials.tsv", new[] { "renderer\tmaterial\tshader\temission_enabled\temission\ttexture" }.Concat(rows));
                var lights = renderers.SelectMany(r => r.sharedMaterials).Where(m => m && m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor").maxColorComponent > 0.01f).Distinct().ToArray();
                File.WriteAllLines(Output + "/emissive-materials.tsv", new[] { "material\tbase_emission\tsolar_emission" }.Concat(lights.Select(m => m.name + "\t" + m.GetColor("_EmissionColor").ToString("F4") + "\t" + ModelTints.Apply(m, 3).GetColor("_EmissionColor").ToString("F4"))));
                Debug.Log("BUNDLE_LIGHT_AUDIT renderers=" + renderers.Length + " materials=" + renderers.SelectMany(r => r.sharedMaterials).Distinct().Count() + " lights=" + lights.Length);
                bundle.Unload(true); bundle = null; EditorApplication.Exit(0);
            }
            catch (Exception error) { if (bundle) bundle.Unload(true); File.WriteAllText(Output + "/audit-error.txt", error.ToString()); Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
