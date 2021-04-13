using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace EggsBuffs.BuffComponents
{
    [RequireComponent(typeof(CharacterBody))]
    class TemporalChainHandler : MonoBehaviour
    {
        private float removeStacksTimer;
        private float coolDownTimer;
        public GameObject inflictor;
        private CharacterBody characterBody;
        private int slowCount;
        private SetStateOnHurt targetStateOnHurt;
        private void Awake()
        {
            removeStacksTimer = 8f;
            coolDownTimer = 0f;
            characterBody = base.GetComponent<CharacterBody>();
        }
        private void FixedUpdate()
        {
            //Keep stack count from exceeding limit
            if(characterBody.GetBuffCount(BuffsLoading.buffDefTemporalChains) > 8)
            {
                for(int i = 0; i < characterBody.GetBuffCount(BuffsLoading.buffDefTemporalChains) - 8; i++)
                {
                    characterBody.RemoveBuff(BuffsLoading.buffDefTemporalChains);
                }
            }
            //Set tracker
            slowCount = characterBody.GetBuffCount(BuffsLoading.buffDefTemporalChains);
            //Handle max stack effect
            if (slowCount == 8 && CanStun())
            {
                Detonate();
                for (int i = 0; i < slowCount; i++)
                {
                    characterBody.RemoveBuff(BuffsLoading.buffDefTemporalChains);
                }
                SetCooldown(3f);
            }
            //Handle stack decay timer
            if (removeStacksTimer > 0)
            {
                removeStacksTimer -= Time.fixedDeltaTime;
            }
            else
            {
                characterBody.RemoveBuff(BuffsLoading.buffDefTemporalChains);
                removeStacksTimer = 1f;
            }
            //Handle cooldown timer
            if (coolDownTimer > 0)
            {
                coolDownTimer -= Time.fixedDeltaTime;
            }
            else
            {
                coolDownTimer = 0;
            }
        }
        public void ResetTimer()
        {
            this.removeStacksTimer = 8f;
        }
        public bool CanStun()
        {
            return (coolDownTimer == 0);
        }
        public void SetCooldown(float cooldownLength)
        {
            coolDownTimer = cooldownLength;
        }
        public int GetSlowcount()
        {
            return slowCount;
        }
        public GameObject GetInflictor()
        {
            if (inflictor)
            {
                return inflictor;
            }
            else
            {
                return null;
            }
        }
        public void Detonate()
        {
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
            characterBody.healthComponent.TakeDamage(damageInfo);
            targetStateOnHurt = characterBody.gameObject.GetComponent<SetStateOnHurt>();
            if (targetStateOnHurt)
            {
                targetStateOnHurt.SetStun(2f);
            }
        }
    }
}
