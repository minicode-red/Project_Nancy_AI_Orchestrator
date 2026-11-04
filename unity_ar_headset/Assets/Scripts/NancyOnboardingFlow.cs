using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NancyOnboardingFlow : MonoBehaviour
{
    private enum OnboardingStep { PALM_CHECK, HORNS_TAKEOFF, FIST_LAND, CALIBRATION_COMPLETE }
    //private enum HandGesture { Hand_Activate01, Hand_RockNRoll01, Hand_RockNRoll01 }
    private OnboardingStep currentStep = OnboardingStep.PALM_CHECK;

    [Header("--- HUD Sub-Systems Links ---")]
    [SerializeField] private NancyHUDController hudController;
    [SerializeField] private OVRHand trackingHand;

    [Header("--- Onboarding UI Overlay Panels ---")]
    [SerializeField] private GameObject onboardingCanvasContainer;
    [SerializeField] private GameObject sarMissionInterface; 
    [SerializeField] private GameObject sarSimulationInterface;
    [SerializeField] private GameObject HandGestureInterface;
    [SerializeField] private GameObject NancyLogo;
    [SerializeField] private TextMeshProUGUI instructionTitleText;
    [SerializeField] private TextMeshProUGUI instructionBodyText;
    [SerializeField] private Image gestureVisualPromptIcon; 

    [Header(" --- Animator Gesture Player")]
    [SerializeField] private Animator animator_intro;

    [Header("--- Gesture Icon Assets ---")]
    [SerializeField] private Sprite palmCheckSprite;    // Drag Step 1 Sprite here
    [SerializeField] private Sprite hornsTakeoffSprite;  // Drag Step 2 Sprite here
    [SerializeField] private Sprite fistLandSprite;      // Drag Step 3 Sprite here

    [Header("--- Color Theme Elements ---")]
    public Color tacticalBlue = new Color(0.0f, 0.5f, 1.0f, 1.0f);
    public Color warningAmber = new Color(1.0f, 0.6f, 0.0f, 1.0f);
    public Color successGreen  = new Color(0.0f, 1.0f, 0.4f, 1.0f);
    //public Color HeaderColors = new Color(0.0f, 0.85f, 0.9f, 0.35f);
    public Color HeaderColors = new Color(0.0f, 1.0f, 1.0f, 1.0f); // Cyan color

    private float verificationHoldTimer = 0f;
    private bool stepTransitionActive = false;
    private Camera cachedMainCamera;

    void Start()
    {
        cachedMainCamera = Camera.main;
        sarMissionInterface.SetActive(false);
        sarSimulationInterface.SetActive(false);
        //gestureVisualPromptIcon.SetActive(false);
        if (cachedMainCamera == null)
        {
            Debug.LogError("NancyOnboardingFlow: No camera tagged 'MainCamera' found in the scene!");
        }

        if (onboardingCanvasContainer != null)
        {
            onboardingCanvasContainer.SetActive(true);
        }
        UpdateStepInterface();
    }

    void Update()
    {
        if (stepTransitionActive || trackingHand == null || !trackingHand.IsTracked) return;

        // Fetch proximity values representing hand closure and finger flex
        float indexStrength  = trackingHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middleStrength = trackingHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        float ringStrength   = trackingHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinkyStrength  = trackingHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

        // Evaluation parameters optimized for standard hand geometries (Steps 1 & 2)
        bool isIndexExtended = indexStrength < 0.15f;
        bool isPinkyExtended = pinkyStrength < 0.15f;
        bool isMiddleCurled  = middleStrength > 0.20f;
        bool isRingCurled    = ringStrength > 0.20f;

        // Fist Logic Optimization: Look at finger tracking confidence
        // Meta cameras lose line-of-sight on joints when hidden inside a tight palm fist
        bool isIndexLowConf  = trackingHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.Low;
        bool isMiddleLowConf = trackingHand.GetFingerConfidence(OVRHand.HandFinger.Middle) == OVRHand.TrackingConfidence.Low;
        bool isRingLowConf   = trackingHand.GetFingerConfidence(OVRHand.HandFinger.Ring) == OVRHand.TrackingConfidence.Low;
        bool isPinkyLowConf  = trackingHand.GetFingerConfidence(OVRHand.HandFinger.Pinky) == OVRHand.TrackingConfidence.Low;

        // A valid fist is true if ALL fingers report high pinch strength OR they're curled so tight they go low confidence
        bool isFistClosed = (indexStrength > 0.35f || isIndexLowConf) &&
                            (middleStrength > 0.35f || isMiddleLowConf) &&
                            (ringStrength > 0.35f || isRingLowConf) &&
                            (pinkyStrength > 0.35f || isPinkyLowConf);

        switch (currentStep)
        {
            case OnboardingStep.PALM_CHECK:
                if (cachedMainCamera == null) return;
                
                Vector3 palmNormal = trackingHand.transform.up; 
                Vector3 viewDir = (cachedMainCamera.transform.position - trackingHand.transform.position).normalized;
                
                if (Vector3.Dot(palmNormal, viewDir) > 0.85f)
                {
                    AdvanceStepSequence();
                }
                break;

            case OnboardingStep.HORNS_TAKEOFF:
                if (isIndexExtended && isPinkyExtended && isMiddleCurled && isRingCurled)
                {
                    verificationHoldTimer += Time.deltaTime;
                    if (verificationHoldTimer >= 1.5f) AdvanceStepSequence();
                }
                else
                {
                    verificationHoldTimer = 0f;
                }
                break;

            case OnboardingStep.FIST_LAND:
                // ✊ Fist verification utilizing the hybrid model
                if (isFistClosed)
                {
                    verificationHoldTimer += Time.deltaTime;
                    if (verificationHoldTimer >= 1.5f) AdvanceStepSequence();
                }
                else
                {
                    verificationHoldTimer = 0f;
                }
                break;
        }
    }

    private void AdvanceStepSequence()
    {
        verificationHoldTimer = 0f;
        currentStep++;
        StartCoroutine(ExecuteTransitionAnimation());
    }

    private IEnumerator ExecuteTransitionAnimation()
    {
        stepTransitionActive = true;
        
        if (instructionTitleText != null)
        {
            instructionTitleText.text = "✔ STEP VERIFIED";
            instructionTitleText.color = successGreen;
        }

        yield return new WaitForSeconds(1.2f);
        
        UpdateStepInterface();
        stepTransitionActive = false;
    }

    private void UpdateStepInterface()
    {
        if (instructionTitleText == null || instructionBodyText == null) return;

        instructionTitleText.color = tacticalBlue;

        switch (currentStep)
        {
            case OnboardingStep.PALM_CHECK:
                instructionTitleText.color = HeaderColors;
                instructionTitleText.text = "AVIONICS DIAGNOSTIC: STEP 1/3";                
                instructionBodyText.text = "Raise your <color=#00E5FF>LEFT HAND</color> and turn your palm directly toward your eyes to initialize the Nancy Smart Box HUD environment, also call Drone to base.";
                //if (gestureVisualPromptIcon != null && palmCheckSprite != null)
                //    gestureVisualPromptIcon.sprite = palmCheckSprite;
                animator_intro.Play("Hand_Activate01");
                break;

            case OnboardingStep.HORNS_TAKEOFF:
                instructionTitleText.color = HeaderColors;
                instructionTitleText.text = "AVIONICS DIAGNOSTIC: STEP 2/3";
                instructionBodyText.text = "Form the <color=#FFAC00>SIGN OF THE HORNS (🤘)</color> and hold steady to simulate an autonomous 2-meter airframe takeoff sequence initialization command.";
                //if (gestureVisualPromptIcon != null && hornsTakeoffSprite != null)
                //    gestureVisualPromptIcon.sprite = hornsTakeoffSprite;
                animator_intro.Play("Hand_RockNRoll01");
                break;

            case OnboardingStep.FIST_LAND:
                instructionTitleText.color = HeaderColors;
                instructionTitleText.text = "AVIONICS DIAGNOSTIC: STEP 3/3";
                instructionBodyText.text = "Form a tight, closed <color=#FF1A2A>FIST (✊)</color> and hold steady to simulate triggering the emergency landing vertical recovery protocols.";
                //if (gestureVisualPromptIcon != null && fistLandSprite != null)
                //    gestureVisualPromptIcon.sprite = fistLandSprite;
                animator_intro.Play("Hand_Fit01");
                break;

            case OnboardingStep.CALIBRATION_COMPLETE:
                StartCoroutine(CloseOnboardingOverlay());
                break;
        }
    }

    private IEnumerator CloseOnboardingOverlay()
    {
        instructionTitleText.text = "▲ SYSTEM STANDBY MAVLINK LOCAL SYNCED";
        instructionTitleText.color = successGreen;
        instructionBodyText.text = "Decentralized edge computing node processing fully authorized. Initializing tactical vision camera scanning pipeline...";

        yield return new WaitForSeconds(2.0f);

        if (onboardingCanvasContainer != null)
        {
            onboardingCanvasContainer.SetActive(false);
            HandGestureInterface.SetActive(false);
            NancyLogo.SetActive(false);
            sarMissionInterface.SetActive(true);
            sarSimulationInterface.SetActive(true);
        }
    }
}
