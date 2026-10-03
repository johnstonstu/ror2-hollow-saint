using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    // Read-only inspection/sampling of the already built controller; no asset edits.
    public static class FoundationLocomotionProbe05
    {
        private static readonly string Report = Path.GetFullPath("../artifacts/foundation/bundle05-locomotion-probe.txt");
        private static readonly StringBuilder Log = new StringBuilder();

        public static void RunBatch()
        {
            try { Check(); File.WriteAllText(Report, "PASS\n" + Log); EditorApplication.Exit(0); }
            catch (Exception error) { File.WriteAllText(Report, "FAIL\n" + Log + error); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static void Require(bool pass, string message)
        { if (!pass) throw new InvalidOperationException(message); Log.AppendLine(message); }

        private static void Check()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/HollowSaint/GameFoundation05/Foundation.controller");
            Require(controller != null, "Controller exists");
            var state = controller.layers[0].stateMachine.states.Single(s => s.state.name == "Locomotion").state;
            Require(state.speedParameterActive && state.speedParameter == "moveRate", "Locomotion uses moveRate");
            var gait = state.motion as BlendTree;
            Require(gait && gait.blendType == BlendTreeType.Simple1D && gait.blendParameter == "gaitBlend", "Normalized gait tree");
            Require(gait.children.Length == 2 && gait.children[0].threshold == 0f && gait.children[1].threshold == 1f, "Walk/run thresholds 0/1");
            foreach (var child in gait.children)
            {
                var direction = child.motion as BlendTree;
                Require(direction && direction.blendType == BlendTreeType.SimpleDirectional2D && direction.children.Length == 8, "Eight phase-aligned directions: " + child.motion.name);
                Require(direction.children.All(c => Math.Abs(c.position.magnitude - 1f) < 0.0001f && c.timeScale == 1f && c.cycleOffset == 0f), "Unit headings, unchanged phase/timeScale");
                Require(direction.children.All(c => c.motion.name != "Idle"), "No Idle contribution in moving tree");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HollowSaint/GameFoundation05/mdlHollowSaint.prefab");
            var model = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var animator = model.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Sample(animator, "slow forward walk", 0f, Vector2.up, 0.2f, 26f / 24f);
                Sample(animator, "authored right strafe", 1f, Vector2.right, 1f, 16f / 24f);
                Sample(animator, "double-rate right strafe", 1f, Vector2.right, 2f, 16f / 24f);
                Sample(animator, "walk-run blend at 22.5deg", 0.5f, new Vector2(0.3826834f, 0.9238795f), 1f, 21f / 24f);
                Sample(animator, "backward run", 1f, Vector2.down, 1f, 16f / 24f);
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
        }

        private static void Sample(Animator animator, string name, float gait, Vector2 direction, float rate, float duration)
        {
            animator.SetFloat("gaitBlend", gait);
            animator.SetFloat("rightSpeed", direction.x);
            animator.SetFloat("forwardSpeed", direction.y);
            animator.SetFloat("moveRate", rate);
            animator.Play("Locomotion", 0, 0f);
            animator.Update(0f);
            float before = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            for (int i = 0; i < 60; i++) animator.Update(1f / 60f);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            float delta = state.normalizedTime - before;
            float expected = rate / duration;
            Log.AppendLine(name + " normalizedDelta=" + delta.ToString("F6") + " expected=" + expected.ToString("F6") + " stateLength=" + state.length.ToString("F6"));
            var clips = new List<AnimatorClipInfo>();
            animator.GetCurrentAnimatorClipInfo(0, clips);
            foreach (var clip in clips.Where(c => c.weight > 0.00001f))
                Log.AppendLine("  " + clip.clip.name + " weight=" + clip.weight.ToString("F6") + " length=" + clip.clip.length.ToString("F6"));
            Require(Math.Abs(clips.Sum(c => c.weight) - 1f) < 0.001f, "Clip weights normalize");
            Require(Math.Abs(delta - expected) < 0.005f, "Actual Animator cadence matches duration/rate: " + name);
        }
    }
}
