using UnityEditor;
using UnityEditor.SceneManagement;

namespace HollowSaint.Preview.Editor
{
    public static class OpenPreview
    {
        [MenuItem("Hollow Saint/Open movement preview")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ProbeSetup.ScenePath);
            Selection.activeGameObject = UnityEngine.GameObject.Find("Hollow Saint Preview Player");
        }
    }
}
