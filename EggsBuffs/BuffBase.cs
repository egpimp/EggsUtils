using System;
using RoR2;
using UnityEngine;
using BepInEx;
using R2API;
using System.Security;
using System.Security.Permissions;
using EggsBuffs.BuffComponents;
using EggsBuffs.Properties;


[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace EggsBuffs
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Egg.EggsBuffs", "EggsBuffs", "1.0.2")]
    public class BuffsLoading : BaseUnityPlugin
    {
        public static BuffDef buffDefTemporalChains;
        public static BuffDef buffDefTracking;
        public static BuffDef buffDefAdaptive;
        public static BuffDef buffDefUndying;

        public static float temporalOnHitIndex = 0.0001f;
        public static float trackingOnHitIndex = 0.0002f;
        public static float stasisOnHitIndex = 0.0003f;
        private static float temporalOnHitCompare = 1f;
        private static float trackingOnHitCompare = 2f;
        private static float stasisOnHitCompare = 3f;
        public void Awake()
        {
            //Stacking slow.  At 8 stacks instead take a burst of damage, stun for two seconds, and reset stacks
            buffDefTemporalChains = BuffBuilder(Color.blue,true, Assets.placeHolderIcon, true, "Temporal Chains");
            //Slowed and takes increased damage
            buffDefTracking = BuffBuilder(Color.magenta, false, Assets.trackingIcon, true, "Tracked");
            //Incoming damage capped
            buffDefAdaptive = BuffBuilder(Color.blue, false, Assets.placeHolderIcon, false, "Adaptive Armor");
            //Cannot die
            buffDefUndying = BuffBuilder(Color.red, false, Assets.placeHolderIcon, false, "Undying");

            CustomBuff buffTemporal = new CustomBuff(buffDefTemporalChains);
            BuffAPI.Add(buffTemporal);
            CustomBuff buffTracking = new CustomBuff(buffDefTracking);
            BuffAPI.Add(buffTracking);
            CustomBuff buffAdaptive = new CustomBuff(buffDefAdaptive);
            BuffAPI.Add(buffAdaptive);
            CustomBuff buffUndying = new CustomBuff(buffDefUndying);
            BuffAPI.Add(buffUndying);
            BuffHooks();
            RegisterTokens();
        }
        public BuffDef BuffBuilder(Color color, bool canStack, Sprite icon, bool isDebuff, string buffName)
        {
            var buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = buffName;
            buffDef.buffColor = color;
            buffDef.canStack = canStack;
            buffDef.eliteDef = null;
            buffDef.iconSprite = icon;
            buffDef.isDebuff = isDebuff;
            return buffDef;
        }
        private void BuffHooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if(self)
            {
                //Handle temporal chains debuff effects
                if (self.HasBuff(buffDefTemporalChains))
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
                if(self.HasBuff(buffDefTracking))
                {
                    self.moveSpeed /= 2;
                }
            }
        }
        //If you're reading this, put me out of my misery why is custom damagetype shit so fucked
        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (self)
            {
                //Tracking Debuff Handler
                if (self.body.HasBuff(buffDefTracking))
                {
                    damageInfo.damage *= 1.5f;
                }
                //Adaptive Buff Handler
                if(self.body.HasBuff(buffDefAdaptive))
                {
                    float healthFraction = self.fullCombinedHealth / 5;
                    if (damageInfo.damage > healthFraction)
                    {
                        damageInfo.damage = healthFraction;
                    }
                }
                if(self.body.HasBuff(buffDefUndying))
                {
                    float health = self.health;
                    if(damageInfo.damage >= health)
                    {
                        damageInfo.damage = health - 1;
                    }
                }
                //Damagetype handler
                if ((damageInfo.damageType & DamageType.NonLethal) == DamageType.NonLethal)
                {
                    float retrieveIndexFromCoeff = ProcToDamageTypeDecoder(damageInfo.procCoefficient);
                    float fixCoeff = ReturnProcToNormal(damageInfo.procCoefficient);
                    damageInfo.procCoefficient = fixCoeff;
                    float flattenIndexForCheck = Convert.ToSingle(Math.Floor(retrieveIndexFromCoeff * 10000f));
                    if (flattenIndexForCheck == temporalOnHitCompare)
                    {
                        TemporalChainHandler chainHandler = self.body.gameObject.GetComponent<TemporalChainHandler>();
                        if (!chainHandler)
                        {
                            chainHandler = self.body.gameObject.AddComponent<TemporalChainHandler>();
                        }
                        self.body.AddBuff(buffDefTemporalChains);
                        chainHandler.inflictor = damageInfo.attacker;
                        chainHandler.ResetTimer();
                        damageInfo.damageType = DamageType.Generic;
                    }
                    else if(flattenIndexForCheck == trackingOnHitCompare)
                    {
                        self.body.AddTimedBuff(buffDefTracking,5f);
                        damageInfo.damageType = DamageType.Generic;
                    }
                }
            }
            orig(self,damageInfo);
        }

        private void RegisterTokens()
        {
            LanguageAPI.Add("KEYWORD_MARKING", "<style=cKeywordName>Tracking</style><style=cSub>Slows enemies and increases damage towards them</style>");
            LanguageAPI.Add("KEYWORD_STASIS", "<style=cKeywordName>Stasis</style><style=cSub>Units in stasis are invulnerable but cannot act</style>");
            LanguageAPI.Add("KEYWORD_ADAPTIVE", "<style=cKeywordName>Adaptive Defense</style><style=cSub>Incoming instances of damage are limited to 20% of max health</style>");
        }
        public static float ReturnProcToNormal(float procCoeff)
        {
            var intProc = procCoeff * 10f;
            var fixedProc = Math.Floor(intProc) / 10;
            return Convert.ToSingle(fixedProc);
        }
        //Takes the proc coeff from damageinfo and extracts the encoded damage type index
        public static float ProcToDamageTypeDecoder(float procCoeff)
        {
            var intProc = procCoeff * 10;
            var flattenedCoeff = Math.Floor(intProc);
            var decodedIndex = intProc - flattenedCoeff;
            return Convert.ToSingle(decodedIndex);
        }
        //Adds the damageIndex to the proc coeff so that it can be extracted when checking for damage types
        public static float ProcToDamageTypeEncoder(float damageIndex, float procCoeffToEncode)
        {
            var intProc = procCoeffToEncode * 10;
            var flattenedProc = Math.Floor(intProc);
            var encodedCoeff = (flattenedProc + damageIndex)/10;
            return Convert.ToSingle(encodedCoeff);
        }
    }
}
