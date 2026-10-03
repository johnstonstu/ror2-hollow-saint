using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // Measures how the WD-off gesture layers behave in the real GameFoundation14 controller:
    // (A) after a gesture returns to Empty, does the arm follow the Body layer again?
    // (B) moving a running gesture between UpperBody and UpperArms the way KitAnim.MoveGesture
    //     does: crossfade vs instant play on the destination layer, largest one-frame hand jump.
    public static class GestureLayerCheck
    {
        private const float Dt = 1f / 60f;
        private static readonly StringBuilder log = new StringBuilder();

        public static void RunBatch()
        {
            string path = Path.GetFullPath("../artifacts/gesture-layers01/report.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                ReturnToEmpty();
                Move("crossfade (current)", false);
                Move("instant play on destination", true);
            }
            catch (Exception e) { log.AppendLine("EXCEPTION " + e); }
            File.WriteAllText(path, log.ToString());
            EditorApplication.Exit(0);
        }

        private static (GameObject, Animator, Transform, Transform) Make()
        {
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FoundationBundleBuilder14.Folder + "mdlHollowSaint.prefab"));
            var a = go.GetComponent<Animator>(); a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var all = go.GetComponentsInChildren<Transform>(true);
            foreach (var p in new[] { "attackSpeed", "overlaySpeed", "haloSpeed", "spearSpeed", "moveRate" }) a.SetFloat(p, 1f);
            a.SetFloat("gaitBlend", 0f);
            return (go, a, all.First(t => t.name == "chest"), all.First(t => t.name == "R hand"));
        }

        private static Vector3 Rel(Transform chest, Transform hand) => chest.InverseTransformPoint(hand.position);

        private static void ReturnToEmpty()
        {
            var (g1, a, chest, hand) = Make(); var (g2, c, chestC, handC) = Make();
            try
            {
                a.Update(0f); c.Update(0f);
                int ub = a.GetLayerIndex("UpperBody");
                a.Play("Arc Bolt right", ub, 0f);
                for (int i = 0; i < 40; i++) { a.Update(Dt); c.Update(Dt); }
                a.CrossFadeInFixedTime("Empty", 0.2f, ub);
                for (int i = 0; i < 60; i++) { a.Update(Dt); c.Update(Dt); }
                float diff = (Rel(chest, hand) - Rel(chestC, handC)).magnitude;
                log.AppendLine("A return-to-Empty: R hand vs never-gestured control after 1 s = " + (diff * 100f).ToString("0.0") +
                    " cm (" + (diff < 0.01f ? "Empty passes the Body layer through" : "Empty HOLDS a stale pose") + ")");
            }
            finally { UnityEngine.Object.DestroyImmediate(g1); UnityEngine.Object.DestroyImmediate(g2); }
        }

        private static void Move(string label, bool instant)
        {
            var (g, a, chest, hand) = Make();
            try
            {
                a.Update(0f);
                int ub = a.GetLayerIndex("UpperBody"), arms = a.GetLayerIndex("UpperArms");
                // Leave a stale pose on UpperArms first: an earlier gesture that returned to Empty.
                a.Play("Arc Bolt left", arms, 0f);
                for (int i = 0; i < 30; i++) a.Update(Dt);
                a.CrossFadeInFixedTime("Empty", 0.2f, arms);
                for (int i = 0; i < 40; i++) a.Update(Dt);
                a.Play("Open Circuit arms hold", ub, 0f);
                for (int i = 0; i < 40; i++) a.Update(Dt);
                var info = a.GetCurrentAnimatorStateInfo(ub);
                float at = Mathf.Repeat(info.normalizedTime, 1f);
                Vector3 last = Rel(chest, hand); float worst = 0f; int worstFrame = -1;
                if (instant) a.Play(info.shortNameHash, arms, at); else a.CrossFade(info.shortNameHash, 0.15f, arms, at);
                a.CrossFadeInFixedTime("Empty", 0.15f, ub);
                for (int i = 0; i < 30; i++)
                {
                    a.Update(Dt);
                    Vector3 now = Rel(chest, hand); float step = (now - last).magnitude; last = now;
                    if (step > worst) { worst = step; worstFrame = i; }
                }
                log.AppendLine("B move gesture, " + label + ": largest one-frame R hand step " + (worst * 100f).ToString("0.0") + " cm at frame " + worstFrame);
            }
            finally { UnityEngine.Object.DestroyImmediate(g); }
        }
    }
}
