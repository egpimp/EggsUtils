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
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace EggsUtils
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Egg.EggsUtils", "EggsUtils", "1.1.0")]
    [R2APISubmoduleDependency(new string[]
{
    nameof(LanguageAPI)
})]
    public class EggsUtils : BaseUnityPlugin
    {
        private void Awake()
        {
            //Prep the buffs first
            BuffsLoading.SetupBuffs();
            //Register the lang tokens
            Assets.RegisterTokens();
            //Rev up those hooks
            BuffHooks();
        }
        //This helps us spot our own console logs in a potential mess of logs
        public static void LogToConsole(string logText)
        {
            Debug.Log("EggsMods : " + logText);
        }

        //RecalculateStats and TakeDamage are the main two things that status effects will affect
        private void BuffHooks()
        {
            //Stuff like +-move speed, +-armor, +- most stats
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            //Stuff like +- damage, good for damage reduction or damage boost
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }

        //Hook for all the stat-based buffs / debuffs
        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            //Let the standard stat stuff happen first, this way we don't break shit by accident (Usually)
            orig(self);
            //Prevent nullrefs
            if (self)
            {
                //Handle temporal chains debuff effects
                if (self.HasBuff(BuffsLoading.buffDefTemporalChains))
                {
                    //Declare component for reference
                    TemporalChainHandler chainHandler;
                    //Try to get component, if no exist create and assign it
                    if (!self.gameObject.TryGetComponent<TemporalChainHandler>(out chainHandler)) chainHandler = self.gameObject.AddComponent<TemporalChainHandler>();
                    //Slowcount basically just says how many stacks of the buff exist
                    float slowCount = chainHandler.GetSlowcount();
                    //100% -> 50% movespeed based on stack count
                    self.moveSpeed -= (self.moveSpeed / 2) * (slowCount / 8);
                    //100% -> 66% attackspeed based on stack count
                    self.attackSpeed -= (self.attackSpeed / 3) * (slowCount / 8);
                }

                //Handle tracking debuff effects, just halves movespeed (Rest handled in takedamage)
                if (self.HasBuff(BuffsLoading.buffDefTracking)) self.moveSpeed /= 2;

                //Handle cunning buff effects, again just increased movespeed (Rest handled in takedamage)
                if (self.HasBuff(BuffsLoading.buffDefCunning)) self.moveSpeed *= 1.25f;
            }
        }

        //Hook for all damage based buffs / debuffs
        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            //Again no null pls thanks
            if (self)
            {
                //Tracking Debuff Handler, just increase incoming damage if person taking damage has tracking 'buff'
                if (self.body.HasBuff(BuffsLoading.buffDefTracking)) damageInfo.damage *= 1.5f;

                //Adaptive Buff Handler
                if (self.body.HasBuff(BuffsLoading.buffDefAdaptive))
                {
                    //This should get us 20% of their max health
                    float healthFraction = self.fullCombinedHealth / 5;
                    //If the damage is greater than that 20%, set it to the 20% instead
                    if (damageInfo.damage > healthFraction) damageInfo.damage = healthFraction;
                }

                //Undying Buff Handler
                if (self.body.HasBuff(BuffsLoading.buffDefUndying))
                {
                    //This should jut be current health, no need to be based on max
                    float health = self.health;
                    //If damage would kill, then reduce the damage to one shy of killing.  We don't mind taking damage, but dying is illegal with this buff.
                    if (damageInfo.damage >= health) damageInfo.damage = health - 1;
                }

                //This is set up for handling things dependent on an attacker existing
                if (damageInfo.attacker != null && damageInfo.inflictor != null)
                {
                    //Grab the attackerbody once we know they exist, just helps referencing later
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                    //Cunning Buff Handler, if attacker has buff then multiply the damage, simple.
                    if (damageInfo.attacker.GetComponent<CharacterBody>().HasBuff(BuffsLoading.buffDefCunning)) damageInfo.damage *= 1.75f;

                    //Damagetype handler, welcome to the nightmare fiesta overdrive
                    //Ok so first off we nab the index from the proc coefficient, with proc coefficient x.xyyyy where x is the normal proc coefficient, we turn y into an index in range 0001-9999
                    float retrieveIndexFromCoeff = BuffsLoading.ProcToDamageTypeDecoder(damageInfo.procCoefficient);
                    //This just puts the coefficient to the intended value, so now it would be x.x0000 as if we never did the fancy proc stuff
                    damageInfo.procCoefficient = BuffsLoading.ReturnProcToNormal(damageInfo.procCoefficient);
                    //This makes the 'index' go from a weird decimal to a full on integer
                    int flattenIndexForCheck = Convert.ToInt32(Math.Floor(retrieveIndexFromCoeff * 10000f));
                    //Ok now we check if the damagetype is nonlethal, nonlethal is our flag for 'hey, this might be a custom damagetype'
                    if ((damageInfo.damageType & DamageType.NonLethal) == DamageType.NonLethal)
                    {
                        //Loop through all our custom damagetypes to find if any of them match the index,
                        foreach (BuffsLoading.CustomDamageType damageType in BuffsLoading.damageTypesList)
                        {
                            //If the damagetype has buffs or debuffs to apply...
                            if (damageType.buffDef != null && damageType.onHitIndex == flattenIndexForCheck)
                            {
                                //If duration exists add for that duration
                                if (damageType.buffDuration > 0) self.body.AddTimedBuff(damageType.buffDef, damageType.buffDuration);
                                //If duration does not exist apply permanently
                                else self.body.AddBuff(damageType.buffDef);
                                //Remove nonlethal flag from actual damagetype so it becomes lethal damage
                                damageInfo.damageType ^= DamageType.NonLethal;
                            }
                            //If the damagetype has methods to call...
                            if (damageType.callOnHit != null && damageType.onHitIndex == flattenIndexForCheck)
                            {
                                //Invoke the method with the given info
                                damageInfo = damageType.callOnHit.Invoke(self, damageInfo);
                                //Once again, remove nonlethal damage
                                damageInfo.damageType ^= DamageType.NonLethal;
                            }
                        }
                    }
                }
            }
            //Call the original damage after all of the above, mostly cause we want to intercept rather than post-op with our custom damagetypes and our buff handlers
            orig(self, damageInfo);
        }
    }
}
