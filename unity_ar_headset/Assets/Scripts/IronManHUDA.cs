using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class IronManHUDA : MonoBehaviour
{
    [Header("Hardware Link Elements")]
    public Transform droneTargetAsset;     
    public Transform groundHandlerCamera;  // Assign your VR CenterEyeAnchor node

    [Header("UI Canvas HUD Overlays")]
    public TMP_Text statusMetricsDisplay;  
    public RectTransform targetLockReticle; // Child of a World Space Canvas
    public Image connectionStatusIndicator; 

    [Header("Tracking Parameters")]
    public float operationalBoundaryRadius = 300f; 
    public float hudFloatingDistance = 2.0f; // Floating distance in meters ahead of your face

    [Header("📺 Motion-Sickness Cure Overlays")]
    public RawImage staticParentMapCanvas;    
    public RawImage floatingChildVideoWindow; 
    [SerializeField] private string googleApiKey = "YOUR_DEVELOPER_KEY_HERE"; 

    // Private properties to safely cache coordinate strings without tearing text frames
    private float lastKnownLat = 0f;
    private float lastKnownLon = 0f;

    void Start()
    {
        if (staticParentMapCanvas != null) staticParentMapCanvas.rectTransform.sizeDelta = new Vector2(1024, 1024);
        if (floatingChildVideoWindow != null) floatingChildVideoWindow.rectTransform.sizeDelta = new Vector2(320, 240);
    }

    void Update()
    {
        if (droneTargetAsset == null || groundHandlerCamera == null) return;

        float structuralDistance = Vector3.Distance(groundHandlerCamera.position, droneTargetAsset.position);

        if (statusMetricsDisplay != null)
        {
            statusMetricsDisplay.text = $"LINK STATE: OPERATIONAL\n" +
                                        $"SCOUT DISTANCE: {structuralDistance:F1}m\n" +
                                        $"ALTITUDE CEILING: {droneTargetAsset.position.y:F1}m\n" +
                                        $"LAT: {lastKnownLat:F6}\n" +
                                        $"LON: {lastKnownLon:F6}\n" +
                                        $"GRID ALIGNMENT: LOCKED";
        }

        if (connectionStatusIndicator != null)
        {
            if (structuralDistance > operationalBoundaryRadius)
            {
                connectionStatusIndicator.color = Color.red;
                if (statusMetricsDisplay != null) statusMetricsDisplay.text += "\n[WARNING: BOUNDARY EXCEEDED]";
            }
            else
            {
                connectionStatusIndicator.color = Color.green; 
            }
        }

        if (targetLockReticle != null)
        {
            Vector3 directionToDrone = (droneTargetAsset.position - groundHandlerCamera.position).normalized;
            Vector3 lookDirection = groundHandlerCamera.forward;

            float angularFieldDeviation = Vector3.Angle(lookDirection, directionToDrone);

            if (angularFieldDeviation < 60f)
            {
                targetLockReticle.gameObject.SetActive(true);
                targetLockReticle.position = groundHandlerCamera.position + (directionToDrone * hudFloatingDistance);
                targetLockReticle.rotation = Quaternion.LookRotation(directionToDrone);
                
                float scannerPulse = 1.0f + Mathf.Sin(Time.time * 8f) * 0.05f;
                targetLockReticle.localScale = new Vector3(scannerPulse, scannerPulse, 1.0f);
            }
            else
            {
                targetLockReticle.gameObject.SetActive(false); 
            }
        }
    }

    public void TriggerMapAPIRequest(float lat, float lon)
    {
        // Save coordinates for real-time text visualization on our primary update loop thread
        lastKnownLat = lat;
        lastKnownLon = lon;

        if (staticParentMapCanvas == null) return;
        
        // FIXED URL PATHING: Restructured to utilize the precise, secure path structure required by Google's network gateway parameters
        string mapUrl = $"https://googleapis.com{lat},{lon}&zoom=17&size=1024x1024&key={googleApiKey}";
        StartCoroutine(FetchMapTexture(mapUrl));
    }

    // FIXED: Only ONE clean UnityWebRequest method remains to process your textures
    private System.Collections.IEnumerator FetchMapTexture(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
    
            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedMap = DownloadHandlerTexture.GetContent(www);
                if (staticParentMapCanvas != null)
                {
                    staticParentMapCanvas.texture = downloadedMap;
                }
            }
            else
            {
                Debug.LogWarning($"[MAP API FAIL] HTTP Request error: {www.error}");
            }
        }
    }
}