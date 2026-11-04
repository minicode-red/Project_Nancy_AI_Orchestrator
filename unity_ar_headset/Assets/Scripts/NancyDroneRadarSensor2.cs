using UnityEngine;
using TMPro;

public class NancyDroneRadarSensor2 : MonoBehaviour
{
    [Header("--- Direct Object Reference Matrix ---")]
    [SerializeField] private GameObject dronecude2;

    [Header("--- UI Logging Outputs ---")]  
    [SerializeField] private TextMeshProUGUI loginfo2;
    [SerializeField] private TextMeshProUGUI loginfo3;
    [SerializeField] private MeshRenderer findNancy;
                                                      
    private void OnTriggerEnter(Collider other)
    {
        // 1. Establish the HUD reference early
        NancyHUDController2 hud = FindFirstObjectByType<NancyHUDController2>();
        if (hud == null) return;

        // 2. Build the log message dynamically
        string logMessage = $" INCOMING - {other.name}\n";

        if (dronecude2 != null && dronecude2.CompareTag(this.tag))
        {
            logMessage = " Human ";
            loginfo2.text = logMessage;
            loginfo3.text = logMessage; 
            
        }

        // Evaluate tags and ice HUD event
        if (other.CompareTag("DroneN"))
        {
            logMessage = " ❄️ OBJECT DETECTED ";
            loginfo2.text = logMessage;
            logMessage = " ❄️ OBJECT \nDETECTED ";
            loginfo3.text = logMessage;
            hud.ForceObjectAnomalySpottedTrigger();
            if (findNancy != null && findNancy.material != null){
                findNancy.material.color = Color.green;
            }
        }
        else
        {
            logMessage = $" I AM {this.tag} ";
            loginfo2.text = logMessage;
            logMessage = $" I AM \n{this.tag} ";
            loginfo3.text = logMessage;
            //hud.ForceTargetSpottedTrigger(true);
        }

        // 4. Update the UI text exactly once at the end
        if (loginfo2 != null)
        {
            loginfo2.text = logMessage;
        }
    }
}
