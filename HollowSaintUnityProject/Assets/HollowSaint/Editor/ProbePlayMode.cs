using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HollowSaint.Preview.Editor
{
    [InitializeOnLoad]
    public static class ProbePlayMode
    {
        private const string Pending = "HollowSaint.RuntimeValidation";
        static ProbePlayMode() { EditorApplication.playModeStateChanged += OnState; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(ProbeSetup.ScenePath);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        private static void OnState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            Time.captureDeltaTime = 1f / 60f;
            new GameObject("Runtime acceptance checks").AddComponent<PreviewRuntimeValidation>();
        }
    }
}
