using HollowSaint.FoundationKit.Storm;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Gaze
{
    internal sealed partial class GazeFuelController
    {
        /// <summary>1.2 (Stu): each minor surge also locks a bolt onto the closest enemy near or
        /// under the Saint, so whatever is closing in takes a hit while you aim the beam elsewhere.
        /// Skipped when that enemy is already in the surge's landing zone. Server only; visuals
        /// ride the networked spear beats.</summary>
        partial void UnderStrike(int group, Vector3 impactGround, Vector3 crown)
        {
            if (!NetworkServer.active || !body || group < 1) return;
            try
            {
                var team = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None;
                var search = new BullseyeSearch
                {
                    searchOrigin = body.corePosition, searchDirection = Vector3.down, minAngleFilter = 0f, maxAngleFilter = 180f,
                    minDistanceFilter = 0f, maxDistanceFilter = GazeReleaseTuning.UnderStrikeSeek,
                    teamMaskFilter = TeamMask.GetEnemyTeams(team), filterByLoS = true, filterByDistinctEntity = true,
                    sortMode = BullseyeSearch.SortMode.Distance
                };
                search.RefreshCandidates();
                HurtBox target = null;
                foreach (var box in search.GetResults())
                {
                    if (!box || !box.healthComponent || !box.healthComponent.alive) continue;
                    target = box; break;
                }
                if (!target) return;
                Vector3 hit = target.healthComponent.body ? target.healthComponent.body.corePosition : target.transform.position;
                float radius = GazeReleaseTuning.UnderStrikeRadius;
                if (Vector3.Distance(hit, impactGround) < radius + 1f) return; // the surge is landing there already
                Vector3 ground = hit, normal = Vector3.up;
                RaycastHit floor;
                if (Physics.Raycast(hit + Vector3.up * 0.5f, Vector3.down, out floor, 6f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                { ground = floor.point; normal = floor.normal; }
                float damage = body.damage * GazeReleaseTuning.DamagePerCharge * group * GazeReleaseTuning.UnderStrikeDamageScale;
                StormServer.BeginStormDamage();
                BlastAttack.Result result;
                try
                {
                    result = new BlastAttack
                    {
                        attacker = body.gameObject, inflictor = body.gameObject, teamIndex = team,
                        attackerFiltering = AttackerFiltering.NeverHitSelf,
                        position = hit, radius = radius,
                        falloffModel = BlastAttack.FalloffModel.None, baseDamage = damage, baseForce = 600f,
                        crit = body.RollCrit(),
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                        damageColorIndex = DamageColorIndex.Electrocution,
                        procCoefficient = GazeReleaseTuning.UnderStrikeProc, losType = BlastAttack.LoSType.None
                    }.Fire();
                }
                finally { StormServer.EndStormDamage(); }
                if (result.hitPoints != null)
                    for (int i = 0; i < result.hitCount && i < result.hitPoints.Length; i++)
                    {
                        var hb = result.hitPoints[i].hurtBox;
                        if (hb && hb.healthComponent && hb.healthComponent.alive)
                            StormServer.PrimeStatic(hb.healthComponent, body, StaticPrimePolicy.Amount(ChargedStorm.ChargedStormTuning.GazeStaticPrime, 1f));
                    }
                // A bolt from the crown onto the target, then the splash on the ground beneath it.
                KitFx.Server(Beat.SpearSpread, hit, crown, 1f, sound: true, owner: body);
                KitFx.Server(Beat.SpearBurst, ground, normal, radius, owner: body);
                KitLog.Event("GAZE_UNDER_STRIKE", "group=" + group + " target=" + target.healthComponent.name + " hits=" + result.hitCount);
            }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_GAZE_UNDER_STRIKE " + error); }
        }
    }
}
