// PlayerHealth.cs
//
// Health, death and respawn for the player.
//
// - Implements IDamageable, so anything that damages enemies can damage the
//   player too (hazards, future enemy attacks).
// - Short invulnerability window after each hit.
// - Dies at 0 HP or when falling below "killHeight".
// - On death: player controls are disabled, then after "respawnDelay" the
//   player is moved to the last checkpoint (or the start position), motion
//   is reset and health is refilled.
// - Exposes C# events (HealthChanged, Died, Respawned) for the HUD and
//   other systems to listen to.
//
// SETUP:
//   1. Attach to the "Player" GameObject (same one as PlayerMovementController).
//   2. Optional: place Checkpoint and KillZone objects in the level.
//   (Momentum > Add Health & Respawn To Scene does this for you.)

using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementController))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerabilityTime = 0.5f;

    [Header("Death & Respawn")]
    [SerializeField] private float killHeight = -20f;   // falling below this Y kills the player
    [SerializeField] private float respawnDelay = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugHealth = true; // simple on-screen readout until the HUD exists

    private PlayerMovementController movement;
    private GrappleHookController grapple;
    private MomentumMeleeAttack attack;
    private CharacterController controller;

    private float currentHealth;
    private float invulnerabilityTimer;
    private bool isDead;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    /// <summary>(currentHealth, maxHealth)</summary>
    public event Action<float, float> HealthChanged;
    public event Action Died;
    public event Action Respawned;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        movement = GetComponent<PlayerMovementController>();
        grapple = GetComponent<GrappleHookController>();
        attack = GetComponent<MomentumMeleeAttack>();
        controller = GetComponent<CharacterController>();

        currentHealth = maxHealth;
        respawnPosition = transform.position;
        respawnRotation = transform.rotation;
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }

        if (!isDead && transform.position.y < killHeight)
        {
            Kill();
        }
    }

    // ---------------- Damage ----------------

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (isDead || amount <= 0f || invulnerabilityTimer > 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        invulnerabilityTimer = invulnerabilityTime;
        Debug.Log($"Player took {amount:F1} damage ({currentHealth:F1}/{maxHealth} HP left).");
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Instant death, ignoring invulnerability (kill zones, falling out of the level).</summary>
    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        currentHealth = 0f;
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Die();
    }

    // ---------------- Death & Respawn ----------------

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        respawnPosition = position;
        respawnRotation = rotation;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player died.");

        if (grapple != null)
        {
            grapple.StopGrapple();
        }
        SetControlsEnabled(false);

        Died?.Invoke();
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        // A CharacterController overrides transform changes while enabled,
        // so it must be disabled for the teleport.
        controller.enabled = false;
        transform.SetPositionAndRotation(respawnPosition, respawnRotation);
        controller.enabled = true;

        movement.ResetMotion();

        currentHealth = maxHealth;
        invulnerabilityTimer = invulnerabilityTime;
        isDead = false;
        SetControlsEnabled(true);

        Debug.Log("Player respawned.");
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Respawned?.Invoke();
    }

    private void SetControlsEnabled(bool value)
    {
        movement.enabled = value;
        if (grapple != null) grapple.enabled = value;
        if (attack != null) attack.enabled = value;
    }

    private void OnGUI()
    {
        if (!showDebugHealth)
        {
            return;
        }

        string text = isDead ? "DEAD - respawning..." : $"HP: {currentHealth:F0} / {maxHealth:F0}";
        GUI.Label(new Rect(20f, 20f, 300f, 30f), text);
    }
}
