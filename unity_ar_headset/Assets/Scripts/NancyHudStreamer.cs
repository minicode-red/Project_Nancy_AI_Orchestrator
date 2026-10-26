using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NancyHudStreamer : MonoBehaviour
{
    [Header("Nancy Smart Box Target")]
    public string droneIp = "10.94.70.75";
    public int dronePort = 8080;
    public string streamPath = "/stream.mjpg";

    private RawImage hudScreen;
    private Texture2D mjpegTexture;
    private Thread networkWorkerThread;
    private bool isRunning = false;

    // Double-buffering data pipeline
    private byte[] latestFrameBytes;
    private readonly object bufferLock = new object();
    private bool hasNewFrame = false;

    // Visual eye-strain diagnostic states
    private bool connectionEstablished = false;

    void Start()
    {
        // Automatically finds the RawImage on this object or nested inside child objects
        hudScreen = GetComponentInChildren<RawImage>();
        if (hudScreen == null)
        {
            Debug.LogError("[NANCY HUD] Error: Please attach a RawImage component to this GameObject or its children!");
            return;
        }

        // Set initial standby color to deep dark navy blue (low reflection, eye-strain friendly)
        hudScreen.color = new Color(0f, 0f, 0.15f, 1f);

        // Optimize texture memory layout for Quest Snapdragon mobile GPUs
        mjpegTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        
        isRunning = true;
        networkWorkerThread = new Thread(BackgroundStreamPipeline) { IsBackground = true };
        networkWorkerThread.Start();
        Debug.Log($"[NANCY HUD CORE] Video pipeline initialized targeting: http://{droneIp}:{dronePort}{streamPath}");
    }

    void Update()
    {
        // ─── MAIN THREAD GPU REPAINT LOOP ───
        if (hasNewFrame)
        {
            lock (bufferLock)
            {
                if (latestFrameBytes != null && latestFrameBytes.Length > 0)
                {
                    // Unpack raw network stream bytes directly into texture asset memory
                    if (mjpegTexture.LoadImage(latestFrameBytes))
                    {
                        mjpegTexture.Apply(); // Forces GPU to repaint pixels and clear standby color

                        if (hudScreen != null)
                        {
                            // Reset color to full white default so image colors display perfectly
                            hudScreen.color = Color.white; 
                            hudScreen.texture = mjpegTexture;
                        }

                        if (!connectionEstablished)
                        {
                            Debug.Log("[NANCY HUD] Network link handshake complete. Video rendering active.");
                            connectionEstablished = true;
                        }
                    }
                }
                hasNewFrame = false;
            }
        }
        else if (!connectionEstablished)
        {
            // Standby Phase: Maintain low-glare dark blue state while searching for network traffic
            if (hudScreen != null)
            {
                hudScreen.color = new Color(0f, 0f, 0.15f, 1f);
            }
        }
    }

    private void BackgroundStreamPipeline()
    {
        while (isRunning)
        {
            TcpClient client = null;
            try
            {
                client = new TcpClient(droneIp, dronePort);
                NetworkStream stream = client.GetStream();

                // Explicit unencrypted HTTP/1.1 request blocks Android OS auto-HTTPS forcing
                string httpRequest = $"GET {streamPath} HTTP/1.1\r\nHost: {droneIp}\r\nConnection: keep-alive\r\n\r\n";
                byte[] requestBytes = Encoding.UTF8.GetBytes(httpRequest);
                stream.Write(requestBytes, 0, requestBytes.Length);

                BinaryReader reader = new BinaryReader(stream);
                MemoryStream frameBuffer = new MemoryStream();

                while (isRunning && client.Connected)
                {
                    byte b = reader.ReadByte();
                    frameBuffer.WriteByte(b);

                    // Scan for standard JPEG End-Of-Image boundary marks (0xFF, 0xD9)
                    if (b == 0xD9 && frameBuffer.Length > 2)
                    {
                        frameBuffer.Position = frameBuffer.Length - 2;
                        if (frameBuffer.ReadByte() == 0xFF)
                        {
                            lock (bufferLock)
                            {
                                latestFrameBytes = frameBuffer.ToArray();
                                hasNewFrame = true;
                            }
                            frameBuffer.SetLength(0); // Instantly flushes allocation memory to prevent garbage tracking leaks
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Soft lock reset: marks connection lost and forces clean retry cycle without halting app
                lock (bufferLock)
                {
                    connectionEstablished = false;
                }
                Debug.LogWarning($"[NANCY HUD LINK] Reconnecting to edge module server node... Reason: {ex.Message}");
                Thread.Sleep(2000); // Guard threshold to prevent CPU thrashing during signal drops
            }
            finally
            {
                if (client != null) client.Close();
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (networkWorkerThread != null && networkWorkerThread.IsAlive) networkWorkerThread.Join(500);
    }

    void OnApplicationQuit()
    {
        isRunning = false;
    }
}
