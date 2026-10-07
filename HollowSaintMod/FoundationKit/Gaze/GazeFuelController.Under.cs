using HollowSaint.FoundationKit.Storm;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Gaze
{
    internal sealed partial class GazeFuelController
    {
        /// <summary>1.2 (Stu): each minor surge also drops a small strike on the ground under the
        /// Saint, so enemies running in underneath the hover take a hit. Skipped when the surge is
        /// already landing there. Server only; visuals ride the networked spear beats.</summary>
        partial void UnderStrike(int group, Vector3 impactGround, Vector3 crown)
        {
            if (!NetworkServer.active || !body || group < 1) return;
            try
            {
                RaycastHit floor;
                if (!Physics.Raycast(body.corePosition, Vector3.down, out floor, 14f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return;
                float radius = GazeReleaseTuning.UnderStrikeRadius;
                if (Vector3.Distance(floor.point, impactGround) < radius + 1f) return;
                float damage = body.damage * GazeReleaseTuning.DamagePerCharge * group * GazeReleaseTuning.UnderStrikeDamageScale;
                StormServer.BeginStormDamage();
                BlastAttack.Result result;
                try
                {
                    result = new BlastAttack
                    {
                        attacker = body.gameObject, inflictor = body.gameObject,
                        teamIndex = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None,
                        attackerFiltering = AttackerFiltering.NeverHitSelf,
                        position = floor.point + Vector3.up * 0.5f, radius = radius,
                        falloffModel = BlastAttack.FalloffModel.None, baseDamage = damage, baseForce = 600f,
                        crit = body.RollCrit(),
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                        damageColorIndex = DamageColorIndex.Electrocution,
                        procCoefficient = GazeReleaseTuning.UnderStrikeProc, losType = BlastAttack.LoSType.None
                    }.Fire();
                }
                finally { StormServer.EndStormDamage(); }
                // A bolt from the crown down to the ground, then the small splash.
                KitFx.Server(Beat.SpearSpread, floor.point, crown, 1f, sound: true, owner: body);
                KitFx.Server(Beat.SpearBurst, floor.point, floor.normal, radius, owner: body);
                KitLog.Event("GAZE_UNDER_STRIKE", "group=" + group + " hits=" + result.hitCount);
            }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_GAZE_UNDER_STRIKE " + error); }
        }
    }
}
