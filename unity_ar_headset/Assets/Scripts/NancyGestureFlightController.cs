using UnityEngine;
using UnityEngine.UI; // Required to communicate with the RawImage component
using TMPro;
using System.Collections.Generic;

public class NancyGestureFlightController : MonoBehaviour
{
    [Header("--- Flight Activation State ---")]
    public bool isGestureFlightActive = true; 
    [SerializeField] private GameObject virtualDroneModel;

    [Header("--- Meta Tracked Hand Links ---")]
    [SerializeField] private OVRHand leftHand;   // Controls Altitude (🤘/✊), Return Home (✋), & Ignition (🤘)
    [SerializeField] private OVRHand rightHand;  // Ergonomic Proportional Gear Shifter + Index Safety Lock

    [Header("--- Home Base Target Configuration ---")]
    [SerializeField] private Transform homeBaseAnchor;
    [SerializeField] private float returnToBaseSpeed = 2.0f;

    [Header("--- Dynamic Physics Calibration ---")]
    [SerializeField] private float flySpeed = 1.5f;             
    [SerializeField] private float movementSmoothing = 2.5f;     
    [SerializeField] private float freezeDampingSpeed = 8.0f;   // Controls the smoothness of the emergency stop
    
    [Tooltip("How many centimeters the hand must move from its resting lap position to begin moving.")]
    [SerializeField] private float gearStickDeadzone = 0.04f;   
    
    [Tooltip("How many centimeters past the deadzone equals maximum speed.")]
    [SerializeField] private float maxJoyStickReach = 0.15f;    

    [Header("--- Virtual Room Boundaries ---")]
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;
    [SerializeField] private float minY = 0.05f; 
    [SerializeField] private float maxY = 3f;
    [SerializeField] private float minZ = -4f;
    [SerializeField] private float maxZ = 4f;

    [Header("--- Dynamic HUD Data Feeds ---")]
    [SerializeField] private TextMeshProUGUI hudFlightModeText;
    
    [Tooltip("Drag the UI Raw Image component that displays the FPV Render Texture here.")]
    [SerializeField] private RawImage fpvMonitorDisplay;

    private MeshRenderer childRenderer;
    private Transform droneTransform;
    private Color lastAppliedColor = Color.clear;

    // Fluid dynamics state cache
    private Vector3 targetVelocity = Vector3.zero;
    private Vector3 currentSmoothVelocity = Vector3.zero;

    // Ergonomic Gear-Stick Tracking Memory
    private Vector3 lapJoystickCenterPoint;
    private bool isJoystickCalibrated = false;

    // Ignition Boot Lock State
    private bool isEngineIgnited = false;

    void Start()
    {
        if (virtualDroneModel != null)
        {
            droneTransform = virtualDroneModel.transform;
            childRenderer = virtualDroneModel.GetComponentInChildren<MeshRenderer>();
        }
        else
        {
            Debug.LogError("[Nancy Avionics] Virtual Drone Model reference missing in Inspector!");
        }

        if (homeBaseAnchor == null && droneTransform != null)
        {
            GameObject automaticHome = new GameObject("Dynamic_Drone_Home_Base");
            automaticHome.transform.position = droneTransform.position;
            homeBaseAnchor = automaticHome.transform;
        }

        // Keep the cockpit camera display monitor hidden at startup until engine ignition
        if (fpvMonitorDisplay != null)
        {
            fpvMonitorDisplay.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[Nancy Avionics] FPV Monitor Display UI RawImage is not assigned!");
        }
    }

    private void Update()
    {
        if (droneTransform == null || childRenderer == null) return;

        if (!isGestureFlightActive)
        {
            baseColorApplication(Color.purple, "SYSTEM OFFLINE: INITIALIZATION REQUIRED");
            return;
        }

        // 1. BOOT IGNITION LOCK
        if (!isEngineIgnited)
        {
            CheckForIgnitionGesture();
            
            if (!isEngineIgnited)
            {
                currentSmoothVelocity = Vector3.zero;
                targetVelocity = Vector3.zero;
                baseColorApplication(new Color(1f, 0.5f, 0f), "🔒 ENGINES LOCKED: SHOW ROCK SIGN (🤘) TO IGNITE");
                return;
            }
        }

        Color frameTargetColor = Color.black;
        string frameHUDMessage = "AVIONICS LINK: HOVERING STANDBY STABLE";

        Vector3 intendedMovementInput = Vector3.zero;
        bool isReturningToBase = false;

        // -------------------------------------------------------------
        // 🫲 LEFT HAND LOOP: ALTITUDE MODIFIERS & HOME BASE TRIGGER
        // -------------------------------------------------------------
        if (leftHand != null && leftHand.IsTracked)
        {
            float index  = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            float middle = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
            float ring   = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
            float pinky  = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

            bool isIndexExtended = index < 0.15f;
            bool isPinkyExtended = pinky < 0.15f;
            bool isMiddleCurled  = middle > 0.20f;
            bool isRingCurled    = ring > 0.20f;

            bool isIndexLowConf  = leftHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.Low;
            bool isMiddleLowConf = leftHand.GetFingerConfidence(OVRHand.HandFinger.Middle) == OVRHand.TrackingConfidence.Low;
            bool isRingLowConf   = leftHand.GetFingerConfidence(OVRHand.HandFinger.Ring) == OVRHand.TrackingConfidence.Low;
            bool isPinkyLowConf  = leftHand.GetFingerConfidence(OVRHand.HandFinger.Pinky) == OVRHand.TrackingConfidence.Low;

            bool isPalmFlatOpen = (index < 0.15f) && (middle < 0.15f) && (ring < 0.15f) && (pinky < 0.15f);
            
            Vector3 palmNormal = leftHand.transform.up; 
            bool isFacingPlayer = Camera.main != null && Vector3.Dot(palmNormal, (Camera.main.transform.position - leftHand.transform.position).normalized) > 0.7f;

            if (isPalmFlatOpen && isFacingPlayer && homeBaseAnchor != null)
            {
                isReturningToBase = true;
                frameTargetColor = Color.blue;
                frameHUDMessage = "🚀 AUTOPILOT ACTIVATED: RETURNING HOME BASE (✋)";
            }
            else if (isIndexExtended && isPinkyExtended && isMiddleCurled && isRingCurled)
            {
                intendedMovementInput.y = 1f;
                frameTargetColor = Color.green;
                frameHUDMessage = "FLIGHT ACTIVE: LIFT UP (🤘)";
            }
            else if ((index > 0.35f || isIndexLowConf) && (middle > 0.35f || isMiddleLowConf) && 
                     (ring > 0.35f || isRingLowConf) && (pinky > 0.35f || isPinkyLowConf))
            {
                intendedMovementInput.y = -1f;
                frameTargetColor = Color.pink;
                frameHUDMessage = "FLIGHT ACTIVE: LANDING/DESCENT (✊)";
            }
            else if (isPalmFlatOpen)
            {
                frameTargetColor = Color.red;
                frameHUDMessage = "FLIGHT_FREEZE_LOCK";
            }
        }

        // -------------------------------------------------------------
        // 🫱 RIGHT HAND LOOP: PROPORTIONAL SHIFTER JOYSTICK WITH INDEX LOCK
        // -------------------------------------------------------------
        if (!isReturningToBase && rightHand != null && rightHand.IsTracked)
        {
            float rightIndexStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            bool isRightIndexLowConf = rightHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.Low;
            
            bool isFlightUnlocked = rightIndexStrength < 0.15f && !isRightIndexLowConf;

            if (isFlightUnlocked)
            {
                Vector3 currentHandPos = rightHand.transform.position;

                if (!isJoystickCalibrated)
                {
                    lapJoystickCenterPoint = currentHandPos;
                    isJoystickCalibrated = true;
                }

                Vector3 shiftingDelta = currentHandPos - lapJoystickCenterPoint;

                if (Mathf.Abs(shiftingDelta.z) > gearStickDeadzone)
                {
                    float excessZ = shiftingDelta.z - (Mathf.Sign(shiftingDelta.z) * gearStickDeadzone);
                    intendedMovementInput.z = Mathf.Clamp(excessZ / maxJoyStickReach, -1f, 1f);
                }

                if (Mathf.Abs(shiftingDelta.x) > gearStickDeadzone)
                {
                    float excessX = shiftingDelta.x - (Mathf.Sign(shiftingDelta.x) * gearStickDeadzone);
                    intendedMovementInput.x = Mathf.Clamp(excessX / maxJoyStickReach, -1f, 1f);
                }

                if (intendedMovementInput.x != 0 || intendedMovementInput.z != 0)
                {
                    frameTargetColor = Color.yellow;
                    frameHUDMessage = "GEARSTICK FLIGHT ACTIVE: DRIFTING AIRFRAME";
                }
            }
            else
            {
                isJoystickCalibrated = false;
                frameHUDMessage = "🔒 SHIFTER LOCKED: POINT INDEX FINGER TO FLY (☝️)";
                frameTargetColor = Color.red; 
            }
        }
        else
        {
            isJoystickCalibrated = false;
        }

        // -------------------------------------------------------------
        // 🌊 THE FLUID INERTIA ENGINE
        // -------------------------------------------------------------
        if (isReturningToBase)
        {
            Vector3 returnDirection = (homeBaseAnchor.position - droneTransform.position).normalized;
            float distanceToHome = Vector3.Distance(homeBaseAnchor.position, droneTransform.position);
            float proximitySpeedGrip = Mathf.Clamp01(distanceToHome / 1.5f);
            targetVelocity = returnDirection * (returnToBaseSpeed * proximitySpeedGrip);
        }
        else
        {
            Vector3 dynamicDirection = (droneTransform.forward * intendedMovementInput.z) + (droneTransform.right * intendedMovementInput.x) + (Vector3.up * intendedMovementInput.y);
            targetVelocity = dynamicDirection * flySpeed;

             // If the drone is moving downward, cut the final vertical speed in half
            if (intendedMovementInput.y < 0)
            {
                targetVelocity.y *= 0.3f; 
            }
        }

        // --- GESTURE FREEZE BRAKE LOCK SYSTEM ---
        if (frameHUDMessage == "FLIGHT_FREEZE_LOCK")
        {
            targetVelocity = Vector3.zero;
            currentSmoothVelocity = Vector3.Lerp(currentSmoothVelocity, Vector3.zero, Time.deltaTime * freezeDampingSpeed);
        }
        else
        {
            currentSmoothVelocity = Vector3.Lerp(currentSmoothVelocity, targetVelocity, Time.deltaTime * movementSmoothing);
        }

        droneTransform.position += currentSmoothVelocity * Time.deltaTime;

        // -------------------------------------------------------------
        // 🔒 CRITICAL BOUNDARY CLAMPING
        // -------------------------------------------------------------
        Vector3 clampedPosition = droneTransform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, minZ, maxZ);

        if (Vector3.Distance(droneTransform.position, clampedPosition) > 0.001f)
        {
            if (clampedPosition.x == minX || clampedPosition.x == maxX) currentSmoothVelocity.x = 0;
            if (clampedPosition.y == minY || clampedPosition.y == maxY) currentSmoothVelocity.y = 0;
            if (clampedPosition.z == minZ || clampedPosition.z == maxZ) currentSmoothVelocity.z = 0;
            
            frameHUDMessage = "✨ WHOOPS! BOUNCED OFF THE INVISIBLE WALL! ✨";
        }

        droneTransform.position = clampedPosition;

        baseColorApplication(frameTargetColor, frameHUDMessage);
    }

    private void CheckForIgnitionGesture()
    {
        if (leftHand != null && leftHand.IsTracked)
        {
            float index  = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            float middle = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
            float ring   = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
            float pinky  = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

            bool isIndexExtended = index < 0.15f;
            bool isPinkyExtended = pinky < 0.15f;
            bool isMiddleCurled  = middle > 0.20f;
            bool isRingCurled    = ring > 0.20f;

            if (isIndexExtended && isPinkyExtended && isMiddleCurled && isRingCurled)
            {
                isEngineIgnited = true;
                
                if (fpvMonitorDisplay != null)
                {
                    fpvMonitorDisplay.gameObject.SetActive(true);
                }
            }
        }
    }

    private void baseColorApplication(Color targetColor, string hudMessage)
    {
        if (lastAppliedColor != targetColor)
        {
            childRenderer.material.color = targetColor;
            lastAppliedColor = targetColor;
        }

        if (hudFlightModeText != null && hudFlightModeText.text != hudMessage)
        {
            hudFlightModeText.text = hudMessage;
        }
    }
}
