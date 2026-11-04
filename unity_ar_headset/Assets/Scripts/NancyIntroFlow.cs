using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NancyIntroFlow : MonoBehaviour
{
    private enum IntroState { TEXT_INTRO, GESTURE_QUIZ, MISSION_STORY, INITIALIZE_MAINFRAME }
    private IntroState currentState = IntroState.TEXT_INTRO;

    [Header("--- Phase Layer Containers ---")]
    [SerializeField] private GameObject phase1IntroPanel;     // The absolute black background panel canvas
    [SerializeField] private GameObject phase2MainframePanel; // The complete Iron Man dashboard group

    [Header("--- i. Cinematic Typography Outlets ---")]
    [SerializeField] private TextMeshProUGUI cinematicTitleText;
    [SerializeField] private TextMeshProUGUI cinematicSubTitleText;

    [Header("--- ii. Onboarding Quiz Outlets ---")]
    [SerializeField] private TextMeshProUGUI quizInstructionsText;
    [SerializeField] private OVRHand leftTrackingHand;

    [Header("--- iii. The Story Catalyst (3D Indicator) ---")]
    [SerializeField] private MeshRenderer storyCatalystSphere; // Drop a 3D Sphere mesh here floating in space
    [SerializeField] private Material redFocusMaterial;       // Target searching asset
    [SerializeField] private Material greenSuccessMaterial;   // Target found asset

    [Header("--- System Audio Bridges ---")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip telemetryPingSound;

    private bool stateProcessingGate = false;
    private float staticHoldTimer = 0f;

    void Start()
    {
        // Enforce tight start state boundaries
        if (phase1IntroPanel != null) phase1IntroPanel.SetActive(true);
        if (phase2MainframePanel != null) phase2MainframePanel.SetActive(false);
        if (storyCatalystSphere != null) storyCatalystSphere.gameObject.SetActive(false);

        StartCoroutine(RunCinematicTextIntro());
    }

    // ==============================================================
    // STEP I: CINEMATIC TEXT EFFECT INTRO
    // ==============================================================
    private IEnumerator RunCinematicTextIntro()
    {
        stateProcessingGate = true;
        
        cinematicTitleText.text = "";
        cinematicSubTitleText.text = "";
        quizInstructionsText.text = "";

        yield return new WaitForSeconds(1.0f);

        // Typewriter effect block: NANCY SMART BOX
        string titleStr = "NANCY SMART BOX";
        // 1. Enable vertex gradients on the component
        cinematicTitleText.enableVertexGradient = true;

        // 2. Define your 4 colors
        //Color topLeft = Color.cyan;
        //Color topRight = Color.cyan;
        //Color bottomLeft = Color.black;
        //Color bottomRight = Color.cyan;

        // 3. Create the gradient structure (TL, TR, BL, BR)
        //TMP_VertexGradient customGradient = new TMP_VertexGradient(topLeft, topRight, bottomLeft, bottomRight);
        
        // Ensure this exact assignment layout matches your line 70
        //TMP_VertexGradient customGradient = new TMP_VertexGradient(Color.red, Color.yellow, Color.blue, Color.green);


        // 4. Assign it to your text object
        //cinematicTitleText.colorGradient = customGradient;

        // 5. Change font size while we are at it
        //titleStr.fontSize = 37.0f;
        cinematicTitleText.fontSize = 90f;
        cinematicTitleText.color = Color.cyan;
        //cinematicTitleText.colorGradient(Color.cyan, Color.cyan, Color.black, Color.cyan);

        foreach (char c in titleStr)
        {
            cinematicTitleText.text += c;
            yield return new WaitForSeconds(0.06f);
        }

        yield return new WaitForSeconds(0.5f);

        // Subtitle Typewriter effect: DECENTRALIZED SEARCH & RESCUE MISSION
        string subStr = "DECENTRALIZED SEARCH & RESCUE MISSION";
        foreach (char c in subStr)
        {
            cinematicSubTitleText.text += c;
            yield return new WaitForSeconds(0.04f);
        }

        yield return new WaitForSeconds(2.0f);
        
        currentState = IntroState.GESTURE_QUIZ;
        stateProcessingGate = false;
        //UpdateIntroFlowStateMachine(); //testing 21, uncomment after test
    }

    // ==============================================================
    // STEP II & III: GESTURE QUIZ & THE STORY CATALYST
    // ==============================================================
    void Update()
    {
        Debug.Log("testing missin story 12"); // testing 12
        if (stateProcessingGate || leftTrackingHand == null || !leftTrackingHand.IsTracked) return;
        Debug.Log("testing missin story 13"); // testing 13

        // Extract skeletal states for the simple onboarding quiz checks
        bool isIndexExtended = !leftTrackingHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool isMiddlePinched = leftTrackingHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
        bool isRingPinched   = leftTrackingHand.GetFingerIsPinching(OVRHand.HandFinger.Ring);
        bool isPinkyExtended = !leftTrackingHand.GetFingerIsPinching(OVRHand.HandFinger.Pinky);
        bool isThumbExtended = leftTrackingHand.GetFingerPinchStrength(OVRHand.HandFinger.Thumb) < 0.25f;

        Debug.Log("testing missin story 3"); // testing 3

        if (currentState != IntroState.GESTURE_QUIZ) // testing 3 
        {
            Debug.Log("testing missin story 4"); // testing 4
            AdvanceToMissionStory(); //testing 1
            // The Quiz: Operator must execute the Rock and Roll gesture (🤘) to prove calibration stability
            /*
            if (isIndexExtended && isMiddlePinched && isRingPinched && isPinkyExtended && isThumbExtended &&
                leftTrackingHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) > 0.85f)
            {
                staticHoldTimer += Time.deltaTime;
                if (staticHoldTimer >= 1.5f) // Must hold steady for 1.5s to pass quiz criteria
                {
                    AdvanceToMissionStory(); 
                }
            }
            else
            {
                staticHoldTimer = 0f;
            }
            */
        }
    }

    private void UpdateIntroFlowStateMachine()
    {
        Debug.Log("testing missin story 7"); // testing 7
        if (currentState == IntroState.GESTURE_QUIZ)
        {
            cinematicTitleText.text = "CALIBRATION QUIZ";
            cinematicSubTitleText.text = "Verify controller-less flight uplink connection.";
            //quizInstructionsText.text = "Form the <color=#00E5FF>SIGN OF THE HORNS (🤘)</color> with your hand and hold steady to authorize edge computing channels."; // testing 9, please uncomment
            currentState = IntroState.MISSION_STORY; // testing 10, please comment after testing
            Debug.Log("testing missin story 8"); // testing 8
        }
    }

    private void AdvanceToMissionStory()
    {
        Debug.Log("testing missin story 1"); //testing 4
        staticHoldTimer = 0f;
        currentState = IntroState.MISSION_STORY;
        StartCoroutine(RunMissionStorySequence());
    }

    private IEnumerator RunMissionStorySequence()
    {
        Debug.Log("testing missin story 11"); // testing 11
        stateProcessingGate = true;

        cinematicTitleText.text = "MISSION PROFILE ACTIVE";
        cinematicSubTitleText.text = "Decentralized cyber-resilient tracker deployed.";
        
        // Explain the narrative clearly so judges grasp the situational stakes instantly
        quizInstructionsText.text = "The system is operating completely offline in a simulated infrastructure loss zone. " +
                                    "Focus your eyes on the <color=#FF1A2A>RED TARGET VECTOR</color> floating in front of you. " +
                                    "When the edge AI matches a subject, the matrix will stabilize to green.";

        yield return new WaitForSeconds(4.0f);

        // Spawn the Story Catalyst: The Red Focus Ball
        if (storyCatalystSphere != null)
        {
            storyCatalystSphere.gameObject.SetActive(true);
            storyCatalystSphere.material = redFocusMaterial;
            // Smoothly pulse size to indicate active searching
            storyCatalystSphere.transform.localScale = Vector3.one * 0.15f;
        }

        // Simulating drone edge tracking verification lock window
        yield return new WaitForSeconds(4.5f);

        // Target found! Flip catalyst sphere to Emerald Green
        if (storyCatalystSphere != null)
        {
            storyCatalystSphere.material = greenSuccessMaterial;
            if (audioSource != null && telemetryPingSound != null)
                audioSource.PlayOneShot(telemetryPingSound);
        }

        quizInstructionsText.text = "<color=#00FF66>✔ TARGET SECTORS LOCKOUT COMPLETED.</color>\n\nInitializing the complete tactical mainframe computing display environment...";

        yield return new WaitForSeconds(3.0f);

        // Cleanly dissolve Phase 1 and launch the Mainframe architecture layout completely
        if (storyCatalystSphere != null) storyCatalystSphere.gameObject.SetActive(false);
        
        phase1IntroPanel.SetActive(false);
        phase2MainframePanel.SetActive(true); // Pops open your beautiful Iron Man layout
        
        currentState = IntroState.INITIALIZE_MAINFRAME;
    }
}
