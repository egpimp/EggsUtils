using UnityEngine;
using RoR2;

namespace EggsUtils.Buffs.BuffComponents
{
    [RequireComponent(typeof(CharacterBody))]
    class TemporalChainHandler : MonoBehaviour
    {
        //Body of the person afflicted
        private CharacterBody characterBody;

        private float coolDownTimer;
        //Timer counts down how long after no stacks have been applied to start removing them
        private float removeStacksTimer;

        //Who inflicted the debuff most recently, mostly just for who to check for proc stuff
        public GameObject inflictor;

        //Basically just the stack count of the buff, we can't let it stack normally cause it's awkward that way and doesn't do how we want
        private int slowCount;

        private SetStateOnHurt targetStateOnHurt;
        private void Awake()
        {
            //Stacks last for 8s before falling off
            removeStacksTimer = 8f;
            //Set this to 0 for later
            coolDownTimer = 0f;
            //Establish characterbody
            characterBody = base.GetComponent<CharacterBody>();
        }
        private void FixedUpdate()
        {
            //If they would have more than 8 stacks of the buff somehow
            if(characterBody.GetBuffCount(BuffsLoading.buffDefTemporalChains) > 8)
            {
                //Remove stacks until we are at 8
                for(int i = 0; i < characterBody.GetBuffCount(BuffsLoading.buffDefTemporalChains) - 8; i++) characterBody.RemoveBuff(BuffsLoading.buffDefTemporalChains);
            }

            //Set stack count tracker
            slowCount = characterBody.GetBuffCount(BuffsLoading.buffDefTemporalChains);

            //Handle max stack effect
            if (slowCount == 8 && CanStun())
            {
                //Applies the damage and stun
                Detonate();
                //Remove all stacks of the buff from the person afflicted
                for (int i = 0; i < slowCount; i++) characterBody.RemoveBuff(BuffsLoading.buffDefTemporalChains);
                //Apply the cooldown before we can detonate again
                SetCooldown(3f);
            }

            //Handle stack decay timer, if the timer before stacks are removed is set, continue counting down
            if (removeStacksTimer > 0) removeStacksTimer -= Time.fixedDeltaTime;
            //Instead if it is 0 (Or less somehow)
            else
            {
                //Remove one stack of the buff
                characterBody.RemoveBuff(BuffsLoading.buffDefTemporalChains);
                //Set the timer to 1 instead of the usual 8, this way it falls off fast once it's not been applied for a while, but also not all at once 
                removeStacksTimer = 1f;
            }

            //Handle cooldown timer
            if (coolDownTimer > 0) coolDownTimer -= Time.fixedDeltaTime;
        }

        //Simply refresh the timer, only applied when the buff is also applied
        public void ResetTimer()
        {
            this.removeStacksTimer = 8f;
        }

        //Does the cooldown allow a stun?
        public bool CanStun()
        {
            return (coolDownTimer == 0);
        }

        //Just sets the cooldown with the given timer, we usually do 3s
        public void SetCooldown(float cooldownLength)
        {
            coolDownTimer = cooldownLength;
        }

        //Plain and simply gets the stack count
        public int GetSlowcount()
        {
            return slowCount;
        }

        //Who shot hannibal
        public GameObject GetInflictor()
        {
            //If exists return them
            if (inflictor) return inflictor;
            //Else shit fucked
            else return null;
        }

        //Does the kersplodey, not actually an explosion
        public void Detonate()
        {
            //Establish damageinfo
            DamageInfo damageInfo = new DamageInfo()
            {
                attacker = GetInflictor(),
                damage = 0.1f * characterBody.healthComponent.fullCombinedHealth,
                damageType = DamageType.Generic,
                damageColorIndex = DamageColorIndex.Default,
                position = characterBody.corePosition,
                crit = false,
                procCoefficient = 0,
                procChainMask = default,
                force = Vector3.zero,
                inflictor = GetInflictor()
            };
            //Dish out the damage
            characterBody.healthComponent.TakeDamage(damageInfo);
            //Check if they have the component required to stun them
            targetStateOnHurt = characterBody.gameObject.GetComponent<SetStateOnHurt>();
            //If it exists actually stun them
            if (targetStateOnHurt) targetStateOnHurt.SetStun(2f);
        }
    }
}
