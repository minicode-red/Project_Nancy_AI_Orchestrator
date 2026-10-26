using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Net;
using System.Threading;

public class MjpegStreamReceiver : MonoBehaviour
{
    [Header("Network Pipeline Target")]
    // 📲 PASTE YOUR RASPBERRY PI 4 HOTSPOT IP HERE IN THE INSPECTOR!
    public string streamUrl = "http://10.16.50.75:8080/stream.mjpg";

    private RawImage targetRawImage;
    private Thread streamThread;
    private bool isRunning = false;
    
    private Texture2D latestFrameTexture;
    private byte[] nextFrameBytesBuffer;
    private readonly object lockGate = new object();
    private bool frameUpdated = false;

    void Start()
    {
        targetRawImage = GetComponent<RawImage>();
        
        // Explicitly create an uninitialized 2x2 texture buffer card
        latestFrameTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        
        isRunning = true;
        streamThread = new Thread(ReceiveStreamBackground) { IsBackground = true };
        streamThread.Start();
        Debug.Log($"[VIDEO ENGINE] MJPEG Stream thread launched targeting: {streamUrl}");
    }

    void Update()
    {
        // ─── THE CRITICAL REPAINT FIX ───
        if (frameUpdated)
        {
            lock (lockGate)
            {
                if (nextFrameBytesBuffer != null && nextFrameBytesBuffer.Length > 0)
                {
                    // 1. Unpack the bytes onto the texture asset memory
                    if (latestFrameTexture.LoadImage(nextFrameBytesBuffer))
                    {
                        // 🔥 CRITICAL FIX: Forces your laptop/Quest GPU to clear the white mask and repaint the pixels!
                        latestFrameTexture.Apply(); 

                        // 2. Link the repainted asset texture to your RawImage screen mesh
                        if (targetRawImage != null) 
                        {
                            targetRawImage.texture = latestFrameTexture;
                        }
                    }
                }
                frameUpdated = false;
            }
        }
    }

    private void ReceiveStreamBackground()
    {
        while (isRunning)
        {
            HttpWebRequest request = null;
            WebResponse response = null;
            Stream stream = null;
            try
            {
                // Force network policies to bypass proxy servers on local hotspot networks
                request = (HttpWebRequest)WebRequest.Create(streamUrl);
                request.Timeout = 4000;
                request.Proxy = null; 
                
                response = request.GetResponse();
                stream = response.GetResponseStream();
                BinaryReader reader = new BinaryReader(stream);

                while (isRunning)
                {
                    byte[] jpegBuffer = FindNextJpegFrame(reader);
                    if (jpegBuffer != null && jpegBuffer.Length > 0)
                    {
                        lock (lockGate)
                        {
                            nextFrameBytesBuffer = jpegBuffer;
                            frameUpdated = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VIDEO STALL] Reconnecting to nancy_core.py stream server... {ex.Message}");
                Thread.Sleep(1500); // Prevents CPU thrashing during network hiccups
            }
            finally
            {
                if (stream != null) stream.Close();
                if (response != null) response.Close();
            }
        }
    }

    private byte[] FindNextJpegFrame(BinaryReader reader)
    {
        MemoryStream ms = new MemoryStream();
        bool foundStart = false;
        
        try
        {
            while (isRunning)
            {
                byte currentByte = reader.ReadByte();
                ms.WriteByte(currentByte);

                // Look for standard JPEG image start boundaries (0xFF, 0xD8)
                if (!foundStart)
                {
                    if (currentByte == 0xD8 && ms.Length >= 2)
                    {
                        ms.Position = ms.Length - 2;
                        if (ms.ReadByte() == 0xFF)
                        {
                            ms.SetLength(0);
                            ms.WriteByte(0xFF);
                            ms.WriteByte(0xD8);
                            foundStart = true;
                        }
                    }
                }
                // Look for standard JPEG image end boundaries (0xFF, 0xD9)
                else
                {
                    if (currentByte == 0xD9 && ms.Length >= 2)
                    {
                        ms.Position = ms.Length - 2;
                        if (ms.ReadByte() == 0xFF)
                        {
                            return ms.ToArray();
                        }
                    }
                }
            }
        }
        catch { return null; }
        return null;
    }

    void OnDestroy()
    {
        isRunning = false;
        if (streamThread != null && streamThread.IsAlive) streamThread.Join(500);
    }

    void OnApplicationQuit()
    {
        isRunning = false;
    }
}
