using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private IEnumerator ChargeMovingTarget(bool slow = false)
        {
            // Success: the native visual stays speed-bounded while a victim
            // retreats, and the authoritative Orb hits that victim exactly once.
            yield return Segment(slow ? "charge-orb-slow-target" : "charge-orb-moving-target");
            var victim = dummies[0]; var original = victim.footPosition;
            float originalSpeed = ChargedStormTuning.OrbSpeed;
            if (slow) ChargedStormTuning.OrbSpeed = 10f;
            chargeDummyMoving = true; chargeMotionVictim = victim;
            var gravity = victim.characterMotor.gravityParameters;
            gravity.channeledAntiGravityGranterCount++; victim.characterMotor.gravityParameters = gravity;
            victim.characterMotor.velocity = Vector3.zero;
            dummies[1].teamComponent.teamIndex = dummies[2].teamComponent.teamIndex = TeamIndex.Player;
            try
            {
                var startPoint = mark + facing * (slow ? 50f : 25f) + Vector3.up * 10f;
                TeleportHelper.TeleportBody(victim, startPoint);
                // Native motor interpolation updates core/eye transforms on a
                // subsequent tick; aim must use the settled relocation.
                yield return Wait(.15f);
                trace.AppendLine("MOTION_FIXTURE slow=" + slow + " core=" + victim.corePosition + " alive=" + victim.healthComponent.alive);
                ReleaseCheck(ChargedStormTargeting.Clear(pilot.corePosition, victim.corePosition), "native motion fixture has a clear lane slow=" + slow);
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                aimTarget = victim.corePosition; fire2 = true;
                float gatheredFor = 0;
                while (gatheredFor < 1.83f)
                {
                    victim.characterMotor.velocity = Vector3.zero;
                    TeleportHelper.TeleportBody(victim, startPoint);
                    aimTarget = victim.corePosition;
                    yield return Wait(.001f); gatheredFor += Time.deltaTime;
                }
                float cone = Vector3.Angle(victim.corePosition - pilot.corePosition, pilot.inputBank.aimDirection);
                trace.AppendLine("MOTION_LAUNCH slow=" + slow + " core=" + victim.corePosition + " cone=" + cone);
                ReleaseCheck(cone < 8f, "native motion fixture is inside launch acquisition cone slow=" + slow);
                chargeSource = DamageSource.Secondary; chargeReports.Clear(); recordingCharges = true;
                fire2 = false;
                float elapsed = 0, maxSpeed = 0; int samples = 0;
                HollowedOrbFlightFx prior = null; Vector3 previous = Vector3.zero;
                while (elapsed < (slow ? 6f : 2.5f))
                {
                    yield return Wait(.001f);
                    float dt = Time.deltaTime; elapsed += dt;
                    victim.characterMotor.velocity = Vector3.zero;
                    TeleportHelper.TeleportBody(victim, startPoint + facing * (slow ? 0f : 8f * elapsed));
                    foreach (var fx in FindObjectsOfType<HollowedOrbFlightFx>())
                    {
                        if (prior == fx && dt > .001f)
                        { samples++; maxSpeed = Mathf.Max(maxSpeed, Vector3.Distance(previous, fx.transform.position) / dt); }
                        prior = fx; previous = fx.transform.position;
                    }
                }
                recordingCharges = false;
                trace.AppendLine("ORB_MOTION slow=" + slow + " samples=" + samples + " maxVisualSpeed=" + maxSpeed);
                ReleaseCheck(samples >= 3 && maxSpeed <= ChargedStormTuning.OrbSpeed + 2f, "native Orb visual preserves authoritative speed slow=" + slow);
                ReleaseCheck(chargeReports.Count == 1 && chargeReports[0].victim == victim.healthComponent, "native Orb hits lone enemy exactly once slow=" + slow);
                Shot(slow ? "orb-slow-restored" : "orb-moving-restored");
            }
            finally
            {
                recordingCharges = false; chargeDummyMoving = false; chargeMotionVictim = null;
                gravity = victim.characterMotor.gravityParameters;
                gravity.channeledAntiGravityGranterCount = Mathf.Max(0, gravity.channeledAntiGravityGranterCount - 1);
                victim.characterMotor.gravityParameters = gravity; victim.characterMotor.velocity = Vector3.zero;
                ChargedStormTuning.OrbSpeed = originalSpeed;
                dummies[1].teamComponent.teamIndex = dummies[2].teamComponent.teamIndex = TeamIndex.Monster;
                TeleportHelper.TeleportBody(victim, original);
                trace.AppendLine("MOTION_RESTORED slow=" + slow + " core=" + victim.corePosition + " alive=" + victim.healthComponent.alive + " health=" + victim.healthComponent.health);
            }
        }
    }
}
