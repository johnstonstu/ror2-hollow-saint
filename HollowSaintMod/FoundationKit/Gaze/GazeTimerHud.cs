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
        private GUIStyle titleStyle, secondsStyle;
        private GazeState observedState;
        private float lastDuration, gainUntil = -1f;
        private float displayScale;
        private int energyTier;
        private string title, gainedTitle;
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
            try { Sample(state); Draw(hud.mainContainerCanvas.pixelRect, state.RemainingBeamSeconds); }
            catch (System.Exception error)
            {
                if (!warned) { warned = true; Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_TIMER " + error); }
                enabled = false;
            }
        }

        private void Sample(GazeState state)
        {
            if (observedState != state)
            {
                observedState = state; lastDuration = state.ActualBeamSeconds; gainUntil = -1f;
                displayScale = Mathf.Max(.1f, state.RemainingBeamSeconds);
                title = Language.GetString("HS_GAZE_TIMER_LABEL").ToUpperInvariant();
            }
            float gained = state.ActualBeamSeconds - lastDuration;
            if (gained > .001f)
            {
                gainedTitle = title + "  +" + gained.ToString("0.#", CultureInfo.InvariantCulture) + "s";
                gainUntil = Time.unscaledTime + .85f;
                displayScale = Mathf.Max(.1f, state.RemainingBeamSeconds);
            }
            lastDuration = state.ActualBeamSeconds;
            energyTier = Mathf.Clamp(state.SuccessfulLaunches, 0, 5);
        }

        private void Draw(Rect viewport, float remaining)
        {
            float scale = Mathf.Clamp(viewport.height / 1080f, .8f, 1.3f);
            EnsureStyles(scale);
            float tenths = Mathf.Ceil(remaining * 10f) / 10f;
            if (tenths != shownSeconds)
            {
                shownSeconds = tenths;
                label = tenths.ToString("0.0", CultureInfo.InvariantCulture) + "s";
            }
            float width = Mathf.Min(168f * scale, viewport.width * .36f);
            float left = viewport.x + (viewport.width - width) * 0.5f;
            float top = Screen.height - viewport.yMax + viewport.height * .745f;
            Color saved = GUI.color;
            Matrix4x4 savedMatrix = GUI.matrix;
            try
            {
                // Canvas.pixelRect is already in display pixels. Another OnGUI participant's
                // scale/rotation must not scale those coordinates a second time.
                GUI.matrix = Matrix4x4.identity;
                Panel(left, top, width, scale, remaining);
                bool gain = Time.unscaledTime < gainUntil;
                GUI.color = gain ? new Color(.55f, 1f, 1f) : new Color(.78f, .84f, .87f);
                GUI.Label(new Rect(left + 8f * scale, top + 2f * scale, width * .65f, 22f * scale), gain ? gainedTitle : title, titleStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(left + width * .65f, top + scale, width * .35f - 8f * scale, 24f * scale), label, secondsStyle);
            }
            finally { GUI.color = saved; GUI.matrix = savedMatrix; }
        }

        private void EnsureStyles(float scale)
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
                secondsStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleRight };
                titleStyle.normal.textColor = secondsStyle.normal.textColor = Color.white;
            }
            titleStyle.fontSize = Mathf.RoundToInt(11f * scale);
            secondsStyle.fontSize = Mathf.RoundToInt(15f * scale);
        }

        private void Panel(float left, float top, float width, float scale, float remaining)
        {
            GUI.color = new Color(.025f, .035f, .045f, .94f);
            GUI.DrawTexture(new Rect(left, top, width, 30f * scale), Texture2D.whiteTexture);
            GUI.color = Time.unscaledTime < gainUntil ? new Color(.65f, 1f, 1f, .85f) : new Color(.58f, .65f, .7f, .45f);
            GUI.DrawTexture(new Rect(left, top, width, scale), Texture2D.whiteTexture);
            GUI.color = new Color(.2f, .27f, .31f, 1f);
            GUI.DrawTexture(new Rect(left + 8f * scale, top + 25f * scale, width - 16f * scale, 3f * scale), Texture2D.whiteTexture);
            GUI.color = new Color(.45f + .035f * energyTier, .90f + .02f * energyTier, 1f, 1f);
            GUI.DrawTexture(new Rect(left + 8f * scale, top + 25f * scale, (width - 16f * scale) * GazeTimerPolicy.Fill(remaining, displayScale), 3f * scale), Texture2D.whiteTexture);
            for (int pip = 0; pip < 5; pip++)
            {
                GUI.color = pip < energyTier ? new Color(.65f, 1f, 1f) : new Color(.2f, .27f, .31f);
                GUI.DrawTexture(new Rect(left + 8f * scale + pip * 5f * scale, top + 21f * scale, 3f * scale, 2f * scale), Texture2D.whiteTexture);
            }
        }
    }

}
