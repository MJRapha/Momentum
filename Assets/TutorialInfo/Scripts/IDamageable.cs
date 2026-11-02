// IDamageable.cs
// Any enemy, boss, or destructible object that can receive damage
// from the player's momentum-based attacks should implement this.

public interface IDamageable
{
    /// <summary>
    /// Called when this object is hit.
    /// </summary>
    /// <param name="amount">Damage amount (already scaled by the attacker's speed, if relevant).</param>
    /// <param name="hitPoint">World-space point where the hit landed, useful for VFX.</param>
    void TakeDamage(float amount, UnityEngine.Vector3 hitPoint);
}
