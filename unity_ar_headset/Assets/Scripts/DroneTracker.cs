using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DroneTracker : MonoBehaviour
{
    [Header("File Path Configuration")]
    [SerializeField] private string csvFilePath = @"C:\Nancy\simulation\recordings\project_nancy_flight_coordinates.csv";
    [SerializeField] private float updateInterval = 0.2f;

    [Header("UI Telemetry Fields")]
    [SerializeField] private TextMeshProUGUI infoTextTarget;

    [Header("2D Map Coordinates Calibration")]
    [SerializeField] private RectTransform mapContainerRect;
    [SerializeField] private RectTransform droneMarkerRect;
    
    // GPS Box Mapping: Calibrate these to match your literal search-and-rescue field boundaries
    [SerializeField] private float minLatitude = 45.0000f;
    [SerializeField] private float maxLatitude = 45.0100f;
    [SerializeField] private float minLongitude = -75.0100f; // Adjusted for West hemisphere horizontal scaling
    [SerializeField] private float maxLongitude = -75.0000f;

    private float _timer;
    private float _simAngle = 0f;
    private bool _isSimulating = false;

    void Start()
    {
        // Safety check: If the directory or file doesn't exist, trigger the simulation layer safely
        if (!File.Exists(csvFilePath))
        {
            Debug.LogWarning($"[TRACKER INFO] Target CSV file not found at path: {csvFilePath}. Booting internal flight simulator mode...");
            
            try
            {
                string directoryPath = Path.GetDirectoryName(csvFilePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Write an initial header row so the tracking parser has a valid schema structure
                File.WriteAllText(csvFilePath, "SystemTime,Lat,Lon,Alt\n00:00:00,45.0050,-75.0050,15.0\n");
                _isSimulating = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SIMULATOR ERROR] Failed to create mock file hierarchy: {ex.Message}");
            }
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;
        
        // If simulation mode is active, continuously generate fake movement data over time
        if (_isSimulating)
        {
            SimulateFlightMovement();
        }

        if (_timer >= updateInterval)
        {
            _timer = 0f;
            ParseTelemetryAndMap();
        }
    }

    private void ParseTelemetryAndMap()
    {
        if (!File.Exists(csvFilePath)) return;

        try
        {
            // Open the file with full shared read/write privileges to avoid conflicts with your Python threads
            using (var fs = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                string lastLine = null;

                // Fast scroll track to capture only the absolute latest written string line
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line)) lastLine = line;
                }

                if (lastLine != null && !lastLine.StartsWith("SystemTime"))
                {
                    string[] data = lastLine.Split(',');
                    if (data.Length >= 4)
                    {
                        float lat = float.Parse(data[1]);
                        float lon = float.Parse(data[2]);
                        float alt = float.Parse(data[3]);

                        // --- Task 2: Update Raw Telemetry Dashboard Numbers ---
                        if (infoTextTarget != null)
                        {
                            string statusColor = _isSimulating ? "yellow" : "green";
                            string statusText = _isSimulating ? "SIMULATOR SIM" : "LIVE HARDWARE";

                            infoTextTarget.text = $"<b>[DRONE CORE TELEMETRY]</b>\n\n" +
                                                  $"• Lat: {lat:F6}°\n" +
                                                  $"• Lon: {lon:F6}°\n" +
                                                  $"• Alt: {alt:F2} m\n\n" +
                                                  $"• Link Status: <color={statusColor}>{statusText}</color>";
                        }

                        // --- Task 3: Update 2D Map Marker Position via Normalization ---
                        if (mapContainerRect != null && droneMarkerRect != null)
                        {
                            // Normalize GPS coordinates down to a fluid scalar range of 0.0 - 1.0
                            float normalizedX = Mathf.InverseLerp(minLongitude, maxLongitude, lon);
                            float normalizedY = Mathf.InverseLerp(minLatitude, maxLatitude, lat);

                            // Calculate the map dimensions directly from its RectTransform component
                            float mapWidth = mapContainerRect.rect.width;
                            float mapHeight = mapContainerRect.rect.height;

                            // Re-center pixels relative to the center origin point anchor of the UI container
                            float pixelX = (normalizedX - 0.5f) * mapWidth;
                            float pixelY = (normalizedY - 0.5f) * mapHeight;

                            droneMarkerRect.anchoredPosition = new Vector2(pixelX, pixelY);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fail-safe pass: Prevents structural file lock updates from throwing exceptions
        }
    }

    private void SimulateFlightMovement()
    {
        // Increment the simulation angle step size tracking loops
        _simAngle += Time.deltaTime * 0.5f; 

        // Generate circular orbit path variables around the absolute mid-point of your map coordinates
        float simLat = 45.0050f + Mathf.Sin(_simAngle) * 0.003f;
        float simLon = -75.0050f + Mathf.Cos(_simAngle) * 0.003f;
        float simAlt = 15.0f + Mathf.Sin(_simAngle) * 3.5f;

        try
        {
            string fakeLine = $"{DateTime.Now:HH:mm:ss},{simLat:F6},{simLon:F6},{simAlt:F2}";
            File.WriteAllText(csvFilePath, $"SystemTime,Lat,Lon,Alt\n{fakeLine}");
        }
        catch (Exception)
        {
            // Protect file writing loops if an asynchronous lock handles data parsing
        }
    }
}
