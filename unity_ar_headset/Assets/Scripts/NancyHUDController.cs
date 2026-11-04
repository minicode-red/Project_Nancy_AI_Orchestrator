using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NancyHUDController : MonoBehaviour
{
    [Header("--- Critical Component Links ---")]
    [SerializeField] private GameObject virtualDroneModel;
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("--- Telemetry Text HUD Displays ---")]
    [SerializeField] private TextMeshProUGUI latitudeText;
    [SerializeField] private TextMeshProUGUI longitudeText;
    [SerializeField] private TextMeshProUGUI altitudeText;
    [SerializeField] private TextMeshProUGUI batteryText;
    [SerializeField] private TextMeshProUGUI flightModeStatusText;

    [Header("--- Visual & Monitor Outputs ---")]
    [SerializeField] private RawImage fpvMonitorDisplay;
    [SerializeField] private TextMeshProUGUI liveReasoningTextBox;

    [Header("--- Dynamic Flight Settings ---")]
    [SerializeField] private float flySpeed = 1.5f;
    [SerializeField] private float movementSmoothing = 2.5f;
    [SerializeField] private float freezeDampingSpeed = 8.0f;
    [SerializeField] private float gearStickDeadzone = 0.04f;
    [SerializeField] private float maxJoyStickReach = 0.15f;

    [Header("--- Mission Interactor Flags ---")]
    [SerializeField] private bool isLockAllowed = false;
    [SerializeField] private bool isRescueConfirmed = false;

    // Interior Telemetry Register Cache
    private float currentLatitude;
    private float currentLongitude;
    private float currentAltitude;
    private float currentBatteryVoltage;
    private string currentFlightMode = "INITIALIZING";
    private int cachedTargetCount = 0;

    private Transform droneTransform;
    private MeshRenderer droneRenderer;
    private Vector3 targetVelocity = Vector3.zero;
    private Vector3 currentSmoothVelocity = Vector3.zero;
    private bool isEngineIgnited = false;

    void Start()
    {
        if (virtualDroneModel != null)
        {
            droneTransform = virtualDroneModel.transform;
            droneRenderer = virtualDroneModel.GetComponentInChildren<MeshRenderer>();
        }
        else
        {
            Debug.LogError("[NANCY HUD] Virtual Drone Model reference missing in Inspector!");
        }

        if (fpvMonitorDisplay != null)
        {
            fpvMonitorDisplay.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (droneTransform == null) return;

        // Process hand gestures and apply smooth translations to the airframe
        ProcessFlightGestures();
        ApplyFluidInertiaPhysics();
    }

    private void ProcessFlightGestures()
    {
        if (leftHand == null || !leftHand.IsTracked) return;

        float index = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middle = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        float ring = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinky = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

        bool isPalmFlatOpen = (index < 0.15f) && (middle < 0.15f) && (ring < 0.15f) && (pinky < 0.15f);

        if (isPalmFlatOpen)
        {
            UpdateHUDFlightMode("FLIGHT_FREEZE_LOCK", Color.red);
        }
    }

    private void ApplyFluidInertiaPhysics()
    {
        if (currentFlightMode == "FLIGHT_FREEZE_LOCK")
        {
            targetVelocity = Vector3.zero;
            currentSmoothVelocity = Vector3.Lerp(currentSmoothVelocity, Vector3.zero, Time.deltaTime * freezeDampingSpeed);
        }
        else
        {
            currentSmoothVelocity = Vector3.Lerp(currentSmoothVelocity, targetVelocity, Time.deltaTime * movementSmoothing);
        }

        droneTransform.position += currentSmoothVelocity * Time.deltaTime;
    }

    /// <summary>
    /// Updates core telemetry values forwarded from the Python network bridge thread.
    /// </summary>
    public void UpdateTelemetryData(float lat, float lon, float alt, float battery, string mode, int targetCount)
    {
        currentLatitude = lat;
        currentLongitude = lon;
        currentAltitude = alt;
        currentBatteryVoltage = battery;

        // Maintain flight freeze if forced locally, otherwise sync with backend status
        if (currentFlightMode != "FLIGHT_FREEZE_LOCK")
        {
            currentFlightMode = mode;
        }

        // --- AUTOMATIC TARGET DETECTION LOCK ENGINE ---
        if (targetCount > 0 && isLockAllowed && currentFlightMode != "FLIGHT_FREEZE_LOCK")
        {
            Debug.LogWarning("⚠️ [AVIONICS AUTO-LOCK] Rescue target verified! Forcing aerodynamic hover freeze lock.");
            UpdateHUDFlightMode("FLIGHT_FREEZE_LOCK", Color.red);

            NancyHUDMuseController museConn = FindFirstObjectByType<NancyHUDMuseController>();
            if (museConn != null)
            {
                museConn.TransmitOutboundCommand("FLIGHT_FREEZE");
            }
        }

        cachedTargetCount = targetCount;

        // Repaint data panels
        if (latitudeText != null) latitudeText.text = $"LAT: {currentLatitude:F6}";
        if (longitudeText != null) longitudeText.text = $"LON: {currentLongitude:F6}";
        if (altitudeText != null) altitudeText.text = $"ALT: {currentAltitude:F2}m";
        if (batteryText != null) batteryText.text = $"BATT: {currentBatteryVoltage:F1}V";
        if (flightModeStatusText != null) flightModeStatusText.text = $"MODE: {currentFlightMode}";
    }

    /// <summary>
    /// Feeds AI situational insight texts onto the HUD panels.
    /// </summary>
    public void DisplayMuseReasoning(string analysisText)
    {
        if (liveReasoningTextBox != null && !isRescueConfirmed)
        {
            liveReasoningTextBox.text = $"[MUSE ANALYSIS]: {analysisText}";
        }
    }

    public void UpdateHUDFlightMode(string modeName, Color feedbackColor)
    {
        currentFlightMode = modeName;
        if (flightModeStatusText != null) flightModeStatusText.text = modeName;
        if (droneRenderer != null) droneRenderer.material.color = feedbackColor;
    }

    public void SetLockAuthorization(bool allowed)
    {
        isLockAllowed = allowed;
        Debug.Log($"[HUD STATE] Automatic Target Lock Authorization changed to: {isLockAllowed}");
    }

    public void TriggerEmergencyRescueConfirmation()
    {
        isRescueConfirmed = true;

        if (liveReasoningTextBox != null)
        {
            liveReasoningTextBox.text = $"<color=red>🚨 [EMERGENCY SERVICES DISPATCHED] 🚨\n" +
                                        $"CRITICAL LOCATION SECURED:\n" +
                                        $"LATITUDE: {currentLatitude:F6}\n" +
                                        $"LONGITUDE: {currentLongitude:F6}</color>";
        }

        Debug.LogError($"🚨 [EMERGENCY SERVICES CALL] Dispatching services to coordinates: {currentLatitude}, {currentLongitude}");
    }
}
