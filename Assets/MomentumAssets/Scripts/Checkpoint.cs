// Checkpoint.cs
//
// When the player walks through this trigger, it becomes their respawn
// point. The player respawns at this object's position, facing its
// forward (blue) axis.
//
// SETUP:
//   1. Create a GameObject with a Collider (e.g. a Box Collider) and tick
//      "Is Trigger".
//   2. Add this script. Rotate the object so its blue arrow points the way
//      the player should face after respawning.

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.1f, 0f);

    private bool activated;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
        {
            return;
        }

        // Respawn facing the checkpoint's forward direction (yaw only).
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Quaternion rotation = forward.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(forward.normalized, Vector3.up)
            : player.transform.rotation;

        player.SetCheckpoint(transform.position + spawnOffset, rotation);

        if (!activated)
        {
            activated = true;
            Debug.Log($"Checkpoint reached: {name}");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1f, 2f, 1f));
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward * 1.5f);
    }
}
