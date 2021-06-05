using BepInEx;
using EggsUtils.Buffs;
using R2API;
using R2API.Utils;
using RoR2;
using EggsUtils.Buffs.BuffComponents;
using System.Security;
using System.Security.Permissions;
using EggsUtils.Properties;
using System;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace EggsUtils
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Egg.EggsUtil", "EggsUtil", "1.0.6")]
    [R2APISubmoduleDependency(new string[]
{
    nameof(BuffAPI),
    nameof(LanguageAPI)
})]
    internal class EggsUtils : BaseUnityPlugin
    {
        private void Awake()
        {
            BuffsLoading.SetupBuffs();
            Assets.RegisterTokens();
            BuffHooks();
        }
        private void BuffHooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }
        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self)
            {
                //Handle temporal chains debuff effects
                if (self.HasBuff(BuffsLoading.buffDefTemporalChains))
                {
                    TemporalChainHandler chainHandler = self.gameObject.GetComponent<TemporalChainHandler>();
                    if (!chainHandler)
                    {
                        chainHandler = self.gameObject.AddComponent<TemporalChainHandler>();
                    }
                    float slowCount = chainHandler.GetSlowcount();
                    self.moveSpeed -= (self.moveSpeed / 2) * (slowCount / 8);
                    self.attackSpeed -= (self.attackSpeed / 3) * (slowCount / 8);
                }

                //Handle tracking debuff effects
                if (self.HasBuff(BuffsLoading.buffDefTracking))
                {
                    self.moveSpeed /= 2;
                }

                //Handle cunning buff effects
                if (self.HasBuff(BuffsLoading.buffDefCunning))
                {
                    self.moveSpeed *= 1.25f;
                }
            }
        }
        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (self)
            {
                //Tracking Debuff Handler
                if (self.body.HasBuff(BuffsLoading.buffDefTracking))
                {
                    damageInfo.damage *= 1.5f;
                }

                //Adaptive Buff Handler
                if (self.body.HasBuff(BuffsLoading.buffDefAdaptive))
                {
                    float healthFraction = self.fullCombinedHealth / 5;
                    if (damageInfo.damage > healthFraction)
                    {
                        damageInfo.damage = healthFraction;
                    }
                }

                //Undying Buff Handler
                if (self.body.HasBuff(BuffsLoading.buffDefUndying))
                {
                    float health = self.health;
                    if (damageInfo.damage >= health)
                    {
                        damageInfo.damage = health - 1;
                    }
                }

                if (damageInfo.attacker != null && damageInfo.inflictor != null)
                {
                    //Cunning Buff Handler
                    {
                        if (damageInfo.attacker.GetComponent<CharacterBody>().HasBuff(BuffsLoading.buffDefCunning))
                        {
                            damageInfo.damage *= 1.75f;
                        }
                    }

                    //Damagetype handler
                    float retrieveIndexFromCoeff = BuffsLoading.ProcToDamageTypeDecoder(damageInfo.procCoefficient);
                    float fixCoeff = BuffsLoading.ReturnProcToNormal(damageInfo.procCoefficient);
                    damageInfo.procCoefficient = fixCoeff;
                    int flattenIndexForCheck = Convert.ToInt32(Math.Floor(retrieveIndexFromCoeff * 10000f));
                    if ((damageInfo.damageType & DamageType.NonLethal) == DamageType.NonLethal)
                    {
                        foreach (BuffsLoading.CustomDamageType damageType in BuffsLoading.damageTypesList)
                        {
                            if (damageType.buffDef != null && damageType.onHitIndex == flattenIndexForCheck)
                            {
                                if (damageType.buffDuration > 0)
                                {
                                    self.body.AddTimedBuff(damageType.buffDef, damageType.buffDuration);
                                }
                                else
                                {
                                    self.body.AddBuff(damageType.buffDef);
                                }
                                damageInfo.damageType = DamageType.Generic;
                                damageInfo.procCoefficient = fixCoeff;
                            }
                            if (damageType.callOnHit != null && damageType.onHitIndex == flattenIndexForCheck)
                            {
                                damageInfo = damageType.callOnHit.Invoke(self, damageInfo);
                                damageInfo.damageType = DamageType.Generic;
                                damageInfo.procCoefficient = fixCoeff;
                            }
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }
    }
}
