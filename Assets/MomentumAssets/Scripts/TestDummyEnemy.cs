// TestDummyEnemy.cs
//
// A minimal stand-in enemy so you can test MomentumMeleeAttack without
// building a real boss yet. Prints the damage taken and destroys itself
// at 0 HP. Replace with real boss logic later.
//
// SETUP:
//   1. Attach to any GameObject with a Collider (e.g. a Cube).
//   2. Put that GameObject on the same layer you assigned to
//      "hittableLayer" in MomentumMeleeAttack.

using UnityEngine;

public class TestDummyEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount:F1} damage ({currentHealth:F1}/{maxHealth} HP left).");

        if (currentHealth <= 0f)
        {
            Debug.Log($"{gameObject.name} destroyed.");
            Destroy(gameObject);
        }
    }
}
