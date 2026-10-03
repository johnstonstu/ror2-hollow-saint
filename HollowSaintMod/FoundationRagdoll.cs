using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.8: a RoR2 RagdollController for the Saint. The cloned Commando DeathState calls
    /// BeginRagdoll on the model; without one the body froze in its last pose until removed.
    /// Bones get kinematic rigidbodies and disabled capsules on the ragdoll layer (RagdollController
    /// disables them at Start) and character joints to their parent bone.
    /// </summary>
    internal static class FoundationRagdoll
    {
        // bone, child it points at (null = sphere), radius, mass, swing limit
        private static readonly (string bone, string child, float radius, float mass, float swing)[] Parts =
        {
            ("pelvis", "spine", 0.14f, 10f, 0f),
            ("spine", "chest", 0.13f, 8f, 20f),
            ("chest", "neck", 0.15f, 8f, 20f),
            ("head", null, 0.12f, 4f, 35f),
            ("L upperarm", "L forearm", 0.06f, 2.5f, 70f),
            ("L forearm", "L hand", 0.05f, 1.5f, 60f),
            ("R upperarm", "R forearm", 0.06f, 2.5f, 70f),
            ("R forearm", "R hand", 0.05f, 1.5f, 60f),
            ("L thigh", "L shin", 0.08f, 5f, 50f),
            ("L shin", "L foot", 0.065f, 3.5f, 45f),
            ("R thigh", "R shin", 0.08f, 5f, 50f),
            ("R shin", "R foot", 0.065f, 3.5f, 45f),
        };

        internal static void Add(GameObject model)
        {
            var all = new Dictionary<string, Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (!all.ContainsKey(t.name)) all[t.name] = t;
            var bones = new List<Transform>();
            var bodies = new Dictionary<Transform, Rigidbody>();
            foreach (var part in Parts)
            {
                Transform bone;
                if (!all.TryGetValue(part.bone, out bone)) { Plugin.Log.LogWarning("HOLLOW_SAINT_RAGDOLL missing " + part.bone); return; }
                bone.gameObject.layer = LayerIndex.ragdoll.intVal;
                var rb = bone.gameObject.AddComponent<Rigidbody>();
                rb.mass = part.mass; rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.None;
                Transform child;
                if (part.child != null && all.TryGetValue(part.child, out child))
                {
                    Vector3 local = child.localPosition;
                    var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
                    float length = local.magnitude;
                    Vector3 abs = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
                    capsule.direction = abs.x > abs.y && abs.x > abs.z ? 0 : abs.y > abs.z ? 1 : 2;
                    capsule.center = local * 0.5f;
                    capsule.radius = part.radius;
                    capsule.height = length + part.radius * 2f;
                    capsule.enabled = false;
                }
                else
                {
                    var sphere = bone.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = part.radius; sphere.center = new Vector3(0f, part.radius * 0.6f, 0f);
                    sphere.enabled = false;
                }
                bodies[bone] = rb;
                bones.Add(bone);
            }
            foreach (var bone in bones)
            {
                var part = System.Array.Find(Parts, p => p.bone == bone.name);
                if (part.swing <= 0f) continue;
                Transform parent = bone.parent;
                while (parent && !bodies.ContainsKey(parent)) parent = parent.parent;
                if (!parent) continue;
                var joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = bodies[parent];
                joint.enableProjection = true;
                joint.lowTwistLimit = new SoftJointLimit { limit = -20f };
                joint.highTwistLimit = new SoftJointLimit { limit = 20f };
                joint.swing1Limit = new SoftJointLimit { limit = part.swing };
                joint.swing2Limit = new SoftJointLimit { limit = part.swing * 0.6f };
            }
            var ragdoll = model.AddComponent<RagdollController>();
            ragdoll.bones = bones.ToArray();
            ragdoll.componentsToDisableOnRagdoll = new MonoBehaviour[0];
            Plugin.Log.LogInfo("HOLLOW_SAINT_RAGDOLL bones=" + bones.Count);
        }
    }
}
