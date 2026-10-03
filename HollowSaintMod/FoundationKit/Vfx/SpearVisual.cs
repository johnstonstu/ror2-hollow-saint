using HollowSaint.FoundationKit.SpearDischarge;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>One weapon appearance for held, flying and planted phases.</summary>
    [DefaultExecutionOrder(181)]
    public sealed class SpearVisual : MonoBehaviour
    {
        public CharacterBody Owner;
        public bool Powered;
        /// <summary>v0.9: brightness multiplier on the sheath arcs (charge, lock-in step, ready flash).</summary>
        public float Gain = 1f;
        /// <summary>v0.9.10: palette for a spear with no owner body on this machine (the lodged spear).</summary>
        public SkinFxPalette PaletteOverride;
        private Transform tip, contact;
        private readonly LightningLine[] arcs = new LightningLine[4];
        private SkinFxPalette palette;
        private readonly List<MeshRenderer> energy = new List<MeshRenderer>();
        private void Awake()
        {
            VfxAssets.Load();
            tip = SpearCarry.Find(gameObject, "SpearTip");
            contact = SpearCarry.Find(gameObject, "SpearContact");
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                // AssetDatabase names materials after their files; use stable mesh names.
                if (renderer.name.StartsWith("Pulse entering spear")) renderer.enabled = false;
                else if (renderer.name.Contains("energy") || renderer.name.Contains("lightning"))
                    energy.Add(renderer);
            }
            for (int i = 0; i < arcs.Length; i++)
            {
                var line = new GameObject("HS_SpearFeed" + i).AddComponent<LightningLine>();
                line.transform.SetParent(transform, false);
                line.loop = line.manualTick = true; line.drawTime = 0;
                line.width = 0.28f; line.jag = 0.1f; line.branches = i == 0 ? 1 : 0;
                line.coreScale = i == 0 ? 1f : 0.5f; // v0.9.1: only the main shaft arc is white-hot
                arcs[i] = line;
            }
        }
        private void LateUpdate()
        {
            if (!tip || !contact) return;
            var next = PaletteOverride ?? SkinFxPalette.ForBody(Owner);
            if (palette != next)
            {
                palette = next;
                foreach (var renderer in energy) renderer.sharedMaterial = palette.Material(VfxAssets.ArcCore);
            }
            Vector3 axis = tip.position - transform.position;
            Vector3 from = contact.position;
            var circuit = Owner ? Owner.GetComponent<BodyCurrentFx>() : null;
            float distance = circuit ? circuit.SpearDistance : 2f;
            float front = Mathf.Repeat(Time.time * LightningRhythm.Speed - distance, LightningRhythm.Speed * LightningRhythm.Period);
            float travel = front / Mathf.Max(0.001f, Vector3.Distance(from, tip.position));
            for (int i = 0; i < arcs.Length; i++)
            {
                var line = arcs[i];
                line.gameObject.SetActive(Powered);
                if (!Powered) continue;
                line.SetPalette(palette);
                if (i == 0) { line.start = from; line.end = tip.position; line.width = 0.36f * Gain * LightningRhythm.Gain(Time.time, distance); }
                else if (i == 1)
                { line.start = Vector3.Lerp(from, tip.position, travel); line.end = Vector3.Lerp(from, tip.position, Mathf.Min(1f, travel + 0.16f)); line.width = travel < 1f ? 0.7f * Gain : 0f; }
                else
                {
                    Vector3 side = transform.right * (i == 2 ? -0.13f : 0.13f);
                    line.start = transform.position + axis * 0.72f + side;
                    line.end = tip.position - axis * 0.08f;
                    line.width = 0.26f * Gain * LightningRhythm.Gain(Time.time, distance + axis.magnitude * 0.8f);
                }
                line.Tick(Time.deltaTime);
            }
        }
    }
}
