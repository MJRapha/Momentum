// MomentumMeleeAttack.cs
//
// Implements the "Momentum is the Weapon" design pillar: a melee attack
// whose damage scales with how fast the player is currently moving.
// Reads speed from PlayerMovementController.CurrentSpeed.
//
// SETUP:
//   1. Attach to the same "Player" GameObject as PlayerMovementController.
//   2. Assign "playerCamera" (used as the origin/direction of the attack).
//   3. Put enemies/bosses on "hittableLayer" and have them implement
//      IDamageable (see IDamageable.cs).
//
// INPUT: Fire2 (default: Right Mouse Button / Alt) to attack.

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

        float damage = baseDamage + movement.CurrentSpeed * damagePerSpeedUnit;

        Collider[] hits = Physics.OverlapSphere(
            playerCamera.position + playerCamera.forward * attackRange,
            attackRadius,
            hittableLayer);

        foreach (Collider hitCollider in hits)
        {
            if (hitCollider.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage, hitCollider.ClosestPoint(playerCamera.position));
            }
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
