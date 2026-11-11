// CameraLook.cs
//
// Simple first-person mouse look. Attach this to the Camera itself
// (child of the Player object). The player body yaw rotation is applied
// to "playerBody" so wall-run raycasts (which use transform.right/forward
// on the player) stay in sync with where the camera is facing.
//
// SETUP:
//   1. Attach to the Main Camera, which should be a child of "Player".
//   2. Drag the "Player" root object into the "playerBody" field.

using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
