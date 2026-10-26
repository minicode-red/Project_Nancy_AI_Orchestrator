using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SearchAndRescueMapManager : MonoBehaviour
{
    [Header("UI Canvas Components")]
    public RawImage mapContainer;
    public RectTransform droneMarker;

    [Header("Public Free Map Settings")]
    private string openStreetMapUrlPattern = "https://cartocdn.com{0}/{1}/{2}.png";
    
    [Range(1, 19)] public int zoomLevel = 16; 

    private Vector2Int currentCenterTileIdx = new Vector2Int(-1, -1);
    private Dictionary<string, Texture2D> tileCache = new Dictionary<string, Texture2D>();

    private void Start()
    {
        if (mapContainer == null || droneMarker == null)
        {
            Debug.LogError("🔴 MAP ERROR: Assign UI items in the inspector!");
            return;
        }

        mapContainer.color = Color.white;
        Debug.Log("🌍 MAP STATUS: Requesting open-source public map layers...");
        UpdateDroneLocation(51.5074, -0.1278); // London starter test coordinates
    }

    public void UpdateDroneLocation(double latitude, double longitude)
    {
        Vector2Int targetTileIdx = WorldToTilePos(longitude, latitude, zoomLevel);

        if (targetTileIdx != currentCenterTileIdx)
        {
            currentCenterTileIdx = targetTileIdx;
            StartCoroutine(RefreshMapGrid(currentCenterTileIdx));
        }

        PositionDroneMarker(latitude, longitude, currentCenterTileIdx);
    }

    private IEnumerator RefreshMapGrid(Vector2Int centerTile)
    {
        Texture2D compositeMapTex = new Texture2D(512, 512, TextureFormat.RGB24, false);
        
        // Flattened loops to prevent compilation transmission errors
        int xOffset = 0;
        int yOffset = 0;

        for (xOffset = 0; xOffset < 2; xOffset++)
        {
            for (yOffset = 0; yOffset < 2; yOffset++)
            {
                int tileX = centerTile.x + xOffset;
                int tileY = centerTile.y + yOffset;
                string cacheKey = zoomLevel + "_" + tileX + "_" + tileY;

                Texture2D tileTexture = null;

                if (tileCache.ContainsKey(cacheKey))
                {
                    tileTexture = tileCache[cacheKey];
                }
                else
                {
                    string url = string.Format(openStreetMapUrlPattern, zoomLevel, tileX, tileY);
                    
                    using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
                    {
                        webRequest.SetRequestHeader("User-Agent", "Quest3SearchAndRescueDroneSystemApp/2.0 (MissionControlVRPlatform)");

                        yield return webRequest.SendWebRequest();

                        if (webRequest.result == UnityWebRequest.Result.Success)
                        {
                            tileTexture = DownloadHandlerTexture.GetContent(webRequest);
                            tileCache[cacheKey] = tileTexture;
                        }
                        else
                        {
                            Debug.LogError("❌ MAP BLOCK: Code " + webRequest.responseCode + ". Msg: " + webRequest.error);
                            tileTexture = CreateFallbackGridTexture();
                        }
                    }
                }

                if (tileTexture != null)
                {
                    Color[] pixels = tileTexture.GetPixels();
                    int targetPixelX = xOffset * 256;
                    int targetPixelY = (1 - yOffset) * 256; 
                    compositeMapTex.SetPixels(targetPixelX, targetPixelY, 256, 256, pixels);
                }
            }
        }

        compositeMapTex.Apply();
        mapContainer.texture = compositeMapTex;
    }

    private Texture2D CreateFallbackGridTexture()
    {
        Texture2D fallback = new Texture2D(256, 256, TextureFormat.RGB24, false);
        Color[] fillPixels = new Color[256 * 256];
        Color groundColor = new Color(0.85f, 0.88f, 0.85f); 
        Color gridLineColor = new Color(0.75f, 0.78f, 0.75f);

        for (int i = 0; i < fillPixels.Length; i++)
        {
            int row = i / 256;
            int col = i % 256;
            
            if (row < 3 || row > 252 || col < 3 || col > 252)
                fillPixels[i] = gridLineColor;
            else
                fillPixels[i] = groundColor;
        }
        
        fallback.SetPixels(fillPixels);
        fallback.Apply();
        return fallback;
    }

    private void PositionDroneMarker(double lat, double lon, Vector2Int centerTile)
    {
        double leftLon = TileToLongitude(centerTile.x, zoomLevel);
        double rightLon = TileToLongitude(centerTile.x + 2, zoomLevel);
        double topLat = TileToLatitude(centerTile.y, zoomLevel);
        double bottomLat = TileToLatitude(centerTile.y + 2, zoomLevel);

        float percentageX = (float)((lon - leftLon) / (rightLon - leftLon));
        float percentageY = (float)((lat - bottomLat) / (topLat - bottomLat));

        Rect transformRect = mapContainer.rectTransform.rect;
        float finalUiX = (percentageX * transformRect.width) - (transformRect.width / 2f);
        float finalUiY = (percentageY * transformRect.height) - (transformRect.height / 2f);

        droneMarker.anchoredPosition = new Vector2(finalUiX, finalUiY);
    }

    #region Geographic Conversions
    private Vector2Int WorldToTilePos(double lon, double lat, int zoom)
    {
        int x = (int)Math.Floor((lon + 180.0) / 360.0 * Math.Pow(2.0, zoom));
        int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom));
        return new Vector2Int(x, y);
    }

    private double TileToLongitude(int x, int zoom)
    {
        return x / Math.Pow(2.0, zoom) * 360.0 - 180.0;
    }

    private double TileToLatitude(int y, int zoom)
    {
        double n = Math.PI - 2.0 * Math.PI * y / Math.Pow(2.0, zoom);
        return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
    }
    #endregion
}
