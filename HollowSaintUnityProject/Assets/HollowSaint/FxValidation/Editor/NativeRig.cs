using System.IO;
using UnityEditor;
using UnityEngine;

namespace HollowSaint.PreviewValidation
{
    internal static class NativeRig
    {
        internal static string Version => File.Exists("Assets/HollowSaint/GameFoundation13/mdlHollowSaint.prefab") ? "13" : File.Exists("Assets/HollowSaint/GameFoundation12/mdlHollowSaint.prefab") ? "12" : "11";
        private static bool mapsReady;
        internal static void LoadLightMaps()
        {
            if (mapsReady || Version != "13") return;
            var bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../artifacts/foundation/bundle13/hollowsaintassets"));
            if (!bundle) throw new System.InvalidOperationException("Bundle13 light maps failed to load");
            try { FoundationLightMaps.Load(bundle); mapsReady = true; }
            finally { bundle.Unload(false); }
        }
        internal static GameObject Model { get { LoadLightMaps(); return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HollowSaint/GameFoundation" + Version + "/mdlHollowSaint.prefab"); } }
        internal static GameObject Spear => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HollowSaint/GameFoundation11/mdlConduitSpear.prefab");
    }
}
