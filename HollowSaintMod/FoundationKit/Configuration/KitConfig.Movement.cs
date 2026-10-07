using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindMovement(ConfigFile c)
        {
            F(c, move, "Move speed", FoundationBody.BaseMoveSpeed, v => { FoundationBody.BaseMoveSpeed = v; FoundationBody.ApplyMovement(); }, 4f, 14f, 0.1f, "Base move speed in m/s (Commando is 7).");
            F(c, move, "Sprint multiplier", FoundationBody.SprintMultiplier, v => { FoundationBody.SprintMultiplier = v; FoundationBody.ApplyMovement(); }, 1f, 3f, 0.05f, "Sprint speed multiplier (Commando is 1.45).");
            F(c, move, "Jump power", FoundationBody.BaseJumpPower, v => { FoundationBody.BaseJumpPower = v; FoundationBody.ApplyMovement(); }, 8f, 30f, 0.5f, "Base jump power (Commando is 15).");
            B(c, move, "Arm life", FoundationArmPose.LifeEnabled, v => FoundationArmPose.LifeEnabled = v, "Procedural arm motion layered over the animation: breathing, idle sway, finger and wrist motion, follow-through and reaction to movement (acceleration, turns, jumps, landings). The casting arm drops to 25% during skill gestures.");
            B(c, move, "Aim follows crosshair", FoundationAimPose.Enabled, v => FoundationAimPose.Enabled = v, "The torso, neck and head follow where you aim (full in combat, a relaxed look out of combat).");
            B(c, misc, "Item displays (restart)", FoundationBody.ItemDisplays, v => FoundationBody.ItemDisplays = v, "Show picked-up items on the Saint (placements borrowed from Commando on matching mounts). Restart to apply.");
            B(c, misc, "Impact feel", Vfx.ImpactFeelSettings.Enabled, v => Vfx.ImpactFeelSettings.Enabled = v, "Brief hit-pause and camera shake on spear impacts and Thunderbolt strikes.");
            B(c, misc, "RoR2 body shader (restart)", FoundationHopoo.Enabled, v => FoundationHopoo.Enabled = v, "Use Risk of Rain 2's own body shader. Adds the elite body colour tint, but the dark skins and night stages look much darker. Takes effect on the next game start.");
            B(c, misc, "Status overlays (restart)", FoundationHopoo.OverlaysOnStandard, v => FoundationHopoo.OverlaysOnStandard = v, "Show cloak, shield, crit, immunity and elite overlays on the Saint with the normal shading. Off hides him completely while cloaked. Takes effect on the next game start.");
            F(c, move, "Arm life intensity", FoundationArmPose.LifeIntensity, v => FoundationArmPose.LifeIntensity = v, 0f, 2f, 0.05f, "Scale of the procedural arm life (0 = off, 1 = default).");

            // Arm life components (v0.7 arm reactions). Each multiplies "Arm life intensity";
            // the "Arm life" toggle still turns everything off.
            F(c, move, "Arm reaction to movement", FoundationArmPose.ReactionIntensity, v => FoundationArmPose.ReactionIntensity = v, 0f, 2f, 0.05f, "Arms trail acceleration, drag outward on turns and trail back while sprinting or gliding (0 = off, 1 = default).");
            F(c, move, "Arm air and landing reaction", FoundationArmPose.AirIntensity, v => FoundationArmPose.AirIntensity = v, 0f, 2f, 0.05f, "Arms float up while airborne and dip on jumps and landings, scaled by landing speed (0 = off, 1 = default).");
            F(c, move, "Arm idle sway", FoundationArmPose.IdleSwayIntensity, v => FoundationArmPose.IdleSwayIntensity = v, 0f, 2f, 0.05f, "Slow calm sway of the whole arm (0 = off, 1 = default).");
            F(c, move, "Arm follow-through", FoundationArmPose.FollowThrough, v => FoundationArmPose.FollowThrough = v, 0f, 2f, 0.05f, "How far elbow, wrist and fingers lag behind the shoulder in the reactions (0 = arm moves as one piece, 1 = default).");
        }

    }
}
