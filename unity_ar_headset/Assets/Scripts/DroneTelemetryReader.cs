using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class DroneTelemetryReader : MonoBehaviour
{
    [Header("Telemetry Configuration")]
    [SerializeField] private string csvFilePath = @"C:\Nancy\simulation\recordings\project_nancy_flight_coordinates.csv"; 
    [SerializeField] private TextMeshProUGUI infoTextTarget;
    [SerializeField] private float updateIntervalSeconds = 0.5f;

    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= updateIntervalSeconds)
        {
            _timer = 0f;
            ReadLatestTelemetryLine();
        }
    }

    private void ReadLatestTelemetryLine()
    {
        if (!File.Exists(csvFilePath) || infoTextTarget == null) return;

        try
        {
            // Open as read-only and share-ready to completely bypass file access conflicts with Python
            using (var fileStream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                string lastLine = null;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line)) lastLine = line;
                }

                if (lastLine != null && !lastLine.StartsWith("SystemTime"))
                {
                    string[] data = lastLine.Split(',');
                    if (data.Length >= 4)
                    {
                        // Structure metrics natively for the SAR overlay box
                        string hudOutput = $"<b>[PROJECT NANCY TELEMETRY]</b>\n\n" +
                                           $"• Time Index: {data[0]} ms\n" +
                                           $"• Latitude:   {data[1]}°\n" +
                                           $"• Longitude:  {data[2]}°\n" +
                                           $"• Altitude:   {data[3]} meters\n" +
                                           $"• Status:     <color=green>ACTIVE MONITORING</color>";
                        infoTextTarget.text = hudOutput;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Silently pass if file access frames match exactly on the exact same millisecond
        }
    }
}