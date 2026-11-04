using UnityEngine;

public class DroneVisionController : MonoBehaviour
{
    [Header("--- Edge Compute Optimization ---")]
    [Tooltip("Matches the inference cycle of YOLOv8n running on a local Raspberry Pi 4 edge module.")]
    [SerializeField] private float targetInferenceFPS = 18f;

    [Header("--- Telemetry Extraction Data Outputs ---")]
    public float simulatedAltitude = 0f;
    public float simulatedGroundSpeed = 0f;
    public Vector3 simulatedHeading = Vector3.forward;

    private Camera droneEyeCamera;
    private float frameInterval;
    private float nextFrameTimer;
    private Transform droneTransform;
    private Vector3 lastPosition;

    void Start()
    {
        droneEyeCamera = GetComponent<Camera>();
        droneTransform = transform.parent != null ? transform.parent : transform;
        lastPosition = droneTransform.position;
        
        frameInterval = 1f / targetInferenceFPS;
        
        // Turn off automatic rendering so we can manually trigger it at edge speeds
        if (droneEyeCamera != null)
        {
            droneEyeCamera.enabled = false;
        }
    }

    void Update()
    {
        if (droneTransform == null || droneEyeCamera == null) return;

        // 1. DYNAMIC TELEMETRY EXTRACTION
        // Calculates smooth ground speed and heights to feed our upcoming automated logs
        simulatedAltitude = droneTransform.position.y;
        Vector3 displacement = droneTransform.position - lastPosition;
        float actualSpeed = displacement.magnitude / Time.deltaTime;
        
        // Apply smooth interpolation to speed telemetry to match real flight logs
        simulatedGroundSpeed = Mathf.Lerp(simulatedGroundSpeed, actualSpeed, Time.deltaTime * 3f);
        simulatedHeading = droneTransform.forward;
        lastPosition = droneTransform.position;

        // 2. EDGE LATENCY SIMULATION ENGINE
        // Manually renders the camera view only when the time interval matches the Pi 4 processing speed
        nextFrameTimer += Time.deltaTime;
        if (nextFrameTimer >= frameInterval)
        {
            droneEyeCamera.Render();
            nextFrameTimer = 0f;
        }
    }
}
