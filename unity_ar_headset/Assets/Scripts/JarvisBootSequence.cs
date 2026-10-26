using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JarvisBootSequence : MonoBehaviour
{
    [Header("UI Component Targets")]
    public TMP_Text diagnosticText;
    public CanvasGroup hudCanvasGroup;

    [Header("Boot-Up Settings")]
    public float typeSpeed = 0.03f;
    public float glitchDuration = 0.8f;

    private string[] bootLines = new string[]
    {
        "INITIALIZING MARK MXXVI PLATFORM...",
        "LOADING OS CORE RUNTIP V6.01...",
        "CONNECTING TO QUEST_3_PASSTHROUGH_DRIVERS... OK",
        "SYNCING SATELLITE CORE RETAINERS...",
        "CALIBRATING EYE RECT TRACKERS... COMPLETE",
        "ALL SYSTEMS GO. WELCOME BACK, SIR."
    };

    void Start()
    {
        if (diagnosticText != null && hudCanvasGroup != null)
        {
            StartCoroutine(PlayBootSequence());
        }
    }

    IEnumerator PlayBootSequence()
    {
        hudCanvasGroup.alpha = 0f; // Hide main HUD elements initially
        diagnosticText.text = "";

        // Phase 1: Typewriter boot lines
        foreach (string line in bootLines)
        {
            for (int i = 0; i <= line.Length; i++)
            {
                diagnosticText.text = diagnosticText.text.Substring(0, diagnosticText.text.Length - (i > 0 ? 1 : 0)) + line[Mathf.Min(i, line.Length - 1)] + "_";
                yield return new WaitForSeconds(typeSpeed);
            }
            diagnosticText.text = diagnosticText.text.TrimEnd('_') + "\n";
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);
        diagnosticText.text = "SYSTEMS ONLINE.";

        // Phase 2: Cinematic Screen Flicker
        float elapsed = 0f;
        while (elapsed < glitchDuration)
        {
            hudCanvasGroup.alpha = Random.Range(0.2f, 1.0f); // Quick random flashes
            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
            elapsed += 0.1f;
        }

        hudCanvasGroup.alpha = 1.0f; // Fully stabilize the primary HUD
    }
}
