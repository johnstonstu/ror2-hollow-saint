using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>
    /// Character-select presentation: when the mannequin appears, the halo crackles to
    /// life with a charge-up sound, then idles with a slow crown of arcs and a pulsing
    /// chest core. Purely local; the display prefab has no CharacterBody.
    /// </summary>
    public sealed class DisplayFx : MonoBehaviour
    {
        private Transform halo;
        private Transform core;
        private Light coreLight;
        private LightningLine[] crown;
        private float age;
        private float crackleTimer;
        private CharacterModel model;
        private SkinFxPalette palette;

        private void OnEnable()
        {
            age = 0f;
            VfxAssets.Load();
            model = GetComponent<CharacterModel>();
            palette = SkinFxPalette.ForModel(model);
            if (!halo) halo = Find("halo socket");
            if (!core) core = Find("core socket");
            // Playtest: Play_mage_R_start was far too loud on the select screen; a short crackle instead.
            Util.PlaySound("Play_mage_m1_cast_lightning", gameObject);
            if (halo)
            {
                VfxParticles.Burst(halo.position, Quaternion.identity, palette.Material(VfxAssets.Spark), 18, 0.6f,
                    new Vector2(1.5f, 4f), new Vector2(0.03f, 0.07f), palette.Arc, stretch: 0.05f);
                VfxParticles.Burst(halo.position, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.4f,
                    Vector2.zero, new Vector2(0.6f, 0.7f), palette.Arc);
            }
            if (core && !coreLight)
            {
                var go = new GameObject("HS_DisplayCoreLight");
                go.transform.SetParent(core, false);
                coreLight = go.AddComponent<Light>();
                coreLight.type = LightType.Point;
                coreLight.range = 2.5f;
                coreLight.color = palette.Arc;
                coreLight.shadows = LightShadows.None;
            }
            if (halo && crown == null)
            {
                crown = new LightningLine[4];
                for (int i = 0; i < 4; i++)
                {
                    var line = new GameObject("HS_DisplayCrown" + i).AddComponent<LightningLine>();
                    line.transform.SetParent(transform, false);
                    line.loop = true;
                    line.width = 0.35f;
                    line.branches = 0;
                    line.jag = 0.25f;
                    crown[i] = line;
                }
            }
        }

        private Transform Find(string boneName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == boneName) return t;
            return null;
        }

        private void Update()
        {
            age += Time.deltaTime;
            var next = SkinFxPalette.ForModel(model);
            if (next != palette)
            {
                palette = next;
                if (coreLight) coreLight.color = palette.Arc;
            }
            if (coreLight) coreLight.intensity = 0.8f + 0.4f * Mathf.Sin(age * 2.2f);
            if (!halo || crown == null) return;

            // Arcs fade in over the first second, then idle gently.
            float strength = Mathf.Clamp01(age);
            float spin = age * 0.7f;
            Vector3 up = transform.up;
            Vector3 right = transform.right;
            for (int i = 0; i < crown.Length; i++)
            {
                if (!crown[i]) continue;
                crown[i].SetPalette(palette);
                float a0 = spin + i * Mathf.PI * 0.5f;
                float a1 = a0 + Mathf.PI * 0.3f;
                crown[i].width = 0.35f * strength;
                crown[i].start = halo.position + (Mathf.Cos(a0) * right + Mathf.Sin(a0) * up) * 0.55f;
                crown[i].end = halo.position + (Mathf.Cos(a1) * right + Mathf.Sin(a1) * up) * 0.55f;
            }

            crackleTimer -= Time.deltaTime;
            if (crackleTimer <= 0f)
            {
                crackleTimer = Random.Range(0.4f, 1.1f);
                LightningLine.Spawn(halo.position + Random.onUnitSphere * 0.3f,
                    halo.position + Random.onUnitSphere * 0.5f, 0.1f, 0.3f, 0, 0.3f, palette: palette);
            }
        }
    }
}
