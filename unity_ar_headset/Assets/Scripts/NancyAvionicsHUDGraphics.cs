using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class NancyAvionicsHUDGraphics : MonoBehaviour
{
    [Header("--- Color Palette System ---")]
    [Tooltip("Changes the overlay filter tint color scheme (e.g., clear, thermal blue, or night vision green).")]
    [SerializeField] private Color hudThemeColor = new Color(0f, 1f, 0.5f, 1f); // Neon Avionics Mint Green

    [Header("--- HUD Graphical Overlays ---")]
    [Tooltip("Drag a crosshair sprite texture asset here.")]
    [SerializeField] private Sprite cockpitCrosshairSprite;

    private Image crosshairImageUI;
    private RawImage monitorRawImage;

    void Start()
    {
        monitorRawImage = GetComponent<RawImage>();
        
        // Ensure the monitor is colored correctly
        if (monitorRawImage != null)
        {
            monitorRawImage.color = Color.white; // Keep source camera pixels native
        }

        CreateHolographicOverlays();
    }

    private void CreateHolographicOverlays()
    {
        // 1. Generate an automated runtime Crosshair Overlay GameObject
        if (cockpitCrosshairSprite != null)
        {
            GameObject crosshairObject = new GameObject("HUD_Crosshair_Overlay");
            crosshairObject.transform.SetParent(this.transform, false);

            crosshairImageUI = crosshairObject.AddComponent<Image>();
            crosshairImageUI.sprite = cockpitCrosshairSprite;
            crosshairImageUI.color = hudThemeColor;

            // Scale crosshair sizing cleanly over the center panel
            RectTransform rect = crosshairObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64f, 64f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        // 2. Inject an automated Scanline Interference Matrix
        GameObject scanlineObject = new GameObject("HUD_Interference_Scanlines");
        scanlineObject.transform.SetParent(this.transform, false);

        Image scanlineImage = scanlineObject.AddComponent<Image>();
        scanlineImage.color = new Color(hudThemeColor.r, hudThemeColor.g, hudThemeColor.b, 0.04f); // Ultra-faint interference overlay

        // Stretch across the entire FPV viewport canvas dimension
        RectTransform scanlineRect = scanlineObject.GetComponent<RectTransform>();
        scanlineRect.anchorMin = Vector2.zero;
        scanlineRect.anchorMax = Vector2.one;
        scanlineRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// External API enabling your Vision Shifter to dynamically alter the HUD overlay color matrix 
    /// </summary>
    public void ReconfigureHUDAestheticColor(Color newSensorColor)
    {
        hudThemeColor = newSensorColor;
        if (crosshairImageUI != null) 
        {
            crosshairImageUI.color = hudThemeColor;
        }
    }
}
