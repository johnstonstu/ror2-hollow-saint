using System;
using HollowSaint.FoundationKit.Gaze.Fx;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    /// <summary>
    /// Presentation of the actual core-centered spherical BlastAttack, on every observer.
    /// Reads the replicated buff rather than cast events (including late joining clients).
    /// Transparent edge geometry only: no collider, damage, targeting or shielding.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class OpenCircuitDomeFx : MonoBehaviour
    {
        private CharacterBody body;
        private GameObject visualRoot;
        private Edge[] edges;
        private SkinFxPalette palette;
        private bool failed;
        private static bool warned;

        private sealed class Edge
        {
            internal OpenCircuitDomeGeometry.Point[] Unit;
            internal Vector3[] World;
            internal LineRenderer Core, Glow;
            internal bool Lower;
        }

        private void Awake() { body = GetComponent<CharacterBody>(); }
        private void OnDisable() { DestroyVisuals(); }
        private void OnDestroy() { DestroyVisuals(); }

        private void LateUpdate()
        {
            // A dedicated server needs no local renderers; hosts still render normally.
            if (NetworkServer.active && !NetworkClient.active) return;
            if (!body) { DestroyVisuals(); return; }
            bool alive = body.healthComponent && body.healthComponent.alive;
            if (!alive) { DestroyVisuals(); return; }
            bool buff = OpenCircuitBuff.Def && body.HasBuff(OpenCircuitBuff.Def);
            var model = body.modelLocator && body.modelLocator.modelTransform
                ? body.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            bool visible = !model || model.invisibilityCount <= 0;
            float radius = KitTuning.OpenCircuitRadius; // Exactly the damage radius, including live config.
            if (!OpenCircuitDomeGeometry.ShouldShow(alive, buff, visible, isActiveAndEnabled)
                || !OpenCircuitDomeGeometry.IsValidRadius(radius))
            {
                SetVisible(false);
                return;
            }
            if (failed) return;
            try
            {
                var current = SkinFxPalette.ForBody(body);
                if (edges == null) Build(current);
                if (!ReferenceEquals(current, palette)) ApplyPalette(current);
                SetVisible(true);
                UpdateEdges(body.corePosition, radius);
            }
            catch (Exception error)
            {
                failed = true;
                DestroyVisuals();
                if (!warned)
                {
                    warned = true;
                    Plugin.Log.LogWarning("HOLLOW_SAINT_OPEN_CIRCUIT_DOME_DISABLED: " + error);
                }
            }
        }

        private void Build(SkinFxPalette current)
        {
            VfxAssets.Load();
            // Reuse the owned, cached neutral clone so vertex skin hues are not
            // multiplied through ArcCore's house tint and cyan/copper remap.
            GazeContrastAssets.Load();
            if (!GazeContrastAssets.Core || !VfxAssets.ArcGlow)
                throw new InvalidOperationException("shared arc materials unavailable");
            visualRoot = new GameObject("HS_OpenCircuitDomeEdges");
            visualRoot.transform.SetParent(transform, false);
            edges = new Edge[OpenCircuitDomeGeometry.PathCount];
            for (int i = 0; i < edges.Length; i++)
            {
                var unit = OpenCircuitDomeGeometry.CreateUnitPath(i);
                var edge = new Edge { Unit = unit, World = new Vector3[unit.Length],
                    Lower = OpenCircuitDomeGeometry.IsLower(i) };
                edges[i] = edge;
                // Sparse tapered ribbons, not closed latitude/meridian wires.
                edge.Glow = MakeRenderer("Edge" + i + "Hue", unit.Length);
                edge.Core = MakeRenderer("Edge" + i + "Core", unit.Length);
            }
            ApplyPalette(current);
        }

        private LineRenderer MakeRenderer(string name, int count)
        {
            var child = new GameObject(name);
            child.transform.SetParent(visualRoot.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.positionCount = count;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.14f, 1f),
                new Keyframe(0.86f, 1f), new Keyframe(1f, 0f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void ApplyPalette(SkinFxPalette current)
        {
            palette = current;
            var glowMaterial = palette.Material(VfxAssets.ArcGlow);
            if (!glowMaterial) throw new InvalidOperationException("palette arc material unavailable");
            foreach (var edge in edges)
            {
                edge.Core.sharedMaterial = GazeContrastAssets.Core;
                edge.Glow.sharedMaterial = glowMaterial;
            }
        }

        private void UpdateEdges(Vector3 center, float radius)
        {
            // A restrained current traveling through edges, never expansion of the volume.
            float time = Time.time;
            var origin = new OpenCircuitDomeGeometry.Point(center.x, center.y, center.z);
            for (int path = 0; path < edges.Length; path++)
            {
                var edge = edges[path];
                OpenCircuitDomeGeometry.WriteUnitPath(path, time, edge.Unit);
                for (int i = 0; i < edge.Unit.Length; i++)
                {
                    var p = OpenCircuitDomeGeometry.ScaleTranslate(edge.Unit[i], origin, radius);
                    edge.World[i] = new Vector3(p.X, p.Y, p.Z);
                }
                edge.Core.SetPositions(edge.World);
                edge.Glow.SetPositions(edge.World);
                float current = 0.88f + 0.12f * Mathf.Sin(time * 2f + path * 0.9f);
                float strength = edge.Lower ? 0.18f : path < 4 ? 0.85f : 0.48f;
                Color coreColor = palette.Arc; coreColor.a = 0.48f;
                SetStyle(edge.Core, 0.018f, coreColor, strength * current);
                SetStyle(edge.Glow, 0.065f, new Color(0.4f, 0.4f, 0.4f, 0.32f), strength * current);
            }
        }

        private static void SetStyle(LineRenderer line, float width, Color color, float strength)
        {
            // Keep the tapered widthCurve; startWidth/endWidth would rewrite its endpoints.
            line.widthMultiplier = width;
            color *= strength;
            line.startColor = line.endColor = color;
        }

        private void SetVisible(bool visible)
        {
            if (edges == null) return;
            foreach (var edge in edges)
            {
                if (edge == null) continue;
                if (edge.Core) edge.Core.enabled = visible;
                if (edge.Glow) edge.Glow.enabled = visible;
            }
        }

        private void DestroyVisuals()
        {
            SetVisible(false);
            if (visualRoot) { visualRoot.SetActive(false); Destroy(visualRoot); }
            visualRoot = null;
            edges = null;
            palette = null;
        }
    }
}
