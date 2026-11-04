using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class NancyHUDMuseController : MonoBehaviour
{
    [Header("--- Central Avionics Link Router ---")]
    [SerializeField] private NancyHUDController mainHUDController;

    [Header("--- Network Connection Configuration ---")]
    [SerializeField] private string pythonBackendIP = "10.193.201.75";
    [SerializeField] private int inboundTelemetryPort = 5006;
    [SerializeField] private int outboundGesturePort = 5007;

    [Header("--- Muse Voice Engine Setup ---")]
    [SerializeField] private string museModelIdentifier = "Llama-4-Maverick-17B-128E-Instruct-FP8";// "muse-spark-1.3";
    [SerializeField] private bool autoEnableVoiceTranscription = true;

    private UdpClient inboundListenerSocket;
    private UdpClient outboundSenderSocket;
    private Thread networkStreamThread;
    private bool isNetworkActive = false;

    // Cache to prevent thread exceptions when invoking Unity UI pipelines
    private string crossThreadPayloadString = "";
    private bool hasNewPacketWaiting = false;

    [Serializable]
    private class PythonTelemetryPayload
    {
        public string state;
        public string label;
        public float confidence;
        public string time_remaining;
        public int target_count;
        public float pi_temp;
        public float battery_v;
        public float latitude;
        public float longitude;
        public float altitude;
    }

    void Start()
    {
        if (mainHUDController == null)
        {
            mainHUDController = FindFirstObjectByType<NancyHUDController>();
            if (mainHUDController == null) Debug.LogError("[MUSE CONNECT] Critical Warning: No NancyHUDController connected!");
        }

        outboundSenderSocket = new UdpClient();
        StartAsynchronousNetworkWorker();

        // AUTOMATED JUDGES TEST: Instantly fire an outbound initialization handshake to Python
        TransmitOutboundCommand("MUSE_AGENT_BOOTSTRAP_INITIALIZE");
    }

    void Update()
    {
        // Safely parse JSON strings on Unity's main thread to prevent random UI loop crashes
        if (hasNewPacketWaiting)
        {
            ParseAndApplyPayload(crossThreadPayloadString);
            hasNewPacketWaiting = false;
        }
    }

    private void StartAsynchronousNetworkWorker()
    {
        isNetworkActive = true;
        networkStreamThread = new Thread(ReceiveIncomingDataWorker);
        networkStreamThread.IsBackground = true;
        networkStreamThread.Start();
        Debug.Log($"[MUSE INIT] Async listener online on background port: {inboundTelemetryPort}");
    }

    private void ReceiveIncomingDataWorker()
    {
        try
        {
            inboundListenerSocket = new UdpClient(inboundTelemetryPort);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (isNetworkActive)
            {
                byte[] rawDataBytes = inboundListenerSocket.Receive(ref remoteEndPoint);
                string jsonStringData = Encoding.UTF8.GetString(rawDataBytes);

                if (!string.IsNullOrEmpty(jsonStringData))
                {
                    crossThreadPayloadString = jsonStringData;
                    hasNewPacketWaiting = true;
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.LogWarning($"[MUSE SOCKET CLOSE]: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MUSE NETWORK CRASH]: {ex.Message}");
        }
    }

    private void ParseAndApplyPayload(string rawJson)
    {
        try
        {
            PythonTelemetryPayload payload = JsonUtility.FromJson<PythonTelemetryPayload>(rawJson);
            
            if (payload != null && mainHUDController != null)
            {
                // Route telemetry data values directly to the primary HUD class registers
                mainHUDController.UpdateTelemetryData(
                    payload.latitude, 
                    payload.longitude, 
                    payload.altitude, 
                    payload.battery_v, 
                    payload.state,
                    payload.target_count
                );

                // If Muse Spark returns descriptive situational insights, paint them onto the screen
                if (!string.IsNullOrEmpty(payload.label) && payload.label != "None")
                {
                    string analyticalString = $"🤖 [MUSE SPARK REASONING]: {payload.label} (Conf: {payload.confidence * 100:F0}%). " +
                                             $"Telemetry core reporting {payload.pi_temp:F1}°C.";
                    mainHUDController.DisplayMuseReasoning(analyticalString);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[JSON SERIALIZATION ERROR] Failed parsing Python buffer data: {ex.Message}");
        }
    }

    /// <summary>
    /// Public interface allowing other scripts to seamlessly transmit plaintext strings 
    /// or instruction filters to the Python core.
    /// </summary>
    public void TransmitOutboundCommand(string commandPayload)
    {
        try
        {
            if (outboundSenderSocket != null && !string.IsNullOrEmpty(commandPayload))
            {
                byte[] byteBuffer = Encoding.UTF8.GetBytes(commandPayload);
                outboundSenderSocket.Send(byteBuffer, byteBuffer.Length, pythonBackendIP, outboundGesturePort);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MUSE TRANSMIT ERROR] Failed sending string to backend: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts a raw Unity AudioClip memory block into a standardized 16-bit PCM 
    /// binary byte stream and transmits it across the low-latency UDP socket pipeline.
    /// </summary>
    public void TransmitVoiceStream(AudioClip voiceClip)
    {
        if (voiceClip == null)
        {
            Debug.LogWarning("[MUSE AUDIO] Transmit aborted: Provided AudioClip is null.");
            return;
        }

        try
        {
            float[] sampleArray = new float[voiceClip.samples * voiceClip.channels];
            voiceClip.GetData(sampleArray, 0);

            byte[] binaryPcmBuffer = new byte[sampleArray.Length * 2];
            int conversionScaleFactor = 32367;

            for (int i = 0; i < sampleArray.Length; i++)
            {
                float clampedSample = Mathf.Clamp(sampleArray[i], -1f, 1f);
                short bitValueShort = (short)(clampedSample * conversionScaleFactor);

                byte[] localBytePair = BitConverter.GetBytes(bitValueShort);
                binaryPcmBuffer[i * 2] = localBytePair[0];
                binaryPcmBuffer[i * 2 + 1] = localBytePair[1];
            }

            if (outboundSenderSocket != null)
            {
                outboundSenderSocket.Send(binaryPcmBuffer, binaryPcmBuffer.Length, pythonBackendIP, outboundGesturePort);
                Debug.Log($"[MUSE AUDIO] Asynchronously dispatched {binaryPcmBuffer.Length} bytes of raw audio stream to backend.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MUSE AUDIO EXCEPTION] Encoding pipeline broke: {ex.Message}");
        }
    }

    // ==================================================================
    // 🔘 POKE INTERACTOR BUTTON UI INTERFACES
    // ==================================================================

    /// <summary>
    /// POKE BUTTON 1: Connect this method to your UI Toggle button component.
    /// Pass true when toggled ON (Lock Allowed), and false when toggled OFF (No Lock Allowed).
    /// </summary>
    public void OnPokeButton1Toggled(bool isButtonOn)
    {
        if (mainHUDController != null)
        {
            mainHUDController.SetLockAuthorization(isButtonOn);
        }
    }

    /// <summary>
    /// POKE BUTTON 2: Connect this method to your UI Push Button event.
    /// Fires an absolute verification payload to Python and logs an explicit emergency services dispatch request.
    /// </summary>
    public void OnPokeButton2Pressed()
    {
        if (mainHUDController != null)
        {
            mainHUDController.TriggerEmergencyRescueConfirmation();
            
            // This tells Python to execute your Meta Muse endpoint processing workflow immediately!
            TransmitOutboundCommand("MUSE_COMPUTE_REQUEST_RESCUE_CONFIRMED_CALL_EMERGENCY_SERVICES");
        }
    }

    void OnDestroy()
    {
        isNetworkActive = false;
        if (inboundListenerSocket != null) inboundListenerSocket.Close();
        if (outboundSenderSocket != null) outboundSenderSocket.Close();
        
        if (networkStreamThread != null && networkStreamThread.IsAlive)
        {
            networkStreamThread.Abort();
        }
        Debug.Log("[MUSE CONNECT] Socket interfaces disconnected cleanly.");
    }
}
