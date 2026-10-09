using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private bool isolateReviewFixtures;
        private readonly HashSet<CharacterMaster> isolatedMasters = new HashSet<CharacterMaster>();
        private void FixedUpdate() => MaintainFixtureIsolation();

        private void MaintainFixtureIsolation()
        {
            if (!isolateReviewFixtures || !NetworkServer.active || !pilot || !pilot.teamComponent) return;
            foreach (var director in CombatDirector.instancesList.ToArray()) if (director) director.enabled = false;
            var enemyTeams = TeamMask.GetEnemyTeams(pilot.teamComponent.teamIndex);
            foreach (var body in CharacterBody.readOnlyInstancesList.ToArray())
            {
                if (!body || !body.teamComponent || !enemyTeams.HasTeam(body.teamComponent.teamIndex) ||
                    !body.master || isolatedMasters.Contains(body.master)) continue;
                var master = body.master;
                trace.AppendLine("FIXTURE_EXCLUDED body=" + body.name + " team=" + body.teamComponent.teamIndex +
                    " point=" + body.corePosition + " reason=foreign-stage-spawn");
                foreach (var ai in master.GetComponents<BaseAI>()) ai.SetBaseAIEnabled(false);
                // No combat death callbacks: they can spawn attacks, parasites or XP.
                NetworkServer.Destroy(body.gameObject);
                if (master) NetworkServer.Destroy(master.gameObject);
            }
        }
    }
}
