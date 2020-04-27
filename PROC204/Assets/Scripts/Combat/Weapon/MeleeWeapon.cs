﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    [SerializeField] float attackRate;
    [SerializeField] int damage;
    [SerializeField] float attackRange;
    [SerializeField] ParticleSystem slashEffect;
    [SerializeField] float stunTimeWhenParried = 2f;

    [Header("Sound FX")]
    [SerializeField] RandomAudioPlayer swingPlayer;
    [SerializeField] RandomAudioPlayer hitPlayer;

    public override float AttackRate { get => attackRate; }

    //CACHE REFERENCES

    Mover mover;
    CharacterController charController;
    CombatTarget combatTarget;

    public delegate void OnDealDamage();
    public event OnDealDamage onDealDamage;

    protected override void Awake()
    {
        base.Awake();

        mover = GetComponentInParent<Mover>();
        charController = GetComponentInParent<CharacterController>();
        combatTarget = GetComponentInParent<CombatTarget>();
    }

    private void Update()
    {
        SlashEffect();
    }

    //Enable sword slash when attacking or parrying
    private void SlashEffect()
    {
        if (slashEffect == null) return;

        bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") || animator.GetCurrentAnimatorStateInfo(0).IsName("Parry");

        var emission = slashEffect.emission;
        if (isAttacking) emission.enabled = true;
        else emission.enabled = false;
    }

    //Gets attack center required to place hitbox that detects enemies to be damaged
    private Vector3 GetMeleeAttackCenter()
    {
        Vector3 offset = Vector3.right * mover.Direction * charController.radius; //offset to direction player is facing
        Vector3 attackStartPos =  charController.transform.TransformPoint(charController.center) + offset;

        return attackStartPos + (Vector3.right * mover.Direction * (attackRange / 2)); //Take into account weapon range
    }

    //Hitbox dimensions halved due to halfextents in Physics.OverlapBox()
    private Vector3 GetHitBox()
    {
        return new Vector3(attackRange / 2, charController.height / 2, 1f / 2);
    }

    //Called by melee attack animation event
    //Damages enemies within hitbox
    public void Hit()
    {
        Collider[] colliders = Physics.OverlapBox(GetMeleeAttackCenter(), GetHitBox(), Quaternion.identity, TargetLayerMask);

        foreach (Collider collider in colliders) //Cycle through potential enemies
        {
            CombatTarget enemy = collider.gameObject.GetComponent<CombatTarget>();
            if (enemy != null)
            {
                bool success = enemy.TakeDamage(damage, mover.Position); //successful if enemy not parrying

                if (success)
                {
                    onDealDamage?.Invoke();
                    hitPlayer?.PlayRandomAudio();
                }
                else combatTarget.Stun(stunTimeWhenParried); //stunned if attack unsuccessful
            }
        }        
    }

    //Used by AI controller to see whether AI should move closer to hit target
    public bool CheckTargetInMeleeRange()
    {
        Collider[] colliders = Physics.OverlapBox(GetMeleeAttackCenter(), GetHitBox(), Quaternion.identity, TargetLayerMask);

        foreach (Collider collider in colliders)
        {
            Health health = collider.gameObject.GetComponent<Health>();
            if (health != null && !health.IsDead) return true;
        }
        return false;
    }

    //Visualize melee attack box
    private void OnDrawGizmos()
    {
        if (mover == null) return;
        Gizmos.DrawWireCube(GetMeleeAttackCenter(), GetHitBox() * 2);
    }

    //Use weapon triggers the attack animation
    public override void UseWeapon()
    {
        if (!isReady) return; //Wait until weapon recharged

        isReady = false;

        animator.SetTrigger("attackTrigger");
        swingPlayer?.PlayRandomAudio(); 

        Invoke(nameof(WeaponReady), attackRate);
    }
}
