// GrappleHookController.cs
//
// A simple raycast-based grapple hook. On Fire1 (left mouse button by
// default), it casts from the camera. If it hits something on
// "grappleableLayer", the player is pulled toward that point until they
// get close, release the button, or press Jump (which jumps out of the
// pull while keeping momentum).
//
// This script does NOT move the CharacterController itself. It only
// tracks grapple state and supplies a pull velocity; PlayerMovementController
// applies it in its single controller.Move() call. That way gravity doesn't
// build up during the pull, and the pull speed is kept on release
// (slingshot) and counts toward momentum-based damage.
//
// SETUP:
//   1. Attach to the same "Player" GameObject as PlayerMovementController.
//   2. Assign "playerCamera" (the first-person camera).
//   3. Put anything the hook can attach to on the layers in
//      "grappleableLayer" (by default: Wall + Grappleable).
//   4. (Optional) Assign a LineRenderer for the visual rope.
//
// INPUT: Fire1 (default: Left Mouse Button / Ctrl) hold to grapple, release to let go.

using UnityEngine;

[RequireComponent(typeof(PlayerMovementController))]
public class GrappleHookController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private LineRenderer rope;

    [Header("Grapple Settings")]
    [SerializeField] private float maxGrappleDistance = 25f;
    [SerializeField] private float pullSpeed = 20f;
    [SerializeField] private float releaseDistance = 1.5f;
    [SerializeField] private LayerMask grappleableLayer;

    private CharacterController controller;
    private bool isGrappling;
    private Vector3 grapplePoint;
    private Vector3 lastPullVelocity;

    public bool IsGrappling => isGrappling;
    public Vector3 GrapplePoint => grapplePoint;

    private Vector3 BodyCenter => controller != null ? transform.position + controller.center : transform.position;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (rope != null)
        {
            rope.enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            TryStartGrapple();
        }

        if (Input.GetButtonUp("Fire1"))
        {
            StopGrapple();
        }

        if (isGrappling)
        {
            UpdateRopeVisual();
        }
    }

    private void TryStartGrapple()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("GrappleHookController: playerCamera is not assigned.");
            return;
        }

        bool hitSomething = Physics.Raycast(
            playerCamera.position,
            playerCamera.forward,
            out RaycastHit hit,
            maxGrappleDistance,
            grappleableLayer,
            QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            isGrappling = true;
            grapplePoint = hit.point;

            if (rope != null)
            {
                rope.enabled = true;
                rope.positionCount = 2;
            }
        }
    }

    /// <summary>
    /// Velocity the player should move with this frame while grappling.
    /// Called by PlayerMovementController. Releases the grapple once the
    /// player is within releaseDistance and returns the last pull velocity
    /// so the momentum carries on.
    /// </summary>
    public Vector3 GetPullVelocity(Vector3 from)
    {
        if (!isGrappling)
        {
            return lastPullVelocity;
        }

        Vector3 toPoint = grapplePoint - from;
        if (toPoint.magnitude <= releaseDistance)
        {
            StopGrapple();
            return lastPullVelocity;
        }

        lastPullVelocity = toPoint.normalized * pullSpeed;
        return lastPullVelocity;
    }

    private void UpdateRopeVisual()
    {
        if (rope == null)
        {
            return;
        }

        rope.SetPosition(0, BodyCenter);
        rope.SetPosition(1, grapplePoint);
    }

    public void StopGrapple()
    {
        isGrappling = false;
        if (rope != null)
        {
            rope.enabled = false;
        }
    }
}
