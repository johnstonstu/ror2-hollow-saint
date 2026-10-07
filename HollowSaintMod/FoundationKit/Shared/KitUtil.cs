using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{

    /// <summary>Shared helpers for sockets, aiming, attribution and capped AoE.</summary>
    public static class KitUtil
    {
        /// <summary>v0.9.9: the item events a native hit sends, for our manual DamageInfo hits.
        /// OnHitEnemy rolls on-hit items; OnHitAll is the event Brilliant Behemoth listens to.
        /// Vanilla BlastAttack and projectile impacts send both, and only for hits that were not
        /// rejected (blocked or immune), so this mirrors that. Proc 0 sends nothing.</summary>
        public static void ReportHit(DamageInfo info, GameObject victim)
        {
            if (info == null || victim == null || info.rejected || info.procCoefficient <= 0f || GlobalEventManager.instance == null) return;
            GlobalEventManager.instance.OnHitEnemy(info, victim);
            GlobalEventManager.instance.OnHitAll(info, victim);
        }

        /// <summary>Resolves a ChildLocator child by name, falling back to a transform
        /// search. Returns null rather than throwing.</summary>
        public static Transform ResolveSocket(CharacterBody body, string childName)
        {
            if (body == null) return null;
            var model = body.modelLocator != null ? body.modelLocator.modelTransform : null;
            if (model == null) return null;
            var locator = model.GetComponent<ChildLocator>();
            if (locator != null)
            {
                var found = locator.FindChild(childName);
                if (found != null) return found;
            }
            var nested = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nested.Length; i++)
                if (nested[i].name == childName) return nested[i];
            return null;
        }

        public static Vector3 EyePosition(CharacterBody body)
        {
            if (body != null && body.aimOriginTransform != null) return body.aimOriginTransform.position;
            return body != null ? ((Component)body).transform.position : Vector3.zero;
        }

        public static bool IsHollowSaint(CharacterBody body)
        {
            return body != null && body.baseNameToken == KitTokens.Name;
        }

        /// <summary>True when the attacker is a Hollow Saint body and the source is one
        /// of its skills.</summary>
        public static bool IsHollowSaintSkillDamage(GameObject attacker, HsDamageSource source)
        {
            if (source == HsDamageSource.None || attacker == null) return false;
            return IsHollowSaint(attacker.GetComponent<CharacterBody>());
        }

        /// <summary>Maps the vanilla skill damage source on a hit back to our skill. Our
        /// skills tag every hit Primary/Secondary/Special; item procs arrive as
        /// NoneSpecified. Single copy (it used to be duplicated in two files).</summary>
        public static HsDamageSource SourceOf(DamageInfo damageInfo)
        {
            if (damageInfo == null) return HsDamageSource.None;
            switch (damageInfo.damageType.damageSource)
            {
                case DamageSource.Primary: return HsDamageSource.ArcBolt;
                case DamageSource.Secondary: return HsDamageSource.Stormspear;
                case DamageSource.Special: return HsDamageSource.OpenCircuit;
                default: return HsDamageSource.None;
            }
        }

        /// <summary>Server only. Damages up to maxTargets distinct enemies within radius,
        /// nearest first. BlastAttack has no target cap, which is why this exists.
        /// Returns the hit positions (empty when nothing was struck).</summary>
        public static List<Vector3> CappedBlast(CharacterBody attacker, Vector3 origin, float radius, int maxTargets,
            float damage, bool crit, float procCoefficient, DamageTypeCombo damageType,
            DamageColorIndex color, bool linearFalloff, HealthComponent exclude = null, ProcChainMask procChainMask = default(ProcChainMask))
        {
            var hits = new List<Vector3>();
            if (!NetworkServer.active || attacker == null) return hits;
            var team = attacker.teamComponent != null ? attacker.teamComponent.teamIndex : TeamIndex.None;
            var search = new BullseyeSearch
            {
                searchOrigin = origin,
                searchDirection = Vector3.up,
                minAngleFilter = 0f,
                maxAngleFilter = 180f,
                minDistanceFilter = 0f,
                maxDistanceFilter = radius,
                teamMaskFilter = TeamMask.GetEnemyTeams(team),
                filterByLoS = false,
                filterByDistinctEntity = true,
                sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(attacker.gameObject);

            foreach (var hurtBox in search.GetResults())
            {
                if (hits.Count >= maxTargets) break;
                var health = hurtBox != null ? hurtBox.healthComponent : null;
                if (health == null || !health.alive || health == exclude) continue;
                float scale = 1f;
                if (linearFalloff)
                {
                    float distance = Vector3.Distance(origin, hurtBox.transform.position);
                    scale = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(distance / Mathf.Max(0.01f, radius)));
                }
                var info = new DamageInfo
                {
                    damage = damage * scale,
                    crit = crit,
                    attacker = attacker.gameObject,
                    inflictor = attacker.gameObject,
                    position = hurtBox.transform.position,
                    force = Vector3.zero,
                    procCoefficient = procCoefficient,
                    damageType = damageType,
                    damageColorIndex = color,
                    procChainMask = procChainMask,
                    inflictedHurtbox = hurtBox
                };
                health.TakeDamage(info);
                ReportHit(info, health.gameObject);
                hits.Add(hurtBox.transform.position);
            }
            return hits;
        }
    }
}
