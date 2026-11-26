// DamageZone.cs
//
// A hazard that damages the player while they stand in it (spikes, fire,
// electric floor). Deals "damage" once every "damageInterval" seconds.
// The player's own invulnerability window also limits how often it hits.
//
// SETUP: add to a GameObject with a Collider set to "Is Trigger".

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private float damageInterval = 0.75f;

    private float nextDamageTime;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (Time.time < nextDamageTime)
        {
            return;
        }

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
        {
            return;
        }

        player.TakeDamage(damage, other.ClosestPoint(transform.position));
        nextDamageTime = Time.time + damageInterval;
    }
}
