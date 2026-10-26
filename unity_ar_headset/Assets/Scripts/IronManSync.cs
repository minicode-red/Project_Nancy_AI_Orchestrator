using UnityEngine;

public class IronManSync : MonoBehaviour
{
    [Header("Tracking Target Links")]
    public Transform groundHandlerTarget; 
    public Transform droneCameraMount;    

    [Header("🔗 Network HUD Interface Bridge")]
    public IronManHUD linkedHUDComponent;
    private float lastMapQueryTime = 0f;

    void Update()
    {
        // 1. Maintain asset physical camera vector tracking locks
        if (groundHandlerTarget != null && droneCameraMount != null)
        {
            Vector3 headingVector = groundHandlerTarget.position - droneCameraMount.position;
            if (headingVector != Vector3.zero && headingVector.sqrMagnitude > 0.01f)
            {
                droneCameraMount.rotation = Quaternion.LookRotation(headingVector);
            }
        }

        // 2. Parse incoming network coordinates down into map API interface
        if (TelemetryReceiver.Instance != null && TelemetryReceiver.Instance.HasNewData)
        {
            TelemetryReceiver.Instance.HasNewData = false;
            var data = TelemetryReceiver.Instance.LatestPacket;

            // Strict 5-second cooldown protect loop limits Google API billing charges
            if (linkedHUDComponent != null && Time.time - lastMapQueryTime > 5.0f)
            {
                linkedHUDComponent.TriggerMapAPIRequest(data.latitude, data.longitude);
                lastMapQueryTime = Time.time;
            }
        }
    }
}
