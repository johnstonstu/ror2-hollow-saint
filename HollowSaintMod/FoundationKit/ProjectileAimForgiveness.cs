using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{
    /// <summary>
    /// Server flight correction for near misses. Acquires once in a narrow launch cone,
    /// keeps that enemy, and spends a finite angular budget. Never reacquires after loss.
    /// ProjectileSimple follows the rotated forward vector; normal projectile networking
    /// carries the corrected flight, while impact components still own all damage.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-10)]
    public sealed class ProjectileAimForgiveness : MonoBehaviour
    {
        public bool isSpear;
        private HurtBox target;
        private TeamIndex attackerTeam;
        private SphereCollider sphere;
        private ProjectileSimple flight;
        private Rigidbody motion;
        private float remainingTurn = AimForgivenessRules.TotalTurnDegrees;
        internal HurtBox LockedTarget => target;
        internal float TurnUsed => AimForgivenessRules.TotalTurnDegrees - remainingTurn;

        private void Start()
        {
            if (!NetworkServer.active) { enabled = false; return; }
            sphere = GetComponent<SphereCollider>();
            flight = GetComponent<ProjectileSimple>();
            motion = GetComponent<Rigidbody>();
            // Do not attempt radius-blind correction if a template loses its collider.
            if (!sphere || !flight) { enabled = false; return; }
            var controller = GetComponent<ProjectileController>();
            float cone = isSpear ? Stormspear.StormspearTuning.AssistConeDegrees : KitTuning.ArcBoltAssistConeDegrees;
            if (!controller || !controller.teamFilter || cone <= 0f) { enabled = false; return; }
            attackerTeam = controller.teamFilter.teamIndex;
            var search = new BullseyeSearch
            {
                searchOrigin = transform.position,
                searchDirection = transform.forward,
                maxAngleFilter = Mathf.Min(6f, cone),
                maxDistanceFilter = AimForgivenessRules.Range,
                teamMaskFilter = TeamMask.GetEnemyTeams(attackerTeam),
                filterByLoS = true,
                filterByDistinctEntity = true,
                sortMode = BullseyeSearch.SortMode.Angle
            };
            search.RefreshCandidates();
            if (controller.owner) search.FilterOutGameObject(controller.owner);
            foreach (var box in search.GetResults())
            {
                if (!box || !box.healthComponent || !box.healthComponent.alive) continue;
                Vector3 offset = Center(box) - transform.position;
                bool visible = !Physics.Linecast(transform.position, Center(box),
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
                if (!AimForgivenessRules.CanAcquire(Vector3.Dot(transform.forward, offset.normalized),
                    offset.magnitude, cone, visible)) continue;
                target = box;
                break;
            }
            if (!target) enabled = false;
        }

        private void FixedUpdate()
        {
            if (!target || !target.healthComponent || !target.healthComponent.alive || remainingTurn <= 0f ||
                !FriendlyFireManager.ShouldDirectHitProceed(target.healthComponent, attackerTeam))
            { enabled = false; return; }
            Vector3 point = Center(target);
            Vector3 offset = point - transform.position;
            if (Vector3.Dot(transform.forward, offset) <= 0f ||
                Physics.Linecast(transform.position, point, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            { enabled = false; return; }

            float step = AimForgivenessRules.TurnStep(Vector3.Angle(transform.forward, offset),
                Time.fixedDeltaTime, remainingTurn);
            if (step <= 0f) return;
            Vector3 direction = Vector3.RotateTowards(transform.forward, offset.normalized,
                step * Mathf.Deg2Rad, 0f);
            Vector3 scale = transform.lossyScale;
            float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            float speed = Mathf.Max(Mathf.Abs(flight.desiredForwardSpeed), motion ? motion.velocity.magnitude : 0f);
            Vector3 center = transform.TransformPoint(sphere.center);
            if (!ProjectileWorldClearance.TryCorrection(center, transform.forward, direction,
                speed * Time.fixedDeltaTime, radius, out direction))
            {
                // Keep the current unassisted heading and budget. Native collision owns
                // an obstruction on either path; never steer around or through a wall.
                enabled = false;
                return;
            }
            transform.rotation = Util.QuaternionSafeLookRotation(direction);
            remainingTurn -= step;
        }

        private static Vector3 Center(HurtBox box)
        {
            return box.collider ? box.collider.bounds.center : box.transform.position;
        }
    }
}
