// KillZone.cs
//
// Instantly kills the player on contact (pits, lava, out-of-bounds).
// The player then respawns at the last checkpoint.
//
// SETUP: add to a GameObject with a Collider set to "Is Trigger".

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            player.Kill();
        }
    }
}
