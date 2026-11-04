using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NancyHUDController2 : MonoBehaviour
{
    [Header("--- Critical Simulation Object Links ---")]
    [SerializeField] private GameObject simulatedDroneModel;
    [SerializeField] private OVRHand leftHand;

    [Header("--- Telemetry HUD Dashboard Displays ---")]
    [SerializeField] private TextMeshProUGUI latitudeText;
    [SerializeField] private TextMeshProUGUI longitudeText;
    [SerializeField] private TextMeshProUGUI altitudeText;
    [SerializeField] private TextMeshProUGUI deviceBatteryText;
    [SerializeField] private TextMeshProUGUI systemTimeText;
    [SerializeField] private TextMeshProUGUI flightModeStatusText;
    [SerializeField] private MeshRenderer findNancyA;

    [Header("--- Visual & Monitor Simulation Outputs ---")]
    [SerializeField] private RawImage simulatedFpvMonitor;
    [SerializeField] private TextMeshProUGUI mockReasoningTextBox;

    [Header("--- Interactor & Trainer Override Flags ---")]
    [SerializeField] private bool isLockAllowed = false;
    [SerializeField] private bool isRescueConfirmed = false;
    [SerializeField] private bool isTargetSpottedBySensor = false;

    // Simulated Telemetry Registers
    private float simLatitude = 50.4501f;
    private float simLongitude = 30.5234f;
    private float simAltitude = 0.0f;
    private string currentFlightMode = "READY_TRAINING_STANDBY";
    private float telemetryDriftTimer = 0f;

    private Transform droneTransform;
    private MeshRenderer droneRenderer;

    void Start()
    {
        if (simulatedDroneModel != null)
        {
            droneTransform = simulatedDroneModel.transform;
            droneRenderer = simulatedDroneModel.GetComponentInChildren<MeshRenderer>();
        }
        else
        {
            Debug.LogError("[NANCY STANDALONE] Simulated Drone Model reference missing!");
        }

        if (simulatedFpvMonitor != null)
        {
            simulatedFpvMonitor.gameObject.SetActive(true); 
        }
    }

    void Update()
    {
        UpdateLocalHardwareMetrics();
        ComputeSimulatedTelemetryFromTransform();
        EvaluatePassiveGestures();
        EvaluateMockTargetEngine();
    }

    private void UpdateLocalHardwareMetrics()
    {
        if (systemTimeText != null) systemTimeText.text = $"TIME: {DateTime.Now:HH:mm:ss}";

        float questBatteryLevel = SystemInfo.batteryLevel;
        if (deviceBatteryText != null)
        {
            if (questBatteryLevel >= 0)
                deviceBatteryText.text = $"HMD BATT: {Mathf.RoundToInt(questBatteryLevel * 100f)}%";
            else
                deviceBatteryText.text = $"HMD BATT: USB_POWER"; 
        }
    }

    private void ComputeSimulatedTelemetryFromTransform()
    {
        if (droneTransform != null)
        {
            simAltitude = droneTransform.position.y;
            simLatitude = 50.4501f + (droneTransform.position.z * 0.00001f);
            simLongitude = 30.5234f + (droneTransform.position.x * 0.00001f);
        }

        telemetryDriftTimer += Time.deltaTime;
        if (telemetryDriftTimer >= 0.5f)
        {
            float varianceX = UnityEngine.Random.Range(-0.000002f, 0.000002f);
            float varianceY = UnityEngine.Random.Range(-0.000002f, 0.000002f);
            
            if (latitudeText != null) latitudeText.text = $"LAT: {(simLatitude + varianceX):F6}";
            if (longitudeText != null) longitudeText.text = $"LON: {(simLongitude + varianceY):F6}";
            if (altitudeText != null) altitudeText.text = $"ALT: {simAltitude:F2}m";
            if (flightModeStatusText != null) flightModeStatusText.text = $"MODE: {currentFlightMode}";
            
            telemetryDriftTimer = 0f;
        }
    }

    private void EvaluatePassiveGestures()
    {
        if (leftHand == null || !leftHand.IsTracked) return;

        float index = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middle = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        float ring = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinky = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

        bool isPalmFlatOpen = (index < 0.15f) && (middle < 0.15f) && (ring < 0.15f) && (pinky < 0.15f);

        if (isPalmFlatOpen)
        {
            UpdateHUDFlightMode("SIM_FREEZE_LOCK", Color.red);
        }
        else if (currentFlightMode == "SIM_FREEZE_LOCK" && index > 0.8f) 
        {
            UpdateHUDFlightMode("SIM_MANUAL_GUIDED", Color.yellow);
        }
    }

    /// <summary>
    /// Trigger target confirmation setting safely from external sensors
    /// </summary>
    public void ForceTargetSpottedTrigger(bool spotted)
    { 
        isTargetSpottedBySensor = spotted;
        if(mockReasoningTextBox != null && !isRescueConfirmed){
            mockReasoningTextBox.text = "<color=red><b>🚨 [MUSE SPARK 1.3 MULTIMODAL REASONING ONLINE]</b></color>\n" +
                                        "<b>Target Profile:</b> Biological Signature Verified (Human Hiker)\n" +
                                        "<b>Situational Analysis:</b> Subject is wearing a red jacket, matches missing hiker profile. " +
                                        "Appears trapped on floor terrain by fallen structure timber debris. " +
                                        "Immediate tactical rescue team dispatch advised.";

        }
    }

    private void EvaluateMockTargetEngine()
    {
        // --- TASK 3: MUSE SPARK 1.3 SITUATIONAL REASONING HOOK ---
        if (isTargetSpottedBySensor && isLockAllowed && currentFlightMode != "SIM_FREEZE_LOCK")
        {
            UpdateHUDFlightMode("SIM_FREEZE_LOCK", Color.red);
            
            if (mockReasoningTextBox != null && !isRescueConfirmed)
            {
                mockReasoningTextBox.text = "<color=red><b>🚨 [MUSE SPARK 1.3 MULTIMODAL REASONING ONLINE]</b></color>\n" +
                                            "<b>Target Profile:</b> Biological Signature Verified (Human Hiker)\n" +
                                            "<b>Situational Analysis:</b> Subject is wearing a red jacket, matches missing hiker profile. " +
                                            "Appears trapped on floor terrain by fallen structure timber debris. " +
                                            "Immediate tactical rescue team dispatch advised.";
            }
        }
    }

    

    /// <summary>
    /// --- TASK 2: YOLOv8 EDGE ANOMALY FILTER DETECTION HOOK ---
    /// </summary>
    public void ForceObjectAnomalySpottedTrigger()
    {
        if (isRescueConfirmed) return;

        if (isLockAllowed)
        {
            UpdateHUDFlightMode("SIM_FREEZE_LOCK", Color.yellow);
        }
        else
        {
            UpdateHUDFlightMode("SIM_TARGET_LOCKED", Color.cyan);
        }

        if (mockReasoningTextBox != null)
        {
            mockReasoningTextBox.text = "<color=cyan><b>📡 [YOLOv8 EDGE CORE ENGINE RUNNING]</b></color>\n" +
                                        "<b>Class Identified:</b> 12 (Structural Obstacle Anomaly Cube)\n" +
                                        "<b>Bounding Box Confidence:</b> 89.4%\n" +
                                        "<b>Status:</b> Non-biological signature logged. Active flight vector warning clear.";
        }
    }

    public void UpdateHUDFlightMode(string modeName, Color feedbackColor)
    {
        currentFlightMode = modeName;
        if (flightModeStatusText != null) flightModeStatusText.text = modeName;
        if (droneRenderer != null) droneRenderer.material.color = feedbackColor;
    }

    // ==================================================================
    // 🔘 SIMULATION INTERACTOR BUTTON INJECTIONS
    // ==================================================================
    
    public void SetLockAuthorization(bool allowed)
    {
        isLockAllowed = allowed;
        Debug.Log($"[SIMULATION] Trainer Lock Authorization state toggled to: {isLockAllowed}");
    }

    public void TriggerEmergencyRescueConfirmation()
    {
        isRescueConfirmed = true;
        isTargetSpottedBySensor = false;

        if (mockReasoningTextBox != null)
        {
            mockReasoningTextBox.text = $"<color=orange>🛰️ [MUSE WONDER AUTOMATED BRIEFING COMPILER] 🛰️\n" +
                                        $"MISSION REPORT GENERATED FOR EMERGENCY SERVICES:\n" +
                                        $"LAST SECURED GPS POSITION -> LAT: {simLatitude:F6} | LON: {simLongitude:F6}\n" +
                                        $"MISSION LOG CLOSURE CODE: NOMINAL SUCCESS.</color>";
        }

        NancyRescueTargetSpawner spawner = FindFirstObjectByType<NancyRescueTargetSpawner>();
        if (spawner != null)
        {
            spawner.TerminateMissionSuccess();
            findNancyA.material.color = Color.red;
        }
    }

    public void TriggerEndGameReset(){
        //NancyRescueTargetSpawner spawner1 = FindFirstObjectByType<NancyRescueTargetSpawner>();
        //if (spawner1 != null){
            //spawner1.TerminateMissionSuccess();
        //}
        if (mockReasoningTextBox != null){// || spawner1 != null){
            mockReasoningTextBox.text = "";
            flightModeStatusText.text = "";
            //spawner1.TerminateMissionSuccess();
            findNancyA.material.color = Color.red;
        }

    }

    public void ApplySimulatedVoiceFilter(string filterText)
    {
        if (mockReasoningTextBox != null)
        {
            mockReasoningTextBox.text = $"<color=green>[MUSE VOICE ACTIVE]: Enforced configuration filter: \"{filterText}\"</color>";
        }
    }
}
