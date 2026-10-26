using UnityEngine;

public class IronManHUB1 : MonoBehaviour
{
    [Header("Tracking Target")]
    [Tooltip("Assign the CenterEyeAnchor from your OVRCameraRig here.")]
    public Transform centerEyeAnchor;

    [Header("HUD Movement Settings")]
    [Tooltip("How fast the HUB catches up to your head movement. Lower = more sluggish lag.")]
    public float followSpeed = 6.0f;
    
    [Tooltip("How fast the HUB matches your head rotation.")]
    public float rotationSpeed = 5.0f;

    [Tooltip("How far forward from your eyes the UI sits.")]
    public float forwardDistance = 0.5f;

    void LateUpdate()
    {
        if (centerEyeAnchor == null) return;

        // 1. Calculate the ideal destination point directly in front of the player's eyes
        Vector3 targetPosition = centerEyeAnchor.position + (centerEyeAnchor.forward * forwardDistance);

        // 2. Smoothly slide (Lerp) the current position toward that target point
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // 3. Smoothly rotate (Slerp) the UI to match the headset's current angle
        transform.rotation = Quaternion.Slerp(transform.rotation, centerEyeAnchor.rotation, Time.deltaTime * rotationSpeed);
    }
}
