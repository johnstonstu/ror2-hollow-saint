using UnityEngine;

namespace HollowSaint.Preview
{
    // Explicit animation bindings preserve values that Blender drivers cannot export.
    public sealed class HollowSaintPreviewSignals : MonoBehaviour
    {
        public float hs_glow, hs_jet, hs_spark_L, hs_spark_R, hs_jet_dir;
        public float hs_move_x, hs_move_y, hs_turn, hs_spear;
        public string lastMarker;

        public void OnClipMarker(string marker) { lastMarker = marker; }
    }
}
