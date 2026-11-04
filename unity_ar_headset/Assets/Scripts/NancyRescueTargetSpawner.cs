using UnityEngine;
using System;

public class NancyRescueTargetSpawner : MonoBehaviour
{
    [Header("--- Task 1: Target Entity Prefabs ---")]
    [Tooltip("Drag your virtual girl character (Nancy) GameObject or prefab here.")]
    [SerializeField] private GameObject nancyTargetModel;
    [Tooltip("Drag a simple Cube prefab or game object here to act as a debris/visual anomaly.")]
    [SerializeField] private GameObject anomalyCubeModel;

    [Header("--- HUD Avionics Links ---")]
    [Tooltip("Link your active training HUD dashboard here.")]
    [SerializeField] private NancyHUDController2 trainerHUD;

    [Header("--- Spawning Boundary Constraints (MR Room Space) ---")]
    [SerializeField] private float spawnRadiusMin = 1.0f;
    [SerializeField] private float spawnRadiusMax = 3.5f;

    private bool isNancyMissionActive = false;
    private bool isObjectMissionActive = false;

    private Vector3 generatedNancyPosition = Vector3.zero;
    private Vector3 generatedObjectPosition = Vector3.zero;

    void Start()
    {
        // Keep elements hidden at startup until judges tap their respective Poke Buttons
        if (nancyTargetModel != null) nancyTargetModel.SetActive(false);
        if (anomalyCubeModel != null) anomalyCubeModel.SetActive(false);

        if (trainerHUD == null)
        {
            trainerHUD = FindFirstObjectByType<NancyHUDController2>();
        }
    }

    /// <summary>
    /// POKE BUTTON: Linked to 'objectpoke'. Spawns a structural debris anomaly cube randomly.
    /// </summary>
    public void OnObjectPokeButtonPressed()
    {
        if (anomalyCubeModel == null) return;

        // Generate a random position inside the room boundaries
        Vector2 randomPoint = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(spawnRadiusMin, spawnRadiusMax);
        generatedObjectPosition = new Vector3(randomPoint.x, 0.3f, randomPoint.y); // Raised slightly to float as debris

        anomalyCubeModel.transform.position = generatedObjectPosition;
        anomalyCubeModel.SetActive(true);
        isObjectMissionActive = true;

        if (trainerHUD != null)
        {
            trainerHUD.UpdateHUDFlightMode("SIM_SCANNING_DEBRIS", Color.cyan);
        }
        Debug.Log($"📦 [ANOMALY SPAWN] Simulated debris cube placed at MR Grid: {generatedObjectPosition}");
    }

    /// <summary>
    /// POKE BUTTON: Linked to 'findnancypoke'. Spawns Nancy completely randomly on the physical floor layout.
    /// </summary>
    public void OnFindNancyPokeButtonPressed()
    {
        if (nancyTargetModel == null) return;

        // Generates a completely new random 2D circle coordinate just like the cube logic
        Vector2 randomPoint = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(spawnRadiusMin, spawnRadiusMax);
        generatedNancyPosition = new Vector3(randomPoint.x, 0.0f, randomPoint.y); // Y = 0 pins her securely onto your real physical floor mesh

        // Teleport her to the newly generated coordinates and activate her in the scene
        nancyTargetModel.transform.position = generatedNancyPosition;
        nancyTargetModel.SetActive(true);
        isNancyMissionActive = true;

        if (trainerHUD != null)
        {
            // Reset sensor lock in case we are restarting a hunt scenario loop
            trainerHUD.ForceTargetSpottedTrigger(false); 
            trainerHUD.UpdateHUDFlightMode("SIM_HUMANITARIAN_SEARCH", Color.cyan);
        }
        Debug.Log($"👤 [RESCUE SPAWN] Target Nancy randomly placed on MR Floor Grid: {generatedNancyPosition}");
    }

    /// <summary>
    /// TRIGGER SENSOR MATRIX: Called when the drone's sensor radar overlaps with any entity.
    /// </summary>
    public void ReportTargetSpottedByDrone(string detectedType)
    {
        if (trainerHUD == null) return;

        if (detectedType == "Human" && isNancyMissionActive)
        {
            // Forces Muse Spark 1.3 deep situational reasoning block
            trainerHUD.ForceTargetSpottedTrigger(true); 
        }
        else if (detectedType == "Object" && isObjectMissionActive)
        {
            // FIXED: Parameter removed to perfectly match the parameterless signature in NancyHUDController2
            trainerHUD.ForceObjectAnomalySpottedTrigger();
        }
    }

    public void TerminateMissionSuccess()
    {
        isNancyMissionActive = false;
        isObjectMissionActive = false;
        
        if (nancyTargetModel != null) nancyTargetModel.SetActive(false);
        if (anomalyCubeModel != null) anomalyCubeModel.SetActive(false);
    }
}
