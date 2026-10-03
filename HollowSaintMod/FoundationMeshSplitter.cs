using System;
using UnityEngine;

namespace HollowSaint
{
    internal static class FoundationMeshSplitter
    {
        // CharacterModel assigns one base material per RendererInfo. Give each
        // submesh its own renderer so later material slots remain visible.
        internal static int Split(SkinnedMeshRenderer source)
        {
            var materials = source.sharedMaterials;
            var mesh = source.sharedMesh;
            if (!mesh || materials.Length != mesh.subMeshCount)
                throw new InvalidOperationException("Hollow Saint body mesh/material slots do not match");
            if (materials.Length < 2) return 0;

            for (int i = 0; i < materials.Length; i++)
            {
                if (!materials[i]) throw new InvalidOperationException("Hollow Saint body material " + i + " is missing");
                int[] triangles = mesh.GetTriangles(i);
                var partMesh = UnityEngine.Object.Instantiate(mesh);
                partMesh.name = mesh.name + " - slot " + i;
                partMesh.subMeshCount = 1;
                partMesh.SetTriangles(triangles, 0);
                partMesh.bounds = mesh.bounds;

                var part = new GameObject(source.name + " - slot " + i);
                part.layer = source.gameObject.layer;
                part.transform.SetParent(source.transform, false);
                var renderer = part.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = partMesh;
                renderer.sharedMaterial = materials[i];
                renderer.bones = source.bones;
                renderer.rootBone = source.rootBone;
                renderer.localBounds = source.localBounds;
                renderer.updateWhenOffscreen = true;
                renderer.quality = source.quality;
                renderer.shadowCastingMode = source.shadowCastingMode;
                renderer.receiveShadows = source.receiveShadows;
                renderer.lightProbeUsage = source.lightProbeUsage;
                renderer.reflectionProbeUsage = source.reflectionProbeUsage;
                renderer.probeAnchor = source.probeAnchor;
                renderer.skinnedMotionVectors = source.skinnedMotionVectors;
                renderer.enabled = source.enabled;
            }

            UnityEngine.Object.DestroyImmediate(source);
            return materials.Length;
        }
    }
}
