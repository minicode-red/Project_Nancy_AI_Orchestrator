using UnityEngine;

public class SmoothHUDLocalSpace : MonoBehaviour
{
    [Header("Tracking Reference")]
    [SerializeField] private Transform targetCamera; 

    [Header("HUD Placement Offset")]
    [SerializeField] private Vector3 offsetFromCamera = new Vector3(0.3f, 0f, 3f);

    [Header("Follow Speed")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 8f;

    [Header("Visual Debugger (LED)")]
    [Tooltip("Drag your 3D Sphere/Cube here")]
    [SerializeField] private Renderer debugLedRenderer; 

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        Debug.Log("Oli my Love");

        // If the camera is missing, keep searching for it every single frame
        if (targetCamera == null)
        {
            // Try searching for the standard Unity Main Camera
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
            // If that fails, try searching specifically for Meta's CenterEyeAnchor
            else
            {
                GameObject oVRCamera = GameObject.Find("CenterEyeAnchor");
                if (oVRCamera != null)
                {
                    targetCamera = oVRCamera.transform;
                }
            }
        }

        // Force our tracking method to run
        TrackAndFollowHead();
    }

    void TrackAndFollowHead()
    {
        // CONDITION 1: Missing critical assignments (Turns YELLOW)
        if (targetCamera == null || transform.parent == null) 
        {
            UpdateLedColor(Color.yellow);
            return;
        }

        try
        {
            // 1. Compute positions
            Vector3 targetWorldPosition = targetCamera.TransformPoint(offsetFromCamera);
            Quaternion targetWorldRotation = targetCamera.rotation;

            // 2. Convert to Local Space
            Vector3 targetLocalPosition = transform.parent.InverseTransformPoint(targetWorldPosition);
            Quaternion targetLocalRotation = Quaternion.Inverse(transform.parent.rotation) * targetWorldRotation;

            // 3. Apply Smooth Movement
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, positionSmoothSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation, rotationSmoothSpeed * Time.deltaTime);

            // CONDITION 2: Code is looping and running completely perfectly (Turns GREEN)
            UpdateLedColor(Color.green);
        }
        catch (System.Exception)
        {
            // CONDITION 3: A math crash or null reference happened inside the loop (Turns RED)
            UpdateLedColor(Color.red);
        }
    }

    // Helper method to change the color of your 3D object dynamically
    void UpdateLedColor(Color targetColor)
    {
        if (debugLedRenderer != null)
        {
            debugLedRenderer.material.color = targetColor;
        }
    }
}
