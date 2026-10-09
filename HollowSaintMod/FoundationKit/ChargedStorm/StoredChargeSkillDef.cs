using RoR2;
using RoR2.Skills;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    public sealed class StoredChargeSkillDef : SkillDef
    {
        public bool allowsUnchargedCast;
        private bool HasFuel(GenericSkill slot)
        {
            var body = slot ? slot.characterBody : null;
            var meter = body ? body.GetComponent<DischargeMeter>() : null;
            return StoredChargeTransport.Ready && body && body.healthComponent && body.healthComponent.alive && meter && (allowsUnchargedCast || meter.Charge > 0) &&
                !meter.GazeOwnsBank && !StoredChargeState.OtherGatherOwnsBody(body);
        }
        public override bool IsReady(GenericSkill slot) => HasFuel(slot) && base.IsReady(slot);
        public override bool CanExecute(GenericSkill slot) => HasFuel(slot) && base.CanExecute(slot);
    }
}
