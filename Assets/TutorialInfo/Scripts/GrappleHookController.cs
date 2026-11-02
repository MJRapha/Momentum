// GrappleHookController.cs
//
// A simple raycast-based grapple hook. On Fire1 (left mouse button by
// default), it casts from the camera. If it hits something on
// "grappleableLayer", the player is pulled toward that point until they
// get close, cancel, or leave the ground voluntarily.
//
// This does NOT use a Rigidbody/SpringJoint, so it works directly with
// the CharacterController-based PlayerMovementController above: it talks
// to the same CharacterController via a public "ExternalMove" call.
//
// SETUP:
//   1. Attach to the same "Player" GameObject as PlayerMovementController.
//   2. Assign "playerCamera" (the first-person camera).
//   3. Create a layer (e.g. "Grappleable") for anything the hook can
//      attach to, and assign it to "grappleableLayer".
//   4. (Optional) Assign a LineRenderer for the visual rope.
//
// INPUT: Fire1 (default: Left Mouse Button / Ctrl) to fire/release.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
            PullTowardGrapplePoint();
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
            grappleableLayer);

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

    private void PullTowardGrapplePoint()
    {
        Vector3 toPoint = grapplePoint - transform.position;

        if (toPoint.magnitude <= releaseDistance)
        {
            StopGrapple();
            return;
        }

        Vector3 pullDirection = toPoint.normalized;
        controller.Move(pullDirection * pullSpeed * Time.deltaTime);
    }

    private void UpdateRopeVisual()
    {
        if (rope == null)
        {
            return;
        }

        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, grapplePoint);
    }

    private void StopGrapple()
    {
        isGrappling = false;
        if (rope != null)
        {
            rope.enabled = false;
        }
    }

    public bool IsGrappling => isGrappling;
}
