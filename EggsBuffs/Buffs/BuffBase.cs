using System;
using RoR2;
using UnityEngine;
using EggsUtils.Buffs.BuffComponents;
using EggsUtils.Properties;
using System.Collections.Generic;

namespace EggsUtils.Buffs
{
    public class BuffsLoading
    {
        public static List<CustomDamageType> damageTypesList = new List<CustomDamageType>();

        public static BuffDef buffDefTemporalChains;
        public static BuffDef buffDefTracking;
        public static BuffDef buffDefAdaptive;
        public static BuffDef buffDefUndying;
        public static BuffDef buffDefCunning;

        public static CustomDamageType temporalChainsOnHit;
        public static CustomDamageType trackingOnHit;
        internal static void SetupBuffs()
        {
            //Stacking slow.  At 8 stacks instead take a burst of damage, stun for two seconds, and reset stacks
            buffDefTemporalChains = BuffBuilder(Color.blue, true, Assets.placeHolderIcon, true, "Temporal Chains");
            temporalChainsOnHit = AssignNewDamageType(buffDefTemporalChains, TemporalChainHandler);
            //Slowed and takes increased damage
            buffDefTracking = BuffBuilder(Color.magenta, false, Assets.trackingIcon, true, "Tracked");
            trackingOnHit = AssignNewDamageType(buffDefTracking);
            //Incoming damage capped
            buffDefAdaptive = BuffBuilder(Color.blue, false, Assets.placeHolderIcon, false, "Adaptive Armor");
            //Cannot die
            buffDefUndying = BuffBuilder(Color.red, false, Assets.placeHolderIcon, false, "Undying");
            //Deal more damage + Move speed
            buffDefCunning = BuffBuilder(Color.green, false, Assets.trackingIcon, false, "Cunning");
        }
        public struct CustomDamageType
        {
            public BuffDef buffDef { get; private set; }
            public int onHitIndex { get; private set; }
            public float procIndex { get; private set; }
            public Func<HealthComponent, DamageInfo, DamageInfo> callOnHit { get; private set; }

            public CustomDamageType(BuffDef buff, int index)
            {
                buffDef = buff;
                onHitIndex = index;
                procIndex = Convert.ToSingle(Math.Round(index / 10000f, 4));
                callOnHit = null;
            }
            public CustomDamageType(Func<HealthComponent, DamageInfo, DamageInfo> call, int index)
            {
                buffDef = null;
                onHitIndex = index;
                procIndex = Convert.ToSingle(Math.Round(index / 10000f, 4));
                callOnHit = call;
            }
            public CustomDamageType(BuffDef buff, Func<HealthComponent, DamageInfo, DamageInfo> call, int index)
            {
                buffDef = buff;
                onHitIndex = index;
                procIndex = Convert.ToSingle(Math.Round(index / 10000f, 4));
                callOnHit = call;
            }
        }
        /// <summary>
        /// Sets up a custom damagetype that applies the given buffdef
        /// </summary>
        /// <param name="buffToApply"></param>
        /// <returns></returns>
        public static CustomDamageType AssignNewDamageType(BuffDef buffToApply)
        {
            int index = damageTypesList.Count + 1;
            CustomDamageType onHit = new CustomDamageType(buffToApply, index) { };
            damageTypesList.Add(onHit);
            return onHit;
        }
        /// <summary>
        /// Sets up a custom damagetype that invokes the given method
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public static CustomDamageType AssignNewDamageType(Func<HealthComponent, DamageInfo, DamageInfo> method)
        {
            int index = damageTypesList.Count + 1;
            CustomDamageType onHit = new CustomDamageType(method, index) { };
            damageTypesList.Add(onHit);
            return onHit;
        }
        /// <summary>
        /// Sets up a custom damagetype that invokes the given method and applies the given buff
        /// </summary>
        /// <param name="buffToApply"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public static CustomDamageType AssignNewDamageType(BuffDef buffToApply, Func<HealthComponent, DamageInfo, DamageInfo> method)
        {
            int index = damageTypesList.Count + 1;
            CustomDamageType onHit = new CustomDamageType(buffToApply, method, index) { };
            damageTypesList.Add(onHit);
            return onHit;
        }
        /// <summary>
        /// Sets up a buffdef to be referenced later
        /// </summary>
        /// <param name="color"></param>
        /// <param name="canStack"></param>
        /// <param name="icon"></param>
        /// <param name="isDebuff"></param>
        /// <param name="buffName"></param>
        /// <returns></returns>
        public static BuffDef BuffBuilder(Color color, bool canStack, Sprite icon, bool isDebuff, string buffName)
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

        private static DamageInfo TemporalChainHandler(HealthComponent component, DamageInfo info)
        {
            TemporalChainHandler chainHandler = component.body.gameObject.GetComponent<TemporalChainHandler>();
            if (!chainHandler)
            {
                chainHandler = component.body.gameObject.AddComponent<TemporalChainHandler>();
            }
            component.body.AddBuff(buffDefTemporalChains);
            chainHandler.inflictor = info.attacker;
            chainHandler.ResetTimer();
            return info;
        }

        internal static float ReturnProcToNormal(float procCoeff)
        {
            var intProc = procCoeff * 10f;
            var fixedProc = Math.Floor(intProc) / 10;
            return Convert.ToSingle(fixedProc);
        }
        //Takes the proc coeff from damageinfo and extracts the encoded damage type index
        internal static float ProcToDamageTypeDecoder(float procCoeff)
        {
            var intProc = procCoeff * 10;
            var flattenedCoeff = Math.Floor(intProc);
            var decodedIndex = intProc - flattenedCoeff;
            return Convert.ToSingle(decodedIndex);
        }
        /// <summary>
        /// Adds the damageIndex to the proc coeff so that it can be extracted when checking for damage types
        /// Make sure you set the damagetype to nonlethal
        /// </summary>
        /// <param name="damageIndex"></param>
        /// <param name="procCoeffToEncode"></param>
        /// <returns></returns>
        public static float ProcToDamageTypeEncoder(float damageIndex, float procCoeffToEncode)
        {
            var intProc = procCoeffToEncode * 10;
            var flattenedProc = Math.Floor(intProc);
            var encodedCoeff = (flattenedProc + damageIndex)/10;
            return Convert.ToSingle(encodedCoeff);
        }
    }
}
