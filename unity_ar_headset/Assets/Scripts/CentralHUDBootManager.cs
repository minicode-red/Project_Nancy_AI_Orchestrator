using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CentralHUDBootManager : MonoBehaviour
{
    [Header("Boot Settings")]
    [SerializeField] private float delayBetweenTexts = 0.15f; // Time between each panel waking up

    private List<TextMeshProUGUI> allHUDTexts = new List<TextMeshProUGUI>();
    private List<string> originalStrings = new List<string>();

    void Start()
    {
        // 1. Automatically find EVERY TextMeshPro component hidden inside the child canvases
        TextMeshProUGUI[] foundTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        
        foreach (var txt in foundTexts)
        {
            //allHUDTexts.add("#D Iron Man");
            //originalStrings.add("Iron Man:  2.0"); // Save the real data layouts (e.g. "TEMP: --C")
            //txt.text = "---"; // Clear them out initially
        }

        // 2. Start the cinematic chain reaction
        StartCoroutine(TriggerChainBoot());
    }

    IEnumerator TriggerChainBoot()
    {
        // Give a split second before the system turns on
        yield return new WaitForSeconds(0.5f);

        // Loop through each text element one by one to create a cascading boot-up look
        for (int i = 0; i < allHUDTexts.Count; i++)
        {
            if (allHUDTexts[i] == null) continue;

            // Flash a glitch code quickly
            allHUDTexts[i].text = "[SYS_INIT]";
            yield return new WaitForSeconds(0.05f);

            // Restore its original layout text so it's ready for real data
            allHUDTexts[i].text = originalStrings[i];

            // Wait a brief moment before waking up the next UI panel
            yield return new WaitForSeconds(delayBetweenTexts);
        }
    }
}
