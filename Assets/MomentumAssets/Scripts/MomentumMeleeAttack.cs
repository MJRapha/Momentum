// MomentumMeleeAttack.cs
//
// Implements the "Momentum is the Weapon" design pillar: a melee attack
// whose damage scales with how fast the player is currently moving.
// Reads speed from PlayerMovementController.Velocity (includes grapple pulls).
//
// SETUP:
//   1. Attach to the same "Player" GameObject as PlayerMovementController.
//   2. Assign "playerCamera" (used as the origin/direction of the attack).
//   3. Put enemies/bosses on "hittableLayer" and have them implement
//      IDamageable (see IDamageable.cs).
//
// INPUT: Fire2 (default: Right Mouse Button / Alt) to attack.

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementController))]
public class MomentumMeleeAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;

    [Header("Attack Settings")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float damagePerSpeedUnit = 2f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackRadius = 0.6f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private LayerMask hittableLayer;

    private PlayerMovementController movement;
    private float cooldownTimer;
    private readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();

    private void Awake()
    {
        movement = GetComponent<PlayerMovementController>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Fire2") && cooldownTimer <= 0f)
        {
            //Debug.Log("Click");
            PerformAttack();
            cooldownTimer = attackCooldown;
        }
    }

    private void PerformAttack()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("MomentumMeleeAttack: playerCamera is not assigned.");
            return;
        }

        // Full 3D speed, so grapple pulls and dives count as momentum too.
        float damage = baseDamage + movement.Velocity.magnitude * damagePerSpeedUnit;

        Collider[] hits = Physics.OverlapSphere(
            playerCamera.position + playerCamera.forward * attackRange,
            attackRadius,
            hittableLayer,
            QueryTriggerInteraction.Ignore);

        hitThisSwing.Clear();
        foreach (Collider hitCollider in hits)
        {
            // Colliders are often on child objects (body parts), with the
            // IDamageable script on the root, so search up the hierarchy.
            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || !hitThisSwing.Add(damageable))
            {
                continue; // not damageable, or already hit by this swing via another collider
            }

            damageable.TakeDamage(damage, hitCollider.ClosestPoint(playerCamera.position));
        }
    }

    // Lets you see the attack's reach in the Scene view while tuning values.
    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerCamera.position + playerCamera.forward * attackRange, attackRadius);
    }
}
