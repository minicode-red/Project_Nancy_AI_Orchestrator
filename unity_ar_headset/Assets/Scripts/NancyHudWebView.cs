using UnityEngine;
using UnityEngine.UI;
using System;
using Gree.UnityWebView; 

public class NancyHudWebView : MonoBehaviour
{
    [Header("Nancy Smart Box Target")]
    public string streamUrl = "http://10.94.70.75:8080/stream.mjpg";

    private WebViewObject webViewObject;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Clear background placeholder texture to keep eye strain low
        RawImage rawImage = GetComponent<RawImage>();
        if (rawImage != null) rawImage.texture = null;

        // Initialize the native Android browser subsystem
        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        webViewObject.Init(
            cb: (msg) => Debug.Log($"[NANCY WEBVIEW] Callback: {msg}"),
            err: (err) => Debug.LogWarning($"[NANCY WEBVIEW] Error: {err}"),
            started: (url) => Debug.Log($"[NANCY WEBVIEW] Started loading: {url}"),
            ld: (url) => {
                Debug.Log($"[NANCY WEBVIEW] Fully loaded: {url}");
                UpdateWebViewPosition();
            }
        );

        // Turn on cleartext/unencrypted HTTP network settings for this browser window
        //webViewObject.SetAllowHTTPReadAccess(true);

        // Send the browser window directly to your Nancy Smart Box stream node
        webViewObject.LoadURL(streamUrl.Trim());
        webViewObject.SetVisibility(true);
    }

    void Update()
    {
        // Dynamically forces the browser frame to stick perfectly to your moving HUD panel
        if (webViewObject != null && rectTransform != null)
        {
            UpdateWebViewPosition();
        }
    }

    private void UpdateWebViewPosition()
    {
        // Maps your Unity layout screen boundaries straight to the Quest's physical display pixels
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector2 screenMin = mainCam.WorldToScreenPoint(corners[0]);
        Vector2 screenMax = mainCam.WorldToScreenPoint(corners[2]);

        webViewObject.SetMargins(
            (int)screenMin.x,
            Screen.height - (int)screenMax.y,
            Screen.width - (int)screenMax.x,
            (int)screenMin.y
        );
    }

    void OnDestroy()
    {
        if (webViewObject != null)
        {
            webViewObject.SetVisibility(false);
            Destroy(webViewObject.gameObject);
        }
    }
}
