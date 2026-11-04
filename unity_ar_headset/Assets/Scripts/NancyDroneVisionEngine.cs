using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NancyDroneVisionEngine : MonoBehaviour
{
    public enum DroneVisionMode { StandardRGB, ThermalFLIR, LiDARPointCloud }

    [Header("--- Active Sensor State ---")]
    [SerializeField] private DroneVisionMode currentSensorMode = DroneVisionMode.StandardRGB;
    [SerializeField] private TextMeshProUGUI hudSensorTypeText;

    [Header("--- Target Custom Materials ---")]
    [Tooltip("The standard realistic texture material for Nancy.")]
    [SerializeField] private Material standardNancyMaterial;
    [Tooltip("An unlit glowing emissive red/orange shader material for heat tracking.")]
    [SerializeField] private Material thermalHeatMaterial;
    [Tooltip("A wireframe or holographic digital grid shader material.")]
    [SerializeField] private Material lidarGridMaterial;

    [Header("--- Character Renderer Component ---")]
    [Tooltip("Drag Nancy's SkinnedMeshRenderer or MeshRenderer component here.")]
    [SerializeField] private Renderer nancyMeshRenderer;

    [Header("--- Camera Stack Post-Processing Volume ---")]
    [Tooltip("Optional: Drag a UI RawImage or Screen Filter overlay component here to tint the whole FPV feed color scheme.")]
    [SerializeField] private RawImage fpvFilterOverlay;

    void Start()
    {
        ApplyVisionSensorParameters();
    }

    /// <summary>
    /// POKE BUTTON INTERACTION HOOK: Connect this to a dedicated 'Sensor Toggle' 
    /// Poke Button on your HUD workspace to dynamically iterate vision filters.
    /// </summary>
    public void CycleNextSensorVisionMode()
    {
        // Cycle sequentially through the enum modes
        int nextModeIndex = ((int)currentSensorMode + 1) % System.Enum.GetNames(typeof(DroneVisionMode)).Length;
        currentSensorMode = (DroneVisionMode)nextModeIndex;

        ApplyVisionSensorParameters();
    }

    private void ApplyVisionSensorParameters()
    {
        if (nancyMeshRenderer == null) return;

        switch (currentSensorMode)
        {
            case DroneVisionMode.StandardRGB:
                // Normal digital camera profile
                nancyMeshRenderer.material = standardNancyMaterial;
                if (fpvFilterOverlay != null) fpvFilterOverlay.color = new Color(1f, 1f, 1f, 0f); // Completely transparent
                if (hudSensorTypeText != null) hudSensorTypeText.text = "SENSOR FEED: STANDARD RGB EO";
                break;

            case DroneVisionMode.ThermalFLIR:
                // High-contrast search tier environment filter
                nancyMeshRenderer.material = thermalHeatMaterial; // Target glows like hot lava
                if (fpvFilterOverlay != null) fpvFilterOverlay.color = new Color(0.05f, 0.1f, 0.4f, 0.35f); // Deep cold blue tint overlay
                if (hudSensorTypeText != null) hudSensorTypeText.text = "SENSOR FEED: THERMAL IR (FLIR)";
                break;

            case DroneVisionMode.LiDARPointCloud:
                // Spatial scanning laser distance matrix
                nancyMeshRenderer.material = lidarGridMaterial; // Target shifts into green wireframe outline
                if (fpvFilterOverlay != null) fpvFilterOverlay.color = new Color(0.0f, 0.5f, 0.0f, 0.15f); // Micro matrix green tint overlay
                if (hudSensorTypeText != null) hudSensorTypeText.text = "SENSOR FEED: ACTIVE LIDAR ECHO MAP";
                break;
        }

        Debug.Log($"[AVIONICS CORE] Drone vision sensor array successfully reconfigured to: {currentSensorMode}");
    }
}
