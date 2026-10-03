using System;
using UnityEngine;

namespace HollowSaint.PreviewValidation
{
    // Multiple Camera.Render calls in one editor frame can reuse GPU skinning
    // from the first pose. BakeMesh evaluates the current bones on the CPU.
    // These reusable static renderers show that exact pose and its property blocks.
    internal sealed class PosedMeshSnapshot : IDisposable
    {
        private readonly SkinnedMeshRenderer[] sources;
        private readonly MeshRenderer[] renderers;
        private readonly Mesh[] meshes;
        private readonly bool[] enabled;
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

        internal PosedMeshSnapshot(GameObject model)
        {
            sources = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            renderers = new MeshRenderer[sources.Length]; meshes = new Mesh[sources.Length]; enabled = new bool[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                var child = new GameObject("Posed snapshot " + sources[i].name);
                child.transform.SetParent(sources[i].transform, false);
                meshes[i] = new Mesh { name = "Snapshot " + sources[i].name };
                child.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                renderers[i] = child.AddComponent<MeshRenderer>();
                renderers[i].shadowCastingMode = sources[i].shadowCastingMode;
                renderers[i].receiveShadows = sources[i].receiveShadows;
                renderers[i].enabled = false;
            }
        }

        internal void Show()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                enabled[i] = sources[i].enabled;
                sources[i].BakeMesh(meshes[i]);
                renderers[i].sharedMaterials = sources[i].sharedMaterials;
                sources[i].GetPropertyBlock(properties); renderers[i].SetPropertyBlock(properties);
                renderers[i].enabled = enabled[i]; sources[i].enabled = false;
            }
        }

        internal void Hide()
        {
            for (int i = 0; i < sources.Length; i++) { sources[i].enabled = enabled[i]; renderers[i].enabled = false; }
        }

        public void Dispose()
        {
            Hide();
            for (int i = 0; i < sources.Length; i++)
            { UnityEngine.Object.DestroyImmediate(renderers[i].gameObject); UnityEngine.Object.DestroyImmediate(meshes[i]); }
        }
    }
}
