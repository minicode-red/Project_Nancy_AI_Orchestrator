using UnityEngine;
using TMPro;

public class NancyDroneRadarSensor : MonoBehaviour
{
    [Header("--- Direct Object Reference Matrix ---")]
    [SerializeField] private GameObject dronecude;
    [SerializeField] private MeshRenderer findNancy;

    [Header("--- UI Logging Outputs ---")]  
    [SerializeField] private TextMeshProUGUI loginfo;
    [SerializeField] private TextMeshProUGUI loginfo4;
    
    private void OnTriggerEnter(Collider other)
    {
        // 1. Establish the HUD reference early
        NancyHUDController2 hud = FindFirstObjectByType<NancyHUDController2>();
        if (hud == null) return;

        // 2. Build the log message dynamically
        string logMessage = $"INCOMING - {other.name}  ";

        if (dronecude != null && dronecude.CompareTag(this.tag))
        {
            logMessage = " Human ";
            loginfo.text = logMessage;
            loginfo4.text = logMessage;
            
        }

        //Evaluate tags and fire HUD events
        if (other.CompareTag("DroneN"))
        {
            logMessage = "🔥 HUMAN DETECTED ";
            loginfo.text = logMessage;
            logMessage = "🔥 HUMAN \nDETECTED ";
            loginfo4.text = logMessage;
            hud.ForceTargetSpottedTrigger(true);
            if (findNancy != null && findNancy.material != null){
                findNancy.material.color = Color.green;
            }
        }
        else
        {
            logMessage = $"I AM {this.tag}";
            loginfo.text = logMessage;
            logMessage = $"I AM \n{this.tag}";
            loginfo4.text = logMessage;
            //hud.ForceTargetSpottedTrigger(true);
        }

        // 4. Update the UI text exactly once at the end
        if (loginfo != null)
        {
            loginfo.text = logMessage;
        }
    }
}
