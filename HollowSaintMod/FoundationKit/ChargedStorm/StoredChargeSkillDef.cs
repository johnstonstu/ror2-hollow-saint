using RoR2;
using RoR2.Skills;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    public sealed class StoredChargeSkillDef : SkillDef
    {
        public bool allowsUnchargedCast;
        /// <summary>1.3.1 fix: Thundercloud's free cast (ChargedStormTuning.CloudFreeCast) must also pass
        /// this gate with an empty bank; it previously required a charge to start at all.</summary>
        public bool followsCloudFreeCast;
        private bool HasFuel(GenericSkill slot)
        {
            // A released Cloud owns the next Special press for dismissal, including
            // when Lysate Cell or a stock reset would otherwise permit another cast.
            if (slot && slot.stateMachine && slot.stateMachine.state is StoredChargeState active &&
                active.Kind == 0 && active.Released) return false;
            var body = slot ? slot.characterBody : null;
            var meter = body ? body.GetComponent<DischargeMeter>() : null;
            return StoredChargeTransport.Ready && body && body.healthComponent && body.healthComponent.alive && meter && (allowsUnchargedCast || meter.Charge > 0 || (followsCloudFreeCast && ChargedStormTuning.CloudFreeCast)) &&
                !meter.GazeOwnsBank && !StoredChargeState.OtherGatherOwnsBody(body);
        }
        public override bool IsReady(GenericSkill slot) => HasFuel(slot) && base.IsReady(slot);
        public override bool CanExecute(GenericSkill slot) => HasFuel(slot) && base.CanExecute(slot);
    }
}
