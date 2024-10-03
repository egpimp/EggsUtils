using BepInEx;
using EggsUtils.Buffs;
using EggsUtils.Buffs.BuffComponents;
using EggsUtils.Properties;
using RoR2;
using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace EggsUtils
{
    [BepInDependency(API_NAME, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(COMPAT_NAME, COMPAT_TITLE, COMPAT_VERS)]
    public class EggsUtils : BaseUnityPlugin
    {
        //Mod strings
        public const string COMPAT_NAME = "com.Egg.EggsUtils";
        public const string COMPAT_TITLE = "EggsUtils";
        public const string COMPAT_VERS = "1.2.8";
        //Hard Dependancies
        public const string API_NAME = "com.bepis.r2api";

        private void Awake()
        {
            //Logger init
            Log.Init(Logger);
            //Prep the buffs first
            BuffsLoading.SetupBuffs();
            //Register the assets
            EggAssets.RegisterAssets();
            //Rev up those hooks
            BuffHooks();
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
                    if (!self.gameObject.TryGetComponent(out chainHandler)) chainHandler = self.gameObject.AddComponent<TemporalChainHandler>();
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
            if (self && self.alive && self.body)
            {
                //Nab armor multiplier
                float armorMult = 1f - self.body.armor / (100f + Mathf.Abs(self.body.armor));
                //Tracking Debuff Handler, just increase incoming damage if person taking damage has tracking 'buff'
                if (self.body.HasBuff(BuffsLoading.buffDefTracking)) damageInfo.damage *= 1.5f;

                //Nab net damage (After armor and other damage multipliers)
                float netDamage = damageInfo.damage * armorMult;

                //Adaptive Buff Handler
                if (self.body.HasBuff(BuffsLoading.buffDefAdaptive))
                {
                    //Authors note: I previously forgot to factor in armor lol

                    //This should get us 20% of their max health
                    float healthFraction = self.fullCombinedHealth / 5;
                    //If the net damage is greater than that 20%, set it to the 20% instead
                    //Formula newNetDamage = newDamage * armorMult, and we have newNet and armorMult, thus newDamage = newNet / armorMult
                    if (netDamage > healthFraction) damageInfo.damage = healthFraction / armorMult;
                    //Prolly shoulda done this sooner but we are cutting out knockback too, fuggit
                    damageInfo.force = Vector3.zero;
                }

                //Undying Buff Handler
                if (self.body.HasBuff(BuffsLoading.buffDefUndying))
                {
                    //This should just be current health, no need to be based on max
                    float health = self.health;
                    //If damage would kill, then reduce the damage to one shy of killing.  We don't mind taking damage, but dying is illegal with this buff.
                    if (damageInfo.damage >= health) damageInfo.damage = (health - 1) / armorMult;
                }

                //This is set up for handling things dependent on an attacker existing
                if (damageInfo.attacker && damageInfo.inflictor)
                {
                    //Grab the attackerbody once we know they exist, just helps referencing later
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                    //Cunning Buff Handler, if attacker has buff then multiply the damage, simple.
                    if (attackerBody && attackerBody.HasBuff(BuffsLoading.buffDefCunning)) damageInfo.damage *= 1.75f;

                    //Damagestackbuff handler
                    if (attackerBody && attackerBody.HasBuff(BuffsLoading.buffDefStackingDamage))
                    {
                        //Get net damage multiplier
                        float damageMult = 1 + 0.1f * attackerBody.GetBuffCount(BuffsLoading.buffDefStackingDamage);
                        //Get damage after multiplying
                        damageInfo.damage *= damageMult;
                    }

                    //Damagetype handler, welcome to the nightmare fiesta overdrive
                    //Ok so first off we nab the index from the proc coefficient, with proc coefficient x.xyyyy where x is the normal proc coefficient, we turn y into an index in range 0001-9999
                    float retrieveIndexFromCoeff = BuffsLoading.ProcToDamageTypeDecoder(damageInfo.procCoefficient);
                    //This just puts the coefficient to the intended value, so now it would be x.x0000 as if we never did the fancy proc stuff
                    damageInfo.procCoefficient = BuffsLoading.ReturnProcToNormal(damageInfo.procCoefficient);
                    //This makes the 'index' go from a weird decimal to a full on integer
                    int flattenIndexForCheck = Convert.ToInt32(Math.Floor(retrieveIndexFromCoeff * 10000f));
                    //Ok now we check if the damagetype is nonlethal, nonlethal is our flag for 'hey, this might be a custom damagetype'
                    if ((damageInfo.damageType.damageType & DamageType.NonLethal) == DamageType.NonLethal)
                    {
                        //Loop through all our custom damagetypes to find if any of them match the index,
                        foreach (BuffsLoading.CustomDamageType damageType in BuffsLoading.damageTypesList)
                        {
                            if (damageType.onHitIndex != flattenIndexForCheck) continue;
                            //If the damagetype has buffs or debuffs to apply...
                            if (damageType.buffDef)
                            {
                                //If duration exists add for that duration
                                if (damageType.buffDuration > 0) self.body.AddTimedBuff(damageType.buffDef, damageType.buffDuration);
                                //If duration does not exist apply permanently
                                else self.body.AddBuff(damageType.buffDef);
                                //Remove nonlethal flag from actual damagetype so it becomes lethal damage
                                damageInfo.damageType ^= DamageType.NonLethal;
                            }
                            //If the damagetype has methods to call...
                            if (damageType.callOnHit != null)
                            {
                                //Invoke the method with the given info
                                damageInfo = damageType.callOnHit.Invoke(self, damageInfo);
                                //Once again, remove nonlethal damage
                                damageInfo.damageType.damageType ^= DamageType.NonLethal;
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
