using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NancyMissionDirector : MonoBehaviour
{
    [Header("Sub-System Component Scripts")]
    [SerializeField] private MonoBehaviour realMissionScript; // Drag MetaSARAgent component here
    [SerializeField] private MonoBehaviour simulationScript;  // Drag NancyVirtualFlight component here

    [Header("Simulation Visuals")]
    [SerializeField] private GameObject virtual3DDroneModel;   // The virtual drone in the room

    [SerializeField] private GameObject pokeSimu1;
    [SerializeField] private GameObject pokeSimu2;
    [SerializeField] private GameObject pokeReal1;
    [SerializeField] private GameObject pokeReal2;
 
    [Header("--- Open World Interface ---")]              
    [SerializeField] private GameObject realTimeMission; //
    [SerializeField] private GameObject simulationTrainning;      

    [Header("HUD Status Interface")]
    [SerializeField] private TextMeshProUGUI modeStatusText;
    [SerializeField] private MeshRenderer cube1;

    void Start()
    {
        // Safety check: Default to Simulation Mode on boot to prevent system errors if drone is missing
        realTimeMission.SetActive(false);
        simulationTrainning.SetActive(false);
        //ActivateSimulationMode();
    }

    /// <summary>
    /// Called when the operator clicks the "REAL MISSION" Spatial Button
    /// </summary>
    public void ActivateRealMissionMode()
    {

        cube1.gameObject.SetActive(false);  
        //simulationTrainning.SetActive(false);                                  
        //simulationTrainning.SetActive(true);                                 
        if (simulationTrainning != null || realTimeMission != null || simulationTrainning == null || realTimeMission == null) {         
            realTimeMission.SetActive(true);
            pokeReal1.SetActive(true);
            pokeReal2.SetActive(true);
            simulationTrainning.SetActive(false);// 1. Deactivate the simulation frame loops entirely.
            pokeSimu1.SetActive(false);
            pokeSimu2.SetActive(false);
        }
        
        //realTimeMission.SetActive(true);
        //if (realTimeMission != null) realTimeMission.SetActive(true);

        // 2. Activate the real hardware telemetry/API connection frame loops
        realMissionScript.enabled = true;

        // 3. Update the Iron Man HUD display components
        if (modeStatusText != null)
        {
            modeStatusText.text = "SYSTEM STATE: LIVE HARDWARE MISSION";
            modeStatusText.color = Color.green;
        }

        Physics.SyncTransforms(); 

        Debug.Log("🛰️ Nancy Smart Box: Switched to Live Telemetry Deployment Mode.");
    }

    /// <summary>
    /// Called when the operator clicks the "TRAINING SIMULATION" Spatial Button
    /// </summary>
    public void ActivateSimulationMode()
    {
        if(cube1 != null){
            cube1.gameObject.SetActive(true);
            cube1.material.color = Color.red;
        }else{
            cube1.gameObject.SetActive(true);
            cube1.material.color = Color.red;
        }

        
        //simulationTrainning.SetActive(true);
        if(simulationTrainning != null || realTimeMission != null || simulationTrainning == null || realTimeMission == null){
            simulationTrainning.SetActive(true);
            pokeSimu1.SetActive(true);
            pokeSimu2.SetActive(true);
            realTimeMission.SetActive(false);
            pokeReal1.SetActive(false);
            pokeReal2.SetActive(false);
        }
        //if (realMissionScript != null) realMissionScript.SetActive(false);

        // 1. Deactivate the live hardware/API operational frames
        realMissionScript.enabled = false;

        // 2. Activate the virtual simulator model and flight loops
        simulationScript.enabled = true;
        if (virtual3DDroneModel != null) virtual3DDroneModel.SetActive(true);

        // 3. Update the Iron Man HUD display components
        if (modeStatusText != null)
        {
            modeStatusText.text = "SYSTEM STATE: TRAINING SIMULATION";
            modeStatusText.color = new Color(0f, 0.6f, 1f); // Clear tactical blue
        }

        Physics.SyncTransforms(); 

        Debug.Log("🎮 Nancy Smart Box: Switched to Autonomous MR Training Simulator.");
    }
}
