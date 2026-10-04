using System.Globalization;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>A local-only readout of the actual acknowledged beam deadline.</summary>
    [DisallowMultipleComponent]
    internal sealed class GazeTimerHud : MonoBehaviour
    {
        private CharacterBody body;
        private EntityStateMachine crown;
        private GUIStyle labelStyle;
        private float shownSeconds = -1f;
        private string label;
        private bool warned;

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            crown = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
        }

        private HUD LocalHud()
        {
            foreach (var hud in HUD.readOnlyInstanceList)
                if (hud && hud.localUserViewer != null && hud.localUserViewer.cachedBody == body &&
                    hud.mainContainer && hud.mainContainer.activeInHierarchy && hud.mainContainerCanvas)
                    return hud;
            return null;
        }

        private void OnGUI()
        {
            // Repaint only; this adds no input controls or persistent UI objects.
            if (Event.current.type != EventType.Repaint || !body || !body.healthComponent ||
                !body.healthComponent.alive || !HUD.cvHudEnable.value) return;
            if (!crown) crown = EntityStateMachine.FindByCustomName(gameObject, KitRegistration.CrownMachineName);
            var state = crown ? crown.state as GazeState : null;
            if (state == null || !state.TimerVisible) return;
            var hud = LocalHud();
            if (!hud) return;
            try { Draw(hud.mainContainerCanvas.pixelRect, state.RemainingBeamSeconds); }
            catch (System.Exception error)
            {
                if (!warned) { warned = true; Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_TIMER " + error); }
                enabled = false;
            }
        }

        private void Draw(Rect viewport, float remaining)
        {
            if (labelStyle == null)
                labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            float tenths = Mathf.Ceil(remaining * 10f) / 10f;
            if (tenths != shownSeconds)
            {
                shownSeconds = tenths;
                label = Language.GetString("HS_GAZE_TIMER_LABEL") + " " + tenths.ToString("0.0", CultureInfo.InvariantCulture) + "s";
            }
            float width = Mathf.Min(190f, viewport.width * 0.32f);
            float left = viewport.x + (viewport.width - width) * 0.5f;
            float top = Screen.height - viewport.yMax + viewport.height * 0.81f;
            Color saved = GUI.color;
            try
            {
                GUI.color = new Color(0f, 0f, 0f, 0.8f);
                GUI.DrawTexture(new Rect(left - 3f, top - 21f, width + 6f, 33f), Texture2D.whiteTexture);
                GUI.color = new Color(0.35f, 0.9f, 1f, 0.9f);
                GUI.DrawTexture(new Rect(left, top + 2f, width * GazeTimerPolicy.Fill(remaining), 6f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(left, top - 21f, width, 23f), label, labelStyle);
            }
            finally { GUI.color = saved; }
        }
    }

}
