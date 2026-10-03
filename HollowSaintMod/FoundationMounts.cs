using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HollowSaint
{
    internal static class FoundationMounts
    {
        private static readonly (string Alias, string Bone)[] Names =
        {
            ("Head", "head"), ("Chest", "chest"), ("Stomach", "spine"), ("Pelvis", "pelvis"),
            ("UpperArmL", "L upperarm"), ("UpperArmR", "R upperarm"),
            ("LowerArmL", "L forearm"), ("LowerArmR", "R forearm"),
            ("HandL", "L hand"), ("HandR", "R hand"),
            ("ThighL", "L thigh"), ("ThighR", "R thigh"),
            ("CalfL", "L shin"), ("CalfR", "R shin"), ("FootL", "L foot"), ("FootR", "R foot"),
            ("MuzzleLeft", "L muzzle"), ("MuzzleRight", "R muzzle"),
            ("Core", "core socket"), ("Halo", "halo socket"),
            ("HeelL", "L heel socket"), ("HeelR", "R heel socket")
        };

        internal static void Add(GameObject model, Transform hurtbox)
        {
            var bones = model.GetComponentsInChildren<Transform>(true);
            var entries = new List<ChildLocator.NameTransformPair>();
            foreach (var pair in Names)
            {
                var matches = bones.Where(t => t.name == pair.Bone).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Ambiguous/missing mount bone: " + pair.Bone);
                entries.Add(new ChildLocator.NameTransformPair { name = pair.Alias, transform = matches[0] });
            }
            entries.Add(new ChildLocator.NameTransformPair { name = "MainHurtbox", transform = hurtbox });
            var locator = model.AddComponent<ChildLocator>();
            foreach (var entry in entries) locator.AddChild(entry.name, entry.transform);
        }
    }
}
