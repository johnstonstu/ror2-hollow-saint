using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HollowSaint.Preview
{
    public sealed class PreviewRuntimeValidation : MonoBehaviour
    {
        private readonly List<string> checks = new List<string>();
        private bool completed;
        private string output;

        private void Awake()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/unity-setup/proof04/runtime-verification06.txt"));
            Application.logMessageReceived += OnLog;
        }

        private IEnumerator Start()
        {
            var motor = FindObjectOfType<HollowSaintPreviewMotor>();
            motor.DriveForValidation(Vector2.zero);
            yield return new WaitForSeconds(1.2f);
            Check(motor.GetComponent<CharacterController>().isGrounded, "Character settles on floor");
            var body = System.Array.Find(motor.GetComponentsInChildren<SkinnedMeshRenderer>(), r => r.name.StartsWith("HF BODY"));
            Check(body != null && body.bones.Length > 20, "Body imports as a weighted skinned mesh");
            var idleMesh = new Mesh();
            body.BakeMesh(idleMesh);
            Vector3 origin = motor.transform.position;
            motor.DriveForValidation(Vector2.up);
            yield return new WaitForSeconds(1.3f);
            float runDistance = Vector3.Distance(origin, motor.transform.position);
            Check(runDistance > 6 && runDistance < 9, "Run distance matches 6 m/s: " + runDistance.ToString("F3"));
            Check(motor.currentState == "Run forward", "Run start reaches running state");
            var animator = motor.animator;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            checks.Add("ANIMATOR initialized=" + animator.isInitialized + " enabled=" + animator.enabled + " speed=" + animator.speed + " culling=" + animator.cullingMode + " avatar=" + animator.avatar.isValid + " state=" + state.shortNameHash + " run=" + state.IsName("Run forward") + " time=" + state.normalizedTime);
            foreach (var info in animator.GetCurrentAnimatorClipInfo(0)) checks.Add("CLIP " + info.clip.name + " weight=" + info.weight);
            Check(state.IsName("Run forward") && state.normalizedTime > 0.1f, "Animator evaluates running state and advances time");
            var runMesh = new Mesh();
            body.BakeMesh(runMesh);
            float deformation = 0;
            var before = idleMesh.vertices;
            var after = runMesh.vertices;
            for (int i = 0; i < before.Length; i++) deformation = Mathf.Max(deformation, Vector3.Distance(before[i], after[i]));
            Check(deformation > 0.1f, "Animation deforms body vertices: " + deformation.ToString("F3") + "m");
            Destroy(idleMesh);
            Destroy(runMesh);
            Capture(motor.followCamera, "Runtime_Run");
            motor.DriveForValidation(Vector2.zero);
            yield return new WaitForSeconds(0.9f);
            Check(motor.currentState == "Idle", "Braking reaches idle");
            float ground = motor.transform.position.y, peak = ground;
            motor.DriveForValidation(Vector2.zero, jump: true);
            for (int i = 0; i < 100; i++) { peak = Mathf.Max(peak, motor.transform.position.y); yield return null; }
            Check(peak - ground > 0.6f, "Jump gains height: " + (peak - ground).ToString("F3"));
            yield return new WaitForSeconds(1.5f);
            Check(motor.GetComponent<CharacterController>().isGrounded, "Jump lands on floor");
            origin = motor.transform.position;
            motor.DriveForValidation(Vector2.up, glide: true);
            yield return new WaitForSeconds(1.3f);
            float glideDistance = Vector3.Distance(origin, motor.transform.position);
            Check(glideDistance > runDistance + 2, "Glide travels faster than run: " + glideDistance.ToString("F3"));
            Check(motor.currentState == "Glide loop", "Glide enter reaches loop");
            Check(motor.animator.GetComponent<HollowSaintPreviewSignals>().hs_jet > 0.5f, "Sampled jet curve animates in Unity");
            Capture(motor.followCamera, "Runtime_Glide");
            motor.DriveForValidation(Vector2.zero);
            yield return new WaitForSeconds(1);
            origin = motor.transform.position;
            motor.DriveForValidation(Vector2.zero, dash: true);
            yield return new WaitForSeconds(0.7f);
            Check(Vector3.Distance(origin, motor.transform.position) > 4, "Left dash moves character");
            yield return new WaitForSeconds(0.5f);
            motor.DriveForValidation(Vector2.zero, cast: true);
            yield return new WaitForSeconds(0.2f);
            Check(motor.currentState == "Arc Bolt right", "Cast input selects attack clip");
            Capture(motor.followCamera, "Runtime_Cast");
            yield return new WaitForSeconds(0.8f);
            Check(motor.currentState == "Idle", "Cast returns to idle");
            Finish(true);
        }

        private void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(800, 1000, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(800, 1000, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 800, 1000), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(output), name + ".png"), image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = old;
            Destroy(image);
            Destroy(target);
        }

        private void Check(bool success, string text)
        {
            checks.Add((success ? "PASS " : "FAIL ") + text);
            if (!success) { Finish(false); throw new System.InvalidOperationException(text); }
        }

        private void OnLog(string text, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                checks.Add("UNITY ERROR " + text + "\n" + trace);
                Finish(false);
            }
        }

        private void Finish(bool success)
        {
            if (completed) return;
            completed = true;
            File.WriteAllLines(output, checks);
            Application.logMessageReceived -= OnLog;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(success ? 0 : 1);
#endif
        }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}
