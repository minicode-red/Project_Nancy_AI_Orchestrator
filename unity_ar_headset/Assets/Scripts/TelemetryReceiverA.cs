using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TelemetryReceiverA : MonoBehaviour
{
    public static TelemetryReceiverA Instance { get; private set; }

    public struct PacketStructure
    {
        public uint timestamp;
        public float latitude;
        public float longitude;
        public float altitude;

        public uint Timestamp => timestamp;
        public float Latitude => latitude;
        public float Longitude => longitude;
        public float Altitude => altitude;
    }

    public bool HasNewData { get; set; } 
    public PacketStructure LatestPacket; 

    [Header("Target 3D Drone Asset Mapping")]
    public Transform autonomousDroneTarget; 

    [Header("Network Pipeline Tuning")]
    public int listenPort = 14550; 

    [Header("High-Precision Matrix Calibration")]
    // 🌍 REMEMBER: Set these values in the Unity Inspector to match your actual takeoff field coordinates!
    [SerializeField] private float originLatitude = 38.8977f;   
    [SerializeField] private float originLongitude = -77.0365f; 
    [SerializeField] private float gpsToMeterFactor = 111000f;  

    private UdpClient udpListener;
    private Thread networkThread;
    private bool isThreadActive = false;

    private Vector3 externalPositionBuffer = Vector3.zero;
    private bool containsNewPacketData = false;
    private readonly object dataLockGate = new object();

    // Cache private reference to avoid expensive GetComponent calls on the network thread
    private IronManHUD cachedHud;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (autonomousDroneTarget == null) autonomousDroneTarget = this.transform;

        // Secure a local handle to the HUD manager script component
        cachedHud = GetComponent<IronManHUD>();

        isThreadActive = true;
        networkThread = new Thread(new ThreadStart(ReceivePacketsBackground));
        networkThread.IsBackground = true;
        networkThread.Start();
        Debug.Log($"[TELEMETRY] Listening for Project Nancy arrays on port {listenPort}...");
    }

    void Update()
    {
        if (autonomousDroneTarget != null)
        {
            lock (dataLockGate)
            {
                // Smooth interpolation filters raw data jitter over phone link channels
                autonomousDroneTarget.position = Vector3.Lerp(autonomousDroneTarget.position, externalPositionBuffer, Time.deltaTime * 6f);
            }
        }
    }

    private void ReceivePacketsBackground()
    {
        try
        {
            udpListener = new UdpClient();
            // Configure socket reuse to ensure the Quest 3 Android OS can easily re-bind during app switches
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));
            
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, listenPort);

            while (isThreadActive)
            {
                byte[] rawPacketBytes = udpListener.Receive(ref remoteEndPoint);
                
                // FIXED: Enforce rigid byte boundary allocations to shield against buffered multi-packet errors
                if (rawPacketBytes.Length != 20) continue;
                if (rawPacketBytes[0] != 0xAA || rawPacketBytes[1] != 0xBB) continue;

                uint timestamp   = BitConverter.ToUInt32(rawPacketBytes, 2);
                float latitude   = BitConverter.ToSingle(rawPacketBytes, 6);
                float longitude  = BitConverter.ToSingle(rawPacketBytes, 10);
                float altitude   = BitConverter.ToSingle(rawPacketBytes, 14);

                float latitudeDelta = latitude - originLatitude;
                float longitudeDelta = longitude - originLongitude;

                // FIXED: Corrected 3D spatial alignment mappings for VR environments
                // Longitude (East/West) ──► Maps to Unity X Axis
                // Latitude (North/South) ──► Maps to Unity Z Axis
                float unityX = longitudeDelta * gpsToMeterFactor;
                float unityZ = latitudeDelta * gpsToMeterFactor;
                float unityY = altitude; 

                lock (dataLockGate)
                {
                    externalPositionBuffer = new Vector3(unityX, unityY, unityZ);
                    containsNewPacketData = true;

                    LatestPacket.timestamp = timestamp;
                    LatestPacket.latitude = latitude;
                    LatestPacket.longitude = longitude;
                    LatestPacket.altitude = altitude;
                    
                    HasNewData = true; 

                    // Trigger the Iron Man live display coordinate system update
                    if (cachedHud != null)
                    {
                        cachedHud.TriggerMapAPIRequest(latitude, longitude);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (isThreadActive) Debug.LogWarning($"[NET BYPASS] Thread cycle slip: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        isThreadActive = false;
        if (udpListener != null) udpListener.Close();
    }

    void OnApplicationQuit()
    {
        isThreadActive = false;
        if (udpListener != null) udpListener.Close();
    }
}