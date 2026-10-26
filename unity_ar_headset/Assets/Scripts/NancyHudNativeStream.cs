using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;

public class NancyHudNativeStream : MonoBehaviour
{
    [Header("Nancy Smart Box Target")]
    public string streamUrl = "http://10.94.70.75:8080/stream.mjpg";

    private RawImage hudScreen;
    private Texture2D frameTexture;
    private bool isStreaming = false;

    void Start()
    {
        hudScreen = GetComponent<RawImage>();
        if (hudScreen == null)
        {
            Debug.LogError("[NANCY NATIVE] Attach this script directly to your UI RawImage GameObject!");
            return;
        }

        // Low-glare dark standby color setup to prevent eye strain
        hudScreen.color = new Color(0f, 0f, 0.15f, 1f);
        frameTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        isStreaming = true;
        StartCoroutine(StreamMjpegPipeline());
    }

    IEnumerator StreamMjpegPipeline()
    {
        while (isStreaming)
        {
            using (UnityWebRequest request = new UnityWebRequest(streamUrl, UnityWebRequest.kHttpVerbGET))
            {
                // Force an unmanaged memory buffer handler to catch chunks in real-time
                DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
                request.downloadHandler = handler;
                
                request.SendWebRequest();

                int lastProcessedIndex = 0;
                MemoryStream frameAccumulator = new MemoryStream();

                while (!request.isDone && isStreaming)
                {
                    byte[] downloadedBytes = handler.data;

                    if (downloadedBytes != null && downloadedBytes.Length > lastProcessedIndex)
                    {
                        // Parse new bytes added to the buffer since the last frame step
                        for (int i = lastProcessedIndex; i < downloadedBytes.Length; i++)
                        {
                            frameAccumulator.WriteByte(downloadedBytes[i]);

                            // Evaluate continuous JPEG structure closure boundaries (0xFF, 0xD9)
                            if (frameAccumulator.Length > 2 && downloadedBytes[i] == 0xD9)
                            {
                                frameAccumulator.Position = frameAccumulator.Length - 2;
                                if (frameAccumulator.ReadByte() == 0xFF)
                                {
                                    byte[] completeFrame = frameAccumulator.ToArray();
                                    
                                    // Inject raw image data straight to GPU asset allocations
                                    if (frameTexture.LoadImage(completeFrame))
                                    {
                                        frameTexture.Apply();
                                        hudScreen.color = Color.white; // Clear standby navy overlay
                                        hudScreen.texture = frameTexture;
                                    }

                                    frameAccumulator.SetLength(0); // Flush stream instantly to save RAM
                                }
                            }
                        }
                        lastProcessedIndex = downloadedBytes.Length;
                    }
                    yield return null; // Balance workload across frame rendering loops
                }

                // If the stream breaks or server forces connection management drop
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"[NANCY LINK STALL] Connection reset needed: {request.error}");
                    hudScreen.color = new Color(0f, 0f, 0.15f, 1f); // Revert to standby layout
                    yield return new WaitForSeconds(2.0f); 
                }
            }
        }
    }

    void OnDestroy()
    {
        isStreaming = false;
    }
}
