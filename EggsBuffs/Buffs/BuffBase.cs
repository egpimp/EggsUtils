using EggsUtils.Buffs.BuffComponents;
using EggsUtils.Properties;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using static R2API.ContentAddition;

namespace EggsUtils.Buffs
{
    public class BuffsLoading
    {        
        //Buffdefs
        public static BuffDef buffDefTemporalChains;
        public static BuffDef buffDefTracking;
        public static BuffDef buffDefAdaptive;
        public static BuffDef buffDefUndying;
        public static BuffDef buffDefCunning;

        //Applies temporal chains on hit
        public static CustomDamageType temporalChainsOnHit;
        //Applies tracking on hit
        public static CustomDamageType trackingOnHit;

        //List of all our buffdefs
        private static List<BuffDef> defList = new List<BuffDef>();
        //List of all our custom damage types
        public static List<CustomDamageType> damageTypesList = new List<CustomDamageType>();

        //Main method for handling buff loading and adding
        internal static void SetupBuffs()
        {
            //Stacking slow.  At 8 stacks instead take a burst of damage, stun for two seconds, and reset stacks
            buffDefTemporalChains = BuffBuilder(Color.blue, true, Assets.placeHolderIcon, true, "Temporal Chains");
            temporalChainsOnHit = AssignNewDamageType(buffDefTemporalChains, 0f, TemporalChainHandler);
            defList.Add(buffDefTemporalChains);

            //Slowed and takes increased damage
            buffDefTracking = BuffBuilder(Color.magenta, false, Assets.trackingIcon, true, "Tracked");
            trackingOnHit = AssignNewDamageType(buffDefTracking, 5f);
            defList.Add(buffDefTracking);

            //Incoming damage capped
            buffDefAdaptive = BuffBuilder(Color.blue, false, Assets.placeHolderIcon, false, "Adaptive Armor");
            defList.Add(buffDefAdaptive);

            //Cannot die
            buffDefUndying = BuffBuilder(Color.red, false, Assets.placeHolderIcon, false, "Undying");
            defList.Add(buffDefUndying);

            //Deal more damage + Move speed
            buffDefCunning = BuffBuilder(Color.blue, false, Assets.trackingIcon, false, "Cunning");
            defList.Add(buffDefCunning);

            //Adds all the buffs via R2API (Thanks r2api devs)
            foreach(BuffDef def in defList) AddBuffDef(def);
        }

        //Helps us handle custom damage types
        public struct CustomDamageType
        {
            //Buffdef to apply, if it exists
            public BuffDef buffDef { get; private set; }
            //Duration of the buff (0 means infinite)
            public float buffDuration { get; private set; }
            //Decimal index of the buff
            public float procIndex { get; private set; }
            //Index of the damagetype
            public int onHitIndex { get; private set; }
            //Method for the buff to call, if any
            public Func<HealthComponent, DamageInfo, DamageInfo> callOnHit { get; private set; }

            //Constructs damagetype with given buff, but no methods
            internal CustomDamageType(BuffDef buff, float duration, int index)
            {
                buffDef = buff;
                onHitIndex = index;
                procIndex = Convert.ToSingle(Math.Round(index / 10000f, 4));
                callOnHit = null;
                buffDuration = duration;
            }
            //Constructs damagetype with method, but no buffs
            internal CustomDamageType(Func<HealthComponent, DamageInfo, DamageInfo> call, int index)
            {
                buffDef = null;
                onHitIndex = index;
                procIndex = Convert.ToSingle(Math.Round(index / 10000f, 4));
                callOnHit = call;
                buffDuration = 0;
            }
            //Constructs damagetype with buffs and method
            internal CustomDamageType(BuffDef buff, float duration, Func<HealthComponent, DamageInfo, DamageInfo> call, int index)
            {
                buffDef = buff;
                onHitIndex = index;
                procIndex = Convert.ToSingle(Math.Round(index / 10000f, 4));
                callOnHit = call;
                buffDuration = duration;
            }
        }

        //Assigns damage type with given buff
        internal static CustomDamageType AssignNewDamageType(BuffDef buffToApply, float duration)
        {
            //Index starts at 1, is intentional
            int index = damageTypesList.Count + 1;
            CustomDamageType onHit = new CustomDamageType(buffToApply, duration, index) { };
            damageTypesList.Add(onHit);
            return onHit;
        }

        //Assigns damage type with given method
        private static CustomDamageType AssignNewDamageType(Func<HealthComponent, DamageInfo, DamageInfo> method)
        {
            //Index starts at 1, is intentional
            int index = damageTypesList.Count + 1;
            CustomDamageType onHit = new CustomDamageType(method, index) { };
            damageTypesList.Add(onHit);
            return onHit;
        }

        //Assigns damage type with buff and method
        private static CustomDamageType AssignNewDamageType(BuffDef buffToApply, float duration, Func<HealthComponent, DamageInfo, DamageInfo> method)
        {
            //Index starts at 1, intentional
            int index = damageTypesList.Count + 1;
            CustomDamageType onHit = new CustomDamageType(buffToApply, duration, method, index) { };
            damageTypesList.Add(onHit);
            return onHit;
        }

        //Simple method to simplify making buffs
        private static BuffDef BuffBuilder(Color color, bool canStack, Sprite icon, bool isDebuff, string buffName)
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

        //This handles the temporal chain on hit damagetype
        private static DamageInfo TemporalChainHandler(HealthComponent component, DamageInfo info)
        {
            //Establish component for referencing
            TemporalChainHandler chainHandler;
            //If it exists on the body, use it, otherwise add it
            if (!component.body.gameObject.TryGetComponent<TemporalChainHandler>(out chainHandler)) chainHandler = component.body.gameObject.AddComponent<TemporalChainHandler>();
            //Add a stack of the buff to the body affected
            component.body.AddBuff(buffDefTemporalChains);
            //Set the inflictor to whoever is do the hurty
            chainHandler.inflictor = info.attacker;
            //Refresh the debuff
            chainHandler.ResetTimer();
            //Returns the info for the rest to be done normally
            return info;
        }

        //Fixes proc coefficient and removes the encoded index (x.xyyyy -> x.x0000 where x.x is proc coeff and yyyy is damagetype index)
        internal static float ReturnProcToNormal(float procCoeff)
        {
            //Whole number + tenths place should be the actual proc coeff, we use only what's after that
            var intProc = procCoeff * 10f;
            //Floor it to remove our encoded index and divide it back by 10
            var fixedProc = Math.Floor(intProc) / 10;
            //Return the fixed coeff
            return Convert.ToSingle(fixedProc);
        }

        //Takes the proc coeff from damageinfo and extracts the encoded damage type index
        internal static float ProcToDamageTypeDecoder(float procCoeff)
        {
            //This nabs us the intended proc coefficient as an integer with our index trailing behind in the decimal position
            var intProc = procCoeff * 10;
            //Flattenedcoeff becomes an integer representing the proc coefficient
            var flattenedCoeff = Math.Floor(intProc);
            //This should get us only the decimals behind the proc coefficient
            var decodedIndex = intProc - flattenedCoeff;
            //And boom return the index in form 0.yyyy where y is our index
            return Convert.ToSingle(decodedIndex);
        }
        
        //Takes the intended proc coefficient and tags on the damagetype index
        public static float ProcToDamageTypeEncoder(float damageIndex, float procCoeffToEncode)
        {
            //Turns the intended proc coeff to a pseudo integer
            var intProc = procCoeffToEncode * 10;
            //Gets us just the proc coefficient in case there is random garbage floating behind it
            var flattenedProc = Math.Floor(intProc);
            //Add the index we are using to trail behind the proc coefficient and return it to 'normal'
            var encodedCoeff = (flattenedProc + damageIndex)/10;
            //Return the weird thing we just made
            return Convert.ToSingle(encodedCoeff);
        }
    }
}
